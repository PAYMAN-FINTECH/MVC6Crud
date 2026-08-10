namespace MVC6Crud.Models.PaymanApp
{
    public class OpenMoneyPaymentTokenResponse
    {
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string Mtx { get; set; }
        public int Attempts { get; set; }
        public string SubAccountsId { get; set; }
        public string Id { get; set; }
        public string Entity { get; set; }
        public string Status { get; set; }
    }
}
