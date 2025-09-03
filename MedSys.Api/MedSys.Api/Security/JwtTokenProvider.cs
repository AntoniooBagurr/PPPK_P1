using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MedSys.Api.Security;

public static class JwtTokenProvider
{
    public static string CreateToken(string secureKey, int expirationMinutes, Guid doctorId, string fullName, string? email)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secureKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, doctorId.ToString()),
            new(ClaimTypes.Name, fullName ?? string.Empty),
        };
        if (!string.IsNullOrWhiteSpace(email))
            claims.Add(new Claim(ClaimTypes.Email, email));

        var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
