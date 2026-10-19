namespace ITender.Integration.Api.TranferObjects
{
    public class TenderFilterRequest : PagedRequest
    {
        public int? StatusCode { get; set; }
        public Guid? EmployerId { get; set; }
        public string? TenderNumber { get; set; }
    }
}
