namespace MVC6Crud.Models
{
    public class PayPayment
    {
        public string EnterCustomerNumber { get; set; }
        public string Amount { get; set; }

        public string SelectPaymentGateWay { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }
        public bool Sucess {  get; set; }
        public bool IsSrrEnable { get; set; }
        public string phone { get; set; }
        public bool EaseBuzzEnable { get; set; }
    }
}
