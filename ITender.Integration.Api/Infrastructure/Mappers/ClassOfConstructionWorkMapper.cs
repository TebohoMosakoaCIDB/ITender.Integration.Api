using ITender.Integration.Api.Models;
using Microsoft.Xrm.Sdk;

namespace ITender.Integration.Api.Infrastructure.Mappers
{
    public static class ClassOfConstructionWorkMapper
    {
        public const string EntityName = "nv_classofworktype";

        public static class Fields
        {
            public const string Id = "nv_classofworktypeid";
            public const string Name = "nv_name";
            public const string Description = "nv_description";
        }

        public static Entity ToEntity(ClassOfConstructionWorkModel domain)
        {
            var entity = new Entity(EntityName);

            if (!string.IsNullOrEmpty(domain.Name))
                entity[Fields.Name] = domain.Name;

            if (!string.IsNullOrEmpty(domain.Description))
                entity[Fields.Description] = domain.Description;

            return entity;
        }

        public static ClassOfConstructionWorkModel? ToDomain(Entity entity)
        {
            if (entity == null) return null;

            return new ClassOfConstructionWorkModel
            {
                Id = entity.Id,

                Name = entity.GetAttributeValue<string>("nv_name"),
                Description = entity.GetAttributeValue<string>("nv_description"),
            };
        }
    }
}
