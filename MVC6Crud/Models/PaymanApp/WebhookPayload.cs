namespace MVC6Crud.Models.PaymanApp
{
    public class WebhookPayload
    {
        public string EventId { get; set; }
        public Data Data { get; set; }
        public string EventVersion { get; set; }
        public string EventSource { get; set; }
        public string EventType { get; set; }
        public string SentAt { get; set; }
        public DateTime EventTimeStamp { get; set; }
    }

    public class Data
    {
        public Amount Amount { get; set; }
        public Fees Fees { get; set; }
        public Tax Tax { get; set; }
        public string ClientReferenceId { get; set; }
        public string PaymentReferenceId { get; set; }
        public string Message { get; set; }
        public string Mode { get; set; }
        public string BankTransactionReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Remarks { get; set; }
        public string ScheduledAt { get; set; }
        public string Status { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Amount
    {
        public string Currency { get; set; }
        public decimal Value { get; set; }
    }

    public class Fees
    {
        public string Currency { get; set; }
        public decimal Value { get; set; }
    }

    public class Tax
    {
        public string Currency { get; set; }
        public decimal Value { get; set; }
    }

}
