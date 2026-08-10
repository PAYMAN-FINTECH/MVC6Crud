
using MVC6Crud.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Protocol;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using XAct.Library.Settings;

namespace MVC6Crud.Data
{
    public class JiopayService
    {
        //private string merchantId = "JP2001100063255";
        //private string secretKey = "41d2971e1fd444cca9364f44d4b5e3b6";
       // private string baseUrl = "https://uat.jiopay.co.in";
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _cfg;

        public JiopayService(ApplicationDbContext db, IConfiguration cfg)
        { 
            _db = db;
            _cfg = cfg;
        }
        public async Task<string> CreatePayment(decimal amount, string txnId, string email, string CMobile, string loadGateways)
        {
            string txnDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            string returnUrl = "";
            var merchantId = "";
            var baseUrl = ""; 

            if(loadGateways == "JioPay")
            {
                 returnUrl = "https://edu.paymanfintech.in/JioPay/Return";
                 merchantId = _cfg["JioPay:merchantId"];
                 baseUrl = _cfg["JioPay:baseUrl"];
            }
            else
            {
                 returnUrl = "https://fastag.payman.in/JioPay/Return";
                 merchantId = _cfg["JioPayFasTag:merchantId"];
                 baseUrl = _cfg["JioPayFasTag:baseUrl"];
            }

                string amountStr = amount.ToString("0.00", CultureInfo.InvariantCulture);

            var requestObj = new
            {
                amount = amountStr,
                currencyCode = "356",
                customerEmailID = email,
                customerMobileNo = CMobile,
                merchantId = merchantId,
                merchantTxnNo = txnId,
                payType = "0",
                returnURL = returnUrl,
                transactionType = "SALE",
                txnDate = txnDate
            };

            // 🔥 Generate HMAC-SHA256 Hash (Alphabetical Sorting)
            string secureHash = GenerateJiopayHash(requestObj, loadGateways);

            // Convert to JObject
            var finalRequest = JObject.FromObject(requestObj);
            finalRequest["secureHash"] = secureHash;

            // 🔥 Convert once (IMPORTANT)
            string finalJson = finalRequest.ToString(Formatting.None);

            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(
                    finalJson,
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(
                    baseUrl + "/pg/api/v2/initiateSale",
                    content);

                var result = await response.Content.ReadAsStringAsync();

                dynamic data = JsonConvert.DeserializeObject(result);

                //var bxx = baseUrl + "/tsp/pg/api/v2/initiateSale";
                if (data.responseCode != "R1000")
                    throw new Exception("Jiopay Error: " + data.responseDescription);

                return data.redirectURI + "?tranCtx=" + data.tranCtx;
            }
        }

        // 🔐 OFFICIAL HASH METHOD
        public string GenerateJiopayHash(object requestObject, string loadedGateway)
        {
            var secretKey = "";

            if (loadedGateway == "JioPay")
            {
                secretKey = _cfg["JioPay:secretKey"];
            }
            else
            {
                secretKey = _cfg["JioPayFasTag:secretKey"];
            }

                JObject jo = JObject.FromObject(requestObject);

            // Sort keys alphabetically
            var sortedProperties = jo.Properties()
                                     .OrderBy(p => p.Name)
                                     .ToList();

            StringBuilder valueString = new StringBuilder();

            foreach (var prop in sortedProperties)
            {
                valueString.Append(prop.Value.ToString());
            }

            string message = valueString.ToString();

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));

                StringBuilder builder = new StringBuilder();
                foreach (byte b in hashBytes)
                    builder.Append(b.ToString("x2")); // lowercase

                return builder.ToString();
            }
        }

        public async Task<object> GenerateDQR(decimal amount, string merchantRefNo, string email, string mobile, string loadGateways)
        {
            var merchantId = _cfg["JioPay:merchantId"];
            var baseUrl = _cfg["JioPay:baseUrl"];

            if (loadGateways == "JioPay")
            {
              //  returnUrl = "https://edu.paymanfintech.in/JioPay/Return";
                merchantId = _cfg["JioPay:merchantId"];
                baseUrl = _cfg["JioPay:baseUrl"];
            }
            else
            {
                // returnUrl = "https://fastag.payman.in/JioPay/Return";
                merchantId = _cfg["JioPayFasTag:merchantId"];
                baseUrl = _cfg["JioPayFasTag:baseUrl"];
            }
            string amountStr = amount.ToString("0.00", CultureInfo.InvariantCulture);

            var requestObj = new Dictionary<string, string>
    {
        { "amount", amountStr },
        { "currency", "356" },
        { "customerID", merchantRefNo },
        { "emailID", email ?? "" },
        { "invoiceDate", DateTime.Now.ToString("yyyyMMdd") },
        { "invoiceNo", merchantRefNo },
        { "merchantId", merchantId },
        { "merchantRefNo", merchantRefNo },
        { "mobileNo", mobile ?? "" },
        { "requestType", "UPIQR" }
    };

            // 🔐 Generate Secure Hash
            string secureHash = GenerateJiopayHash(requestObj, loadGateways);

            requestObj.Add("secureHash", secureHash);

            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(requestObj);

                var response = await client.PostAsync(
                    baseUrl + "/tsp/pg/api/generateQR",
                    content);

                var result = await response.Content.ReadAsStringAsync();

                dynamic data = JsonConvert.DeserializeObject(result);

                if (data.respHeader.returnCode != 200)
                    throw new Exception("JioPay QR Error: " + data.respHeader.desc);

                string json = JsonConvert.SerializeObject(requestObj);

                return new
                {
                    upiQR = data.respBody.upiQR,
                    merchantRefNo = merchantRefNo
                };
            }
        }

    }

}
