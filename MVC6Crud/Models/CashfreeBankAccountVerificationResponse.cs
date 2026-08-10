namespace MVC6Crud.Models
{
    public class CashfreeBankAccountVerificationResponse
    {
        public long reference_id { get; set; }
        public string name_at_bank { get; set; }
        public string BankName { get; set; }
        public string Utr { get; set; }
        public string City { get; set; }
        public string Branch { get; set; }
        public int Micr { get; set; }
        public int? NameMatchScore { get; set; }  // Nullable int for NameMatchScore
        public string NameMatchResult { get; set; }
        public string account_status { get; set; }
        public string AccountStatusCode { get; set; }
    }
}
