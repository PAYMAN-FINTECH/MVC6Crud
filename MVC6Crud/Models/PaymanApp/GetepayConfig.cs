namespace MVC6Crud.Models.PaymanApp
{
    public class GetepayConfig
    {
        public string mid { get; set; }
        public string terminalId { get; set; }
        public string key { get; set; }
        public string iv { get; set; }
        public string url { get; set; }
    }

    public class GetepayRequest
    {
        public string terminalId { get; set; }
        public string udf1 { get; set; }
        public string udf2 { get; set; }
        public string udf3 { get; set; }
        public string udf4 { get; set; }
        public string udf5 { get; set; }
        public string udf6 { get; set; }
        public string udf7 { get; set; }
        public string udf8 { get; set; }
        public string udf9 { get; set; }
        public string udf10 { get; set; }
        public string mid { get; set; }
        public string amount { get; set; }
        public string merchantTransactionId { get; set; }
        public string transactionDate { get; set; }
        public string ru { get; set; }
        public string callbackUrl { get; set; }
        public string currency { get; set; }
        public string paymentMode { get; set; }
        public string bankId { get; set; }
        public string txnType { get; set; }
        public string productType { get; set; }
        public string txnNote { get; set; }
        public string vpa { get; set; }
    }

    public class GetepayOrderResponse
    {
        public string paymentUrl { get; set; }
        public string getepayTxnId { get; set; }
    }

    public class GetepayRequestWrapper
    {
        public string mid { get; set; }
        public string terminalId { get; set; }
        public string req { get; set; }
    }

    public class GetepayResponseWrapper
    {
        public string response { get; set; }
    }

    public class GetepayRequery
    {
        public string paymentId { get; set; }
        public string mid { get; set; }
        public string terminalId { get; set; }
    }

    public class GetepayRequeryResponse
    {
        public string getepayTxnId { get; set; }
        public string mid { get; set; }
        public string txnAmount { get; set; }
        public string txnStatus { get; set; }
        public string merchantOrderNo { get; set; }
        public string udf1 { get; set; }
        public string udf2 { get; set; }
        public string udf3 { get; set; }
        public string udf4 { get; set; }
        public string udf5 { get; set; }
        public string udf6 { get; set; }
        public string udf7 { get; set; }
        public string udf8 { get; set; }
        public string udf9 { get; set; }
        public string udf10 { get; set; }
        public string udf41 { get; set; }
        public string custRefNo { get; set; }
        public string paymentMode { get; set; }
        public string message { get; set; }
        public string paymentStatus { get; set; }
        public string txnDate { get; set; }
        public string surcharge { get; set; }
        public string totalAmount { get; set; }
        public string settlementAmount { get; set; }
        public string settlementRefNo { get; set; }
        public string settlementDate { get; set; }
        public string settlementStatus { get; set; }
        public string txnNote { get; set; }
    }




}
