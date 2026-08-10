using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace MVC6Crud.Models
{
    [Index(nameof(UserId), nameof(Password), nameof(Name))]
    public class User
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

    }
}
