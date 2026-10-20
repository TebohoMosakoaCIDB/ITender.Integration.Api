using System.ComponentModel.DataAnnotations;

namespace ITender.Integration.Api.TranferObjects
{
    public class PracticalCompletionRequest
    {
        [Required]
        public DateTime? CompletionDate { get; set; }

        public decimal? VariationsValue { get; set; }
        public decimal? AdjustmentsValue { get; set; }

        [Required]
        public decimal? EstimatedFinalValueInclVat { get; set; }

        public bool DisputesReferred { get; set; }
    }
}
