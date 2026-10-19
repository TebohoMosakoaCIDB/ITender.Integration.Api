using ITender.Integration.Api.Enums;
using ITender.Integration.Api.Infrastructure.Crm;
using ITender.Integration.Api.Infrastructure.ETender;
using ITender.Integration.Api.Models;
using ITender.Integration.Api.TranferObjects;
using ITender.Integration.Api.Validation;
using System.Diagnostics.Contracts;
using System.Security.Policy;

namespace ITender.Integration.Api.Services;

public sealed record AdvertiseResult(int ETenderId, string? ETenderStatus, DateTime PublishedAtUtc);
public sealed record PublishResult(int ETenderId, string? ETenderStatus, DateTime PublishedAtUtc);
public interface ITenderService
{
    Task<List<LookupItemDto>> GetCancellationReasonsAsync(CancellationToken ct);
    Task<TenderModel?> AdvertiseToITenderAsync(Guid tenderId, CancellationToken ct);
    Task<AdvertiseResult?> AdvertiseToETenderAsync(Guid tenderId, string correlationId, CancellationToken ct);
    Task<Guid> AwardAsync(Guid tenderId, AwardTenderRequest request, string correlationId, CancellationToken ct);

    Task<TenderModel> CreateAsync(CreateTenderModel model, CancellationToken ct);
    Task<PublishResult> CreateAndPublishAsync(CreateTenderModel model, string correlationId, CancellationToken ct);

    Task<TenderModel?> CancelAsync(Guid tenderId, CancelTenderRequest request, string correlationId, CancellationToken ct);
    Task<TenderModel?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<(IEnumerable<TenderModel> Items, int TotalCount)> GetPagedAsync(TenderFilterRequest filter, CancellationToken ct);
}

