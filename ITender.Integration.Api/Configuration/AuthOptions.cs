using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ITender.Integration.Api.Configuration;

public sealed class AuthOptions
{
    public const string Section = "Authentication";

    public string Issuer { get; set; } = "ITender.Integration.Api";
    public string Audience { get; set; } = "ITender.Integration.Api";

    /// <summary>HMAC-SHA256 signing key, at least 32 bytes (Base64 or plain text). Keep in secrets, never in source control.</summary>
    public string SigningKey { get; set; } = "";

    public int AccessTokenMinutes { get; set; } = 30;

    /// <summary>Consecutive failed logins before the CRM credential is locked.</summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>Dataverse statuscode value that means "Active" on nv_credentials (normally 1).</summary>
    public int ActiveStatusCode { get; set; } = 1;

    /// <summary>
    /// Username -> roles. TODO DEC-009: replace with the approved production role/permission matrix
    /// (ideally stored in CRM). A user with no roles is refused a token.
    /// </summary>
    public Dictionary<string, string[]> Roles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static SymmetricSecurityKey BuildKey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Authentication:SigningKey is not configured.");

        byte[] bytes;
        try { bytes = Convert.FromBase64String(raw); }
        catch (FormatException) { bytes = Encoding.UTF8.GetBytes(raw); }

        if (bytes.Length < 32)
            throw new InvalidOperationException("Authentication:SigningKey must be at least 32 bytes.");

        return new SymmetricSecurityKey(bytes);
    }
}
