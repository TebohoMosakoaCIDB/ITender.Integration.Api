using System.ComponentModel.DataAnnotations;

namespace ITender.Integration.Api.TranferObjects;

public class AwardTenderRequest
{
    [Required] public string? ContractTitle { get; set; }
    [Required] public DateTime? AwardDate { get; set; }
    [Required] public DateTime? EndDate { get; set; }
    public Guid? ClassOfConstructionWorksId { get; set; }
    public int? TenderValueRangeId { get; set; }

    [Required] public decimal? TotalContractValueInclVat { get; set; }

    // BR-19 (Assumption: Max 80)
    public int? FinancialPoints { get; set; }

    // BR-19 (Assumption: Max 20)
    public int? PreferencePoints { get; set; }

    // Forms of Contract
    public bool FormOfContractJBCC { get; set; }
    public bool FormOfContractGCC { get; set; }
    public bool FormOfContractNEC { get; set; }
    public bool FormOfContractFIDIC { get; set; }

    // Contractors (Can be 1 for standard, or multiple for JV)
    [Required]
    public List<AwardContractorModel> Contractors { get; set; } = new();
    public string? Description { get; set; }     
    public int? TypeOfContractId { get; set; }  

    // Location
    public Guid? ProvinceId { get; set; }       
    public Guid? MetroDistrictId { get; set; }    
    public Guid? LocalMunicipalityId { get; set; } 
}

public class AwardContractorModel
{
    [Required] public Guid? ContractorId { get; set; }
    public int JvSharePercentage { get; set; }
    public bool IsLeadContractor { get; set; }
    public string? EnterpriseName { get; set; }
    public string? RegistrationNumber { get; set; }
}