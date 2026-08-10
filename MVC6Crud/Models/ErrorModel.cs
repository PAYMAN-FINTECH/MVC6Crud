namespace MVC6Crud.Models
{
    public class ErrorModel
    {
        public Guid Id { get; set; }
        public string agId { get; set; }
        public string payload { get; set; }
        public string reqTime { get; set; }
        public string respTime { get; set; }
        public string requestId { get; set; }
        public string uid { get; set; }
        public bool statuscode {  get; set; }
        public string jsonBody { get; set; }

    }
}
