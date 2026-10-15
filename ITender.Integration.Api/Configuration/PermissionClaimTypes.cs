namespace ITender.Integration.Api.Configuration
{
    public static class PermissionClaimTypes
    {
        public const string CanAwardTenders =
            "permission:can_award_tenders";

        public const string CanPracticallyCompleteContracts =
            "permission:can_practically_complete_contracts";

        public const string CanTerminateContracts =
            "permission:can_terminate_contracts";

        public const string CanChangeTenderAndSubmitCR =
            "permission:can_change_tender_and_submit_cr";

        public const string CreateAndEditTender =
            "permission:create_and_edit_tender";

        public const string EditAndAdvertiseTender =
            "permission:edit_and_advertise_tender";
    }
}
