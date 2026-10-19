using ITender.Integration.Api.Constants;
using ITender.Integration.Api.Infrastructure.Crm;
using ITender.Integration.Api.Infrastructure.Mappers;
using ITender.Integration.Api.Models;
using ITender.Integration.Api.TranferObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.Text.RegularExpressions;

namespace ITender.Integration.Api.Services
{
    public interface ILookupService
    {
        #region Metro District

        Task<PagedResult<MetroDistrictModel>> GetAllMetroDistrictsAsync(
            PagedRequest request,
            CancellationToken ct = default);

        Task<MetroDistrictModel?> GetMetroDistrictByIdAsync(
            Guid id,
            CancellationToken ct = default);

        Task<PagedResult<MetroDistrictModel>> GetMetroDistrictsByProvinceIdAsync(
            Guid provinceId,
            PagedRequest request,
            CancellationToken ct = default);
        #endregion

        #region Province
        Task<ProvinceModel?> GetProvinceByIdAsync(Guid provinceId, CancellationToken ct = default);
        Task<PagedResult<ProvinceModel>> GetAllProvinces(PagedRequest request, CancellationToken ct = default);
        #endregion

        #region Type Of Contract
        Task<TypeOfContractModel?> GetTypeOfContractById(int id, CancellationToken ct = default);
        Task<List<TypeOfContractModel>> GetAllTypeOfContracts(CancellationToken ct = default);
        #endregion

        #region City

        Task<PagedResult<CityModel>> GetAllCitiesAsync(
            PagedRequest request,
            CancellationToken ct = default);

        Task<CityModel?> GetCityByIdAsync(
            Guid id,
            CancellationToken ct = default);

        Task<PagedResult<CityModel>> GetCitiesByProvinceIdAsync(
            Guid provinceId,
            PagedRequest request,
            CancellationToken ct = default);

        #endregion

        #region Class Of Construction Work

        Task<PagedResult<ClassOfConstructionWorkModel>> GetAllClassOfConstructionWorksAsync(
            PagedRequest request,
            CancellationToken ct = default);

        Task<ClassOfConstructionWorkModel?> GetClassOfConstructionWorkByIdAsync(
            Guid id,
            CancellationToken ct = default);

        Task<ClassOfConstructionWorkModel?> GetClassOfConstructionWorkByNameAsync(
            string name,
            string[]? columns = null,
            CancellationToken ct = default);

        #endregion

        #region Class Of Work Type Sub Category

        Task<PagedResult<ClassOfWorkTypeSubModel>> GetAllClassOfWorkTypeSubCategoriesAsync(
            PagedRequest request,
            CancellationToken ct = default);

        Task<ClassOfWorkTypeSubModel?> GetClassOfWorkTypeSubCategoryByIdAsync(
            Guid id,
            CancellationToken ct = default);

        Task<PagedResult<ClassOfWorkTypeSubModel>> GetClassOfWorkTypeSubCategoriesByWorkTypeIdAsync(
            Guid classOfWorkTypeId,
            PagedRequest request,
            string[]? columns = null,
            CancellationToken ct = default);

        #endregion

        #region Tender Value Range
        Task<TenderValueRangeModel?> GetTenderValueRangeById(int id, CancellationToken ct = default);
        Task<List<TenderValueRangeModel>> GetAllTenderValueRange(CancellationToken ct = default);
        Task<int?> ResolveApprovedGradeFromTenderRangeAsync(int tenderRangeId, CancellationToken ct = default);
        #endregion
    }
    /// <summary>
    /// Read-only master-data lookups.
    ///  - Table-backed lookups (province, district, city, class of works, sub-category): list queries return ACTIVE records only,
    ///    ordered by name; by-id queries are not filtered by state.
    ///  - Option-set lookups (type of contract, tender value range): labels are read from Dataverse metadata and cached for an hour.
    /// </summary>
    public sealed class LookupService(ICrmServiceFactory crmFactory, IMemoryCache cache) : ILookupService
    {
        private readonly IOrganizationServiceAsync2 crm = crmFactory.Service;
        private static readonly TimeSpan OptionSetCacheTime = TimeSpan.FromHours(1);
        private const int MaxPageSize = 500;

