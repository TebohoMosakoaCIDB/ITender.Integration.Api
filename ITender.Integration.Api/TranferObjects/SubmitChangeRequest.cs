using System.ComponentModel.DataAnnotations;

namespace ITender.Integration.Api.TranferObjects
{
    public class SubmitChangeRequest
    {
        [Required]
        public string? Reason { get; set; }

        // The specific fields they want to change:
        public DateTime? NewClosingDateTime { get; set; }
        public string? NewTitle { get; set; }
        public string? NewDescription { get; set; }
    }
}
