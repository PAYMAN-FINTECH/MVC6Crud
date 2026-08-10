namespace MVC6Crud.Models.App
{
    public class PMUsers
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public string? ProfileImageUrl { get; set; } // base64 or url
        public bool? IsKycCompleted { get; set; }
        public bool? IsActive { get; set; }
        public string? UserType { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? PinHash { get; set; }
        public string? PinResetOtp { get; set; }
        public DateTime? PinResetOtpExpiry { get; set; }
    }
}
