namespace MVC6Crud.Models.PaymanApp
{
    public class PayManHistory
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? UserPhone { get; set; }
        public string? TxnId { get; set; }
        public decimal? Amount { get; set; }
        public string? CardNumber { get; set; }
        public string? Mode { get; set; }
        public bool? Status { get; set; }
        public DateTime? Created { get; set; }
        public decimal? AvlBalance { get; set; }
        public Guid? PayInId { get; set; }
    }
}
