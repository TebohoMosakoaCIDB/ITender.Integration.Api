using ITender.Integration.Api.Enums;
using ITender.Integration.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace ITender.Integration.Api.TranferObjects;

public class CreateTenderModel
{
    // --- Notice details ---
    [Required] public string? EmployerTenderNumber { get; set; }
    [Required] public string? Title { get; set; }              // maps to nv_tendertitle
    [Required] public string? Name { get; set; }                // maps to nv_name (primary)
    [Required] public string? TendersInvitedFor { get; set; }
    public string? PreferencesOffered { get; set; }             // optional per business decision
    public string? EligibilityCriteria { get; set; }            // optional per business decision

    // --- Location ---
    [Required] public Guid? ProvinceId { get; set; }
    [Required] public Guid? MetroDistrictId { get; set; }
    [Required] public Guid? LocalMunicipalityId { get; set; }   // now mandatory per spec

    // Lat/Long/GPS — in scope per your answer
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? GpsCoordinateTypeId { get; set; }

    // --- Contract details ---
    [Required] public int? TypeOfContractId { get; set; }
    [Required] public Guid? ClassOfConstructionWorksId { get; set; }
    [Required] public Guid? SubCategoryId { get; set; }         // now mandatory per spec
    public Guid? AlternateClassOfConstructionWorksId { get; set; }
    [Required] public int? TenderValueRangeId { get; set; }
    public int? EmergingEnterpriseSupportId { get; set; }
    public string? NameOfTargetedDevelopmentProgramme { get; set; }
    public int? NationalContractorDevelopmentProgrammeId { get; set; }  // changed to int?
    public int? IsTermContract { get; set; }
    public TenderStatus? Status { get; set; }

    // --- Employer ---
    [Required] public Guid? EmployerId { get; set; }            // now mandatory per spec

    // --- Address 1 (primary) ---
    [Required] public AddressModel PrimaryAddress { get; set; } = new();

    // --- Address 2 (additional collection) ---
    public bool AdditionalCollectionAddress { get; set; } = false;   // the flag
    public AddressModel? AdditionalCollectionAddressDetails { get; set; }

    // --- Documents and payment ---
    [Required] public DateTime? DocumentsAvailableFrom { get; set; }
    public decimal? DepositAmount { get; set; }
    public bool MethodOfPaymentBankGuaranteedCheque { get; set; } = false;
    public bool MethodOfPaymentCash { get; set; } = false;
    public bool MethodOfPaymentProofOfDeposit { get; set; } = false;   // fixed typo
    public string? FurtherPaymentAndCollectionInformation { get; set; }

    // --- Contacts ---
    public List<ContactForTenderModel> ContactPerson { get; set; } = new();

    // --- Clarification meeting ---
    [Required] public int ClarificationMeetingRequired { get; set; }
    public int? ClarificationMeetingCompulsory { get; set; }
    public string? ClarificationMeetingPlace { get; set; }
    public DateTime? ClarificationMeetingDateAndTime { get; set; }

    public int? AdditionalClarificationMeeting { get; set; }
    public int? AddClarificationMeetingCompulsory { get; set; }
    public string? AddClarificationMeetingPlace { get; set; }
    public DateTime? AddClarificationMeetingDateAndTime { get; set; }

    // --- Submission ---
    [Required] public DateTime ClosingDateTime { get; set; }
    public bool NotAcceptedEmail { get; set; } = false;
}