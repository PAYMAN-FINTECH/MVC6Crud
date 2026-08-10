using System.Xml.Serialization;

namespace MVC6Crud.Models.PaymanApp
{
    [XmlRoot("billFetchResponse")]
    public class BillAvenueBillFetchResponse
    {
        public string responseCode { get; set; }

        [XmlArray("inputParams")]
        [XmlArrayItem("input")]
        public List<InputParam> inputParams { get; set; }

        public BillerResponseBillAvenue billerResponse { get; set; }

        [XmlArray("additionalInfo")]
        [XmlArrayItem("info")]
        public List<AdditionalInfo> additionalInfo { get; set; }
        public bool status { get; set; }
        public string message { get; set; }
        public string EnquiryReferenceId {  get; set; }
        public string paramValue1 { get; set; }
        public string paramValue2 { get; set; }
        public string paymentMode { get; set; }

        public string BillerResponse1 { get; set; }
        public string AdddditionalInfo { get; set; }
        public string BillFetchResponse { get; set; }

        [XmlElement("errorInfo")]
        public ErrorInfo errorInfo { get; set; }
    }

    public class InputParam
    {
        public string paramName { get; set; }
        public string paramValue { get; set; }
    }

    public class BillerResponseBillAvenue
    {
        public string billAmount { get; set; }
        public string billDate { get; set; }
        public string customerName { get; set; }
        public string dueDate { get; set; }
    }

    public class AdditionalInfo
    {
        public string infoName { get; set; }
        public string infoValue { get; set; }
    }


    [XmlRoot("ExtBillPayResponse")]
    public class ExtBillPayResponse
    {
        [XmlElement("responseCode")]
        public string ResponseCode { get; set; }

        [XmlElement("responseReason")]
        public string ResponseReason { get; set; }

        [XmlElement("txnRefId")]
        public string TxnRefId { get; set; }

        [XmlElement("approvalRefNumber")]
        public string ApprovalRefNumber { get; set; }

        [XmlElement("txnRespType")]
        public string TxnRespType { get; set; }

        [XmlArray("inputParams")]
        [XmlArrayItem("input")]
        public List<ExtInputParam> InputParams { get; set; }

        [XmlElement("CustConvFee")]
        public decimal CustConvFee { get; set; }

        [XmlElement("RespAmount")]
        public decimal RespAmount { get; set; }

        [XmlElement("RespBillDate")]
        public string RespBillDate { get; set; }

        [XmlElement("RespCustomerName")]
        public string RespCustomerName { get; set; }

        [XmlElement("RespDueDate")]
        public string RespDueDate { get; set; }
    }

    public class ExtInputParam
    {
        [XmlElement("paramName")]
        public string ParamName { get; set; }

        [XmlElement("paramValue")]
        public string ParamValue { get; set; }
    }

    public class ExtBillPayResponseResult
    {
        public ExtBillPayResponse extBillPayResponse { get; set; }
        public bool success {  get; set; }
    }

    [XmlRoot("billerInfoResponse", Namespace = "")]
    public class BillerInfoResponseObj
    {
        [XmlElement("responseCode")]
        public string responseCode { get; set; }

        [XmlElement("biller")]
        public BillerObj biller { get; set; }
    }

    public class BillerObj
    {
        [XmlElement("billerPaymentModes")]
        public string billerPaymentModes { get; set; }

        [XmlElement("billerStatus")]
        public string billerStatus { get; set; }
    }

    [XmlRoot("DepositEnquiryResponse")]
    public class DepositEnquiryResponse
    {
        [XmlElement("responseCode")]
        public string ResponseCode { get; set; }

        [XmlElement("instituteId")]
        public string InstituteId { get; set; }

        [XmlElement("currentBalance")]
        public decimal CurrentBalance { get; set; }

        [XmlElement("currency")]
        public string Currency { get; set; }

        [XmlElement("transaction")]
        public BillAveneuTransaction Transaction { get; set; }
    }

    public class BillAveneuTransaction
    {
        // If transaction has attributes/elements, add them here.
        // Right now it's empty since your sample shows <transaction/>
    }
    public class BillFetchWrapper
    {
        public string BillerResponse { get; set; }
        public string AdddditionalInfo { get; set; }
        public string BillFetchResponse { get; set; }
    }



    public class ExtBillPayResponse11
    {
        [XmlElement("responseCode")]
        public string ResponseCode { get; set; }

        [XmlElement("responseReason")]
        public string ResponseReason { get; set; }

        [XmlElement("txnList")]
        public TxnList11 TxnList { get; set; }

        [XmlElement("refundTxn")]
        public RefundTxn11 RefundTxn { get; set; }

        [XmlElement("statusRequestId")]
        public string StatusRequestId { get; set; }
    }

    [XmlRoot("transactionStatusResp")]
    public class ExtBillPayResponse111
    {
        [XmlElement("responseCode")]
        public string ResponseCode { get; set; }

        [XmlElement("responseReason")]
        public string ResponseReason { get; set; }

        [XmlElement("txnList")]
        public TxnList11 TxnList { get; set; }

        [XmlElement("refundTxn")]
        public RefundTxn11 RefundTxn { get; set; }

        [XmlElement("statusRequestId")]
        public string StatusRequestId { get; set; }
    }

    public class TxnList11
    {
        [XmlElement("agentId")]
        public string AgentId { get; set; }

        [XmlElement("amount")]
        public string Amount { get; set; }

        [XmlElement("billerId")]
        public string BillerId { get; set; }

        [XmlElement("txnDate")]
        public string TxnDate { get; set; }

        [XmlElement("txnReferenceId")]
        public string TxnReferenceId { get; set; }

        [XmlElement("txnStatus")]
        public string TxnStatus { get; set; }

        [XmlElement("mobile")]
        public string Mobile { get; set; }

        [XmlElement("approvalRefNumber")]
        public string ApprovalRefNumber { get; set; }

        [XmlElement("inputParams")]
        public List<InputParams11> InputParams { get; set; }

        [XmlElement("txnRespType")]
        public string TxnRespType { get; set; }

        [XmlElement("custConvFee")]
        public string CustConvFee { get; set; }

        [XmlElement("respCustomerName")]
        public string RespCustomerName { get; set; }

        [XmlElement("respDueDate")]
        public string RespDueDate { get; set; }

        [XmlElement("payRequestId")]
        public string PayRequestId { get; set; }
    }

    public class InputParams11
    {
        [XmlElement("paramName")]
        public string ParamName { get; set; }

        [XmlElement("paramValue")]
        public string ParamValue { get; set; }
    }

    public class RefundTxn11
    {
        [XmlElement("refundRespMessage")]
        public string RefundRespMessage { get; set; }
    }

    [XmlRoot("billFetchResponse")]
    public class BillFetchResponseErrorModel
    {
        [XmlElement("responseCode")]
        public string ResponseCode { get; set; }

        [XmlElement("errorInfo")]
        public ErrorInfo ErrorInfo { get; set; }
    }

    public class ErrorInfo
    {
        [XmlElement("error")]
        public ErrorDetail Error { get; set; }
    }

    public class ErrorDetail
    {
        [XmlElement("errorCode")]
        public string ErrorCode { get; set; }

        [XmlElement("errorMessage")]
        public string ErrorMessage { get; set; }
    }




}
