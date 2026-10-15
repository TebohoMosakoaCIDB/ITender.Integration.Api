using System.Security.Cryptography;
using System.Text;

namespace ITender.Integration.Api.Infrastructure.Security;

public interface IPasswordVerifier
{
    bool Verify(string? stored, string supplied);
}

/// <summary>
/// Verifies a supplied password against the value held in CRM (nv_credentials.nv_password).
///  - "pbkdf2$iterations$salt$hash"  -> salted PBKDF2-SHA256 (recommended target format)
///  - anything else                  -> LEGACY: plain text, or AES ciphertext (Encryption Key/IV), compared in constant time.
/// </summary>
public sealed class PasswordVerifier(ISecretProtector protector) : IPasswordVerifier
{
    private const string Prefix = "pbkdf2$";
    private const int Iterations = 600_000;

    public bool Verify(string? stored, string supplied)
    {
        if (string.IsNullOrEmpty(stored) ||
            string.IsNullOrEmpty(supplied))
        {
            return false;
        }

        return stored.StartsWith(Prefix, StringComparison.Ordinal)
            ? VerifyPbkdf2(stored, supplied)
            : FixedTimeEquals(protector.Unprotect(stored), supplied);
    }

    /// <summary>Use this when migrating stored passwords to the recommended format.</summary>
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPbkdf2(string stored, string supplied)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations)) return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(supplied, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var x = Encoding.UTF8.GetBytes(a);
        var y = Encoding.UTF8.GetBytes(b);
        return x.Length == y.Length && CryptographicOperations.FixedTimeEquals(x, y);
    }
}
