namespace MVC6Crud.Models
{
    public class PayOutModels
    {
    }
    public class Header
    {
        public string operatingSystem { get; set; }
        public string sessionId { get; set; }
        public string version { get; set; }
        public string date { get; set; }
        public string requestId { get; set; }
    }

    public class Transaction
    {
        public string requestType { get; set; }
        public string requestSubType { get; set; }
        public int tranCode { get; set; }
        public double txnAmt { get; set; }
        public string id { get; set; }
        public double surChargeAmount { get; set; }
        public int txnCode { get; set; }
        public int userType { get; set; }
    }

    public class PayOutBean
    {
        public string payoutId { get; set; }
        public string mobileNo { get; set; }
        public string customerId { get; set; }
        public string userId { get; set; }
        public string txnAmount { get; set; }
        public string accountNo { get; set; }
        public string ifscCode { get; set; }
        public string accountHolderName { get; set; }
        public string txnType { get; set; }
        public string aggregatorId { get; set; }
        public string txnStatus { get; set; }
        public string bankRefNo { get; set; }
        public string spkRefNo { get; set; }
        public string bankStatus { get; set; }
        public string statusCode { get; set; }
        public string statusDesc { get; set; }
        public string orderRefNo { get; set; }
        public string customerName { get; set; }
        public string aggregtorName { get; set; }
        public string emailId { get; set; }
    }

    public class Response
    {
        public string code { get; set; }
        public string description { get; set; }
    }

    public class Root
    {
        public Header header { get; set; }
        public Transaction transaction { get; set; }
        public PayOutBean payOutBean { get; set; }
        public Response response { get; set; }
    }

    public class HeaderRequest
    {
        public string operatingSystem { get; set; }
        public string sessionId { get; set; }
        public string version { get; set; }
    }

    public class UserInfo
    {
        // Add properties for UserInfo if needed
    }

    public class TransactionRequest
    {
        public string requestType { get; set; }
        public string requestSubType { get; set; }
        public int tranCode { get; set; }
        public double txnAmt { get; set; }
        public string id { get; set; }
        public double surChargeAmount { get; set; }
        public int txnCode { get; set; }
        public int userType { get; set; }
    }

    public class PayOutBeanRequest
    {
        public string mobileNo { get; set; }
        public string txnAmount { get; set; }
        public string accountNo { get; set; }
        public string ifscCode { get; set; }
        public string bankName { get; set; }
        public string accountHolderName { get; set; }
        public string txnType { get; set; }
        public string accountType { get; set; }
        public string emailId { get; set; }
        public string orderRefNo { get; set; }
        public int count { get; set; }
    }

    public class RootRequest
    {
        public HeaderRequest header { get; set; }
        public UserInfo userInfo { get; set; }
        public TransactionRequest transaction { get; set; }
        public PayOutBeanRequest payOutBean { get; set; }
    }
}