        // ---- default column sets (taken from the mappers) ----
        private static readonly string[] ProvinceColumns = { ProvinceMapper.Fields.Name };
        private static readonly string[] MetroColumns =
            { MetroDistrictMapper.Fields.Name, MetroDistrictMapper.Fields.ProvinceId, MetroDistrictMapper.Fields.StateCode };
        private static readonly string[] CityColumns = { CityMapper.Fields.Name, CityMapper.Fields.ProvinceId };
        private static readonly string[] ClassColumns =
            { ClassOfConstructionWorkMapper.Fields.Name, ClassOfConstructionWorkMapper.Fields.Description };
        private static readonly string[] SubCategoryColumns =
            { ClassOfWorkTypeSubMapper.Fields.Name, ClassOfWorkTypeSubMapper.Fields.ClassOfWorkTypeId };

        // =========================================================== Metro District
        public Task<PagedResult<MetroDistrictModel>> GetAllMetroDistrictsAsync(PagedRequest request, CancellationToken ct = default)
            => PageAsync(ActiveQuery(CrmEntityNames.MetroDistrict, MetroColumns, MetroDistrictMapper.Fields.Name),
                         request, MetroDistrictMapper.ToDomain, ct);

        public Task<MetroDistrictModel?> GetMetroDistrictByIdAsync(Guid id, CancellationToken ct = default)
            => GetByIdAsync(CrmEntityNames.MetroDistrict, MetroDistrictMapper.Fields.Id, MetroColumns, id, MetroDistrictMapper.ToDomain, ct);

        public Task<PagedResult<MetroDistrictModel>> GetMetroDistrictsByProvinceIdAsync(Guid provinceId, PagedRequest request, CancellationToken ct = default)
        {
            var query = ActiveQuery(CrmEntityNames.MetroDistrict, MetroColumns, MetroDistrictMapper.Fields.Name);
            query.Criteria.AddCondition(MetroDistrictMapper.Fields.ProvinceId, ConditionOperator.Equal, provinceId);
            return PageAsync(query, request, MetroDistrictMapper.ToDomain, ct);
        }

        // =========================================================== Province
        public Task<ProvinceModel?> GetProvinceByIdAsync(Guid provinceId, CancellationToken ct = default)
            => GetByIdAsync(CrmEntityNames.Province, ProvinceMapper.Fields.Id, ProvinceColumns, provinceId, ProvinceMapper.ToDomain, ct);

        public Task<PagedResult<ProvinceModel>> GetAllProvinces(PagedRequest request, CancellationToken ct = default)
            => PageAsync(ActiveQuery(CrmEntityNames.Province, ProvinceColumns, ProvinceMapper.Fields.Name),
                         request, ProvinceMapper.ToDomain, ct);

        // =========================================================== Type of contract (option set)
        public async Task<TypeOfContractModel?> GetTypeOfContractById(int id, CancellationToken ct = default)
            => (await GetAllTypeOfContracts(ct)).FirstOrDefault(x => x.Id == id);

        public async Task<List<TypeOfContractModel>> GetAllTypeOfContracts(CancellationToken ct = default)
            => (await OptionsAsync(CrmEntityNames.Tender, CrmEntityNames.TypeOfContract, ct))
                .Select(o => new TypeOfContractModel { Id = o.Value, Name = o.Label })
                .ToList();

        // =========================================================== City
        public Task<PagedResult<CityModel>> GetAllCitiesAsync(PagedRequest request, CancellationToken ct = default)
            => PageAsync(ActiveQuery(CrmEntityNames.City, CityColumns, CityMapper.Fields.Name),
                         request, CityMapper.ToDomain, ct);

        public Task<CityModel?> GetCityByIdAsync(Guid id, CancellationToken ct = default)
            => GetByIdAsync(CrmEntityNames.City, CityMapper.Fields.Id, CityColumns, id, CityMapper.ToDomain, ct);

