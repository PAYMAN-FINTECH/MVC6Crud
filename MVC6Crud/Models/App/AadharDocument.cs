using MVC6Crud.Models.PaymanApp;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVC6Crud.Models.App
{
    public class AadharDocument
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public byte[] AadhaarFile { get; set; }
        public string FileType { get; set; }

        // Optional: encrypted or raw
        public bool IsEncrypted { get; set; } = true;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
