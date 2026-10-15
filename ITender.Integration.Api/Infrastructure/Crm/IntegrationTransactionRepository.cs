using ITender.Integration.Api.Constants;
using ITender.Integration.Api.Common;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using S = ITender.Integration.Api.Infrastructure.Crm.IntegrationTransactionSchema;

namespace ITender.Integration.Api.Infrastructure.Crm;

public sealed record TransactionRecord(Guid Id, string Status, string? ResponseJson, int RetryCount);

/// <summary>Persistent audit + idempotency store (BR-24, BR-25, BR-26, NFR-06).</summary>
public interface IIntegrationTransactionRepository
{
    Task<TransactionRecord?> FindAsync(string idempotencyKey, string businessObject, string operation, CancellationToken ct);
    Task<Guid> CreateAsync(TransactionContext ctx, string requestJson, CancellationToken ct);
    Task MarkRetryAsync(Guid id, int newRetryCount, string correlationId, CancellationToken ct);
    Task CompleteAsync(Guid id, string status, string responseJson, CancellationToken ct);
}

public sealed class IntegrationTransactionRepository(IOrganizationServiceAsync2 crm) : IIntegrationTransactionRepository
{
    public async Task<TransactionRecord?> FindAsync(string key, string businessObject, string operation, CancellationToken ct)
    {
        var q = new QueryExpression(CrmEntityNames.IntegrationTransaction)
        {
            ColumnSet = new ColumnSet(S.Status, S.ResponseJson, S.RetryCount),
            TopCount = 1,
            NoLock = true
        };
        q.Criteria.AddCondition(S.IdempotencyKey, ConditionOperator.Equal, key);
        q.Criteria.AddCondition(S.BusinessObject, ConditionOperator.Equal, businessObject);
        q.Criteria.AddCondition(S.Operation, ConditionOperator.Equal, operation);

        var e = (await crm.RetrieveMultipleAsync(q, ct)).Entities.FirstOrDefault();
        return e is null
            ? null
            : new TransactionRecord(e.Id, e.GetAttributeValue<string>(S.Status) ?? "",
                                    e.GetAttributeValue<string>(S.ResponseJson),
                                    e.GetAttributeValue<int?>(S.RetryCount) ?? 0);
    }

    public async Task<Guid> CreateAsync(TransactionContext c, string requestJson, CancellationToken ct)
    {
        var e = new Entity(CrmEntityNames.IntegrationTransaction)
        {
            [S.Name] = c.CorrelationId,
            [S.IdempotencyKey] = c.IdempotencyKey,
            [S.BusinessObject] = c.BusinessObject,
            [S.Operation] = c.Operation,
            [S.BusinessKey] = c.BusinessKey,
            [S.SourceSystem] = c.SourceSystem,
            [S.TargetSystem] = c.TargetSystem,
            [S.Status] = "InProgress",
            [S.RequestJson] = requestJson,
            [S.RetryCount] = 0
        };
        return await crm.CreateAsync(e, ct);
    }

    public Task MarkRetryAsync(Guid id, int newRetryCount, string correlationId, CancellationToken ct)
        => crm.UpdateAsync(new Entity(CrmEntityNames.IntegrationTransaction, id)
        {
            [S.Status] = "InProgress",
            [S.RetryCount] = newRetryCount,
            [S.Name] = correlationId
        }, ct);

    public Task CompleteAsync(Guid id, string status, string responseJson, CancellationToken ct)
        => crm.UpdateAsync(new Entity(CrmEntityNames.IntegrationTransaction, id)
        {
            [S.Status] = status,
            [S.ResponseJson] = responseJson
        }, ct);
}