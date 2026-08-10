using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace MVC6Crud.Data
{
    public class PaytmService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public PaytmService(ApplicationDbContext db, IConfiguration cfg)
        {
            _db = db;
            _config = cfg;
        }


        public async Task<string> CreateTransaction(string orderId, decimal amount, string custId)
        {
            string mid = "TvEYNH13757085326682";
            string key = "Pbfl_lLdr7bLm3cQ";

            // ✅ STRICT DICTIONARY FORMAT
            var body = new Dictionary<string, object>();

            body.Add("requestType", "Payment");
            body.Add("mid", mid);
            body.Add("websiteName", "DEFAULT");
            body.Add("orderId", orderId);
            body.Add("callbackUrl", "https://edu.payman.in/Paytm/Callback");

            var txnAmount = new Dictionary<string, string>();
            txnAmount.Add("value", amount.ToString("0.00"));
            txnAmount.Add("currency", "INR");

            body.Add("txnAmount", txnAmount);

            var userInfo = new Dictionary<string, string>();
            userInfo.Add("custId", custId);

            body.Add("userInfo", userInfo);

            // ✅ SERIALIZE ONCE (VERY IMPORTANT)
            string bodyJson = JsonConvert.SerializeObject(body, Formatting.None);

            // ✅ CHECKSUM
            string signature = Paytm.Checksum.generateSignature(bodyJson, key);

            var head = new Dictionary<string, string>();
            head.Add("signature", signature);

            var requestBody = new Dictionary<string, object>();
            requestBody.Add("body", body);
            requestBody.Add("head", head);

            string finalJson = JsonConvert.SerializeObject(requestBody, Formatting.None);

            string url = $"https://secure.paytmpayments.com/theia/api/v1/initiateTransaction?mid={mid}&orderId={orderId}";

            using var client = new HttpClient();

            var response = await client.PostAsync(url,
                new StringContent(finalJson, Encoding.UTF8, "application/json"));

            string result = await response.Content.ReadAsStringAsync();

            // 🔍 DEBUG (VERY IMPORTANT)
            Console.WriteLine("REQUEST BODY: " + bodyJson);
            Console.WriteLine("SIGNATURE: " + signature);
            Console.WriteLine("RESPONSE: " + result);

            return result;
        }

        public string GenerateSignature(string body, string merchantKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(merchantKey);
            var bodyBytes = Encoding.UTF8.GetBytes(body);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hash = hmac.ComputeHash(bodyBytes);
                return Convert.ToBase64String(hash);
            }
        }


    }
}
