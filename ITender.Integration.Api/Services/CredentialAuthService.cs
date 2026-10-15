using ITender.Integration.Api.Configuration;
using ITender.Integration.Api.Infrastructure.Crm;
using ITender.Integration.Api.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace ITender.Integration.Api.Services;

public sealed record AuthResult(
    bool Success, int HttpStatus, string? Message = null,
    string? AccessToken = null, int ExpiresIn = 0, bool RequiresPasswordChange = false);

public interface ICredentialAuthService
{
    Task<AuthResult> AuthenticateAsync(string username, string password, string correlationId, CancellationToken ct);
}

/// <summary>Authenticates against the CRM credentials entity (BR-01 / FR-001) and issues a JWT.</summary>
public sealed class CredentialAuthService(
    ICredentialRepository credentials,
    IPasswordVerifier verifier,
    ITokenService tokens,
    IOptions<AuthOptions> options,
    ILogger<CredentialAuthService> log) : ICredentialAuthService
{
    private const string InvalidMessage = "The username or password is incorrect.";
    private static readonly string DummyHash = PasswordVerifier.Hash(Guid.NewGuid().ToString("N"));

    public async Task<AuthResult> AuthenticateAsync(string username, string password, string correlationId, CancellationToken ct)
    {
        var cfg = options.Value;
        var user = await credentials.GetByUsernameAsync(username.Trim(), ct);

        if (user is null)
        {
            verifier.Verify(DummyHash, password); // keep timing similar for unknown users
            return Deny("TokenDenied", username, correlationId, "Unknown user", 401, InvalidMessage);
        }

        if (!verifier.Verify(user.Password, password))
        {
            if (!user.IsLocked)
            {
                var locked = await credentials.RecordFailureAsync(user.Id, user.FailedCount, cfg.MaxFailedAttempts, ct);
                if (locked) log.LogWarning("SecurityEvent AccountLocked for {User}. Correlation {CorrelationId}", Mask(username), correlationId);
            }
            return Deny("TokenDenied", username, correlationId, "Wrong password", 401, InvalidMessage);
        }

        // Password is correct from here on, so state-specific messages do not leak account existence.
        if (user.IsLocked)
            return Deny("TokenDenied", username, correlationId, "Account locked", 401,
                "This account has been locked. Please contact an administrator for assistance.");

        if (user.StatusCode != cfg.ActiveStatusCode)
            return Deny("TokenDenied", username, correlationId, "Account inactive", 401,
                "This account has been deactivated. Please contact your administrator to reactivate it.");

        if (user.MfaEnabled)
            return Deny("TokenDenied", username, correlationId, "MFA account on token endpoint", 403,
                "Multi-factor accounts cannot be used for integration tokens. Use a dedicated integration account.");

        if (user.ForcePasswordChange)
            return Deny("TokenDenied", username, correlationId, "Password change required", 403,
                "You must change your password before continuing.") with { RequiresPasswordChange = true };

        await credentials.RecordSuccessAsync(user.Id, ct);

        var (token, expiresIn) = tokens.Create(user);

        log.LogInformation(
            "SecurityEvent TokenIssued for {User}. Correlation {CorrelationId}",
            Mask(username),
            correlationId);

        return new AuthResult(
            true,
            200,
            AccessToken: token,
            ExpiresIn: expiresIn);
    }

    // Logs the security event (BRS 18.1) without ever logging the password or a token.
    private AuthResult Deny(string evt, string username, string correlationId, string reason, int status, string message)
    {
        log.LogWarning("SecurityEvent {Event} for {User}: {Reason}. Correlation {CorrelationId}",
            evt, Mask(username), reason, correlationId);
        return new AuthResult(false, status, message);
    }

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        return at <= 1 ? "***" : email[0] + "***" + email[at..];
    }
}
