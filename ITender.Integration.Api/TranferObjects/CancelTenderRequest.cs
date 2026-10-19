using System.ComponentModel.DataAnnotations;

namespace ITender.Integration.Api.TranferObjects
{
    public class CancelTenderRequest
    {
        /// <summary>Lookup to nv_tendercancelreason.</summary>
        [Required] public Guid? ReasonId { get; set; }

        /// <summary>Free text; required when ReasonId indicates "Other".</summary>
        public string? ReasonOther { get; set; }

        /// <summary>BR-15: business decision date (separate from system transaction date).</summary>
        [Required] public DateTime? DecisionDate { get; set; }
    }
}
