using DocumentFormat.OpenXml.Presentation;

namespace MVC6Crud.Models
{
    public class TransectionHistory
    {
        public string UserId {  get; set; }
        public string UserName { get; set; }

        public decimal PayInAmount {  get; set; }
        public decimal PayOutAmount { get; set; }
        public DateTime DateTime { get; set; }

        public string AvaliableBalance { get; set; }
        public decimal PayInCommision {  get; set; }
        public decimal PayOutCommision { get; set; }
    }

    public class PayInHistory
    {
        public string EnterDate { get; set; }
        public int Sno { get; set; }
        public string TxnDate { get; set; }
        public string PaymentDetails { get; set; }
        public decimal TxnAmount { get; set; }
        public decimal TxnCharges { get; set; }
        public decimal creditAmount { get; set; }
        public string TxnStatus { get; set; }
        public string AccountNo { get; set; }
        public string Name { get; set; }
        public decimal DistributerComm { get; set; }
        public string UserName { get; set; }
        public string CardNo { get; set; } 

    }
}
