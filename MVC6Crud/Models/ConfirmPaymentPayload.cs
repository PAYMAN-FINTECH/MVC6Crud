namespace MVC6Crud.Models
{
    public class ConfirmPaymentPayload
    {
        public string razorpay_payment_id { get; }
        public string razorpay_order_id { get; }
        public string razorpay_signature { get; }
    }
}
