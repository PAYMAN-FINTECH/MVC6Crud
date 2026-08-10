using System.Xml.Serialization;

namespace MVC6Crud.Models.PaymanWeb
{
    public class BillAvenuePaymentRequest
    {
        public string BillerId { get; set; }
        public string Mobile { get; set; }
        public string CustomerNumber { get; set; }
        public string Last4Digits { get; set; }
        public decimal Amount { get; set; }
    }
    [XmlRoot("billerInfoResponse")]
    public class BillerInfoResponse
    {
        [XmlElement("responseCode")]
        public string ResponseCode { get; set; }

        [XmlElement("biller")]
        public Biller Biller { get; set; }
    }

    public class Biller
    {
        public string billerId { get; set; }
        public string billerAliasName { get; set; }
        public string billerName { get; set; }
        public string billerCategory { get; set; }
        public bool billerAdhoc { get; set; }
        public string billerCoverage { get; set; }
        public string billerFetchRequiremet { get; set; }
        public string billerPaymentExactness { get; set; }
        public string billerSupportBillValidation { get; set; }
        public string supportPendingStatus { get; set; }
        public string supportDeemed { get; set; }
        public string billerStatus { get; set; }
        public string billerTimeout { get; set; }

        [XmlArray("billerInputParams")]
        [XmlArrayItem("paramInfo")]
        public List<ParamInfo> billerInputParams { get; set; }

        [XmlArray("billerAdditionalInfo")]
        [XmlArrayItem("paramInfo")]
        public List<ParamInfo> billerAdditionalInfo { get; set; }

        [XmlElement("billerAmountOptions")]
        public string billerAmountOptions { get; set; }

        [XmlElement("billerPaymentModes")]
        public string billerPaymentModes { get; set; }

        [XmlArray("billerPaymentChannels")]
        [XmlArrayItem("paymentChannelInfo")]
        public List<PaymentChannelInfo> billerPaymentChannels { get; set; }

        public string billerDescription { get; set; }
        public string rechargeAmountInValidationRequest { get; set; }
        public string billerAdditionalInfoPayment { get; set; }
        public string planAdditionalInfo { get; set; }
        public string planMdmRequirement { get; set; }
        public string billerResponseType { get; set; }
        public string billerPlanResponseParams { get; set; }
        public string interchangeFeeCCF1 { get; set; }
    }

    public class ParamInfo
    {
        public string paramName { get; set; }
        public string dataType { get; set; }
        public string isOptional { get; set; }
        public string minLength { get; set; }
        public string maxLength { get; set; }
        public string regEx { get; set; }
        public string visibility { get; set; }
    }

    public class PaymentChannelInfo
    {
        public string paymentChannelName { get; set; }
        public string minAmount { get; set; }
        public string maxAmount { get; set; }
    }
}
