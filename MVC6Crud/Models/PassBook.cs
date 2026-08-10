using Razorpay.Api;

namespace MVC6Crud.Models
{
    public class PassBook
    {
        public string TransectionId { get; set; }
        public string UserId { get; set; }
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string AccountNo { get; set; }
        public string Status { get; set; }
        public string AccountHolderName { get; set; }
        public string txtType { get; set; }
        public string commission { get; set; }
        public string GateWayType { get; set; }
        public string CreatedDate { get; set; }
        public string IfscCode { get; set; }
    }
}
