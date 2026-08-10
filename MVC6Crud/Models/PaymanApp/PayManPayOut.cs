namespace MVC6Crud.Models.PaymanApp
{
    public class PayManPayOut
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserPhone { get; set; }

        public string? PayOutId { get; set; }
        public string? RefId { get; set; }
        public string? AccountHolderName { get; set; }
        public string? AccountNo { get; set; }
        public string? IfscCode {  get; set; }
        public decimal? Amount { get; set; }
        public decimal? PayoutCommission {  get; set; }
        public string? BeneId { get; set; }
        public string? PayOutType { get; set; }
        public string? TxnType { get; set; }
        public string Email { get; set; }
        public bool? Status { get; set; }
        public DateTime? DateTime { get; set; }
        public string? Result { get; set; }
        public string? Device { get; set; }


    }
}
