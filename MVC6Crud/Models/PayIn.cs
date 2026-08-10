using Microsoft.EntityFrameworkCore;
using XAct.Users;

namespace MVC6Crud.Models
{
    [Index(nameof(UserId))]
    public class PayIn
    {
        public Guid Id { get; set; }
        public string OrderId { get; set; }
        public string OrderRefNumber { get; set; }
        public string Currency { get; set; }
        public string UserId { get; set; }
        public string MobileNumber { get; set; }
        public int Amount { get; set; }
        public string Sttaus { get; set; }
        public int Refound { get; set; }
        public int GateWay { get; set; }
        public DateTime CreatedDate { get; set; }  
        public decimal PayInCommission { get; set; }
        public string DistributerUserId {  get; set; }
        public decimal PaymanCommission { get; set; }
        public decimal DistibuterCommission { get; set; }

        public string IssueBank { get; set; }
    }
}
