namespace MVC6Crud.Models.PaymanApp
{
    public class BankAccountApp
    {
        public string accountNumber { get; set; }
        public string branchCode { get; set; }
        public Balance balance { get; set; }
    }
    public class Balance
    {
        public string currency { get; set; }
        public decimal value { get; set; }
    }
}