public sealed class TenderService(
    ITenderRepository tenders,
    IContractRepository contracts,
    IReferenceDataService referenceData,
    IETenderClient etender,
    ILogger<TenderService> log) : ITenderService
{
    private const int StatusDraft = 1;
    private const int StatusAdvertised = 100000000;
    private const int StatusCancelled = 100000001;

    public Task<List<LookupItemDto>> GetCancellationReasonsAsync(CancellationToken ct)
    => tenders.GetCancellationReasonsAsync(ct);
    public async Task<TenderModel?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(id, ct);
        if (tender is null) return null;

        return await HydrateContactsAsync(tender, ct);
    }
    public async Task<(IEnumerable<TenderModel> Items, int TotalCount)> GetPagedAsync(TenderFilterRequest filter, CancellationToken ct)
    {
        var (items, total) = await tenders.GetPagedAsync(filter, ct);

        // Hydrate contacts for the list (warning: this makes N+1 queries)
        foreach (var item in items)
        {
            await HydrateContactsAsync(item, ct);
        }

        return (items, total);
    }
    public async Task<TenderModel?> AdvertiseToITenderAsync(Guid tenderId, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(tenderId, ct);
        if (tender is null) return null;

        if (tender.StatusCodeId == StatusCancelled)
            throw new InvalidOperationException("A cancelled tender cannot be advertised.");
        if (tender.StatusCodeId == StatusAdvertised)
            return tender; // idempotent — already advertised
        if (tender.StatusCodeId != StatusDraft)
            throw new InvalidOperationException($"Tender is in status {tender.StatusCodeId}; expected Draft.");
        if (tender.ClosingDateTime is null)
            throw new InvalidOperationException("Tender has no closing date.");
        if (string.IsNullOrWhiteSpace(tender.EmployerTenderNumber))
            throw new InvalidOperationException("Tender has no employer tender number.");

        // FR-008: closing date must not be earlier than the advertisement date (which is "now").
        if (tender.ClosingDateTime.Value <= DateTime.UtcNow)
            throw new InvalidOperationException(
                "Closing date must be after the advertisement date (FR-008).");

        var now = DateTime.UtcNow;
        await tenders.MarkAdvertisedAsync(tenderId, now, ct);

        log.LogInformation("Tender {TenderId} advertised locally at {Now:O}", tenderId, now);

        return await tenders.GetByIdAsync(tenderId, ct);
    }
    public async Task<AdvertiseResult?> AdvertiseToETenderAsync(Guid tenderId, string correlationId, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(tenderId, ct);
        if (tender is null) return null;

        if (tender.StatusCodeId != StatusAdvertised)
            throw new InvalidOperationException(
                "Tender must be advertised locally before publishing to e-Tender.");

        if (string.IsNullOrWhiteSpace(tender.EmployerTenderNumber))
            throw new InvalidOperationException("Tender has no employer tender number to publish.");

        var request = tender.ToETenderRequest();
        var idempotencyKey = $"tender:{tender.EmployerTenderNumber}:publish";

        var result = await etender.PublishTenderAsync(request, correlationId, idempotencyKey, ct);

        if (!result.Success)
            throw new InvalidOperationException($"e-Tender publish failed: {result.Error}");

        log.LogInformation(
            "Tender {TenderId} published to e-Tender. Correlation {CorrelationId}",
            tenderId, correlationId);

        return new AdvertiseResult(0, "Published", DateTime.UtcNow);
    }
    public async Task<TenderModel> CreateAsync(CreateTenderModel model, CancellationToken ct)
    {
        // FR-005 reference-data validation
        if (!await referenceData.EmployerExistsAsync(model.EmployerId!.Value, ct))
            throw new InvalidOperationException($"Employer {model.EmployerId} not found in CIDB master data.");

        if (!await referenceData.IsValidLocationAsync(
                model.ProvinceId!.Value, model.MetroDistrictId!.Value, model.LocalMunicipalityId, ct))
            throw new InvalidOperationException("Invalid province / metro-district / local-municipality combination.");

        if (!await referenceData.IsValidClassAndSubCategoryAsync(
                model.ClassOfConstructionWorksId!.Value, model.SubCategoryId!.Value, ct))
            throw new InvalidOperationException("Sub-category does not belong to the selected class of construction works.");

        if (!await referenceData.ContractTypeExistsAsync(model.TypeOfContractId!.Value, ct))
            throw new InvalidOperationException($"Contract type {model.TypeOfContractId} is not a valid option.");

        if (!await referenceData.TenderValueRangeExistsAsync(model.TenderValueRangeId!.Value, ct))
            throw new InvalidOperationException($"Tender value range {model.TenderValueRangeId} is not a valid option.");

        model.Status = TenderStatus.Draft;
        var id = await tenders.CreateAsync(model, ct);
        log.LogInformation("Tender {TenderId} created (draft)", id);
        return await tenders.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Tender {id} was created but could not be reloaded.");
    }
    public async Task<PublishResult> CreateAndPublishAsync(CreateTenderModel model, string correlationId, CancellationToken ct)
    {
        var created = await CreateAsync(model, ct);

        if (string.IsNullOrWhiteSpace(created.EmployerTenderNumber))
            throw new InvalidOperationException("Created tender has no employer tender number to publish.");

        var request = created.ToETenderRequest();
        var idempotencyKey = $"tender:{created.EmployerTenderNumber}:publish";

        var result = await etender.PublishTenderAsync(request, correlationId, idempotencyKey, ct);

        if (!result.Success)
            throw new InvalidOperationException($"e-Tender publish failed: {result.Error}");

        var now = DateTime.UtcNow;
        await tenders.MarkPublishedAsync(created.Id, now, ct);

        log.LogInformation(
            "Tender {TenderId} created and published to e-Tender. Correlation {CorrelationId}",
            created.Id, correlationId);

        return new PublishResult(0, "Published", now);
    }
    public async Task<TenderModel?> CancelAsync(Guid tenderId, CancelTenderRequest request, string correlationId, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(tenderId, ct);
        if (tender is null) return null;

        // BR-14: only eligible statuses can be cancelled.
        if (tender.StatusCodeId == StatusCancelled)
            return tender; // idempotent

        if (tender.StatusCodeId != StatusAdvertised)
            throw new InvalidOperationException($"Tender is in status {tender.StatusCodeId}; only Advertised tenders can be cancelled.");

        // BR-18: no cancellation after award. (Prevents the DB update below)
        if (tender.Awarded == true)
            throw new InvalidOperationException("Awarded tenders cannot be cancelled. Use contract termination instead.");

        // Look up the reason label for logging / notification
        var reasonLabel = await tenders.GetCancellationReasonLabelAsync(request.ReasonId!.Value, ct);
        if (reasonLabel is null)
            throw new InvalidOperationException($"Cancellation reason {request.ReasonId} not found in nv_tendercancelreason.");

        // GUESS: cancelled-by user is the current JWT subject.
        Guid? cancelledByUserId = null;

        // Persist to CRM
        await tenders.MarkCancelledAsync(
            tenderId,
            request.ReasonId!.Value,
            request.ReasonOther,
            request.DecisionDate!.Value,
            cancelledByUserId,
            ct);

        log.LogInformation(
            "Tender {TenderId} cancelled. Reason: {Reason}. Decision date: {DecisionDate:O}. Correlation {CorrelationId}",
            tenderId, reasonLabel, request.DecisionDate, correlationId);

        // Reload to return the updated state
        var updated = await tenders.GetByIdAsync(tenderId, ct);

        // FR-012 / BR-16: notify affected recipients.
        // TODO: Enable when notification service is ready.
        // if (updated is not null)
        // {
        //     try
        //     {
        //         await notification.NotifyCancellationAsync(updated, reasonLabel, correlationId, ct);
        //     }
        //     catch (Exception ex)
        //     {
        //         log.LogError(ex, "Notification failed after cancellation. Correlation {CorrelationId}", correlationId);
        //     }
        // }

        return updated;
    }
    private async Task<TenderModel> HydrateContactsAsync(TenderModel tender, CancellationToken ct)
    {
        tender.ContactPerson = await tenders.GetContactsByTenderIdAsync(tender.Id, ct);
        return tender;
    }
    public async Task<Guid> AwardAsync(Guid tenderId, AwardTenderRequest request, string correlationId, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(tenderId, ct);
        if (tender is null)
            throw new InvalidOperationException("Tender not found.");

        if (tender.StatusCodeId != StatusAdvertised)
            throw new InvalidOperationException($"Tender is in status {tender.StatusCodeId}. Only Advertised tenders can be awarded.");

        if (tender.Awarded == true)
            throw new InvalidOperationException("Tender has already been awarded.");

        // The repository handles creating the contract, the JV records, and flipping the tender.Awarded flag.
        var contractId = await contracts.CreateAwardAsync(tenderId, request, ct);

        log.LogInformation(
            "Tender {TenderId} awarded. Contract {ContractId} created. Correlation {CorrelationId}",
            tenderId, contractId, correlationId);

        return contractId;
    }
}