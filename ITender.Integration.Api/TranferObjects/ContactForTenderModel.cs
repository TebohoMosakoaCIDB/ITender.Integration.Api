using System.ComponentModel.DataAnnotations;

namespace ITender.Integration.Api.TranferObjects
{
    public class ContactForTenderModel
    {
        [Required]
        public string PersonToQuery { get; set; }
        [Required]
        public string MobilePhoneNumber { get; set; }
        public string TelephoneNumber { get; set; }
        public string FaxNumber { get; set; }
        public string Email { get; set; }
    }
}
