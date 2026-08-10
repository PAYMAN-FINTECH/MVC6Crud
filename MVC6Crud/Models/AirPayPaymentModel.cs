namespace MVC6Crud.Models
{
    public class AirPayPaymentModel
    {
        public string MerchantId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SecretKey { get; set; }
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string OrderId { get; set; }
        public string ReturnUrl { get; set; }
        public string CancelUrl { get; set; }
    }
}
