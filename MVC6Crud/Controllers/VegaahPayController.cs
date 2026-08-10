using Irony.Parsing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVC6Crud.Data;
using MVC6Crud.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Security.Cryptography;
using System.Text;

namespace MVC6Crud.Controllers
{
    public class VegaahPayController : Controller
    {
        private readonly IConfiguration _cfg;
        private readonly OperationService _op;
        private readonly PaymentService _ps;
        private readonly ApplicationDbContext _db;

        public VegaahPayController(IConfiguration cfg, OperationService op, PaymentService ps, ApplicationDbContext db)
        {
            _cfg = cfg; _op = op; _ps = ps; _db = db;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Initiate(string orderId, string amount, string actionType)
        {
            try
            {
                var hosted = _cfg["Vegaah:TerminalId"];
                var password = _cfg["Vegaah:Password"];
                var secret = _cfg["Vegaah:Secret"];
                var trackid = orderId;
                var currency = "INR";

                // Convert amount -> always 2 decimals
                decimal amt = Convert.ToDecimal(amount);
                string formattedAmount = amt.ToString("0.00");

                // Generate signature
                var strHash = CryptoHelperGateway.GenerateSignature(
                    trackid, hosted, password, secret, formattedAmount, currency);

                var userDataObject = new
                {
                    entry1 = "entry",
                    receiptUrl = "https://localhost:44384/VegaahPay/ProcessPostRequest",
                };
                string userDataJson = System.Text.Json.JsonSerializer.Serialize(userDataObject);

                // Build payload json
                var payload = new
                {
                    terminalId = hosted,
                    password = password,
                    signature = strHash,
                    paymentType = "1",
                    amount = formattedAmount,
                    currency = currency,
                    order = new
                    {
                        orderId = trackid,
                        description = "Purchase of product XYZ"
                    },
                    customer = new
                    {
                        customerEmail = "jurrajanardhan@gmail.com",
                        billingAddressStreet = "101 Mahape",
                        billingAddressCity = "Mumbai",
                        billingAddressState = "Maharashtra",
                        billingAddressPostalCode = "400709",
                        billingAddressCountry = "IN"
                    },
                    additionalDetails = new
                    {
                        userData = userDataJson    // << serialized string
                    }
                };

                string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);


                // Call payment service
                var targetUrl = await _ps.GetTargetUrlAsync(jsonPayload);

                return Json(new { success = true, url = targetUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        
        public async Task ProcessPostRequest()
        {
            using var reader = new StreamReader(Request.Body);
            string rawData = await reader.ReadToEndAsync();

            // Example data: termId=xxx&data=ENCRYPTED_STRING
            var keyValuePairs = System.Web.HttpUtility.ParseQueryString(rawData);

            string encryptedData = keyValuePairs["data"];

            if (string.IsNullOrEmpty(encryptedData))
                return; // log error

            // Replace spaces
            encryptedData = encryptedData.Replace(" ", "+");

            // 🔐 Now decrypt the response
            string decryptedJson = DecryptResponse(encryptedData);

        }

        private string DecryptResponse(string encryptedResponse)
        {
            string merchantKey = "d2d78842669f2e1df7a4536419d507a4f5ae8bfdf455450597e570d59faa2f4e"; // same as PHP merKey

            // Convert hex key to bytes
            byte[] keyBytes = HexStringToByteArray(merchantKey);

            // Step 1: Base64 decode
            byte[] encryptedBytes = Convert.FromBase64String(encryptedResponse);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }


        public static byte[] HexStringToByteArray(string hexString)
        {
            int length = hexString.Length;
            byte[] data = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
            {
                data[i / 2] = (byte)((GetHexValue(hexString[i]) << 4) + GetHexValue(hexString[i + 1]));
            }
            return data;
        }

        private static int GetHexValue(char hexChar)
        {
            if (hexChar >= '0' && hexChar <= '9')
                return hexChar - '0';

            if (hexChar >= 'A' && hexChar <= 'F')
                return hexChar - 'A' + 10;

            if (hexChar >= 'a' && hexChar <= 'f')
                return hexChar - 'a' + 10;

            throw new ArgumentException("Invalid hex character");
        }

        public String getEncryptedResponse(String jsonResponse, String merKey)
        {
            String encryptedResponse = "";
            try
            {
                // Convert merchant key to SecretKey
              //  SecretKey secretKey = new SecretKeySpec(hexStringToByteArray(merKey), "AES");
                // Encrypt and encode the JSON data
                //encryptedResponse = encryptAndEncode(jsonResponse, secretKey);
            }
            catch (Exception e)
            {
                // Handle exception
            }
            return encryptedResponse;
        }

        

        //[HttpPost]
        //public async Task<IActionResult> Initiate( string orderId,  string amount,  string actionType)
        //{
        //    try
        //    {
        //        var hosted = _cfg["Vegaah:TerminalId"];
        //        var password = _cfg["Vegaah:Password"];
        //        var secret = _cfg["Vegaah:Secret"];
        //        var trackid = "M11_Trackkjhjhjfddsffdslslkdlkdf";//_cfg["Vegaah:TrackId"];
        //        var currency = "INR";//_cfg["Vegaah:Currency"] ?? "SAR";

        //        // VERY IMPORTANT: Amount must have 2 decimals
        //        decimal amt = Convert.ToDecimal(amount);


        //        // compute signature
        //        var strHash = CryptoHelperGateway.GenerateSignature(trackid, hosted, password, secret, amt, currency);

        //        var sTransId = "";
        //        var token = "";
        //        var cardToken = "";
        //        var merchantIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        //        var metadata = "";

        //        JObject payload;

        //        switch (actionType)
        //        {
        //            case "1":
        //            case "4":
        //                payload = _op.GeneratePurchaseOrPreAuth(sTransId, token, cardToken, "IN", "First", "Last", "Addr", "City", "State", "400709", "9999999999", "user@example.com", orderId, hosted, password, secret, amount, currency, actionType, strHash, merchantIp, orderId, "", metadata);
        //                break;
        //            case "3":
        //            case "6":
        //            case "7":
        //                payload = _op.GenerateVoid(sTransId, "IN", "user@example.com", hosted, password, amount, currency, orderId, actionType, strHash, merchantIp, sTransId, "", metadata);
        //                break;
        //            case "10":
        //                payload = _op.GenerateInquiry(sTransId, "IN", "user@example.com", hosted, password, amount, currency, orderId, actionType, strHash, merchantIp, sTransId, "", metadata);
        //                break;
        //            case "12":
        //                payload = _op.GenerateTokenization(orderId, cardToken, "400709", "Addr", "City", "State", "user@example.com", "9999999999", token, "IN", hosted, password, amount, currency, sTransId, actionType, strHash, merchantIp, sTransId, "", metadata);
        //                break;
        //            default:
        //                return Json(new { success = false, message = "Unsupported action type" });
        //        }

        //        var jsonPayload = payload.ToString();

        //        //var initHistory = new PayInHistory
        //        //{
        //        //    ParentId = null,
        //        //    OrderId = orderId,
        //        //    UserId = User?.Identity?.Name ?? "guest",
        //        //    Amount = decimal.TryParse(amount, out var am) ? am : 0,
        //        //    Currency = currency,
        //        //    Status = "INITIATED",
        //        //    ActionType = actionType,
        //        //    RawResponse = jsonPayload
        //        //};
        //        //_db.PayInHistories.Add(initHistory);
        //        //await _db.SaveChangesAsync();

        //        var targetUrl = await _ps.GetTargetUrlAsync(jsonPayload);

        //        // We could parse transactionId and save as ParentId if available in response.
        //        //initHistory.RawResponse = jsonPayload;
        //        //await _db.SaveChangesAsync();

        //        return Json(new { success = true, url = targetUrl });
        //    }
        //    catch (Exception ex)
        //    {
        //        //Logger.Error("Initiate error: " + ex.ToString());
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}

    }
}
