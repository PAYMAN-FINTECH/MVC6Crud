namespace MVC6Crud.Models.PaymanApp
{
    public class PaymentRequestApp
    {
        public string BillerId { get; set; }
        public string CustomerMobile { get; set; }
        public double Amount { get; set; }
        public string Phone {  get; set; }
        public string PaymentMode { get; set; }
        public string EnquiryReferenceId { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string LastFourDigits { get; set; }
        public string customerName {  get; set; }
        public string holderMobile { get; set; }
        public string? Device { get; set; }
        public string BillerResponse { get; set; }
        public string AdddditionalInfo { get; set; }
        public string BillFetchResponse { get; set; }
    }
}
