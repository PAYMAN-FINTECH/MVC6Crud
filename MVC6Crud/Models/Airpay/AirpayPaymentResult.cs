namespace MVC6Crud.Models.Airpay
{
    public class AirpayPaymentResult
    {
        public string PaymentUrl { get; set; } = string.Empty;

        public string MerchantId { get; set; } = string.Empty;

        public string PrivateKey { get; set; } = string.Empty;

        public string EncData { get; set; } = string.Empty;

        public string Checksum { get; set; } = string.Empty;

        public string OrderId { get; set; } = string.Empty;
    }
}
