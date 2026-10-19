namespace ITender.Integration.Api.Configuration;

/// <summary>
/// Hardcoded Dataverse option-set values used by the integration API.
/// SOURCE OF TRUTH: Dataverse metadata for nv_tender.
/// If the option-set definitions change in Dataverse, update this file.
/// Last verified: 2026-10-16.
/// </summary>
public static class TenderOptionSets
{
    // ---- nv_formofcontract (Yes/No flags) ----
    // Assuming standard 100000000 = Yes
    public const int FormOfContractYes = 100000000;

    // Placeholder for the Tender status code when awarded. 
    // TODO: Replace with the actual Dataverse statuscode for "Awarded"
    public const int StatusAwarded = 100000002;
    public static class IsEOIOrTender
    {
        public const int EOI = 100000000;
        public const int Tender = 100000001;
    }

    // ---- nv_contractpartofepwp ----
    public static class ContractPartOfEPWP
    {
        public const int Yes = 100000000;
        public const int No = 100000001;
    }
    public const int Yes = 100000000;
    public const int No = 100000001;

    // ---- nv_TenderValueRange ----
    // Labels as at 2026-10-16:
    //  100000008 → (1) R 30,000 – R 500,000
    //  100000000 → (2) R 500,000 – R 1,000,000
    //  100000001 → (3) R 1,000,000 – R 3,000,000
    //  100000002 → (4) R 3,000,000 – R 6,000,000
    //  100000003 → (5) R 6,000,000 – R 10,000,000
    //  100000004 → (6) R 10,000,000 – R 20,000,000
    //  100000005 → (7) R 20,000,000 – R 60,000,000
    //  100000006 → (8) R 60,000,000 – R 200,000,000
    //  100000007 → (9) R 200,000,000 – No Limit
    public static class TenderValueRange
    {
        public const int Range1 = 100000008;
        public const int Range2 = 100000000;
        public const int Range3 = 100000001;
        public const int Range4 = 100000002;
        public const int Range5 = 100000003;
        public const int Range6 = 100000004;
        public const int Range7 = 100000005;
        public const int Range8 = 100000006;
        public const int Range9 = 100000007;

        public static readonly IReadOnlySet<int> All = new HashSet<int>
        {
            Range1, Range2, Range3, Range4, Range5,
            Range6, Range7, Range8, Range9
        };
    }

    // ---- nv_typeofcontract ----
    // TODO: verify values in Dataverse metadata before enabling.
    // Placeholder — replace with real values once enumerated.
    public static class TypeOfContract
    {
        public const int NewWorksInstallationExtension = 100000000;
        public const int MaintenanceRepairRefurbishment = 100000001;
        public const int DismantlingDemolition = 100000002;

        public static readonly IReadOnlySet<int> All = new HashSet<int>
        {
            NewWorksInstallationExtension,
            MaintenanceRepairRefurbishment,
            DismantlingDemolition
        };
    }
}