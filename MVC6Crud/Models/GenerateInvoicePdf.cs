using Microsoft.AspNetCore.Mvc.Rendering;

namespace MVC6Crud.Models
{
    public class GenerateInvoicePdf
    {
        public string InvoiceId { get; set; }
        public List<SelectListItem> Options { get; set; }
        public string SelectedOption { get; set; }

        public List<SelectListItem> Amount { get; set; }
        public string SelectedAmountOption { get; set; }
    }
}