        public Task<PagedResult<CityModel>> GetCitiesByProvinceIdAsync(Guid provinceId, PagedRequest request, CancellationToken ct = default)
        {
            var query = ActiveQuery(CrmEntityNames.City, CityColumns, CityMapper.Fields.Name);
            query.Criteria.AddCondition(CityMapper.Fields.ProvinceId, ConditionOperator.Equal, provinceId);
            return PageAsync(query, request, CityMapper.ToDomain, ct);
        }

        // =========================================================== Class of construction work (nv_classofworktype)
        public Task<PagedResult<ClassOfConstructionWorkModel>> GetAllClassOfConstructionWorksAsync(PagedRequest request, CancellationToken ct = default)
            => PageAsync(ActiveQuery(CrmEntityNames.ClassOfWorkType, ClassColumns, ClassOfConstructionWorkMapper.Fields.Name),
                         request, ClassOfConstructionWorkMapper.ToDomain, ct);

        public Task<ClassOfConstructionWorkModel?> GetClassOfConstructionWorkByIdAsync(Guid id, CancellationToken ct = default)
            => GetByIdAsync(CrmEntityNames.ClassOfWorkType, ClassOfConstructionWorkMapper.Fields.Id, ClassColumns, id,
                            ClassOfConstructionWorkMapper.ToDomain, ct);

        public async Task<ClassOfConstructionWorkModel?> GetClassOfConstructionWorkByNameAsync(
            string name, string[]? columns = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var query = ActiveQuery(CrmEntityNames.ClassOfWorkType, columns is { Length: > 0 } ? columns : ClassColumns,
                                    ClassOfConstructionWorkMapper.Fields.Name);
            query.TopCount = 1;
            query.Criteria.AddCondition(ClassOfConstructionWorkMapper.Fields.Name, ConditionOperator.Equal, name.Trim());

            var entity = (await crm.RetrieveMultipleAsync(query, ct)).Entities.FirstOrDefault();
            return entity is null ? null : ClassOfConstructionWorkMapper.ToDomain(entity);
        }

        // =========================================================== Class of work type sub-category
        public Task<PagedResult<ClassOfWorkTypeSubModel>> GetAllClassOfWorkTypeSubCategoriesAsync(PagedRequest request, CancellationToken ct = default)
            => PageAsync(ActiveQuery(CrmEntityNames.ClassOfWorkTypeSub, SubCategoryColumns, ClassOfWorkTypeSubMapper.Fields.Name),
                         request, ClassOfWorkTypeSubMapper.ToDomain, ct);

        public Task<ClassOfWorkTypeSubModel?> GetClassOfWorkTypeSubCategoryByIdAsync(Guid id, CancellationToken ct = default)
            => GetByIdAsync(CrmEntityNames.ClassOfWorkTypeSub, ClassOfWorkTypeSubMapper.Fields.Id, SubCategoryColumns, id,
                            ClassOfWorkTypeSubMapper.ToDomain, ct);

        public Task<PagedResult<ClassOfWorkTypeSubModel>> GetClassOfWorkTypeSubCategoriesByWorkTypeIdAsync(
            Guid classOfWorkTypeId, PagedRequest request, string[]? columns = null, CancellationToken ct = default)
        {
            var query = ActiveQuery(CrmEntityNames.ClassOfWorkTypeSub, columns is { Length: > 0 } ? columns : SubCategoryColumns,
                                    ClassOfWorkTypeSubMapper.Fields.Name);
            query.Criteria.AddCondition(ClassOfWorkTypeSubMapper.Fields.ClassOfWorkTypeId, ConditionOperator.Equal, classOfWorkTypeId);
            return PageAsync(query, request, ClassOfWorkTypeSubMapper.ToDomain, ct);
        }

        // =========================================================== Tender value range (option set)
        public async Task<TenderValueRangeModel?> GetTenderValueRangeById(int id, CancellationToken ct = default)
            => (await GetAllTenderValueRange(ct)).FirstOrDefault(x => x.Id == id);

        public async Task<List<TenderValueRangeModel>> GetAllTenderValueRange(CancellationToken ct = default)
            => (await OptionsAsync(CrmEntityNames.Tender, CrmEntityNames.TenderValueRange, ct))
                .Select(o => new TenderValueRangeModel { Id = o.Value, Name = o.Label })
                .ToList();

