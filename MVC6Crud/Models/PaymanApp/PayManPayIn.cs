namespace MVC6Crud.Models.PaymanApp
{
    public class PayManPayIn
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? TxnId { get; set; }
        public string? EasePayId { get; set; }
        public string? UserPhone { get; set; }
        public string? Email { get; set; }
        public string? CardNumber { get; set; }
        public string? EaseCardNum { get; set; }
        public decimal? Amount { get; set; }
        public decimal? PayInCommission { get; set; }
        public decimal? PaymanCommission { get; set; }
        public string? Gateway { get; set; }
        public string? BankName { get; set; }
        public string? CardBrand { get; set; }
        public string? IsCorporate { get; set; }
        public DateTime? Created { get; set; }

        public bool? Status { get; set; }
        public string? Result { get; set; }
        public string? Device { get; set; }

        public string? CreditCardHolderName { get; set; }
        public string? CreditCardHolderNum { get; set; }

        public string? CardholderMobileNo { get; set; }


    }
}
