namespace MVC6Crud.Models.PaymanApp
{
    public class BillInstantPayResponse
    {
        public string StatusCode { get; set; }
        public string ActCode { get; set; }
        public string Status { get; set; }
        public BillData1 Data { get; set; }
        public string Timestamp { get; set; }
        public string IpayUuid { get; set; }
        public string OrderId { get; set; }
        public string Environment { get; set; }
        public string InternalCode { get; set; }
    }

    public class BillData1
    {
        public string EnquiryReferenceId { get; set; }
        public string CustomerName { get; set; }
        public string BillNumber { get; set; }
        public string BillPeriod { get; set; }
        public string BillDate { get; set; }
        public string BillDueDate { get; set; }
        public string BillAmount { get; set; }
        public List<CustomerParamDetail1> CustomerParamsDetails { get; set; }
        public List<BillDetail1> BillDetails { get; set; }
        public List<AdditionalDetail1> AdditionalDetails { get; set; }
    }

    public class CustomerParamDetail1
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class BillDetail1
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class AdditionalDetail1
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
