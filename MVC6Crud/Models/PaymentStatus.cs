namespace MVC6Crud.Models
{
    public class PaymentStatus
    {
        public bool success { get; set; }
        public string code { get; set; }
        public string message { get; set; }
        public data data { get; set; }
    }

    public class data
    {
        public string merchantId { get; set; }
        public string merchantTransactionId { get; set; }
        public string transactionId { get; set; }
        public int amount { get; set; }

        public string state { get; set; }
        public string responseCode { get; set; }
        public paymentInstrument paymentInstrument { get; set; }
        public feesContext feesContext { get; set; }
    }

    public class feesContext
    {
        public int amount { get; set; }
    }

    public class paymentInstrument
    {
        public string type { get; set; }
        public string cardType { get; set; }
        public string pgTransactionId { get; set; }
        public string bankTransactionId { get; set; }
        public string pgAuthorizationCode { get; set; }
        public string arn { get; set; }

        public string bankId { get; set; }
        public string brn { get; set; }
    }
    public class EmployeeViewModel
    {
        public int EmployeeId { get; set; }

        public List<Itemlist> EmployeesList { get; set; }
        public decimal Amount { get; set; }
    }
    public class Itemlist
    {
        public string Text { get; set; }
        public int Value { get; set; }
    }

}
