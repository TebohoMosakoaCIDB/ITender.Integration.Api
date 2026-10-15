using System.Text.Json;
using ITender.Integration.Api.Common;

namespace ITender.Integration.Api.Middleware;

public static class HttpContextExtensions
{
    private const string Key = "CorrelationId";
    public static string GetCorrelationId(this HttpContext c) =>
        c.Items.TryGetValue(Key, out var v) && v is string s ? s : Guid.NewGuid().ToString("N");
    public static void SetCorrelationId(this HttpContext c, string id) => c.Items[Key] = id;
}

/// <summary>NFR-03: every transaction carries a correlation id (generated if the caller did not send one).</summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> log)
{
    public const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers[Header].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100) id = Guid.NewGuid().ToString("N");

        context.SetCorrelationId(id);
        context.Response.Headers[Header] = id;

        using (log.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
            await next(context);
    }
}

/// <summary>BR-27: unexpected failures still return the standard envelope, never a stack trace.</summary>
public sealed class ExceptionEnvelopeMiddleware(RequestDelegate next, ILogger<ExceptionEnvelopeMiddleware> log)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var correlationId = context.GetCorrelationId();
            log.LogError(ex, "Unhandled exception. Correlation {CorrelationId}", correlationId);

            var body = new
            {
                correlationId,
                timestamp = DateTimeOffset.UtcNow,
                status = "Failed",
                errors = new[] { new ApiError("UNEXPECTED_ERROR", "An unexpected error occurred. Quote the correlation id to support.", null, ErrorCategory.Permanent) }
            };

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(body, Json.Options));
        }
    }
}
