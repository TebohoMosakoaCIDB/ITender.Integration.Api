namespace ITender.Integration.Api.Constants
{
    /// <summary>
    /// Single source of truth for Dataverse entity (table) logical names.
    /// Repositories, mappers and validators must reference these constants, never string literals.
    /// </summary>
    public static class CrmEntityNames
    {
        public const string Account = "account";
        public const string Contact = "contact";
        public const string ContactPermissions = "nv_contact_permission";
        public const string Contract = "nv_contract";
        public const string Application = "nv_application";
        public const string City = "nv_city";
        public const string Credentials = "nv_credentials";
        public const string ClassOfWork = "nv_classofwork";
        public const string ClassOfWorkType = "nv_classofworktype";
        public const string ClassOfWorkTypeSub = "nv_classofworktypesubcategory";
        public const string ConstrustionContractContractors = "nv_constructioncontractcontractors";
        public const string FinancialStatement = "nv_financialstatement";
        public const string MetroDistrict = "nv_metrodistrict";
        public const string Ownership = "nv_ownership";
        public const string ScannedDocument = "nv_scanneddocument";
        public const string Grade = "nv_grading";
        public const string DocumentType = "nv_documenttype";
        public const string Province = "nv_province";
        public const string PublicHoliday = "nv_publicholiday";
        public const string Site = "site";
        public const string TaxClearanceCertificate = "nv_taxclearancecertificate";
        public const string RegisteredProfessionals = "nv_registeredprofessionalallocation";
        public const string TenderValueRange = "nv_tendervaluerange";
        public const string TrackRecord = "nv_trackrecord";
        public const string Tender = "nv_tender";
        public const string TypeOfContract = "nv_typeofcontract";
        public const string ContractorDevelopmentProgramme = "nv_contractordevelopmentprogramme";
        public const string CdpSubmission = "nv_cdpsubmission";

        // Added for the integration API (NEW entity, does not exist in CRM yet)
        public const string IntegrationTransaction = "nv_integrationtransaction";
    }
}