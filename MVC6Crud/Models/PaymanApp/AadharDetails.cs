using MVC6Crud.Controllers;

namespace MVC6Crud.Models.PaymanApp
{
    public class AadharDetails
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Phone {  get; set; }
        public string? AadharNo {  get; set; }
        public string? AadharRefId { get; set; }
        public string? Otp {  get; set; }
        public string? Name {  get; set; }
        public string? Address {  get; set; }
        public bool? Status { get; set; }
        public DateTime? Created { get; set; }
    }
    public class SignedDocumentAgreement
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public byte[]? DocumentData { get; set; }
        public string? Status { get; set; }
        public int? Reference_Id { get; set; }
        public string? Verification_Id { get; set; }
        public int? Document_Id { get; set; }
        public string? Signed_Doc_Url { get; set; }
        public bool? Is_Notified { get; set; }
        public string? Name { get; set; }
        public string? Gender { get; set; }
        public string? Year_Of_Birth { get; set; }
        public string? Postal_Code { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? Serial_Number { get; set; }
        public string? Ip_Address { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public string? Signing_Time { get; set; }
        public string? Aadhaar_Last_Four_Digit { get; set; }
        public bool isVerifed { get; set; }
    }
}
