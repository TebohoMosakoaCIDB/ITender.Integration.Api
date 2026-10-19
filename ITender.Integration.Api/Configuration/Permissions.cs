namespace ITender.Integration.Api.Configuration;

/// <summary>
/// Canonical permission values emitted as "permission" claims in the JWT.
/// Adding a new permission = add a constant here + one row in PermissionMap.
/// </summary>
public static class Permissions
{
    // Tender
    public const string CreateAndEditTender = "tender:create_edit";
    public const string EditAndAdvertiseTender = "tender:edit_advertise";

    // Award
    public const string AwardTenders = "award:tenders";

    // Contract
    public const string TerminateContracts = "contract:terminate";
    public const string PracticallyCompleteContracts = "contract:practically_complete";

    // Change
    public const string ChangeTenderAndSubmitCr = "change:tender_submit_cr";
}