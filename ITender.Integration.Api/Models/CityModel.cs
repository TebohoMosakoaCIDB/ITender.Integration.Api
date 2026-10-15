namespace ITender.Integration.Api.Models
{
    public class CityModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public Guid? ProvinceId { get; set; }
    }
}