        /// <summary>
        /// The grade is the number in brackets at the start of the range label, e.g. "(9) R 200,000,000 – No Limit" -> 9.
        /// </summary>
        public async Task<int?> ResolveApprovedGradeFromTenderRangeAsync(int tenderRangeId, CancellationToken ct = default)
        {
            var range = await GetTenderValueRangeById(tenderRangeId, ct);
            if (range is null) return null;

            var match = Regex.Match(range.Name ?? "", @"^\s*\((\d+)\)");
            return match.Success ? int.Parse(match.Groups[1].Value) : null;
        }

        // ===================================================================== helpers

        private static QueryExpression ActiveQuery(string entity, string[] columns, string orderBy)
        {
            var query = new QueryExpression(entity)
            {
                ColumnSet = new ColumnSet(columns),
                NoLock = true
            };
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            query.AddOrder(orderBy, OrderType.Ascending);   // stable order is required for page-number paging
            return query;
        }

        private async Task<T?> GetByIdAsync<T>(
            string entity, string idField, string[] columns, Guid id, Func<Entity, T?> map, CancellationToken ct) where T : class
        {
            var query = new QueryExpression(entity)
            {
                ColumnSet = new ColumnSet(columns),
                TopCount = 1,
                NoLock = true
            };
            query.Criteria.AddCondition(idField, ConditionOperator.Equal, id);

            var found = (await crm.RetrieveMultipleAsync(query, ct)).Entities.FirstOrDefault();
            return found is null ? null : map(found);
        }

        private async Task<PagedResult<T>> PageAsync<T>(
            QueryExpression query, PagedRequest request, Func<Entity, T?> map, CancellationToken ct) where T : class
        {
            var (page, size) = Paging(request);
            query.PageInfo = new PagingInfo { PageNumber = page, Count = size, ReturnTotalRecordCount = true };

            var result = await crm.RetrieveMultipleAsync(query, ct);
            var items = result.Entities.Select(map).OfType<T>().ToList();

            // TotalRecordCount is -1 when unavailable and is capped by Dataverse at 5000
            var total = result.TotalRecordCount >= 0 ? result.TotalRecordCount : items.Count;
            return BuildPage(items, total, page, size);
        }

        // --- The ONLY two places that touch PagedRequest / PagedResult members. Adjust here if your types differ. ---
        private static (int Page, int Size) Paging(PagedRequest r)
            => (Math.Max(1, r.PageNumber), Math.Clamp(r.PageSize, 1, MaxPageSize));

        private static PagedResult<T> BuildPage<T>(List<T> items, int total, int page, int size)
            => new() { Items = items, TotalCount = total, PageNumber = page, PageSize = size };
        // ---------------------------------------------------------------------------------------------------------

        /// <summary>Option-set (picklist) labels from Dataverse metadata, cached.</summary>
        private async Task<IReadOnlyList<(int Value, string Label)>> OptionsAsync(string entity, string attribute, CancellationToken ct)
        {
            var key = $"optionset:{entity}:{attribute}";
            if (cache.TryGetValue(key, out IReadOnlyList<(int Value, string Label)>? cached) && cached is not null)
                return cached;

            var response = (RetrieveAttributeResponse)await crm.ExecuteAsync(new RetrieveAttributeRequest
            {
                EntityLogicalName = entity,
                LogicalName = attribute,
                RetrieveAsIfPublished = true
            }, ct);

            if (response.AttributeMetadata is not EnumAttributeMetadata meta || meta.OptionSet is null)
                throw new InvalidOperationException($"'{entity}.{attribute}' is not an option-set column.");

            var options = meta.OptionSet.Options
                .Where(o => o.Value.HasValue)
                .Select(o => (
                    Value: o.Value!.Value,
                    Label: o.Label?.UserLocalizedLabel?.Label
                           ?? o.Label?.LocalizedLabels.FirstOrDefault()?.Label
                           ?? o.Value.Value.ToString()))
                .ToList();

            cache.Set(key, (IReadOnlyList<(int Value, string Label)>)options, OptionSetCacheTime);
            return options;
        }
    }
}
