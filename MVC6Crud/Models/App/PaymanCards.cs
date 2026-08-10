namespace MVC6Crud.Models.App
{
    public class PaymanCards
    {
        public Guid Id { get; set; }
        public string BillerId { get; set; }
        public string UserPhone { get; set; }
        public string CardRegisterdPhone { get; set; }
        public string CardNumber { get; set; }
        public string CardLastFourDigits { get; set; } 
        public DateTime Craeted {  get; set; }
    }
}
