using ITender.Integration.Api.Configuration;
using ITender.Integration.Api.Models;
using Microsoft.Xrm.Sdk;

namespace ITender.Integration.Api.Infrastructure.Mappers;

/// <summary>
/// Maps a Dataverse nv_tender entity to the API's TenderModel.
/// Option-set Yes/No convention: 100000000 = Yes, 100000001 = No.
/// </summary>
public static class TenderMapper
{
    private const int OptionYes = 100000000;
    private const int OptionNo = 100000001;

    public static TenderModel ToDomain(Entity entity)
    {
        return new TenderModel
        {
            Id = entity.Id,

            // --- Notice details ---
            EmployerTenderNumber = GetString(entity, TenderSchema.EmployerTenderNumber),
            Title = GetString(entity, TenderSchema.TenderTitle),
            TendersInvitedFor = GetString(entity, TenderSchema.TendersInvitedFor),
            PreferencesOffered = GetString(entity, TenderSchema.PreferencesOffered),
            EligibilityCriteria = GetString(entity, TenderSchema.EligibilityCriteria),
            CidbRefNo = GetString(entity, TenderSchema.CidbReferenceNumber),

            // --- Employer ---
            EmployerId = GetLookupId(entity, TenderSchema.EmployerId),
            EmployerName = GetLookupName(entity, TenderSchema.EmployerId),

            // --- Location ---
            ProvinceId = GetLookupId(entity, TenderSchema.ProvinceId),
            ProvinceName = GetLookupName(entity, TenderSchema.ProvinceId),
            MetroDistrictId = GetLookupId(entity, TenderSchema.MetroDistrictMunicipalityId),
            MetroDistrictName = GetLookupName(entity, TenderSchema.MetroDistrictMunicipalityId),
            LocalMunicipalityId = GetLookupId(entity, TenderSchema.LocalMunicipalityId),
            LocalMunicipalityName = GetLookupName(entity, TenderSchema.LocalMunicipalityId),

            // --- Contract details ---
            TypeOfContractId = GetOption(entity, TenderSchema.TypeOfContract),
            TypeOfContractName = GetOptionLabel(entity, TenderSchema.TypeOfContract),

            ClassOfConstructionWorksId = GetLookupId(entity, TenderSchema.ClassOfConstructionWorksId),
            ClassOfConstructionWorksName = GetLookupName(entity, TenderSchema.ClassOfConstructionWorksId),

            AlternateClassOfConstructionWorksId = GetLookupId(entity, TenderSchema.ClassOfConstructionWorksAltId),
            AlternateClassOfConstructionWorksName = GetLookupName(entity, TenderSchema.ClassOfConstructionWorksAltId),

            SubCategoryId = GetLookupId(entity, TenderSchema.ClassOfConstructionWorksSubCategoryId),
            SubCategoryName = GetLookupName(entity, TenderSchema.ClassOfConstructionWorksSubCategoryId),

            TenderValueRangeId = GetOption(entity, TenderSchema.TenderValueRange),
            TenderValueRangeName = GetOptionLabel(entity, TenderSchema.TenderValueRange),

            EmergingEnterpriseSupport = GetOption(entity, TenderSchema.EmergingEnterpriseSupport),
            NameOfTargetedDevelopmentProgramme = GetString(entity, TenderSchema.NameOfTargetedDevelopmentProgramme),
            NationalContractorDevelopmentProgramme = GetOption(entity, TenderSchema.NationalContractorDevelopmentProgramme),
            IsTermContract = GetOptionAsBool(entity, TenderSchema.TermContract),

            // --- Documents & payment ---
            DocumentsAvailableFrom = GetDate(entity, TenderSchema.DateAndTimeDocumentsAvailableFrom),
            DepositAmount = GetMoney(entity, TenderSchema.DepositAmount),

            // --- Address 1 (primary) ---
            PrimaryAddressLine1 = GetString(entity, TenderSchema.Address1Line1),
            PrimaryAddressLine2 = GetString(entity, TenderSchema.Address1Line2),
            PrimaryAddressLine3 = GetString(entity, TenderSchema.Address1Line3),
            PrimaryAddressLine4 = GetString(entity, TenderSchema.Address1Line4),

            // --- Address 2 (additional collection) ---
            PostalAddressLine1 = GetString(entity, TenderSchema.Address2Line1),
            PostalAddressLine2 = GetString(entity, TenderSchema.Address2Line2),
            PostalAddressLine3 = GetString(entity, TenderSchema.Address2Line3),
            PostalAddressLine4 = GetString(entity, TenderSchema.Address2Line4),

            // --- Clarification meeting ---
            ClarificationMeetingRequired = GetOption(entity, TenderSchema.ClarificationMeeting),
            ClarificationMeetingPlace = GetString(entity, TenderSchema.ClarificationMeetingPlace),
            ClarificationMeetingDateAndTime = GetDate(entity, TenderSchema.ClarificationMeetingDateAndTime),
            ClarificationMeetingCompulsory = GetOptionAsBool(entity, TenderSchema.ClarificationMeetingCompulsory),

            AdditionalClarificationMeetingRequired = GetOptionAsBool(entity, TenderSchema.AddClarificationMeeting),
            AdditionalClarificationMeetingPlace = GetString(entity, TenderSchema.AddClarificationMeetingPlace),
            AdditionalClarificationMeetingDateAndTime = GetDate(entity, TenderSchema.AddClarificationMeetingDateAndTime),
            AdditionalClarificationMeetingCompulsory = GetOptionAsBool(entity, TenderSchema.AddClarificationMeetingCompulsory),

            // --- Submission ---
            ClosingDateTime = GetDate(entity, TenderSchema.ClosingDateAndTime),
            IsClosed = GetBool(entity, TenderSchema.IsClosed) ?? false,

            // --- Status ---
            StatusCodeId = GetOption(entity, TenderSchema.StatusCode),
            DateAdvertised = GetDate(entity, TenderSchema.DateAdvertised),

            // EOI is a Choice column (OptionSetValue). Mapping 100000000 to true.
            IsEOI = GetOption(entity, TenderSchema.IsEOIorTender) == TenderOptionSets.IsEOIOrTender.EOI,

            // Awarded is a standard Yes/No (Boolean) column in Dataverse.
            Awarded = GetBool(entity, TenderSchema.Awarded),

            // EPWP is a Choice column (OptionSetValue).
            IsPartOfEPWP = GetOption(entity, TenderSchema.ContractPartOfEPWP) == TenderOptionSets.ContractPartOfEPWP.Yes,

            // --- Cancellation ---
            DateCancelled = GetDate(entity, TenderSchema.DateCancelled),
            CancellationReasonId = GetLookupId(entity, TenderSchema.CancellationReason),
            CancellationReasonName = GetLookupName(entity, TenderSchema.CancellationReason),
            CancellationReasonOther = GetString(entity, TenderSchema.CancellationReasonOther),
        };
    }

