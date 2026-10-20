using System.ComponentModel.DataAnnotations;

namespace ITender.Integration.Api.TranferObjects
{
    public class TerminateContractRequest
    {
        [Required]
        public DateTime? TerminationDate { get; set; }

        [Required]
        public int? ReasonId { get; set; } // Changed to int!

        public string? OtherReason { get; set; }
    }
}
