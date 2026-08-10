using MVC6Crud.Models.CashFree;

namespace MVC6Crud.Models.PaymanApp
{
    public class VegaahOrder
    {
        public string orderId { get; set; }
        public string description { get; set; }
    }

    public class VegaahCustomer
    {
        public string customerEmail { get; set; }
    }

    public class VegaahInitiateRequest
    {
        public string terminalId { get; set; }
        public string password { get; set; }
        public string signature { get; set; }
        public string paymentType { get; set; }
        public string amount { get; set; }
        public string currency { get; set; }
        public VegaahOrder order { get; set; }
        public VegaahCustomer customer { get; set; }
    }

    public class VegaahInitiateResponse
    {
        public string responseCode { get; set; }
        public string responseDescription { get; set; }
        public string transactionId { get; set; }
        public dynamic paymentLink { get; set; }
    }

    public class VegaahFinalResponse
    {
        public string result { get; set; }           // SUCCESS / FAILURE
        public string transactionId { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string signature { get; set; }
        public OrderDetails orderDetails { get; set; }
    }

    public class OrderDetails
    {
        public string orderId { get; set; }
    }





    //response

    public class PaymentDecryptResponse
    {
        public string AuthCode { get; set; }
        public string TransactionDateTime { get; set; }
        public string Signature { get; set; }
        public string Eci { get; set; }
        public string TerminalId { get; set; }
        public PaymentInstrument1 PaymentInstrument { get; set; }
        public AdditionalDetails1 AdditionalDetails { get; set; }
        public string TransactionId { get; set; }
        public AmountDetails1 AmountDetails { get; set; }
        public string CustomerName { get; set; }
        public string ResponseCode { get; set; }
        public string Rrn { get; set; }
        public string MerchantName { get; set; }
        public string Result { get; set; }
        public OrderDetails1 OrderDetails { get; set; }
        public string ResponseDescription { get; set; }
        public string MerchantId { get; set; }
        public CardDetails1 CardDetails { get; set; }
        public string CustomerEmail { get; set; }
        public string PaymentMethod { get; set; }
        public string Currency { get; set; }
        public CustomerDetails1 CustomerDetails { get; set; }
        public string userPhone { get; set; }
        public string Gatewayname { get; set; }
    }

    public class CustomerDetails1
    {
        public string CardHolderName { get; set; }
    }


    public class OrderDetails1
    {
        public string OrderId { get; set; }
    }


    public class PaymentInstrument1
    {
        public string PaymentMethod { get; set; }
    }

    public class AdditionalDetails1
    {
        public string UserData { get; set; }
    }

    public class AmountDetails1
    {
        public string Amount { get; set; }
        public string OriginalAmount { get; set; }
    }
    public class CardDetails1
    {
        public string MaskedCard { get; set; }
        public string CardBrand { get; set; }
    }

}
