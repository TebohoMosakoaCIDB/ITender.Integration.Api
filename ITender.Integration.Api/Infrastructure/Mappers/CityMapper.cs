using ITender.Integration.Api.Models;
using Microsoft.Xrm.Sdk;

namespace ITender.Integration.Api.Infrastructure.Mappers
{
    public static class CityMapper
    {
        public const string EntityName = "nv_city";

        public static class Fields
        {
            public const string Id = "nv_cityid";
            public const string Name = "nv_name";
            public const string ProvinceId = "nv_provinceid";
        }

        public static Entity ToEntity(CityModel domain)
        {
            var entity = new Entity(EntityName);

            entity[Fields.Id] = domain.Id;

            if (!string.IsNullOrEmpty(domain.Name))
                entity[Fields.Name] = domain.Name;

            if (domain.ProvinceId.HasValue)
                entity[Fields.ProvinceId] = domain.ProvinceId.Value;

            return entity;
        }

        public static CityModel? ToDomain(Entity entity)
        {
            if (entity == null) return null;

            return new CityModel
            {
                Id = entity.Id,
                Name = entity.GetAttributeValue<string>(Fields.Name),
                ProvinceId = entity.GetAttributeValue<EntityReference>(Fields.ProvinceId)?.Id
            };
        }
    }
}
