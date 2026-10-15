using ITender.Integration.Api.Enums;
using ITender.Integration.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace ITender.Integration.Api.TranferObjects
{
    public class CreateTenderModel
    {
        //Notice Details
        [Required]
        public string? EmployerTenderNumber { get; set; }
        [Required]
        public string? Title { get; set; }
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? TendersInvitedFor { get; set; }
        [Required]
        public string? PreferencesOffered { get; set; }
        [Required]
        public string? EligibilityCriteria { get; set; }
        //Location Details
        [Required]
        public Guid? ProvinceId { get; set; }
        [Required]
        public Guid? MetroDistrictId { get; set; }
        public Guid? LocalMunicipalityId { get; set; }
        //Contract Details
        [Required]
        public int? TypeOfContractId { get; set; } = 100000000;
        [Required]
        public Guid? ClassOfConstructionWorksId { get; set; }
        public Guid? SubCategoryId { get; set; }
        public Guid? AlternateClassOfConstructionWorksId { get; set; }
        [Required]
        public int? TenderValueRangeId { get; set; }
        public int? EmergingEnterpriseSupportId { get; set; } = 100000000;
        public string? NameOfTargetedDevelopmentProgramme { get; set; }
        public int NationalContractorDevelopmentProgrammeId { get; set; } = 100000000;
        public int? IsTermContract { get; set; } = 100000000;
        public TenderStatus? Status { get; set; }
        //Employer Details
        public Guid? EmployerId { get; set; }
        [Required]
        public AddressModel PrimaryAddress { get; set; } = new AddressModel();
        public AddressModel? AdditionalCollectionAddress { get; set; }
        [Required]
        public DateTime? DocumentsAvailableFrom { get; set; }
        public decimal? DepositAmount { get; set; }
        public Boolean? MethodOfPaymentBankGuaranteedCheque { get; set; } = false;
        public Boolean? MethodOfPaymentCash { get; set; } = false;
        public Boolean? MethodOfPaymentProofOfDepost { get; set; } = false;
        public String? FurtherPaymentAndCollectionInformation { get; set; }
        //Contact Details
        public List<ContactForTenderModel> ContactPerson { get; set; } = new List<ContactForTenderModel>();
        //Clarification Meeting details
        [Required]
        public int ClarificationMeetingRequired { get; set; } = 100000000;
        public string? ClarificationMeetingPlace { get; set; }
        public DateTime? ClarificationMeetingDateAndTime { get; set; }
        public int? ClarificationMeetingCompulsory { get; set; } = 100000000;
        public int? AdditionalClarificationMeeting { get; set; } = 100000000;
        public string? AddClarificationMeetingPlace { get; set; }
        public DateTime? AddClarificationMeetingDateAndTime { get; set; }
        public int? AddClarificationMeetingCompulsory { get; set; } = 100000000;
        //Submission Details
        [Required]
        public DateTime ClosingDateTime { get; set; }
        public Boolean NotAcceptedEmail { get; set; } = false;
    }
}
