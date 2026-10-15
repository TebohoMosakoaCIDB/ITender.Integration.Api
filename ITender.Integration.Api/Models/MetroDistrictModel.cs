namespace ITender.Integration.Api.Models
{
    public class MetroDistrictModel
    {
        public Guid Id { get; set; }
        public Guid? ProvinceId { get; set; }
        public int? StateCode { get; set; }
        public string? Name { get; set; }
    }
}
