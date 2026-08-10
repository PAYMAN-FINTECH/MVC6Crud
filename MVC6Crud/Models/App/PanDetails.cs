using System.ComponentModel.DataAnnotations;

namespace MVC6Crud.Models.App
{
    public class PanDetails
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public string UserPhone { get; set; }

        public string ReferenceId { get; set; }
        public string VerificationId { get; set; }

        public string Status { get; set; }        // SUCCESS / FAILED
        public string PanNumber { get; set; }     // BKSPJ1156M
        public string PanType { get; set; }       // Individual
        public string NameOnPan { get; set; }     // JURRA JANARDHAN
        public DateTime Dob { get; set; }
        public string Gender { get; set; }

        public string XmlFileUrl { get; set; }    // Temporary DigiLocker XML URL

        public bool IsVerified { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
