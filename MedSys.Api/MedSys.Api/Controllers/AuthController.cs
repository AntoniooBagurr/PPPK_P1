using MedSys.Api.Data;
using MedSys.Api.Models;
using MedSys.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace MedSys.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDb _db;
    private readonly IConfiguration _cfg;

    public AuthController(AppDb db, IConfiguration cfg)
    {
        _db = db; _cfg = cfg;
    }

    public class DoctorLoginDto
    {
        [Required] public string Email { get; set; } = default!;
        [Required] public string Password { get; set; } = default!;
    }


    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] DoctorLoginDto dto, CancellationToken ct)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(
            x => x.Email != null && x.Email.ToLower() == dto.Email.ToLower(), ct);

        if (doctor == null) return BadRequest("Neispravan e-mail ili lozinka.");

        var hash = PasswordHashProvider.GetHash(dto.Password, doctor.PwdSalt);
        if (hash != doctor.PwdHash) return BadRequest("Neispravan e-mail ili lozinka.");

        var key = _cfg["JWT:SecureKey"]!;
        var minutes = int.TryParse(_cfg["JWT:ExpirationMinutes"], out var m) ? m : 120;

        var token = JwtTokenProvider.CreateToken(key, minutes, doctor.Id, doctor.FullName, doctor.Email);
        return Ok(new { token, doctor = new { doctor.Id, doctor.FullName, doctor.Email } });
    }

    public class SetPasswordDto
    {
        [Required] public Guid DoctorId { get; set; }
        [Required, MinLength(8)] public string NewPassword { get; set; } = default!;
    }

    [HttpPost("set-password")]
    public async Task<ActionResult> SetPassword([FromBody] SetPasswordDto dto, CancellationToken ct)
    {
        var doc = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == dto.DoctorId, ct);
        if (doc == null) return NotFound("Doctor not found");

        var salt = PasswordHashProvider.GetSalt();
        var hash = PasswordHashProvider.GetHash(dto.NewPassword, salt);

        doc.PwdSalt = salt;
        doc.PwdHash = hash;

        await _db.SaveChangesAsync(ct);
        return Ok();
    }
}
