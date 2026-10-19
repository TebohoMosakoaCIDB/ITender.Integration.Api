using ITender.Integration.Api.Infrastructure.Crm;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;

namespace ITender.Integration.Api.Validation;

public sealed class ReferenceDataService(ICrmServiceFactory crmFactory) : IReferenceDataService
{
    private readonly IOrganizationServiceAsync2 _crm = crmFactory.Service;

    private const string AccountEntity = "account";
    private const string ProvinceEntity = "nv_province";
    private const string MetroDistrictEntity = "nv_metrodistrict";
    private const string LocalMunicipalityEntity = "nv_localmunicipality";
    private const string SubCategoryEntity = "nv_classofconstructionworkssubcategory";

    public async Task<bool> EmployerExistsAsync(Guid employerId, CancellationToken ct)
    {
        var q = new QueryExpression(AccountEntity)
        {
            ColumnSet = new ColumnSet("name"),
            TopCount = 1,
            NoLock = true
        };
        q.Criteria.AddCondition("accountid", ConditionOperator.Equal, employerId);

        var result = await _crm.RetrieveMultipleAsync(q, ct);
        return result.Entities.Count > 0;
    }

    public Task<bool> ContractTypeExistsAsync(int contractTypeId, CancellationToken ct)
    {
        return Task.FromResult(Configuration.TenderOptionSets.TypeOfContract.All.Contains(contractTypeId));
    }

    public Task<bool> TenderValueRangeExistsAsync(int valueRangeId, CancellationToken ct)
    {
        return Task.FromResult(Configuration.TenderOptionSets.TenderValueRange.All.Contains(valueRangeId));
    }

    public async Task<bool> IsValidLocationAsync(
        Guid provinceId, Guid metroDistrictId, Guid? localMunicipalityId, CancellationToken ct)
    {
        // Metro must belong to the province
        var metroQ = new QueryExpression(MetroDistrictEntity)
        {
            ColumnSet = new ColumnSet("nv_metrodistrictid"),
            TopCount = 1,
            NoLock = true
        };
        metroQ.Criteria.AddCondition("nv_metrodistrictid", ConditionOperator.Equal, metroDistrictId);
        metroQ.Criteria.AddCondition("nv_provinceid", ConditionOperator.Equal, provinceId);

        var metro = await _crm.RetrieveMultipleAsync(metroQ, ct);
        if (metro.Entities.Count == 0) return false;

        // Local municipality (if provided) must belong to the district
        if (localMunicipalityId.HasValue)
        {
            var localQ = new QueryExpression(LocalMunicipalityEntity)
            {
                ColumnSet = new ColumnSet("nv_localmunicipalityid"),
                TopCount = 1,
                NoLock = true
            };
            localQ.Criteria.AddCondition("nv_localmunicipalityid", ConditionOperator.Equal, localMunicipalityId.Value);
            localQ.Criteria.AddCondition("nv_metrodistrictid", ConditionOperator.Equal, metroDistrictId);

            var local = await _crm.RetrieveMultipleAsync(localQ, ct);
            if (local.Entities.Count == 0) return false;
        }

        return true;
    }

    public async Task<bool> IsValidClassAndSubCategoryAsync(
        Guid classId, Guid subCategoryId, CancellationToken ct)
    {
        var q = new QueryExpression(SubCategoryEntity)
        {
            ColumnSet = new ColumnSet("nv_classofconstructionworkssubcategoryid"),
            TopCount = 1,
            NoLock = true
        };
        q.Criteria.AddCondition("nv_classofconstructionworkssubcategoryid", ConditionOperator.Equal, subCategoryId);
        q.Criteria.AddCondition("nv_classofconstructionworksid", ConditionOperator.Equal, classId);

        var result = await _crm.RetrieveMultipleAsync(q, ct);
        return result.Entities.Count > 0;
    }
}