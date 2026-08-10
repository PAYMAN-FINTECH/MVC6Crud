namespace MVC6Crud.Models
{
    public class EncriptedObject
    {
        public string agId { get; set; }
        public string payload { get; set; }
        public string reqTime { get; set; }
        public string respTime { get; set; }
        public string requestId { get; set; }
        public string uid { get; set; }

    }

    public class BeneficiaryAddResponse
    {
        public string status { get; set; }
        public string userMessage { get; set; }
        public string responseDataType { get; set; }
        public string responseData { get; set; }
        public string responseCode { get; set; }
        public string errorMessage { get; set; }
        public object sysErrorMessage { get; set; }
    }

    public class Vendor
    {
        public string contact_type { get; set; }
        public string name { get; set; }
        public string org_name { get; set; }
        public string email_id { get; set; }
        public string mobile_no { get; set; }
        public string me_id { get; set; }
        public List<Bank> banks { get; set; }
        public string pan_no { get; set; }
        public string registration_type { get; set; }
        public string gst_no { get; set; }
        public string notes { get; set; }

        // Example object instantiation
        public static Vendor GetExample()
        {
            return new Vendor
            {
                contact_type = "Vendor",
                name = "Navaneetha",
                org_name = "Safexpay",
                email_id = "jurrajanardhan@gmail.com",
                mobile_no = "6302286450",
                me_id = "AGEN3250012853",
                banks = new List<Bank>
            {
                new Bank
                {
                    account_no = "62284008742",
                    ifsc_code = "SBIN0020140",
                    category_type = "BANK",
                    account_holder_name = "Janardhan",
                    code = ""
                }
            },
                pan_no = "",
                registration_type = "Consumer",
                gst_no = "",
                notes = "testing"
            };
        }

        public static Vendor GetExample1(BeneficiaryAccounts beneficiaryAccounts)
        {
           
            return new Vendor
            {
                contact_type = "Vendor",
                name = beneficiaryAccounts.ContactName,
                org_name = "PayMan",
                email_id = "jurrajanardhan@gmail.com",
                mobile_no = beneficiaryAccounts.MobileNumber,
                me_id = "AGEN3250012853",
                banks = new List<Bank>
            {
                new Bank
                {
                    account_no = beneficiaryAccounts.AccountNo,
                    ifsc_code = beneficiaryAccounts.IfscCode.ToUpper(),
                    category_type = "BANK",
                    account_holder_name = beneficiaryAccounts.ContactName,
                    code = ""
                }
            },
                pan_no = "",
                registration_type = "Consumer",
                gst_no = "",
                notes = "testing"
            };
        }
    }

    public class Bank
    {
        public string account_no { get; set; }
        public string ifsc_code { get; set; }
        public string category_type { get; set; }
        public string account_holder_name { get; set; }
        public string code { get; set; }
    }
    public class BeneficiaryAPIResponseBean
    {
        public string bene_id { get; set; }
        public string account_no { get; set; }
        public string ifsc_code { get; set; }
        public string verficationFlag { get; set; }
        public int page { get; set; }
        public int size { get; set; }

        public string account_name { get; set; }
    }

    public class RootObject
    {
        public string contact_id { get; set; }
        public string contact_name { get; set; }
        public List<BeneficiaryAPIResponseBean> beneficiaryAPIResponseBean { get; set; }
        public string date { get; set; }
    }

    public class PayWithBeanHeader
    {
        public string operatingSystem { get; set; }
        public string sessionId { get; set; }
        public string version { get; set; }
    }

    public class PayWithBeanTransaction
    {
        public string requestType { get; set; }
        public string requestSubType { get; set; }
        public int tranCode { get; set; }
        public double txnAmt { get; set; }
        public double surChargeAmount { get; set; }
        public int txnCode { get; set; }
        public int userType { get; set; }
    }

    public class PayOutBeanPay
    {
        public string mobileNo { get; set; }
        public string txnAmount { get; set; }
        public string beneId { get; set; }
        public int count { get; set; }
        public string orderRefNo { get; set; }
        public string payMode { get; set; }
    }

    public class PayWithBeanRootObject
    {
        public PayWithBeanHeader header { get; set; }
        public PayWithBeanUserInfo userInfo { get; set; }
        public PayWithBeanTransaction transaction { get; set; }
        public PayOutBeanPay payOutBean { get; set; }
    }

    public class PayWithBeanUserInfo
    {
        // properties here
    }

    public class PayWithBeanResponseHeader
    {
        public string operatingSystem { get; set; }
        public string sessionId { get; set; }
        public string version { get; set; }
        public string date { get; set; }
        public string requestId { get; set; }
    }

    public class PayWithBeanResponseTransaction
    {
        public string requestType { get; set; }
        public string requestSubType { get; set; }
        public int tranCode { get; set; }
        public double txnAmt { get; set; }
        public double surChargeAmount { get; set; }
        public int txnCode { get; set; }
        public int userType { get; set; }
    }

    public class PayWithBeanResponsePayOutBean
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

    public class PayWithBeanResponseResponse
    {
        public string code { get; set; }
        public string description { get; set; }
    }

    public class PayWithBeanResponseRootObject
    {
        public PayWithBeanResponseHeader header { get; set; }
        public PayWithBeanResponseTransaction transaction { get; set; }
        public PayWithBeanResponsePayOutBean payOutBean { get; set; }
        public PayWithBeanResponseResponse response { get; set; }
    }
    public class LyraPaymentDetails
    {
        public string Uuid { get; set; }
        public DateTime Date { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Status { get; set; }
        public string OrderId { get; set; }
        public string Currency { get; set; }
        public int Amount { get; set; }
        public int Paid { get; set; }
        public int Due { get; set; }
        public int Refunded { get; set; }
        public LyraCustomerDetails Customer { get; set; }
        public string OrderInfo { get; set; }
        public object Udf { get; set; }
        public int Attempts { get; set; }
        public bool TestMode { get; set; }
        public object DropReason { get; set; }
        public string PaymentLink { get; set; }
        public int MaxAttempts { get; set; }
        public bool Surcharge { get; set; }
        public List<LayraTransaction> Transactions { get; set; }
    }
    public class LayraTransaction
    {
        public string Type { get; set; }
        public string Status { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Uuid { get; set; }
        public string PaymentOption { get; set; }
        public string Scheme { get; set; }
        public string ExternalId { get; set; }
        public string AuthNum { get; set; }
        public string AuthResponseCode { get; set; }
        public string CardLast4 { get; set; }
        public string IssuingBank { get; set; }
        public string CardType { get; set; }
        public string CardVariant { get; set; }
    }
    public class RazorPayCard
    {
        public string Id { get; set; }
        public string Entity { get; set; }
        public string Name { get; set; }
        public string Last4 { get; set; }
        public string Network { get; set; }
        public string Type { get; set; }
        public string Issuer { get; set; }
        public bool International { get; set; }
        public bool Emi { get; set; }
        public string SubType { get; set; }
        public string TokenIin { get; set; }
    }

    public class LyraCustomerDetails
    {
        public object Uid { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public object Address { get; set; }
        public object City { get; set; }
        public object State { get; set; }
        public object Zip { get; set; }
        public object Country { get; set; }
    }
    public class StatusHeader
    {
        public string OperatingSystem { get; set; }
        public string SessionId { get; set; }
        public string Version { get; set; }
    }

    public class StatusUserInfo
    {
        // Define properties as needed for user information
    }

    public class StatusTransaction
    {
        public string RequestType { get; set; }
        public string RequestSubType { get; set; }
        public int TranCode { get; set; }
        public double TxnAmt { get; set; }
    }

    public class StatusPayOutBean
    {
        public string PayoutId { get; set; }
        public string OrderRefNo { get; set; }
    }

    public class StatusRootObject
    {
        public StatusHeader Header { get; set; }
        public StatusUserInfo UserInfo { get; set; }
        public StatusTransaction Transaction { get; set; }
        public StatusPayOutBean PayOutBean { get; set; }
    }

    public class SefexBalHeader
    {
        public string OperatingSystem { get; set; }
        public string SessionId { get; set; }
        public string Version { get; set; }
        public DateTime Date { get; set; }
        public string RequestId { get; set; }
    }

    public class SefexBalTransaction
    {
        public string RequestType { get; set; }
        public string RequestSubType { get; set; }
        public int TranCode { get; set; }
        public decimal TxnAmt { get; set; }
        public string Id { get; set; }
        public decimal FinalBalance { get; set; }
        public decimal SurChargeAmount { get; set; }
        public int TxnCode { get; set; }
        public int UserType { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal LienBalance { get; set; }
    }

    public class SefexBalResponse
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }

    public class SefexBalRoot
    {
        public SefexBalHeader Header { get; set; }
        public SefexBalTransaction Transaction { get; set; }
        public SefexBalResponse Response { get; set; }
    }
    public class PInelabsPayoutTransactionRequest
    {
        public string clientReferenceId { get; set; }
        public string payeeName { get; set; }
        public string accountNumber { get; set; }
        public string branchCode { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public PInelabsPayoutAmount amount { get; set; }
        public string mode { get; set; }
        public string remarks { get; set; }
    }

    public class PInelabsPayoutAmount
    {
        public string currency { get; set; }
        public int value { get; set; }
    }

    public class PInelabsPayoutResponse
    {
        public string clientReferenceId { get; set; }
        public string requestReferenceId { get; set; }
        public string paymentReferenceId { get; set; }
        public string payeeName { get; set; }
        public string accountNumber { get; set; }
        public string branchCode { get; set; }
        public string vpa { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public PInelabsPayoutAmount amount { get; set; }
        public string mode { get; set; }
        public string status { get; set; }
        public string message { get; set; }
        public string scheduledAt { get; set; }
        public string remarks { get; set; }
        public List<PInelabsPayoutResponseLink> _links { get; set; }
    }


    public class PInelabsPayoutResponseLink
    {
        public string rel { get; set; }
        public string href { get; set; }
    }
    //ss

    public class Amount
    {
        public string currency { get; set; }
        public int value { get; set; }
    }

    public class Fees
    {
        public string currency { get; set; }
        public int value { get; set; }
    }

    public class Tax
    {
        public string currency { get; set; }
        public int value { get; set; }
    }

    public class Payment
    {
        public string clientReferenceId { get; set; }
        public string paymentReferenceId { get; set; }
        public string bankTransactionReferenceId { get; set; }
        public string mode { get; set; }
        public Amount amount { get; set; }
        public string accountNumber { get; set; }
        public string payeeName { get; set; }
        public Fees fees { get; set; }
        public Tax tax { get; set; }
        public string remarks { get; set; }
        public string status { get; set; }
        public string message { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public string scheduledAt { get; set; }
    }

    public class Link
    {
        public string rel { get; set; }
        public string href { get; set; }
    }

    public class PineLabsPaoutStatusRootObject
    {
        public List<Payment> payments { get; set; }
        public int totalRecords { get; set; }
        public int nextPage { get; set; }
        public int totalPages { get; set; }
        public List<Link> _links { get; set; }
    }

    public class BankAccount
    {
        public string accountNumber { get; set; }
        public string branchCode { get; set; }
        public Balance balance { get; set; }
    }

    public class Balance
    {
        public string currency { get; set; }
        public decimal value { get; set; }
    }

}
