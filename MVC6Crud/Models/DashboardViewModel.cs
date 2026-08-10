

namespace MVC6Crud.Models
{
    public class DashboardViewModel
    {
        //public IEnumerable<TransactionPayManDash> DailyReport { get; set; }
        //public IEnumerable<PayIn> MonthlyReport { get; set; }
        //public IEnumerable<PayIn> AnnualReport { get; set; }
        //public IEnumerable<PayIn> UserBalances { get; set; }

        public decimal PayInAmount {  get; set; }
        public decimal PayInAmountAfterBankCommission {  get; set; }
        public decimal PayOutUsersAvaliableBalance {  get; set; }
        public decimal PayManProfilt {  get; set; }
        public decimal PayManFounds {  get; set; }

        public decimal PayInAmountRazorPaySettelment {  get; set; }
        public decimal PayInAmountLyraPaySettelment { get; set; }
        public decimal PayInAmountRazorPayCommissionSettelment { get; set; }
        public decimal PayInAmountLyraPayCommissionSettelment { get; set; }

    }
}
