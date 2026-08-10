namespace MVC6Crud.Models.PaymanApp
{
    public class AddBeneficiaryRequest
    {
        public string MobileNumber { get; set; }
        public string Name { get; set; }
        public string AccountNumber { get; set; }
        public string TxnType { get; set; }
        public int BankId { get; set; }  // <-- Only Bank ID
        public string UserPhone { get; set; }
    }
}
