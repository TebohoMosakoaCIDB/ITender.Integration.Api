using ITender.Integration.Api.Configuration;
using ITender.Integration.Api.Models;

namespace ITender.Integration.Api.Infrastructure.ETender;

public static class TenderPublishMapper
{
    public static ETenderPublishRequest ToETenderRequest(this TenderModel tender)
    {
        var contact = tender.ContactPerson?.FirstOrDefault();

        return new ETenderPublishRequest
        {
            // TODO: e-Tender ID strategy. 0 = create-new until confirmed.
            // On success, the returned Id must be persisted to nv_etenderid.
            Id = 0,

            TenderNo = tender.EmployerTenderNumber,
            Type = tender.TypeOfContractName,

            Delivery = string.Join(", ",
                new[]
                {
                    tender.PrimaryAddressLine1,
                    tender.PrimaryAddressLine2,
                    tender.PrimaryAddressLine3,
                    tender.PrimaryAddressLine4
                }.Where(x => !string.IsNullOrWhiteSpace(x))),

            Department = tender.EmployerName,
            DatePublished = tender.DateAdvertised,
            CBrief = tender.ClarificationMeetingRequired == TenderOptionSets.Yes,

            Cd = tender.ClosingDateTime?.ToString("dddd, dd MMMM yyyy - HH:mm"),
            Dp = tender.DateAdvertised?.ToString("dddd, dd MMMM yyyy"),

            ClosingDate = tender.ClosingDateTime,
            Brief = tender.ClarificationMeetingDateAndTime?.ToString("dddd, dd MMMM yyyy - HH:mm"),
            CompulsoryBriefingSession = tender.ClarificationMeetingDateAndTime,

            DepartmentId = tender.EmployerId,
            ProvinceId = tender.ProvinceId,

            Status = tender.IsClosed ? "Closed" : "Published",

            Category = tender.SubCategoryName,
            Description = tender.Title,
            Province = tender.ProvinceName,

            ContactPerson = contact?.PersonToQuery,
            Email = contact?.Email,
            Telephone = contact?.TelephoneNumber,
            //Fax = contact?.FaxNumber,

            BriefingVenue = tender.ClarificationMeetingPlace,
            Conditions = tender.EligibilityCriteria,

            SupportDocuments = null,
            Bf = tender.ClarificationMeetingRequired switch
            {
                100000000 => "Yes",
                100000001 => "No",
                _ => null
            },
            BriefingSession = null,
            Bc = tender.ClarificationMeetingCompulsory switch
            {
                true => "Yes",
                false => "No",
                _ => null
            },
            BriefingCompulsory = tender.ClarificationMeetingCompulsory,
            ESubmission = false,

            ClosingReason = null,
            CancelledReason = null,
            AwardedCompanies = null,
            Bidders = null,
            AwardedContact = null,
            CancellationReason = null
        };
    }
}