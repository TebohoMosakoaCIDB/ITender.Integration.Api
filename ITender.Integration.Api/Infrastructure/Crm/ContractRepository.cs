using ITender.Integration.Api.Infrastructure.Mappers;
using ITender.Integration.Api.TranferObjects;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace ITender.Integration.Api.Infrastructure.Crm;

public interface IContractRepository
{
    Task<Guid> CreateAwardAsync(Guid tenderId, AwardTenderRequest request, CancellationToken ct);
    Task CompleteContractAsync(Guid contractId, PracticalCompletionRequest request, CancellationToken ct);
    Task TerminateContractAsync(Guid contractId, TerminateContractRequest request, CancellationToken ct);
}

public sealed class ContractRepository(ICrmServiceFactory crmFactory) : IContractRepository
{
    private readonly IOrganizationServiceAsync2 _crm = crmFactory.Service;

    public async Task<Guid> CreateAwardAsync(Guid tenderId, AwardTenderRequest request, CancellationToken ct)
    {
        // 1. PREVENT DUPLICATE AWARDS
        var checkQuery = new QueryExpression(ContractSchema.EntityLogicalName)
        {
            ColumnSet = new ColumnSet(ContractSchema.Id),
            TopCount = 1
        };
        checkQuery.Criteria.AddCondition(ContractSchema.TenderId, ConditionOperator.Equal, tenderId);

        var existingContracts = await _crm.RetrieveMultipleAsync(checkQuery, ct);
        if (existingContracts.Entities.Count > 0)
        {
            throw new InvalidOperationException("This Tender has already been awarded a Contract.");
        }

        // 1. Fetch the Tender FIRST (so we can copy the Employer)
        var tender = await _crm.RetrieveAsync(TenderSchema.EntityLogicalName, tenderId, new ColumnSet(TenderSchema.EmployerId), ct);
        var employerRef = tender.GetAttributeValue<EntityReference>(TenderSchema.EmployerId);

        // 2. Create the main Contract record
        var contract = new Entity(ContractSchema.EntityLogicalName);

        contract[ContractSchema.TenderId] = new EntityReference(TenderSchema.EntityLogicalName, tenderId);
        contract[ContractSchema.Title] = request.ContractTitle;

        if (!string.IsNullOrWhiteSpace(request.Description))
            contract[ContractSchema.Description] = request.Description;

        if (request.TypeOfContractId.HasValue)
            contract[ContractSchema.TypeOfContract] = new OptionSetValue(request.TypeOfContractId.Value);

        // Copy the Employer from the Tender
        if (employerRef != null)
        {
            contract[ContractSchema.EmployerId] = employerRef;
        }

        // Location
        if (request.ProvinceId.HasValue)
            contract[ContractSchema.ProvinceId] = new EntityReference("nv_province", request.ProvinceId.Value);

        if (request.MetroDistrictId.HasValue)
            contract[ContractSchema.MetroDistrictId] = new EntityReference("nv_metrodistrict", request.MetroDistrictId.Value);

        if (request.LocalMunicipalityId.HasValue)
            contract[ContractSchema.LocalMunicipalityId] = new EntityReference("nv_municipality", request.LocalMunicipalityId.Value);

        // Dates & Value (Make sure ContractSchema casing matches Dataverse EXACTLY)
        contract[ContractSchema.AwardDate] = request.AwardDate;
        contract[ContractSchema.EndDate] = request.EndDate;
        contract[ContractSchema.TotalValueInclVat] = new Money(request.TotalContractValueInclVat!.Value);

        if (request.ClassOfConstructionWorksId.HasValue)
            contract[ContractSchema.ClassOfConstructionWorksId] = new EntityReference("nv_classofconstructionworks", request.ClassOfConstructionWorksId.Value);
        if (request.TenderValueRangeId.HasValue)
            contract[ContractSchema.TenderValueRange] = new OptionSetValue(request.TenderValueRangeId.Value);

        // Calculate JV status based on the payload
        contract[ContractSchema.IsAJV] = request.Contractors.Count > 1;

        // If NumberOfContractors is an integer field (not a choice), set it:
        contract[ContractSchema.NumberOfContractors] = request.Contractors.Count;

        if (request.FinancialPoints.HasValue) contract[ContractSchema.FinancialPoints] = request.FinancialPoints.Value;
        if (request.PreferencePoints.HasValue) contract[ContractSchema.PreferencePoints] = request.PreferencePoints.Value;

        if (request.FormOfContractJBCC) contract[ContractSchema.FormJBCC] = true;
        if (request.FormOfContractGCC) contract[ContractSchema.FormGCC] = true;
        if (request.FormOfContractNEC) contract[ContractSchema.FormNEC] = true;
        if (request.FormOfContractFIDIC) contract[ContractSchema.FormFIDIC] = true;

        // Save the Contract
        var contractId = await _crm.CreateAsync(contract, ct);

        // 3. Create the Contractor(s) / JV records
        foreach (var c in request.Contractors)
        {
            var jv = new Entity(ContractSchema.ContractorEntity);
            jv[ContractSchema.ContractorContractId] = new EntityReference(ContractSchema.EntityLogicalName, contractId);
            jv[ContractSchema.ContractorAccountId] = new EntityReference("account", c.ContractorId!.Value);
            jv[ContractSchema.ContractorJvShare] = c.JvSharePercentage;
            jv[ContractSchema.ContractorIsLead] = c.IsLeadContractor;

            if (!string.IsNullOrWhiteSpace(c.EnterpriseName)) jv[ContractSchema.ContractorEnterpriseName] = c.EnterpriseName;
            if (!string.IsNullOrWhiteSpace(c.RegistrationNumber)) jv[ContractSchema.ContractorRegistrationNumber] = c.RegistrationNumber;

            await _crm.CreateAsync(jv, ct);
        }

        // 4. Update Tender to mark it as Awarded (nv_awarded = true)
        var tenderUpdate = new Entity(TenderSchema.EntityLogicalName, tenderId)
        {
            [TenderSchema.Awarded] = true
        };
        await _crm.UpdateAsync(tenderUpdate, ct);

        return contractId;
    }

