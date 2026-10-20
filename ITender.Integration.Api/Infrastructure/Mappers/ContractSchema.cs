namespace ITender.Integration.Api.Infrastructure.Mappers
{
    public static class ContractSchema
    {
        public const string EntityLogicalName = "nv_contract";
        public const string Id = "nv_contractid";

        // Contract Fields
        public const string TenderId = "nv_tenderid";
        public const string Title = "nv_contracttitle";
        public const string AwardDate = "nv_contractawarddate"; 
        public const string EndDate = "nv_contractenddate";
        public const string TotalValueInclVat = "nv_totalcontractvalueinclvat"; 
        public const string FinancialPoints = "nv_financialpoints";
        public const string PreferencePoints = "nv_preferencepoints";
        public const string NumberOfContractors = "nv_numberofcontractors";

        // Form of Contract
        public const string FormJBCC = "nv_formofcontractjbcc";
        public const string FormGCC = "nv_formofcontractgcc";
        public const string FormNEC = "nv_formofcontractnec3";
        public const string FormFIDIC = "nv_formofcontractfidic";

        // JV / Contractors Entity
        public const string ContractorEntity = "nv_constructioncontractcontractors";
        public const string ContractorContractId = "nv_constructioncontractid";
        public const string ContractorAccountId = "nv_contractorid";
        public const string ContractorJvShare = "nv_jvsharepercentage";
        public const string ContractorIsLead = "nv_leadcontractor";
        public const string ContractorEnterpriseName = "nv_enterprisename";
        public const string ContractorRegistrationNumber = "nv_enterpriseregistrationnumber";

        // Contract Details
        public const string Description = "nv_descriptionofproject";
        public const string TypeOfContract = "nv_typeofcontract";   

        // Location Details
        public const string ProvinceId = "nv_provinceid"; 
        public const string MetroDistrictId = "nv_metrodistrictid"; 
        public const string LocalMunicipalityId = "nv_municipalityid";

        // Award Details
        public const string TermContract = "nv_termcontract"; 

        // Employer Details
        public const string EmployerId = "nv_employer_contract_id";

        // The missing fields
        public const string ClassOfConstructionWorksId = "nv_classofconstructionworks";
        public const string TenderValueRange = "nv_tendervaluerange";
        public const string IsAJV = "nv_isajv";

        // --- Practical Completion ---
        public const string PracticalCompletionDate = "nv_practicalcompletiondate";

        // Variations & Adjustments
        public const string EstimatedValueOfVariations = "nv_Estimatedvalueofvariations";
        public const string EstimatedValueOfAdjustment = "nv_Estimatedvalueofadjustment";
        public const string EstimatedFinalValueInclVat = "nv_EstimatedfinalvalueofcontractinclVAT";

        // Dispute Resolution
        public const string DisputesReferredToResolution = "nv_disputesreferredtodisputeresolutioncontra";

        // --- Termination ---
        public const string TerminationDate = "nv_terminationdate";
        public const string TerminationReason = "nv_ReasonForTermination";
        public const string OtherTerminationReason = "nv_otherterminationreason";

        // Status
        public const string StatusCode = "statuscode";
    }
}
