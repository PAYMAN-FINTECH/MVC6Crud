using Newtonsoft.Json;

namespace MVC6Crud.Models
{
    public class VerifyRequestModel
    {
        public string X_VERIFY { get; set; }
        public string base64 { get; set; }
        public string TransactionId { get; set; }
        public string MERCHANTID { get; set; }
        // Add other properties from the request if needed
        public string CustomerNo { get; set; }
        public string SelectedGateWay { get; set; }
    }
    public class BinLookupResponse
    {
        [JsonProperty("number")]
        public NumberInfo Number { get; set; }

        [JsonProperty("scheme")]
        public string Scheme { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("brand")]
        public string Brand { get; set; }

        [JsonProperty("country")]
        public CountryInfo Country { get; set; }

        [JsonProperty("bank")]
        public BankInfo Bank { get; set; }

        public class NumberInfo
        {
            // Add any properties if needed
        }

        public class CountryInfo
        {
            [JsonProperty("numeric")]
            public string Numeric { get; set; }

            [JsonProperty("alpha2")]
            public string Alpha2 { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("emoji")]
            public string Emoji { get; set; }

            [JsonProperty("currency")]
            public string Currency { get; set; }

            [JsonProperty("latitude")]
            public double Latitude { get; set; }

            [JsonProperty("longitude")]
            public double Longitude { get; set; }
        }

        public class BankInfo
        {
            [JsonProperty("name")]
            public string Name { get; set; }
        }
    }

}
