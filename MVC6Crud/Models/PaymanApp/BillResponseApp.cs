using Microsoft.CodeAnalysis.Differencing;

namespace MVC6Crud.Models.PaymanApp
{
    public class BillResponseApp
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        // Bill Details
        public string ConsumerName { get; set; }
        public string BillNumber { get; set; }
        public string BillDate { get; set; }
        public string DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal MinPayable { get; set; }
        public decimal CuurentOutStanding { get; set; }
        public string PaymentMode { get; set; }
        public string Param1 { set; get; }
        public string Param2 { set; get; }
        public string EnquiryReferenceId { get; set; }

        //Bill Avenue params

        public string BillerId { get; set; }

        public string BillerResponse { get; set; }
        public string AdddditionalInfo { get; set; }
        public string BillFetchResponse { get; set; }
        public string CustomerType { get; set; }


    }
}
