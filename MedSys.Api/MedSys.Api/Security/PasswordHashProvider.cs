using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace MedSys.Api.Security;

public static class PasswordHashProvider
{
    public static string GetSalt()
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(salt);
    }

    public static string GetHash(string password, string b64salt)
    {
        var salt = Convert.FromBase64String(b64salt);
        var hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100_000,
            numBytesRequested: 32);
        return Convert.ToBase64String(hash);
    }
}
