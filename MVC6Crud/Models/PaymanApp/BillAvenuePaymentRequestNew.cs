using System.Xml.Serialization;

namespace MVC6Crud.Models.PaymanApp
{
    [XmlRoot("billAvenuePaymentRequestNew")]
    public class BillAvenuePaymentRequestNew
    {
        public string agentId { get; set; }
        public BillAvenueAgentDeviceInfo agentDeviceInfo { get; set; }
        public BillAvenueCustomerInfo customerInfo { get; set; }
        public string billerId { get; set; }
        public BillAvenueInputParams inputParams { get; set; }
        public BillAvenueBillerResponsenenww billerResponse { get; set; }
        public BillAvenueAdditionalInfonew additionalInfo { get; set; }
        public string paymentRefId { get; set; }
        public BillAvenueAmountInfo amountInfo { get; set; }
        public BillAvenuePaymentMethod paymentMethod { get; set; }
        public BillAvenuePaymentInfo paymentInfo { get; set; }
        public bool billerAdhoc {  get; set; }
    }

    public class BillAvenueAgentDeviceInfo
    {
        public string ip { get; set; }
        public string initChannel { get; set; }
        public string imei { get; set; }
        public string app { get; set; }
        public string os { get; set; }
        public string mac { get; set; }
    }

    public class BillAvenueCustomerInfo
    {
        [XmlElement("REMITTER_NAME")]
        public string RemitterName { get; set; }
        public string customerMobile { get; set; }
        public string customerEmail { get; set; }
        public string customerAdhaar { get; set; }
        public string customerPan { get; set; }
    }

    public class BillAvenueInputParams
    {
        [XmlElement("billAvenueInput")]
        public List<BillAvenueInput> Inputs { get; set; }
    }

    public class BillAvenueInput
    {
        public string paramName { get; set; }
        public string paramValue { get; set; }
    }

    public class BillAvenueBillerResponsenenww
    {
        public string billAmount { get; set; }
        public string billDate { get; set; }
        public string billNumber { get; set; }
        public string billPeriod { get; set; }
        public string customerName { get; set; }
        public string dueDate { get; set; }
    }

    public class BillAvenueAdditionalInfonew
    {
        [XmlElement("billAvenueInfonew")]
        public List<BillAvenueInfonew> Infos { get; set; }
    }

    public class BillAvenueInfonew
    {
        public string infoName { get; set; }
        public string infoValue { get; set; }
    }

    public class BillAvenueAmountInfo
    {
        public string amount { get; set; }
        public string currency { get; set; }
        public string custConvFee { get; set; }
    }

    public class BillAvenuePaymentMethod
    {
        public string paymentMode { get; set; }
        public string quickPay { get; set; }
        public string splitPay { get; set; }
    }

    public class BillAvenuePaymentInfo
    {
        [XmlElement("billAvenueInfonew")]
        public List<BillAvenueInfonew> Infos { get; set; }
    }
}
