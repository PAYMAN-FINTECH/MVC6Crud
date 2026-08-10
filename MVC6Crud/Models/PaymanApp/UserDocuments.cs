namespace MVC6Crud.Models.PaymanApp
{
    public class UserDocuments
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Phone { get; set; }

        public string? PanCardNumber { get; set; }

        public byte[]? AadharFront { get; set; }

        public byte[]? AadharBack { get; set; }

        public byte[]? PanCard { get; set; }

        public DateTime? UploadedAt { get; set; }
    }
}
