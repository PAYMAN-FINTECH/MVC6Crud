using Microsoft.AspNetCore.Mvc.Rendering;
using MVC6Crud.Models.PaymanApp;

namespace MVC6Crud.Models
{
    public class AddBeneficiaryAccountsDTO
    {
        public Guid Id { get; set; }
        public string ContactId { get; set; }
        public string ContactName { get; set; }
        public string BeneId { get; set; }

        public string AccountNo { get; set; }
        public string IfscCode { get; set; }
        public string VerficationFlag { get; set; }
        public int Page { get; set; }
        public List<SelectListItem> Options { get; set; }
        public string SelectedOption { get; set; }

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
        public List<BeneficiaryAccounts> BeneficiaryAccounts { get; set; }
        public string AvaliableAmount { get; set; }
        public string SefexFinalBalance { get; set; }
        public List<PayManBeneficiaryAccounts> PayManBeneficiaryAccounts { get; set; }
        public int? PayOutMinAmount { get; set; }
        public int? PayOutMaxAmount { get; set; }
        public int? MinBalanceAvl { get; set; }
    }

    public class RisterDTO
    {
        public List<UserDTO> registartions { get; set; }
       
    }

    public class UserDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }

        public string Role { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal Margin { get; set; }

        public bool DMTAccessable { get; set; }
        public bool IsActive { get; set; }
        public string DistributeruserId { get; set; }
        public decimal DistributerMarigin { get; set; }
        public bool BalanceTopUp { get; set; }
        public bool ChangePassword { get; set; }
        public bool SrrEnable { get; set; }
        public bool MasterMarginEnable { get; set; }
        public decimal? MasterMargin { get; set; }
        public List<PayIn> payIns { get; set; }
        public List<PayOutTransectionDetails> payOutTransectionDetails { get; set; }

    }
}
