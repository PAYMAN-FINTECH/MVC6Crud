using System.ComponentModel.DataAnnotations;

namespace MVC6Crud.Models
{
    public class PayManHome
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }

        public decimal Amount { get; set; }

        public decimal SefexFinalBalance { get; set; }
        public string InstantPayBalance { get; set; }

        public bool SefexPay { get; set; }
        public bool PineLab { get; set; }
        public bool InstantPay { get; set; }
        public string Role { get; set; }

        public decimal PayInAmount { get; set; }
        public decimal PayInAmountAfterBankCommission { get; set; }
        public decimal PayOutUsersAvaliableBalance { get; set; }
        public decimal PayManProfilt { get; set; }
        public decimal PayManFounds { get; set; }

        public decimal PayInAmountRazorPaySettelment { get; set; }
        public decimal PayInAmountLyraPaySettelment { get; set; }
        public decimal PayInAmountRazorPayCommissionSettelment { get; set; }
        public decimal PayInAmountLyraPayCommissionSettelment { get; set; }
        public string SelectedTime { get; set; }

        public decimal PayInAmountAfterBankCommission1 { get; set; }
        public decimal EasebuzzFeatueSeletment { get; set; }
        public decimal Srrbankamount { get; set; }
        public decimal RazorpayAmount { get; set; }
        public bool PayOutEnable { get; set; }
    }
}
