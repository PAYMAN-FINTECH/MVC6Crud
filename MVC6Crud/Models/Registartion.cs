using System.ComponentModel.DataAnnotations;

namespace MVC6Crud.Models
{
    public class Registartion
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string UserName { get; set; }

        public string PhoneNumber { get; set; }
        public string CustomerType { get; set; }

        public string Margin { get; set; }
        public bool DMTAccessable { get; set; }
        public bool IsActive { get; set; }
        public int Id { get; set; }

        public string AdharNumber { get; set; }
        public bool BalanceTopUp { get; set; }

        public string MariginAccess { get; set; }
        public bool SrrEnable {  get; set; }
        public bool MasterMarginEnable { get; set; }
        public string MasterMargin { get; set; }

    }
}
