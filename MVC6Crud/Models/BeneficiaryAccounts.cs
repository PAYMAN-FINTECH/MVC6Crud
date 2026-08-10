using Microsoft.EntityFrameworkCore;

namespace MVC6Crud.Models
{
    [Index(nameof(MobileNumber), nameof(UserId), nameof(Id))]
    public class BeneficiaryAccounts
    {
        public Guid Id { get; set; }

        public string ContactId { get; set; }
        public string ContactName { get; set; }
        public string BeneId { get; set; }

        public string AccountNo { get; set; }
        public string IfscCode { get; set; }
        public string VerficationFlag { get; set; }
        public int Page { get; set; }

        public int Size { get; set; }

        public string Date { get; set; }
        public string MobileNumber { get; set; }
        public string EmailId { get; set; }
        public string TxnType { get; set; }
        public string Responce { get; set; }
        public bool IsActive { get; set; }
        public string MeId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UserId { get; set; }
        public int VerificationComm { get; set; }
    }


}
