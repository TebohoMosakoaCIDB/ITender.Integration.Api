using System.ComponentModel.DataAnnotations;

namespace ITender.Integration.Api.TranferObjects
{
    public class ContactForTenderModel
    {
        [Required] public string PersonToQuery { get; set; } = "";
        [Required] public string TelephoneNumber { get; set; } = "";
        [Required] public string Email { get; set; } = "";
        public string? MobilePhoneNumber { get; set; }
        //public string? FaxNumber { get; set; }
    }
}
