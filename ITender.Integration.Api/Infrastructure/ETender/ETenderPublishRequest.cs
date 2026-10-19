using System.Text.Json.Serialization;

namespace ITender.Integration.Api.Infrastructure.ETender;

/// <summary>
/// e-Tender create-tender payload. JSON property names match the e-Tender contract.
/// </summary>
public sealed class ETenderPublishRequest
{
    [JsonPropertyName("Id")] public int Id { get; set; }
    [JsonPropertyName("Tender_No")] public string? TenderNo { get; set; }
    [JsonPropertyName("Type")] public string? Type { get; set; }
    [JsonPropertyName("Delivery")] public string? Delivery { get; set; }
    [JsonPropertyName("Department")] public string? Department { get; set; }
    [JsonPropertyName("Date_Published")] public DateTime? DatePublished { get; set; }
    [JsonPropertyName("Cbrief")] public bool? CBrief { get; set; }
    [JsonPropertyName("Cd")] public string? Cd { get; set; }
    [JsonPropertyName("Dp")] public string? Dp { get; set; }
    [JsonPropertyName("Closing_Date")] public DateTime? ClosingDate { get; set; }
    [JsonPropertyName("Brief")] public string? Brief { get; set; }
    [JsonPropertyName("Compulsory_Briefing_Session")] public DateTime? CompulsoryBriefingSession { get; set; }
    [JsonPropertyName("DepartmentId")] public Guid? DepartmentId { get; set; }
    [JsonPropertyName("ProvinceId")] public Guid? ProvinceId { get; set; }
    [JsonPropertyName("Status")] public string? Status { get; set; }
    [JsonPropertyName("Category")] public string? Category { get; set; }
    [JsonPropertyName("Description")] public string? Description { get; set; }
    [JsonPropertyName("Province")] public string? Province { get; set; }
    [JsonPropertyName("ContactPerson")] public string? ContactPerson { get; set; }
    [JsonPropertyName("Email")] public string? Email { get; set; }
    [JsonPropertyName("Telephone")] public string? Telephone { get; set; }
    [JsonPropertyName("Fax")] public string? Fax { get; set; }
    [JsonPropertyName("BriefingVenue")] public string? BriefingVenue { get; set; }
    [JsonPropertyName("Conditions")] public string? Conditions { get; set; }
    [JsonPropertyName("SupportDocuments")] public object? SupportDocuments { get; set; }
    [JsonPropertyName("Bf")] public string? Bf { get; set; }
    [JsonPropertyName("BriefingSession")] public object? BriefingSession { get; set; }
    [JsonPropertyName("Bc")] public string? Bc { get; set; }
    [JsonPropertyName("BriefingCompulsory")] public object? BriefingCompulsory { get; set; }
    [JsonPropertyName("ESubmission")] public bool ESubmission { get; set; }
    [JsonPropertyName("ClosingReason")] public string? ClosingReason { get; set; }
    [JsonPropertyName("CancelledReason")] public string? CancelledReason { get; set; }
    [JsonPropertyName("AwardedCompanies")] public object? AwardedCompanies { get; set; }
    [JsonPropertyName("Bidders")] public object? Bidders { get; set; }
    [JsonPropertyName("Awarded_Contact")] public object? AwardedContact { get; set; }
    [JsonPropertyName("CancellationReason")] public string? CancellationReason { get; set; }
}

public sealed record ETenderPublishResponse(int Id, string? Status);