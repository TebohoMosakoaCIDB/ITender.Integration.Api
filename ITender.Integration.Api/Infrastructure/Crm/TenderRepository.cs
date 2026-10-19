using ITender.Integration.Api.Constants;
using ITender.Integration.Api.Enums;
using ITender.Integration.Api.Infrastructure.Mappers;
using ITender.Integration.Api.Models;
using ITender.Integration.Api.TranferObjects;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace ITender.Integration.Api.Infrastructure.Crm;

public interface ITenderRepository
{
    Task<List<ContactForTenderModel>> GetContactsByTenderIdAsync(Guid tenderId, CancellationToken ct);
    Task<(IEnumerable<TenderModel> Items, int TotalCount)> GetPagedAsync(TenderFilterRequest filter, CancellationToken ct);
    Task<TenderModel?> GetByIdAsync(Guid id, CancellationToken ct);
    Task MarkAdvertisedAsync(Guid id, DateTime dateAdvertised, CancellationToken ct);

    Task MarkCancelledAsync(Guid id, Guid reasonId, string? reasonOther, DateTime decisionDate, Guid? cancelledByUserId, CancellationToken ct);
    Task<string?> GetCancellationReasonLabelAsync(Guid reasonId, CancellationToken ct);

    Task<Guid> CreateAsync(CreateTenderModel model, CancellationToken ct);
    Task MarkPublishedAsync(Guid id, DateTime dateAdvertised, CancellationToken ct);
    Task<List<LookupItemDto>> GetCancellationReasonsAsync(CancellationToken ct);
}

public sealed class TenderRepository(ICrmServiceFactory crmFactory) : ITenderRepository
{
    private readonly IOrganizationServiceAsync2 _crm = crmFactory.Service;
    private const string TenderEntity = "nv_tender";

    public async Task<List<ContactForTenderModel>> GetContactsByTenderIdAsync(Guid tenderId, CancellationToken ct)
    {
        var query = new QueryExpression("contact")
        {
            ColumnSet = new ColumnSet("firstname", "lastname", "mobilephone", "telephone1", "emailaddress1"),
            NoLock = true
        };

        // This is the magic link from your old system
        query.Criteria.AddCondition("nv_tenderid", ConditionOperator.Equal, tenderId);

        var result = await _crm.RetrieveMultipleAsync(query, ct);

        return result.Entities.Select(e => new ContactForTenderModel
        {
            // Your old mapper used FullName, here we combine first + last
            PersonToQuery = $"{e.GetAttributeValue<string>("firstname")} {e.GetAttributeValue<string>("lastname")}".Trim(),
            MobilePhoneNumber = e.GetAttributeValue<string>("mobilephone"),
            TelephoneNumber = e.GetAttributeValue<string>("telephone1"),
            Email = e.GetAttributeValue<string>("emailaddress1"),
            //FaxNumber = e.GetAttributeValue<string>("faxnumber")
        }).ToList();
    }
    public async Task<(IEnumerable<TenderModel> Items, int TotalCount)> GetPagedAsync(TenderFilterRequest filter, CancellationToken ct)
    {
        var query = new QueryExpression(TenderSchema.EntityLogicalName)
        {
            ColumnSet = new ColumnSet(true),
            PageInfo = new PagingInfo
            {
                PageNumber = filter.PageNumber > 0 ? filter.PageNumber : 1,
                Count = filter.PageSize > 0 && filter.PageSize <= 100 ? filter.PageSize : 20,
                ReturnTotalRecordCount = true
            }
        };

        if (filter.StatusCode.HasValue)
            query.Criteria.AddCondition(TenderSchema.StatusCode, ConditionOperator.Equal, filter.StatusCode.Value);

        if (filter.EmployerId.HasValue)
            query.Criteria.AddCondition(TenderSchema.EmployerId, ConditionOperator.Equal, filter.EmployerId.Value);

        if (!string.IsNullOrWhiteSpace(filter.TenderNumber))
            query.Criteria.AddCondition(TenderSchema.EmployerTenderNumber, ConditionOperator.Like, $"%{filter.TenderNumber}%");

        // Optional: Order by newest first
        query.AddOrder(TenderSchema.DateAdvertised, OrderType.Descending);

        var result = await _crm.RetrieveMultipleAsync(query, ct);

        return (
            result.Entities.Select(TenderMapper.ToDomain).ToList(),
            result.TotalRecordCount
        );
    }
    public async Task<List<LookupItemDto>> GetCancellationReasonsAsync(CancellationToken ct)
    {
        var query = new QueryExpression("nv_tendercancelreason")
        {
            ColumnSet = new ColumnSet("nv_name", "nv_message"),
            NoLock = true
        };

        // Optional: Only get active ones (statecode = 0)
        query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

        var result = await _crm.RetrieveMultipleAsync(query, ct);

        return result.Entities.Select(e => new LookupItemDto(
            e.Id,
            e.GetAttributeValue<string>("nv_name") ?? "Unknown",
            e.GetAttributeValue<string>("nv_message")
        )).ToList();
    }
    public async Task<TenderModel?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var query = new QueryExpression(TenderEntity)
        {
            ColumnSet = new ColumnSet(true),
            TopCount = 1,
            NoLock = true
        };
        query.Criteria.AddCondition(TenderSchema.Id, ConditionOperator.Equal, id);

