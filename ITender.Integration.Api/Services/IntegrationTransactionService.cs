using System.Text.Json;
using ITender.Integration.Api.Common;
using ITender.Integration.Api.Infrastructure.Crm;

namespace ITender.Integration.Api.Services;

public interface IIntegrationTransactionService
{
    /// <summary>
    /// Wraps a business operation with audit logging and idempotency (BR-24..BR-26, NFR-06):
    /// a previously succeeded transaction is replayed instead of re-executed; failed ones may be retried.
    /// </summary>
    Task<TransactionResponse<T>> ExecuteAsync<T>(
        TransactionContext ctx,
        object? requestPayload,
        Func<CancellationToken, Task<TransactionResponse<T>>> handler,
        CancellationToken ct);
}

public sealed class IntegrationTransactionService(
    IIntegrationTransactionRepository repository,
    ILogger<IntegrationTransactionService> log) : IIntegrationTransactionService
{
    public async Task<TransactionResponse<T>> ExecuteAsync<T>(
        TransactionContext ctx,
        object? requestPayload,
        Func<CancellationToken, Task<TransactionResponse<T>>> handler,
        CancellationToken ct)
    {
        var existing = await repository.FindAsync(ctx.IdempotencyKey, ctx.BusinessObject, ctx.Operation, ct);
        Guid id;

        if (existing is null)
        {
            id = await repository.CreateAsync(ctx, JsonSerializer.Serialize(requestPayload, Json.Options), ct);
        }
        else if (existing.Status == "Succeeded" && !string.IsNullOrEmpty(existing.ResponseJson))
        {
            // Duplicate: return the original result (BRS 18.1)
            var replay = JsonSerializer.Deserialize<TransactionResponse<T>>(existing.ResponseJson, Json.Options)
                         ?? TransactionResponse<T>.For(ctx);
            replay.CorrelationId = ctx.CorrelationId;
            replay.Status = "Duplicate";
            replay.HttpStatus = 200;
            log.LogInformation("Duplicate transaction replayed. Key {Key}, correlation {CorrelationId}", ctx.IdempotencyKey, ctx.CorrelationId);
            return replay;
        }
        else if (existing.Status == "InProgress")
        {
            return TransactionResponse<T>.For(ctx).Fail(
                [new ApiError("DUPLICATE_IN_PROGRESS", "An identical transaction is still being processed.", null, ErrorCategory.Duplicate)], 409);
        }
        else
        {
            id = existing.Id; // previous attempt failed: retry
            await repository.MarkRetryAsync(id, existing.RetryCount + 1, ctx.CorrelationId, ct);
        }

        try
        {
            var response = await handler(ct);
            await repository.CompleteAsync(id, response.Status, JsonSerializer.Serialize(response, Json.Options), ct);
            return response;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Transaction failed. Correlation {CorrelationId}, {Object}/{Operation}/{Key}",
                ctx.CorrelationId, ctx.BusinessObject, ctx.Operation, ctx.BusinessKey);
            try { await repository.CompleteAsync(id, "Failed", $"{{\"error\":\"{ex.GetType().Name}\"}}", CancellationToken.None); }
            catch (Exception inner) { log.LogError(inner, "Could not record failed transaction {CorrelationId}", ctx.CorrelationId); }
            throw;
        }
    }
}
