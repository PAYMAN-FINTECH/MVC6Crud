namespace MVC6Crud.Models
{
    public class AddBeneficiaryAccounts
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string EmailId { get; set; }
        public string MobileNumber { get; set; }
        public string MeId { get; set; }
        public string AccountNo { get; set; }
        public string IfscCode { get; set; }
        public string AccountHolderName { get; set; }
        public string TxnType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
