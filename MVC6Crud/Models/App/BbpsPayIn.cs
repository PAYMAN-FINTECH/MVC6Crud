namespace MVC6Crud.Models.App
{
    public class BbpsPayIn
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserPhone { get; set; }

        public string TxnId { get; set; }
        public decimal Amount { get; set; }
        public string CustomerEmail { get; set; }
        public string CardNumber { get; set; }
        public string PaymentTxnId { get; set; }
        public string Result { get; set; }
        public string Device {  get; set; }
        public bool Status { get; set; }
        public DateTime Created {  get; set; }

    }
}
