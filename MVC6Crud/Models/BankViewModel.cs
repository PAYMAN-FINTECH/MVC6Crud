using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MVC6Crud.Models
{
    public class BankViewModel
    {
        public List<SelectListItem> Options { get; set; }
        public string CardNumber { get; set; }
        public string IfscCard { get; set; }
        public string SelectedOption { get; set; }
        public string AccountHolderName { get; set; }
        public string PhoneNumber { get; set; }
        public string BillAmount { get; set; }
        public string AvaliableAmount { get; set; }
        public string PayOutAmount { get; set; }
    }
}
