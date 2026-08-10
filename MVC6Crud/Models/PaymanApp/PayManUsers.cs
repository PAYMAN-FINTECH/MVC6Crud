namespace MVC6Crud.Models.PaymanApp
{
    public class PayManUsers
    {
        public Guid Id { get; set; }

        public string? FirstName { get; set; } = null!;

        public string? LastName { get; set; } = null!;

        public string? Email { get; set; } = null!;
        public string? UserPin { get; set; } = null!;
        public bool? PinVerified { get; set; }

        public string? Phone { get; set; } = null!;

        public string? CustomerType { get; set; } = null!;

        public string? Margin { get; set; } = null!;
        //public DateTime? CreatedDate { get; set; }

        public bool? IsActive { get; set; }
        public bool? IsAadherVerified { get; set; }
        public bool? IsAdmin {  get; set; }
        public string? ResetOtp { get; set; }
        public DateTime? OtpGeneratedAt { get; set; }
        public bool? PayIn { get; set; }
        public bool? PayOut { get; set; }
        public bool? CCBill { get; set; }
        public bool? OtpLoginEnabled {  get; set; }
        public decimal? HdfcMargin { get; set; }
        public decimal? CarporateCardMarigin { get; set; }
        public decimal? MasterMarigin { get; set; }
        public string? WedPWD { get; set; }
        public bool? Web { get; set; }
        public bool? App { get; set; }
        public string? BillAvenueAgentId { get; set; }
        public string? BillAvenueAgentName { get; set; }

        public bool? OpenMoney { get; set; }
        public bool? EduCashfree { get; set; }
        public bool? IsAgreement { get; set; }
        public bool? Easebuzz { get; set; }
    }
}
