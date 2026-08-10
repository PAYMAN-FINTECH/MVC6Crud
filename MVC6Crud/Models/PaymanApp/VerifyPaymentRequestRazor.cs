namespace MVC6Crud.Models.PaymanApp
{
    public class VerifyPaymentRequestRazor
    {
        public string RazorpayPaymentId { get; set; } = "";
        public string RazorpayOrderId { get; set; } = "";
        public string RazorpaySignature { get; set; } = "";
        public decimal Amount { get; set; }
        public string Phone { get; set; } = "";
        public string BillerId { get; set; } = "";
        public string CustomerMobile { get; set; } = "";
        public string Merchent { get; set; } = "";
        public string key { get; set; } = "";
        public string workingkey { get; set; } = "";
        public string CardEmail { get; set; } = "";
        public string CardHoderName { get; set; } = "";
        public string CardNum { get; set; } = "";
    }
}
