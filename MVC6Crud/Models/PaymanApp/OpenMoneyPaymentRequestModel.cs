namespace MVC6Crud.Models.PaymanApp
{
    public class OpenMoneyPaymentRequestModel
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
    }
}
