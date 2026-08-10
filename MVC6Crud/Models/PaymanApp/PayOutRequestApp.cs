using System.ComponentModel.DataAnnotations;

namespace MVC6Crud.Models.PaymanApp
{
    public class PayOutRequestApp
    {
        public Guid Id { get; set; }
        public decimal amount { get; set; }
        public string UserPhone { get; set; }
    }
}
