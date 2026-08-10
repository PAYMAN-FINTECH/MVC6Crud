namespace MVC6Crud.Models.PaymanWeb
{
    public class PaymentStatusViewModel
    {
        public bool IsSuccess { get; set; }
        public double Amount { get; set; }
        public string TransactionId { get; set; }
        public string CardNumber { get; set; }
        public string Message { get; set; }
        public string? Gateway { get; set; }
    }
}
