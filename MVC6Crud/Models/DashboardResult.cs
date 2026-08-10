namespace MVC6Crud.Models
{
    public class DashboardResult
    {
        public decimal PayInAmount { get; set; }
        public decimal PayInAmountAfterBankCommission { get; set; }
        public decimal PayManProfit { get; set; }
        public decimal PayOutUsersAvailableBalance { get; set; }
        public decimal PayManFounds { get; set; }
        public decimal PayInAmountLyraPayCommissionSettlement { get; set; }
        public decimal PayInAmountRazorPayCommissionSettlement { get; set; }
        public decimal PayInAmountLyraPaySettlement { get; set; }
        public decimal PayInAmountRazorPaySettlement { get; set; }
    }
    public class DashboardResponse
    {
        public List<DashboardResult> DashboardResult { get; set; }
    }
}