        var e = (await _crm.RetrieveMultipleAsync(query, ct)).Entities.FirstOrDefault();
        return e is null ? null : TenderMapper.ToDomain(e);
    }

    public Task MarkAdvertisedAsync(Guid id, DateTime dateAdvertised, CancellationToken ct)
        => _crm.UpdateAsync(new Entity(TenderEntity, id)
        {
            [TenderSchema.StatusCode] = new OptionSetValue(100000000), // Advertised
            [TenderSchema.DateAdvertised] = dateAdvertised
        }, ct);

    public async Task MarkCancelledAsync(
    Guid id,
    Guid reasonId,
    string? reasonOther,
    DateTime decisionDate,
    Guid? cancelledByUserId,
    CancellationToken ct)
    {
        var tender = new Entity(TenderSchema.EntityLogicalName, id)
        {
            // Use 0 for Active, 100000001 for Cancelled
            [TenderSchema.StateCode] = new OptionSetValue(0),
            [TenderSchema.StatusCode] = new OptionSetValue(100000001),
            [TenderSchema.DateCancelled] = decisionDate,
            [TenderSchema.CancellationReason] = new EntityReference("nv_tendercancelreason", reasonId)
        };

        if (!string.IsNullOrWhiteSpace(reasonOther))
            tender[TenderSchema.CancellationReasonOther] = reasonOther;

        if (cancelledByUserId.HasValue)
            tender[TenderSchema.CancelledByExternalUserId] = new EntityReference("contact", cancelledByUserId.Value);

        await _crm.UpdateAsync(tender, ct);
    }

    public async Task<string?> GetCancellationReasonLabelAsync(Guid reasonId, CancellationToken ct)
    {
        var q = new QueryExpression("nv_tendercancelreason")
        {
            ColumnSet = new ColumnSet("nv_name"),
            TopCount = 1,
            NoLock = true
        };
        q.Criteria.AddCondition("nv_tendercancelreasonid", ConditionOperator.Equal, reasonId);

        var result = await _crm.RetrieveMultipleAsync(q, ct);
        return result.Entities.FirstOrDefault()?.GetAttributeValue<string>("nv_name");
    }

    public async Task<Guid> CreateAsync(CreateTenderModel model, CancellationToken ct)
    {
        var tender = new Entity(TenderSchema.EntityLogicalName);

        // Notice details
        tender[TenderSchema.EmployerTenderNumber] = model.EmployerTenderNumber;
        tender[TenderSchema.TenderTitle] = model.Title;
        tender[TenderSchema.Name] = model.Name;
        tender[TenderSchema.TendersInvitedFor] = model.TendersInvitedFor;
        if (!string.IsNullOrWhiteSpace(model.PreferencesOffered))
            tender[TenderSchema.PreferencesOffered] = model.PreferencesOffered;
        if (!string.IsNullOrWhiteSpace(model.EligibilityCriteria))
            tender[TenderSchema.EligibilityCriteria] = model.EligibilityCriteria;

        // Location
        tender[TenderSchema.ProvinceId] = new EntityReference("nv_province", model.ProvinceId!.Value);
        tender[TenderSchema.MetroDistrictMunicipalityId] = new EntityReference("nv_metrodistrict", model.MetroDistrictId!.Value);
        if (model.LocalMunicipalityId.HasValue)
            tender[TenderSchema.LocalMunicipalityId] = new EntityReference("nv_localmunicipality", model.LocalMunicipalityId.Value);

        // TODO: Latitude, Longitude, GpsCoordinateTypeId — add once logical names are confirmed.

        // Contract details
        if (model.TypeOfContractId.HasValue)
            tender[TenderSchema.TypeOfContract] = new OptionSetValue(model.TypeOfContractId.Value);
        tender[TenderSchema.ClassOfConstructionWorksId] = new EntityReference("nv_classofconstructionwork", model.ClassOfConstructionWorksId!.Value);
        tender[TenderSchema.ClassOfConstructionWorksSubCategoryId] = new EntityReference("nv_classofconstructionworkssubcategory", model.SubCategoryId!.Value);
        if (model.AlternateClassOfConstructionWorksId.HasValue)
            tender[TenderSchema.ClassOfConstructionWorksAltId] = new EntityReference("nv_classofconstructionwork", model.AlternateClassOfConstructionWorksId.Value);
        if (model.TenderValueRangeId.HasValue)
            tender[TenderSchema.TenderValueRange] = new OptionSetValue(model.TenderValueRangeId.Value);
        if (model.EmergingEnterpriseSupportId.HasValue)
            tender[TenderSchema.EmergingEnterpriseSupport] = new OptionSetValue(model.EmergingEnterpriseSupportId.Value);
        if (!string.IsNullOrWhiteSpace(model.NameOfTargetedDevelopmentProgramme))
            tender[TenderSchema.NameOfTargetedDevelopmentProgramme] = model.NameOfTargetedDevelopmentProgramme;
        if (model.NationalContractorDevelopmentProgrammeId.HasValue)
            tender[TenderSchema.NationalContractorDevelopmentProgramme] = new OptionSetValue(model.NationalContractorDevelopmentProgrammeId.Value);
        if (model.IsTermContract.HasValue)
            tender[TenderSchema.TermContract] = new OptionSetValue(model.IsTermContract.Value);

        // Status
        tender[TenderSchema.StatusCode] = new OptionSetValue(1); // Draft

        // Employer
        tender[TenderSchema.EmployerId] = new EntityReference("account", model.EmployerId!.Value);

        // Documents and payment
        tender[TenderSchema.DateAndTimeDocumentsAvailableFrom] = model.DocumentsAvailableFrom;
        if (model.DepositAmount.HasValue)
            tender[TenderSchema.DepositAmount] = new Money(model.DepositAmount.Value);
        tender[TenderSchema.MethodOfPaymentCash] = model.MethodOfPaymentCash;
        tender[TenderSchema.MethodOfPaymentBankGuaranteedCheque] = model.MethodOfPaymentBankGuaranteedCheque;
        tender[TenderSchema.MethodOfPaymentProofOfDeposit] = model.MethodOfPaymentProofOfDeposit;
        if (!string.IsNullOrWhiteSpace(model.FurtherPaymentAndCollectionInformation))
            tender[TenderSchema.AdditionalPaymentAndCollectionDetails] = model.FurtherPaymentAndCollectionInformation;

        // Address 1
        tender[TenderSchema.Address1Line1] = model.PrimaryAddress.Line1;
        tender[TenderSchema.Address1Line2] = model.PrimaryAddress.City;
        tender[TenderSchema.Address1Line3] = model.PrimaryAddress.Province;
        tender[TenderSchema.Address1Line4] = model.PrimaryAddress.PostalCode;

        // Address 2 (conditional)
        tender[TenderSchema.AdditionalCollectionAddressFlag] = model.AdditionalCollectionAddress;
        if (model.AdditionalCollectionAddress && model.AdditionalCollectionAddressDetails is not null)
        {
            tender[TenderSchema.Address2Line1] = model.AdditionalCollectionAddressDetails.Line1;
            tender[TenderSchema.Address2Line2] = model.AdditionalCollectionAddressDetails.City;
            tender[TenderSchema.Address2Line3] = model.AdditionalCollectionAddressDetails.Province;
            tender[TenderSchema.Address2Line4] = model.AdditionalCollectionAddressDetails.PostalCode;
        }

        // Clarification meeting
        tender[TenderSchema.ClarificationMeeting] = new OptionSetValue(model.ClarificationMeetingRequired);
        if (!string.IsNullOrWhiteSpace(model.ClarificationMeetingPlace))
            tender[TenderSchema.ClarificationMeetingPlace] = model.ClarificationMeetingPlace;
        if (model.ClarificationMeetingDateAndTime.HasValue)
            tender[TenderSchema.ClarificationMeetingDateAndTime] = model.ClarificationMeetingDateAndTime.Value;
        if (model.ClarificationMeetingCompulsory.HasValue)
            tender[TenderSchema.ClarificationMeetingCompulsory] = new OptionSetValue(model.ClarificationMeetingCompulsory.Value);

        if (model.AdditionalClarificationMeeting.HasValue)
            tender[TenderSchema.AddClarificationMeeting] = new OptionSetValue(model.AdditionalClarificationMeeting.Value);
        if (!string.IsNullOrWhiteSpace(model.AddClarificationMeetingPlace))
            tender[TenderSchema.AddClarificationMeetingPlace] = model.AddClarificationMeetingPlace;
        if (model.AddClarificationMeetingDateAndTime.HasValue)
            tender[TenderSchema.AddClarificationMeetingDateAndTime] = model.AddClarificationMeetingDateAndTime.Value;
        if (model.AddClarificationMeetingCompulsory.HasValue)
            tender[TenderSchema.AddClarificationMeetingCompulsory] = new OptionSetValue(model.AddClarificationMeetingCompulsory.Value);

        // Submission
        tender[TenderSchema.ClosingDateAndTime] = model.ClosingDateTime;
        tender[TenderSchema.NotAcceptedEmail] = model.NotAcceptedEmail;
        tender[TenderSchema.IsClosed] = false;

        return await _crm.CreateAsync(tender, ct);
    }

    public Task MarkPublishedAsync(Guid id, DateTime dateAdvertised, CancellationToken ct)
        => _crm.UpdateAsync(new Entity(TenderEntity, id)
        {
            [TenderSchema.StatusCode] = new OptionSetValue(100000000), // Advertised
            [TenderSchema.DateAdvertised] = dateAdvertised
        }, ct);
}