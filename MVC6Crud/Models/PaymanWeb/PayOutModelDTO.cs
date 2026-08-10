using System.ComponentModel.DataAnnotations;

namespace MVC6Crud.Models.PaymanWeb
{
    public class PayOutModelDTO
    {
        [Required]
        public string OrderRefId { get; set; }

        [Required]
        public string AccountHolderName { get; set; }

        [Required]
        public string Bank { get; set; }

        [Required]
        public string AccountNumber { get; set; }

        [Required]
        public string UserPhone { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
