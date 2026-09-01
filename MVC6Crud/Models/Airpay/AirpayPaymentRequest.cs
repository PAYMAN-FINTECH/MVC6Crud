namespace MVC6Crud.Models.Airpay
{
    public class AirpayPaymentRequest
    {
            public string BuyerEmail { get; set; } = string.Empty;

            public string BuyerPhone { get; set; } = string.Empty;

            public string BuyerFirstName { get; set; } = string.Empty;

            public string BuyerLastName { get; set; } = string.Empty;

            public string BuyerAddress { get; set; } = string.Empty;

            public string BuyerCity { get; set; } = string.Empty;

            public string BuyerState { get; set; } = string.Empty;

            public string BuyerCountry { get; set; } = "India";

            public string BuyerPinCode { get; set; } = string.Empty;

            public string OrderId { get; set; } = string.Empty;

            public decimal Amount { get; set; }

            public string CurrencyCode { get; set; } = "356";

            public string IsoCurrency { get; set; } = "INR";

            public string CustomVar { get; set; } = string.Empty;

            public string TxnSubType { get; set; } = string.Empty;

            public string Chmod { get; set; } = string.Empty;
            public string ReturnUrl { get; set; } = "";

    }
}
