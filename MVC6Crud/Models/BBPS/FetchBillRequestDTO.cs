namespace MVC6Crud.Models.BBPS
{
    public class FetchBillRequestDTO
    {
        public string BillerId { get; set; }
        public string UserPhone { get; set; }
        public string Category { get; set; }

        public List<BBPSInputParamDTO> Inputs { get; set; }  // 🔥 dynamic
    }

    public class BBPSInputParamDTO
    {
        public string ParamName { get; set; }
        public string ParamValue { get; set; }
    }

    public class BBPSFetchResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }

        public string EnquiryReferenceId { get; set; }

        public string BillerResponse { get; set; }
        public string BillerResponse1 { get; set; }
        public string AdditionalInfo { get; set; }
        public string BillFetchResponse { get; set; }
    }
    public class BBPSPaymentRequest
    {
        public string BillerId { get; set; }
        public string UserPhone { get; set; }
        public double Amount { get; set; }
        public string Category { get; set; }
        public string Device { get; set; }
        public string EnquiryReferenceId { get; set; }
        public string BillFetchResponse { get; set; }
        public string BillerResponse { get; set; }
        public string AdditionalInfo { get; set; }
        public List<BBPSInputParamDTO> Inputs { get; set; }
    }

    public class BBPSParsedBill
    {
        public string CustomerName { get; set; }
        public string BillAmount { get; set; }
        public string DueDate { get; set; }
    }

}
