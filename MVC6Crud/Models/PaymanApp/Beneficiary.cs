namespace MVC6Crud.Models.PaymanApp
{
    public class Beneficiary
    {
        public Guid Id { get; set; }
        public string MobileNumber { get; set; }
        public string Name { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public bool IsVerified { get; set; }
        public string TxnType { get; set; }
        public string BankName { get; set; }
        public string UserPhone { get; set; }
    }
}
