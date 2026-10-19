using ITender.Integration.Api.Configuration;
using ITender.Integration.Api.Infrastructure.Crm;

namespace ITender.Integration.Api.Infrastructure.Mappers;

/// <summary>
/// Maps CRM permission columns to the permission value emitted in the JWT.
/// This is the single place to update when a new permission column is added.
/// </summary>
public static class PermissionMap
{
    public static readonly IReadOnlyList<(string Column, string Permission)> All = new[]
    {
        (CredentialSchema.CreateAndEditTenderPermission,                 Permissions.CreateAndEditTender),
        (CredentialSchema.EditAndAdvertiseTenderPermission,              Permissions.EditAndAdvertiseTender),
        (CredentialSchema.CanAwardTendersPermission,                     Permissions.AwardTenders),
        (CredentialSchema.CanTerminateContractsPermission,               Permissions.TerminateContracts),
        (CredentialSchema.CanPracticallyCompleteContractsPermission,     Permissions.PracticallyCompleteContracts),
        (CredentialSchema.CanChangeTenderAndSubmitCRpermission,          Permissions.ChangeTenderAndSubmitCr),
    };
}