    // ---- helpers ----

    private static string? GetString(Entity e, string field)
        => e.Contains(field) ? e.GetAttributeValue<string>(field) : null;

    private static Guid? GetLookupId(Entity e, string field)
        => e.GetAttributeValue<EntityReference>(field)?.Id;

    private static string? GetLookupName(Entity e, string field)
        => e.GetAttributeValue<EntityReference>(field)?.Name;

    private static int? GetOption(Entity e, string field)
        => e.GetAttributeValue<OptionSetValue>(field)?.Value;

    private static string? GetOptionLabel(Entity e, string field)
        => e.FormattedValues.Contains(field) ? e.FormattedValues[field] : null;

    private static DateTime? GetDate(Entity e, string field)
        => e.GetAttributeValue<DateTime?>(field);

    private static bool? GetBool(Entity e, string field)
    {
        if (!e.Contains(field)) return null;
        return e.GetAttributeValue<bool?>(field);
    }

    /// <summary>
    /// Reads a Yes/No option-set field (100000000 = Yes, 100000001 = No) as a bool.
    /// </summary>
    private static bool? GetOptionAsBool(Entity e, string field)
    {
        var opt = e.GetAttributeValue<OptionSetValue>(field);
        if (opt is null) return null;
        return opt.Value switch
        {
            OptionYes => true,
            OptionNo => false,
            _ => null
        };
    }

    /// <summary>
    /// Reads a Currency (Money) column and returns the numeric value.
    /// </summary>
    private static decimal? GetMoney(Entity e, string field)
    {
        var money = e.GetAttributeValue<Money>(field);
        return money?.Value;
    }
}