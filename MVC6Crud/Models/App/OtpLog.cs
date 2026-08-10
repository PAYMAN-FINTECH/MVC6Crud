namespace MVC6Crud.Models.App
{
    public class OtpLog
    {
        public Guid Id { get; set; }
        public string Phone { get; set; }
        public string Otp { get; set; }
        public DateTime ExpiryTime { get; set; }
        public bool IsUsed { get; set; }
        public DateTime Created { get; set; }
    }

    public class AppLoginRequest
    {
        public string Phone { get; set; }
    }

    public class SmsResponse
    {
        public bool @return { get; set; }

        public string request_id { get; set; }

        public List<string> message { get; set; }
    }
}