    public async Task CompleteContractAsync(Guid contractId, PracticalCompletionRequest request, CancellationToken ct)
    {
        var contract = new Entity(ContractSchema.EntityLogicalName, contractId)
        {
            [ContractSchema.PracticalCompletionDate] = request.CompletionDate,
            [ContractSchema.EstimatedFinalValueInclVat] = new Money(request.EstimatedFinalValueInclVat!.Value),

            // Assuming 100000002 is the Status Reason for "Completed"
            [ContractSchema.StatusCode] = new OptionSetValue(100000002)
        };

        if (request.VariationsValue.HasValue)
            contract[ContractSchema.EstimatedValueOfVariations] = new Money(request.VariationsValue.Value);

        if (request.AdjustmentsValue.HasValue)
            contract[ContractSchema.EstimatedValueOfAdjustment] = new Money(request.AdjustmentsValue.Value);

        // Set dispute resolution (assuming 100000000 = Yes, 100000001 = No)
        contract[ContractSchema.DisputesReferredToResolution] = new OptionSetValue(request.DisputesReferred ? 100000000 : 100000001);

        await _crm.UpdateAsync(contract, ct);
    }

    //Check with compliance which status to take
    public async Task TerminateContractAsync(Guid contractId, TerminateContractRequest request, CancellationToken ct)
    {
        var contract = new Entity(ContractSchema.EntityLogicalName, contractId)
        {
            [ContractSchema.TerminationDate] = request.TerminationDate,
            [ContractSchema.TerminationReason] = new OptionSetValue(request.ReasonId!.Value),
            //[ContractSchema.StatusCode] = new OptionSetValue(ContractSchema.StatusTerminated)
        };

        if (!string.IsNullOrWhiteSpace(request.OtherReason))
        {
            contract[ContractSchema.OtherTerminationReason] = request.OtherReason;
        }

        await _crm.UpdateAsync(contract, ct);
    }
}