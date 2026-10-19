using ITender.Integration.Api.Common;
using ITender.Integration.Api.Configuration;
using ITender.Integration.Api.Infrastructure.Security;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;

namespace ITender.Integration.Api.Infrastructure.ETender;

public sealed record ETenderPublishResult(bool Success, bool IsTransient, string? Error);

public interface IETenderClient
{
    Task<ETenderPublishResult> PublishTenderAsync(
        ETenderPublishRequest request,
        string correlationId,
        string idempotencyKey,
        CancellationToken ct);
}

public sealed class ETenderAuthHandler(IOptions<ETenderApiOptions> options, ISecretProtector protector) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var o = options.Value;
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{o.Username}:{protector.Unprotect(o.Password)}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return base.SendAsync(request, ct);
    }
}

public sealed class ETenderClient(HttpClient http, ILogger<ETenderClient> log) : IETenderClient
{
    // TODO: confirm the real e-Tender publish path.
    private const string PublishPath = "Tenders";

    public async Task<ETenderPublishResult> PublishTenderAsync(
        ETenderPublishRequest request,
        string correlationId,
        string idempotencyKey,
        CancellationToken ct)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Post, PublishPath)
        {
            Content = JsonContent.Create(request, options: Json.Options)
        };
        msg.Headers.Add("X-Correlation-Id", correlationId);
        msg.Headers.Add("Idempotency-Key", idempotencyKey);

        try
        {
            using var response = await http.SendAsync(msg, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
                return new ETenderPublishResult(true, false, null);

            var transient = (int)response.StatusCode is 408 or 429 or >= 500;
            log.LogWarning(
                "e-Tender publish failed ({Status}) correlation {CorrelationId}: {Body}",
                (int)response.StatusCode, correlationId, Truncate(body));

            return new ETenderPublishResult(
                false, transient,
                $"e-Tender returned {(int)response.StatusCode}: {Truncate(body)}");
        }
        catch (Exception ex) when (
            ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            log.LogError(ex, "e-Tender unreachable. Correlation {CorrelationId}", correlationId);
            return new ETenderPublishResult(false, true, "e-Tender is unavailable or timed out.");
        }
    }

    private static string Truncate(string s) => s.Length <= 500 ? s : s[..500] + "...";
}