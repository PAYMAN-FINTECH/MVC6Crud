namespace MVC6Crud.Models.App
{
    public class BbpsTransaction
    {
        public Guid Id { get; set; }
        public string UserPhone { get; set; }

        public string ResponseCode { get; set; }
        public string ResponseReason { get; set; }
        public string TxnRefId { get; set; }
        public string ApprovalRefNumber { get; set; }
        public string TxnRespType { get; set; }
        public decimal CustConvFee { get; set; }
        public decimal RespAmount { get; set; }
        public string RespBillDate { get; set; }
        public string RespCustomerName { get; set; }
        public string RespDueDate { get; set; }

        // InputParams stored as JSON string (e.g., "[{\"paramName\":\"Mobile Number\",\"paramValue\":\"9652724937\"}]")
        public string InputParamsJson { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool Status { get; set; }
        public string StatusCode { get; set; }
    }

}
