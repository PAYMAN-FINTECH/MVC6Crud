using Microsoft.AspNetCore.Mvc.Rendering;

namespace MVC6Crud.Models
{
    public class AddFailedrecords
    {
        public List<SelectListItem> Options { get; set; }
        public string OrderId { get; set; }
        public string OrderRefIf {  get; set; }

        public string Amount { get; set; }  
        public string selectedGateway { get; set; }
        public string SelectedOption { get; set; }
        public string Cardno {  get; set; }
        public string Cardtype { get; set; }
    }
}
