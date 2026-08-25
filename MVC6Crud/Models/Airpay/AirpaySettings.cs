namespace MVC6Crud.Models.Airpay
{
    public class AirpaySettings
    {
        public string MerchantId { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Secret { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;

        public string TokenUrl { get; set; } = string.Empty;

        public string PaymentUrl { get; set; } = string.Empty;

        public string ResponseUrl { get; set; } = string.Empty;
    }
}
