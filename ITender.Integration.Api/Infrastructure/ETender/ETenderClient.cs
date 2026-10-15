using System.Net.Http.Headers;
using System.Text;
using ITender.Integration.Api.Configuration;
using ITender.Integration.Api.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace ITender.Integration.Api.Infrastructure.ETender;

public sealed record ETenderPublishResult(bool Success, bool IsTransient, string? ExternalReference, string? Error);

public interface IETenderClient
{
    Task<ETenderPublishResult> PublishTenderAsync(
        string tenderNumber, object tender, string correlationId, string idempotencyKey, CancellationToken ct);
}

/// <summary>
/// Adds credentials to every e-Tender call.
/// TODO: assumes HTTP Basic auth. If e-Tender issues a token from a login endpoint, replace this handler.
/// </summary>
public sealed class ETenderAuthHandler(IOptions<ETenderApiOptions> options, ISecretProtector protector) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var o = options.Value;
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{o.Username}:{protector.Unprotect(o.Password)}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return base.SendAsync(request, ct);
    }
}

public sealed class ETenderClient(HttpClient http, ILogger<ETenderClient> log) : IETenderClient
{
    // TODO: confirm the real e-Tender endpoint path and request schema; this is a placeholder mapping.
    private const string PublishPath = "api/tenders";

    public async Task<ETenderPublishResult> PublishTenderAsync(
    string tenderNumber, object tender, string correlationId, string idempotencyKey, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, PublishPath)
        {
            Content = JsonContent.Create(new { tenderNumber, tender }, options: Common.Json.Options)
        };
        request.Headers.Add("X-Correlation-Id", correlationId);
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        try
        {
            using var response = await http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
                return new ETenderPublishResult(true, false, ExternalReference: null, Error: null); // TODO: parse e-Tender reference from body

            var transient = (int)response.StatusCode is 408 or 429 or >= 500;
            log.LogWarning("e-Tender publish failed ({Status}) for {TenderNumber}, correlation {CorrelationId}: {Body}",
                (int)response.StatusCode, tenderNumber, correlationId, Truncate(body));
            return new ETenderPublishResult(false, transient, null, $"e-Tender returned {(int)response.StatusCode}: {Truncate(body)}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            log.LogError(ex, "e-Tender unreachable for {TenderNumber}, correlation {CorrelationId}", tenderNumber, correlationId);
            return new ETenderPublishResult(false, true, null, "e-Tender is unavailable or timed out.");
        }
    }

    private static string Truncate(string s) => s.Length <= 500 ? s : s[..500] + "...";
}
