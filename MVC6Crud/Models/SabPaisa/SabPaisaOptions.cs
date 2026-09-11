namespace MVC6Crud.Models.SabPaisa
{
    public class SabPaisaOptions
    {
        public string Environment { get; set; } = "Staging";

        public string StagingBaseUrl { get; set; } = "";
        public string ProductionBaseUrl { get; set; } = "";

        public string ApiKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string MerchantId { get; set; } = "";

        public string ReturnUrl { get; set; } = "";
        public string WebhookUrl { get; set; } = "";

        public string BaseUrl =>
            Environment.Equals("Production",
                StringComparison.OrdinalIgnoreCase)
                ? ProductionBaseUrl
                : StagingBaseUrl;
    }


    public class CreateSabPaisaPaymentRequest
    {
        public string MerchantId { get; set; } = "";

        public string MerchantTxnId { get; set; } = "";

        // Amount in PAISE
        public long Amount { get; set; }

        public string Currency { get; set; } = "INR";

        public string CustomerName { get; set; } = "";

        public string CustomerEmail { get; set; } = "";

        public string CustomerPhone { get; set; } = "";

        public string ReturnUrl { get; set; } = "";

        public string? Description { get; set; }

        public string Checksum { get; set; } = "";

        public long Timestamp { get; set; }
    }

    public class SabPaisaPaymentResponse
    {
        public bool Success { get; set; }

        public string? PaymentId { get; set; }

        public string? CheckoutUrl { get; set; }

        public string? ClientSecret { get; set; }

        public string? Status { get; set; }

        public string? ExpiresAt { get; set; }

        public string? Message { get; set; }

        public string? TraceId { get; set; }
    }

    public class SabPaisaReturnRequest
    {
        public string? TransactionId { get; set; }

        public string? MerchantTxnId { get; set; }

        public string? Status { get; set; }

        public string? Amount { get; set; }

        public string? PaidAmount { get; set; }

        public string? PaymentMode { get; set; }

        public string? Timestamp { get; set; }

        public string? Signature { get; set; }
    }
}
