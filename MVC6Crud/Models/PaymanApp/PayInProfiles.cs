namespace MVC6Crud.Models.PaymanApp
{
    public class PayInProfiles
    {
        public Guid Id { get; set; }
        public string UserPhone { get; set; }
        public string Mobile { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CardNumber { get; set; }
    }
}
