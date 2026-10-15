using ITender.Integration.Api.Models;
using Microsoft.Xrm.Sdk;

namespace ITender.Integration.Api.Infrastructure.Mappers
{
    public static class MetroDistrictMapper
    {
        public const string EntityName = "nv_metrodistrict";

        public static class Fields
        {
            public const string Id = "nv_metrodistrictid";
            public const string ProvinceId = "nv_provinceid";
            public const string StateCode = "statecode";
            public const string Name = "nv_name";
        }

        public static MetroDistrictModel ToDomain(Entity e)
        {
            if (e == null) return null!;

            return new MetroDistrictModel
            {
                Id = e.Id,
                ProvinceId = e.GetAttributeValue<EntityReference>(Fields.ProvinceId)?.Id,
                StateCode = e.GetAttributeValue<OptionSetValue>(Fields.StateCode)?.Value,
                Name = e.GetAttributeValue<string>(Fields.Name)
            };
        }

        public static Entity ToEntity(MetroDistrictModel m)
        {
            var e = new Entity(EntityName);

            if (m.Id != Guid.Empty)
                e.Id = m.Id;

            if (m.ProvinceId.HasValue)
                e[Fields.ProvinceId] = new EntityReference(Fields.ProvinceId, m.ProvinceId.Value);

            if (m.StateCode.HasValue)
                e[Fields.StateCode] = new OptionSetValue(m.StateCode.Value);

            e[Fields.Name] = m.Name;

            return e;
        }
    }
}
