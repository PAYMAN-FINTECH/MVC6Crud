using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVC6Crud.Models
{
    public class AdharVerification
    {
        public Guid Id { get; set; }
        public string UserId {  get; set; }
        public string AdharNumber { get; set; }
        public int AdharRefId { get; set; }
        public int AdharOtp {  get; set; }
        public DateTime CreatedDate { get; set; }
        public bool AdharVerified { get; set; }

        [NotMapped]
        [Required]
        [DataType(DataType.Upload)]
        public IFormFile FrontImageFile { get; set; }  // First file

        [NotMapped]
        [Required]
        [DataType(DataType.Upload)]
        public IFormFile BackImageFile { get; set; }   // Second file

        [NotMapped]
        [Required]
        [DataType(DataType.Upload)]
        public IFormFile PANImageFile { get; set; }   // Second file

        public byte[] FrontImage { get; set; }
        public byte[] BackImage { get; set; }
        public string PanNumber { get; set; }
        public byte[] PanImage { get; set; }
        public string AdharName { get; set; }
        public string AdharAdress {  get; set; }

    }
}
