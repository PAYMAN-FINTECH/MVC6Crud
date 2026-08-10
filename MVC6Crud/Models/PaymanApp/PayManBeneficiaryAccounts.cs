namespace MVC6Crud.Models.PaymanApp
{
    public class PayManBeneficiaryAccounts
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? UserPhone { get; set; }
        public string? ContactId { get; set; }
        public string? ContactName { get; set; }
        public string? BeneId { get; set; }

        public string? AccountNo { get; set; }
        public string? IfscCode { get; set; }
        public string? VerficationFlag { get; set; }
        public string? MobileNumber { get; set; }
        public string? EmailId { get; set; }
        public string? TxnType { get; set; }
        public decimal? VerificationComm {  get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
