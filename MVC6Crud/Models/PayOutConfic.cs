namespace MVC6Crud.Models
{
    public class PayOutConfic
    {
        public Guid Id { get; set; }
        public bool SefexPay { get; set; }
        public bool PineLab { get; set; }
        public string UserId { get; set; }
        public DateTime CraetedDate { get; set; }
        public bool InstantPay { get; set; }
    }
}
