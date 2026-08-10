using Newtonsoft.Json;

namespace MVC6Crud.Models.PaymanApp
{
    public class VerifyAdharOTPResponse1
    {
        [JsonProperty("ref_id")]
        public long RefId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("care_of")]
        public string CareOf { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("dob")]
        public string Dob { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("split_address")]
        public SplitAddress SplitAddress { get; set; }

        [JsonProperty("year_of_birth")]
        public long YearOfBirth { get; set; }

        [JsonProperty("mobile_hash")]
        public string MobileHash { get; set; }

        [JsonProperty("photo_link")]
        public string PhotoLink { get; set; }
    }

    public partial class SplitAddress
    {
        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("dist")]
        public string Dist { get; set; }

        [JsonProperty("house")]
        public string House { get; set; }

        [JsonProperty("landmark")]
        public string Landmark { get; set; }

        [JsonProperty("pincode")]
        public long Pincode { get; set; }

        [JsonProperty("po")]
        public string Po { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("street")]
        public string Street { get; set; }

        [JsonProperty("subdist")]
        public string Subdist { get; set; }

        [JsonProperty("vtc")]
        public string Vtc { get; set; }
    }
}
