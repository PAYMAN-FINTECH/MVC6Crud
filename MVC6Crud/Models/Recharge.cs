using Newtonsoft.Json;

namespace MVC6Crud.Models
{
    public class Recharge
    {
    }
    public class Circle
    {
        [JsonProperty("circlecode")]
        public string CircleCode { get; set; }

        [JsonProperty("circlename")]
        public string CircleName { get; set; }
    }
    public class ResponseData
    {
        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("SuccessMessage")]
        public string SuccessMessage { get; set; }

        [JsonProperty("data")]
        public List<Circle> Data { get; set; }
    }
    public class Operator
    {
        public string OperatorCode { get; set; }
        public string OperatorName { get; set; }
    }

    public class ServiceType
    {
        public string ServiceTypeName { get; set; }
        public List<Operator> Data { get; set; }
    }

    public class ApiResponse
    {
        public string Status { get; set; }
        public string SuccessMessage { get; set; }
        public List<ServiceType> Data { get; set; }
    }

}
