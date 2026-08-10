using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using XAct.Users;
using static System.Net.Mime.MediaTypeNames;

namespace MVC6Crud.Models
{
    [Index(nameof(userId))]
    public class PayOutTransectionDetails
    {
        public Guid Id { get; set; }
        public string payoutId { get; set; }
        public string mobileNo { get; set; }
        public string customerId { get; set; }
        public string userId { get; set; }
        public string beneId { get; set; }
        public string txnAmount { get; set; }
        public string accountNo { get; set; }
        public string ifscCode { get; set; }
        public string accountHolderName { get; set; }
        public string txnType { get; set; }
        public string aggregatorId { get; set; }
        public string txnStatus { get; set; }
        public string spkRefNo { get; set; }
        public string bankStatus { get; set; }
        public string statusCode { get; set; }
        public string statusDesc { get; set; }
        public string orderRefNo { get; set; }
        public string customerName { get; set; }
        public string aggregtorName { get; set; }
        public string emailId { get; set; }
        public int PayoutCommission { get; set; }
        public DateTime? CreatedDate { get; set; }

    }
}


