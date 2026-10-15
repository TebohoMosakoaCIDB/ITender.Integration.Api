using ITender.Integration.Api.Models;
using Microsoft.Xrm.Sdk;

namespace ITender.Integration.Api.Infrastructure.Mappers
{
    public static class ClassOfWorkTypeSubMapper
    {
        public const string EntityName = "nv_classofworktypesubcategory";

        public static class Fields
        {
            public const string Id = "nv_classofworktypesubcategoryid";
            public const string ClassOfWorkTypeId = "nv_classofworktypeid";
            public const string Name = "nv_name";
            public const string Description = "nv_description";
        }

        public static ClassOfWorkTypeSubModel ToDomain(Entity e)
        {
            if (e == null) return null;

            return new ClassOfWorkTypeSubModel
            {
                Id = e.Id,
                ClassOfWorkTypeId = e.GetAttributeValue<EntityReference>(Fields.ClassOfWorkTypeId)?.Id,
                Name = e.GetAttributeValue<string>(Fields.Name)
            };
        }

        public static Entity ToEntity(ClassOfWorkTypeSubModel m)
        {
            var e = new Entity(EntityName);

            if (m.Id != Guid.Empty)
                e.Id = m.Id;
            if (m.ClassOfWorkTypeId.HasValue)
                e[Fields.ClassOfWorkTypeId] = new EntityReference(Fields.ClassOfWorkTypeId, m.ClassOfWorkTypeId.Value);
            e[Fields.Name] = m.Name;

            return e;
        }
    }
}
