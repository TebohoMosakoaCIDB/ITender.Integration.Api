using ITender.Integration.Api.TranferObjects;

namespace ITender.Integration.Api.Models
{
    public class TenderModel
    {
        public TenderModel()
        {
            Id = Guid.NewGuid();
        }
        public Guid Id { get; set; }
        public string? EmployerTenderNumber { get; set; }
        public string CidbRefNo { get; set; }
        public string? Title { get; set; }
        public string? TendersInvitedFor { get; set; }
        public string? PreferencesOffered { get; set; }
        public string? EligibilityCriteria { get; set; }
        public int? TypeOfContractId { get; set; }
        public string? TypeOfContractName { get; set; }
        public Guid? EmployerId { get; set; }
        public string? EmployerName { get; set; }
        public Guid? LocalMunicipalityId { get; set; }
        public string? LocalMunicipalityName { get; set; }
        public Guid? MetroDistrictId { get; set; }
        public string? MetroDistrictName { get; set; }
        public Guid? ProvinceId { get; set; }
        public string? ProvinceName { get; set; }
        public Guid? SubCategoryId { get; set; }
        public string? SubCategoryName { get; set; }
        public Guid? AlternateSubCategoryId { get; set; }
        public string? AlternateSubCategoryName { get; set; }
        public int? TenderValueRangeId { get; set; }
        public string? TenderValueRangeName { get; set; }
        public Guid? ClassOfConstructionWorksId { get; set; }
        public string? ClassOfConstructionWorksName { get; set; }
        public Guid? AlternateClassOfConstructionWorksId { get; set; }
        public string? AlternateClassOfConstructionWorksName { get; set; }
        public int? StatusCodeId { get; set; }
        public int? EmergingEnterpriseSupport { get; set; }
        public int? PartOfTargetedDevelopmentProgramme { get; set; }
        public string? NameOfTargetedDevelopmentProgramme { get; set; }
        public int? NationalContractorDevelopmentProgramme { get; set; }
        public DateTime? DocumentsAvailableFrom { get; set; }
        public DateTime? ClosingDateTime { get; set; }
        public DateTime? DateAdvertised { get; set; }
        public string? ClarificationMeetingRequired { get; set; }
        public string? ClarificationMeetingPlace { get; set; }
        public DateTime? ClarificationMeetingDateAndTime { get; set; }
        public bool? ClarificationMeetingCompulsory { get; set; }
        public bool? AdditionalClarificationMeetingRequired { get; set; }
        public string? AdditionalClarificationMeetingPlace { get; set; }
        public DateTime? AdditionalClarificationMeetingDateAndTime { get; set; }
        public bool? AdditionalClarificationMeetingCompulsory { get; set; }
        public bool IsClosed { get; set; }
        public bool? IsEOI { get; set; }
        public bool? IsTermContract { get; set; }
        public bool? IsPartOfEPWP { get; set; }

        public string PrimaryAddressLine1 { get; set; }
        public string PrimaryAddressLine2 { get; set; }
        public string PrimaryAddressLine3 { get; set; }
        public string PrimaryAddressLine4 { get; set; }
        public string PostalAddressLine1 { get; set; }
        public string PostalAddressLine2 { get; set; }
        public string PostalAddressLine3 { get; set; }
        public string PostalAddressLine4 { get; set; }
        public string DepositAmount { get; set; }
        public List<ContactForTenderModel> ContactPerson { get; set; } = new List<ContactForTenderModel>();
    }
}
