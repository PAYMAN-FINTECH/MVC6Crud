using System.Text.Json.Serialization;

namespace MVC6Crud.Models.CSBPayOut
{
    public class ImpsPaymentRequest
    {
        [JsonPropertyName("header")]
        public header header { get; set; }

        [JsonPropertyName("txnDet")]
        public txnDet txnDet { get; set; }
    }

    public class header
    {
        [JsonPropertyName("entityId")]
        public string entityId { get; set; }

        [JsonPropertyName("functionId")]
        public string functionId { get; set; }

        [JsonPropertyName("action")]
        public string action { get; set; }

        [JsonPropertyName("channel")]
        public string channel { get; set; }

        [JsonPropertyName("source")]
        public string source { get; set; }

        [JsonPropertyName("moduleId")]
        public string moduleId { get; set; }

        [JsonPropertyName("userId")]
        public string userId { get; set; }

        [JsonPropertyName("hostCode")]
        public string hostCode { get; set; }

        [JsonPropertyName("transactionId")]
        public string transactionId { get; set; }
    }


    public class txnDet
    {
        [JsonPropertyName("benAcNo")]
        public string benAcNo { get; set; }

        [JsonPropertyName("benAcType")]
        public string benAcType { get; set; }

        [JsonPropertyName("benIfsc")]
        public string benIfsc { get; set; }

        [JsonPropertyName("benName")]
        public string benName { get; set; }

        [JsonPropertyName("drAcNo")]
        public string drAcNo { get; set; }

        [JsonPropertyName("drAcType")]
        public string drAcType { get; set; }

        [JsonPropertyName("hostCode")]
        public string hostCode { get; set; }

        [JsonPropertyName("instructionDate")]
        public string instructionDate { get; set; }

        [JsonPropertyName("networkCode")]
        public string networkCode { get; set; }

        [JsonPropertyName("pmtType")]
        public string pmtType { get; set; }

        [JsonPropertyName("remarks")]
        public string remarks { get; set; }

        [JsonPropertyName("sourceCode")]
        public string sourceCode { get; set; }

        [JsonPropertyName("txnAmount")]
        public decimal txnAmount { get; set; }

        [JsonPropertyName("txnCcy")]
        public string txnCcy { get; set; }

        [JsonPropertyName("txnType")]
        public string txnType { get; set; }

