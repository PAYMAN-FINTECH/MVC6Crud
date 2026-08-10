using System.ComponentModel.DataAnnotations;

namespace MVC6Crud.Models.App
{
    public class AadharKycDetails
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserPhone { get; set; }

        public long ReferenceId { get; set; }
        public string VerificationId { get; set; }
        public string Status { get; set; }

        public string Name { get; set; }
        public string AadhaarMasked { get; set; }
        public DateTime? Dob { get; set; }
        public string? Gender { get; set; }
        public string? CareOf { get; set; }

        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? District { get; set; }
        public string? Pincode { get; set; }

        public string? PhotoBase64 { get; set; }
        public string? XmlFileUrl { get; set; }

        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
