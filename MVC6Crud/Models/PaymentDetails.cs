namespace MVC6Crud.Models
{
    public class PaymentDetails
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string UserId { get; set; }

        public string Payment_Status { get; set; }
        public string MerchantId { get; set; }
        public string MerchantTransactionId { get; set; }

        public decimal Amount { get; set; }
        public string State { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }

    }
}
