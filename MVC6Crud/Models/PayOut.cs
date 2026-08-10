namespace MVC6Crud.Models
{
    public class PayOut
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }

        public decimal Amount { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }
}
