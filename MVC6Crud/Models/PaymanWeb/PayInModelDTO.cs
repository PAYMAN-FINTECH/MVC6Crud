using System.ComponentModel.DataAnnotations;

namespace MVC6Crud.Models.PaymanWeb
{
    public class PayInModelDTO
    {
        [Required]
        public string OrderRefId { get; set; }

        [Required]
        public string CardNumber { get; set; }

        [Required]
        public string UserPhone { get; set; }

        [Required]
        public string GatewayId { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public string CreatedBy { get; set; }
        public string card { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public class RazorpaySessionModel
    {
        public decimal Amount { get; set; }

        public string Mobile { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }

        // Mask or last 4 digits only (PCI safe)
        public string Card { get; set; }

        public string logonphone {  get; set; }
    }

}
