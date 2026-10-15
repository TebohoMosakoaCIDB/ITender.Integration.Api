using ITender.Integration.Api.Constants;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace ITender.Integration.Api.Infrastructure.Crm;

// Logical names taken from the existing CredentialMapper in ITender.Web.
public static class CredentialSchema
{
    public const string Name = "nv_name";
    public const string Password = "nv_password";
    public const string LastLogin = "nv_lastlogin";
    public const string IsLocked = "nv_islocked";
    public const string MfaEnabled = "cidb_mfaenabled";
    public const string ForcePasswordChange = "nv_forcepasswordchange";
    public const string IncorrectLoginCount = "nv_incorrectlogincount";
    public const string StatusCode = "statuscode";

    // Permissions — verified against Dataverse metadata
    public const string CanAwardTendersPermission =
        "nv_canawardtenderspermission";

    public const string CanPracticallyCompleteContractsPermission =
        "nv_canpracticallycompletecontractspermission";

    public const string CanTerminateContractsPermission =
        "nv_canterminatecontractspermission";

    public const string CanChangeTenderAndSubmitCRpermission =
        "nv_canchangetenderandsubmitcrpermission";

    public const string CreateAndEditTenderPermission =
        "nv_createandedittenderpermission";

    public const string EditAndAdvertiseTenderPermission =
        "nv_editandadvertisetenderpermission";
}

public sealed record CredentialRecord(
    Guid Id, string Username, string? Password, bool IsLocked, bool ForcePasswordChange,
    bool MfaEnabled, int FailedCount, int? StatusCode,
    // Permission flags emitted as claims by TokenService.
    // TODO: not loaded from CRM yet, so they default to false (every permission policy will return 403).
    bool CanAwardTendersPermission = false,
    bool CanPracticallyCompleteContractsPermission = false,
    bool CanTerminateContractsPermission = false,
    bool CanChangeTenderAndSubmitCRpermission = false,
    bool CreateAndEditTenderPermission = false,
    bool EditAndAdvertiseTenderPermission = false);

public interface ICredentialRepository
{
    Task<CredentialRecord?> GetByUsernameAsync(string username, CancellationToken ct);
    Task RecordSuccessAsync(Guid id, CancellationToken ct);
    /// <returns>True if this failure caused the account to lock.</returns>
    Task<bool> RecordFailureAsync(Guid id, int currentCount, int maxAttempts, CancellationToken ct);
}

public sealed class CredentialRepository(IOrganizationServiceAsync2 crm) : ICredentialRepository
{
    public async Task<CredentialRecord?> GetByUsernameAsync(string username, CancellationToken ct)
    {
        var query = new QueryExpression(CrmEntityNames.Credentials)
        {
            ColumnSet = new ColumnSet(
        CredentialSchema.Name,
        CredentialSchema.Password,
        CredentialSchema.IsLocked,
        CredentialSchema.ForcePasswordChange,
        CredentialSchema.MfaEnabled,
        CredentialSchema.IncorrectLoginCount,
        CredentialSchema.StatusCode,
        CredentialSchema.CanAwardTendersPermission,
        CredentialSchema.CanPracticallyCompleteContractsPermission,
        CredentialSchema.CanTerminateContractsPermission,
        CredentialSchema.CanChangeTenderAndSubmitCRpermission,
        CredentialSchema.CreateAndEditTenderPermission,
        CredentialSchema.EditAndAdvertiseTenderPermission),
            TopCount = 1,
            NoLock = true
        };
        // The password is NEVER part of the query; it is verified in code.
        query.Criteria.AddCondition(CredentialSchema.Name, ConditionOperator.Equal, username);

        var e = (await crm.RetrieveMultipleAsync(query, ct)).Entities.FirstOrDefault();
        if (e is null) return null;

        return new CredentialRecord(
            e.Id,
            e.GetAttributeValue<string>(CredentialSchema.Name) ?? username,
            e.GetAttributeValue<string>(CredentialSchema.Password),
            e.GetAttributeValue<bool?>(CredentialSchema.IsLocked) ?? false,
            e.GetAttributeValue<bool?>(CredentialSchema.ForcePasswordChange) ?? false,
            e.GetAttributeValue<bool?>(CredentialSchema.MfaEnabled) ?? false,
            e.GetAttributeValue<int?>(CredentialSchema.IncorrectLoginCount) ?? 0,
            e.GetAttributeValue<OptionSetValue>(CredentialSchema.StatusCode)?.Value,

            // CRM permissions
            e.GetAttributeValue<bool?>(CredentialSchema.CanAwardTendersPermission) ?? false,
            e.GetAttributeValue<bool?>(CredentialSchema.CanPracticallyCompleteContractsPermission) ?? false,
            e.GetAttributeValue<bool?>(CredentialSchema.CanTerminateContractsPermission) ?? false,
            e.GetAttributeValue<bool?>(CredentialSchema.CanChangeTenderAndSubmitCRpermission) ?? false,
            e.GetAttributeValue<bool?>(CredentialSchema.CreateAndEditTenderPermission) ?? false,
            e.GetAttributeValue<bool?>(CredentialSchema.EditAndAdvertiseTenderPermission) ?? false);
    }

    // Partial updates only: the stored password and other columns are never rewritten.
    public Task RecordSuccessAsync(Guid id, CancellationToken ct)
        => crm.UpdateAsync(new Entity(CrmEntityNames.Credentials, id)
        {
            [CredentialSchema.LastLogin] = DateTime.UtcNow,
            [CredentialSchema.IncorrectLoginCount] = 0
        }, ct);

    public async Task<bool> RecordFailureAsync(Guid id, int currentCount, int maxAttempts, CancellationToken ct)
    {
        var newCount = currentCount + 1;
        var lockNow = newCount >= maxAttempts;

        var update = new Entity(CrmEntityNames.Credentials, id) { [CredentialSchema.IncorrectLoginCount] = newCount };
        if (lockNow) update[CredentialSchema.IsLocked] = true;

        await crm.UpdateAsync(update, ct);
        return lockNow;
    }
}