using Microsoft.AspNetCore.Identity;

namespace MVC6Crud.Models.PaymanApp
{
    public class AgreementPage
    {
        public string UserPhone {  get; set; } 
        public string UserName { get; set; }

        public string PanNumber { get; set; }
        public string AadhaarNumber { get; set; }

        public string Address { get; set; }
    }
}