        [JsonPropertyName("sourceRefNo")]
        public string sourceRefNo { get; set; }
    }
    public class CSBOAuthTokenResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string scope { get; set; }
    }
    public class RtgsRequest
    {
        public Header1 header { get; set; }
        public RtgsTxnDet txnDet { get; set; }
    }
    public class Header1
    {
        public string entityId { get; set; }
        public string functionId { get; set; }
        public string action { get; set; }
        public string moduleId { get; set; }
        public string userId { get; set; }
        public string hostCode { get; set; }
        public string source { get; set; }
    }
    public class CSBEncryptedResponse
    {
        public string encData { get; set; }
    }


    public class Header
    {
        public string entityId { get; set; }
        public string functionId { get; set; }
        public string action { get; set; }
        public string moduleId { get; set; }
        public string userId { get; set; }
        public string hostCode { get; set; }
        public string source { get; set; }
    }

    public class RtgsTxnDet
    {
        public string hostCode { get; set; }
        public string sourceCode { get; set; }
        public string networkCode { get; set; }
        public string pmtType { get; set; }
        public string activationDate { get; set; }
        public string remarks { get; set; }
        public string prefundedPayments { get; set; }
        public string transferType { get; set; }

        public CustCrdTrfInitPmtInf custcrdtrfinitPmtinfDto { get; set; }
        public CustCrdTrfInitCdtTxInf custcrdtrfinitCdttxinfDto { get; set; }
    }

    public class CustCrdTrfInitPmtInf
    {
        public string dbtraccothrid { get; set; }
        public string dbtraccnm { get; set; }
        public string reqdexctndt { get; set; }
    }

    public class CustCrdTrfInitCdtTxInf
    {
        public string cdtragtclrsysmmbid { get; set; }
        public string cdtrnm { get; set; }
        public string cdtraccothrid { get; set; }
        public string instrid { get; set; }
        public decimal instdamt { get; set; }
        public string instdamtccy { get; set; }
    }

    public class StatementRequest
    {
        public Args0 args0 { get; set; }
        public Args1 args1 { get; set; }
    }

    public class Args0
    {
        public int bankCode { get; set; }
        public int transactionBranch { get; set; }
        public string localDateTimeText { get; set; }
        public string externalReferenceNo { get; set; }
        public string postingDateText { get; set; }
        public string valueDateText { get; set; }
        public int externalBatchNumber { get; set; }
        public int externalSystemAuditTrailNumber { get; set; }
        public string channel { get; set; }
        public string userId { get; set; }
        public SupervisorContext supervisorContext { get; set; }
        public TellerContext tellerContext { get; set; }
        public string serviceCode { get; set; }
        public Reason reason { get; set; }
    }

    public class SupervisorContext
    {
        public string userId { get; set; }
        public string primaryPassword { get; set; }
    }

    public class TellerContext
    {
        public string userId { get; set; }
        public string primaryPassword { get; set; }
    }

    public class Reason
    {
        public int reasonCode { get; set; }
        public string comment { get; set; }
    }
    public class Args1
    {
        public string accountID { get; set; }
        public string fromDate { get; set; }
        public string toDate { get; set; }
        public int pageSize { get; set; }
        public int pageNumber { get; set; }
        public int noOftransactions { get; set; }
        public string txnReferenceNumber { get; set; }
        public string transactionDate { get; set; }
    }


    public class MiniStatementResponse
    {
        public string postingDate { get; set; }
        public TransactionStatus transactionStatus { get; set; }
        public decimal closingBalance { get; set; }
        public decimal openingBalance { get; set; }
        public List<TransactionInquiryDetailsDTO> transactionInquiryDetailsDTO { get; set; }
    }

    public class TransactionStatus
    {
        public bool FCYHangeHandlingApplied { get; set; }
        public string errorCode { get; set; }
        public object extendedReply { get; set; }
        public string externalReferenceNo { get; set; }
        public bool isOverriden { get; set; }
        public bool isServiceChargeApplied { get; set; }
        public int replyCode { get; set; }
        public int spReturnValue { get; set; }
    }

    public class TransactionInquiryDetailsDTO
    {
        public string postingDate { get; set; }
        public decimal amount { get; set; }
        public string chequeNo { get; set; }
        public int depositNumber { get; set; }
        public string description { get; set; }
        public string flgDrCr { get; set; }
        public int originatingBranch { get; set; }
        public decimal runningTotal { get; set; }
        public string txnDate { get; set; }
        public string valueDate { get; set; }
    }

    public class ImpsSuccessResponse
    {
        public string hostCode { get; set; }
        public string txnRefNo { get; set; }
        public string sourceRefNo { get; set; }
        public string retreivalRefNo { get; set; }
        public string branchCode { get; set; }
        public string benName { get; set; }
        public string MSGSTATUS { get; set; }
        public string TXNID { get; set; }
        public List<Resp> resp { get; set; }
    }

    public class Resp
    {
        public string respCode { get; set; }
        public string respDesc { get; set; }
        public string sourceRefNo { get; set; }
    }

    public class BankTransferResponse
    {
        public string txnrefno { get; set; }
        public string txnRefNo { get; set; }

        public string sourceRefNo { get; set; }

        public string utrno { get; set; }

        public string instrid { get; set; }

        public string benName { get; set; }

        public string MSGSTATUS { get; set; }

        public string TXNID { get; set; }

        public List<Resp1> resp { get; set; }
    }

    public class Resp1
    {
        public string respCode { get; set; }
        public string respDesc { get; set; }
        public string sourceRefNo { get; set; }
        public string instrId { get; set; }
    }


}
