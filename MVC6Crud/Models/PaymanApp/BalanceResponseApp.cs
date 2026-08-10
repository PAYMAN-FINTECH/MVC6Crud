using System.Text.Json.Serialization;

namespace MVC6Crud.Models.PaymanApp
{
    public class BalanceResponseApp
    {
        [JsonPropertyName("statuscode")]
        public string StatusCode { get; set; }

        [JsonPropertyName("actcode")]
        public string ActCode { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("data")]
        public BalanceData Data { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("ipay_uuid")]
        public string IpayUuid { get; set; }

        [JsonPropertyName("orderid")]
        public string OrderId { get; set; }

        [JsonPropertyName("environment")]
        public string Environment { get; set; }

        [JsonPropertyName("internalCode")]
        public string InternalCode { get; set; }
    }
    public class BalanceData
    {
        [JsonPropertyName("bankId")]
        public int BankId { get; set; }

        [JsonPropertyName("bankProfileId")]
        public string BankProfileId { get; set; }

        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; }

        [JsonPropertyName("accountShortNumber")]
        public string AccountShortNumber { get; set; }

        [JsonPropertyName("balance")]
        public BalanceDetails Balance { get; set; }

        [JsonPropertyName("poolReferenceId")]
        public string PoolReferenceId { get; set; }

        [JsonPropertyName("pool")]
        public PoolDetails1 Pool { get; set; }
    }

    public class BalanceDetails
    {
        [JsonPropertyName("total")]
        public string Total { get; set; }

        [JsonPropertyName("lien")]
        public string Lien { get; set; }

        [JsonPropertyName("available")]
        public string Available { get; set; }
    }

    public class PoolDetails1
    {
        [JsonPropertyName("account")]
        public string Account { get; set; }

        [JsonPropertyName("openingBal")]
        public string OpeningBalance { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; }

        [JsonPropertyName("amount")]
        public string Amount { get; set; }

        [JsonPropertyName("closingBal")]
        public string ClosingBalance { get; set; }
    }
}
