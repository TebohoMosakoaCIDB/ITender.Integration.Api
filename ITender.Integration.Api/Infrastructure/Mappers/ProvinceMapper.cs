using ITender.Integration.Api.Models;
using Microsoft.Xrm.Sdk;

namespace ITender.Integration.Api.Infrastructure.Mappers
{
    public static class ProvinceMapper
    {
        public const string EntityName = "nv_province";

        public static class Fields
        {
            public const string Id = "nv_provinceid";
            public const string Name = "nv_name";
        }

        public static ProvinceModel ToDomain(Entity e)
        {
            if (e == null) return null!;

            return new ProvinceModel
            {
                Id = e.Id,
                Name = e.GetAttributeValue<string>(Fields.Name),
            };
        }

        public static Entity ToEntity(ProvinceModel m)
        {
            var e = new Entity(EntityName);

            if (m.Id != Guid.Empty)
                e.Id = m.Id;
            e[Fields.Name] = m.Name;

            return e;
        }
    }
}
