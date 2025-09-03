using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MedSys.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MedSys.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDb _db;
    private readonly byte[] _key;
    private readonly int _expiresMinutes;

    public AuthController(AppDb db, IConfiguration cfg)
    {
        _db = db;
        var key = cfg["JWT:SecureKey"] ?? throw new InvalidOperationException("Missing JWT:SecureKey in configuration.");
        _key = Encoding.UTF8.GetBytes(key);
        _expiresMinutes = int.TryParse(cfg["JWT:ExpirationMinutes"], out var m) ? m : 480; 
    }

    // ===== DTOs =====
    public record LoginRequest(string Username, string Password);
    public record LoginResponse(string Token, object Doctor);

    public record SetPasswordFirstRequest(Guid DoctorId, string NewPassword);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    // ===== AUTH =====

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Username i password su obavezni.");

        var uname = req.Username.Trim();
        var doc = await _db.Doctors
            .FirstOrDefaultAsync(d =>
                (d.Email != null && d.Email.ToLower() == uname.ToLower()) ||
                (d.LicenseNo != null && d.LicenseNo.ToLower() == uname.ToLower()),
                ct);

        if (doc is null)
            return Unauthorized("Bad credentials.");

        if (string.IsNullOrEmpty(doc.PwdSalt) || string.IsNullOrEmpty(doc.PwdHash))
            return StatusCode(403, "Lozinka još nije postavljena za ovog doktora.");

        if (!VerifyPassword(req.Password, doc.PwdSalt!, doc.PwdHash!))
            return Unauthorized("Bad credentials.");

        var token = CreateJwt(doc);
        return Ok(new LoginResponse(token, new { doc.Id, doc.FullName, doc.Email, doc.LicenseNo }));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> Me()
    {
        var id = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var name = User.FindFirstValue(ClaimTypes.Name);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var lic = User.FindFirstValue("lic");
        return Ok(new { id, name, email, licenseNo = lic });
    }

    // ===== PASSWORD FLOW =====

    [AllowAnonymous]
    [HttpPost("set-password")]
    public async Task<ActionResult> SetPasswordFirst([FromBody] SetPasswordFirstRequest dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 8)
            return BadRequest("Lozinka mora imati barem 8 znakova.");

        var doc = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == dto.DoctorId, ct);
        if (doc == null) return NotFound("Doctor not found.");

        if (!string.IsNullOrEmpty(doc.PwdHash) || !string.IsNullOrEmpty(doc.PwdSalt))
            return StatusCode(403, "Lozinka je već postavljena.");

        var (salt, hash) = HashPassword(dto.NewPassword);
        doc.PwdSalt = salt;
        doc.PwdHash = hash;

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 8)
            return BadRequest("Nova lozinka mora imati barem 8 znakova.");

        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(sub, out var doctorId)) return Unauthorized();

        var doc = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId, ct);
        if (doc is null) return Unauthorized();

        if (string.IsNullOrEmpty(doc.PwdSalt) || string.IsNullOrEmpty(doc.PwdHash))
            return StatusCode(403, "Lozinka još nije postavljena (koristi /api/auth/set-password).");

        if (!VerifyPassword(dto.CurrentPassword, doc.PwdSalt!, doc.PwdHash!))
            return StatusCode(403, "Trenutna lozinka nije točna.");

        var (salt, hash) = HashPassword(dto.NewPassword);
        doc.PwdSalt = salt;
        doc.PwdHash = hash;

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    // ===== helpers =====

    private string CreateJwt(dynamic doc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, doc.Id.ToString()),
            new(ClaimTypes.Name, (string)doc.FullName),
            new(ClaimTypes.Email, (string?)doc.Email ?? string.Empty),
            new("lic", (string?)doc.LicenseNo ?? string.Empty)
        };

        var creds = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static bool VerifyPassword(string password, string saltB64, string hashB64)
    {
        var salt = Convert.FromBase64String(saltB64);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var computed = pbkdf2.GetBytes(32);
        var expected = Convert.FromBase64String(hashB64);
        return CryptographicOperations.FixedTimeEquals(computed, expected);
    }

    public static (string saltB64, string hashB64) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return (Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }
}
