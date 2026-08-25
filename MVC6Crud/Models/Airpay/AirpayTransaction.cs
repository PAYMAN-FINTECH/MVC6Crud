namespace MVC6Crud.Models.Airpay
{
    public class AirpayTransaction
    {
        public string OrderId { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Status { get; set; } = "INITIATED";

        public string AirpayTransactionId { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string RawResponse { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
