using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.PaymanApp;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace MVC6Crud.Controllers
{
    public class PaymoveController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;
        private readonly PaymentService _ps;
        public PaymoveController(ApplicationDbContext context, IConfiguration configuration, DataUtils utils, PaymentService ps)
        {
            _context = context;
            _configuration = configuration;
            _dataUtils = utils;
            _ps = ps;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Initiate(string orderId, string amount, string actionType, string email, string phone, string custmobile, string custname, string custcard, string divice)
        {
            try
            {
                var hosted = _configuration["CoreVegaah:TerminalId"];
                var password = _configuration["CoreVegaah:Password"];
                var secret = _configuration["CoreVegaah:Secret"];
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
                    receiptUrl = "https://localhost:44384/FasTag/Receipt",
                };
                string userDataJson = System.Text.Json.JsonSerializer.Serialize(userDataObject);

                // Build payload json
                var payload = new
                {
                    terminalId
                    = hosted,
                    password = password,
                    signature = strHash,
                    paymentType = "1",
                    amount = formattedAmount,
                    currency = currency,
                    order = new
                    {
                        orderId = trackid,
                        description = "travel bills"
                    },
                    customer = new
                    {
                        customerEmail = email,
                        mobileNumber = custmobile,
                        billingAddressStreet = "101 Mahape",
                        billingAddressCity = "Mumbai",
                        billingAddressState = "Maharashtra",
                        billingAddressPostalCode = "400709",
                        billingAddressCountry = "IN"
                    },
                    //additionalDetails = new
                    //{
                    //    userData = userDataJson    // << serialized string
                    //}
                };

                string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);


                // IST time
                var istTime = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                var user = await _context.payManUsers
                    .FirstOrDefaultAsync(u => u.Phone == phone);

                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // ❗ Prevent duplicate orderId
                var existing = await _context.payManPayIns
                    .FirstOrDefaultAsync(x => x.TxnId == orderId);

                if (existing == null)
                {
                    var payIn = new PayManPayIn
                    {
                        UserId = user.Id,
                        UserPhone = phone,
                        TxnId = orderId,              // 🔑 orderId
                        Amount = Convert.ToDecimal(formattedAmount),
                        Gateway = "V",
                        Created = istTime,
                        Status = false,
                        Result = "PENDING",
                        Device = divice,
                        CreditCardHolderNum = custcard,
                        CreditCardHolderName = custname,
                        CardholderMobileNo = custmobile
                    };

                    _context.payManPayIns.Add(payIn);
                    await _context.SaveChangesAsync();
                }



                // Call payment service
                var targetUrl = await _ps.GetTargetUrlAsync(jsonPayload);

                return Json(new { success = true, url = targetUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Receipt(IFormCollection form)
        {
            try
            {
                // 🔑 Read encrypted response
                var encData = form["data"].ToString();

                if (string.IsNullOrWhiteSpace(encData))
                    return BadRequest("Invalid gateway response");

                encData = encData.Replace(" ", "+");

                // 🔐 Decrypt response
                string decryptedJson = DecryptResponse(encData);


                var user1 = new ErrorModel
                {
                    payload = "Paymove response",
                    agId = decryptedJson ?? "",
                    reqTime =  "",
                    respTime =  "",
                    requestId =  "",
                    uid = "",
                    statuscode = true,
                    jsonBody = ""
                };

                _context.errorModels.Add(user1);
                await _context.SaveChangesAsync();

                var paymentResponse =
                    JsonConvert.DeserializeObject<PaymentDecryptResponse>(decryptedJson);
                paymentResponse.Gatewayname = "CoreVegaah";

                var res = await _dataUtils.PayInDbCall(paymentResponse);

                // ✅ Safe redirect with encoding
                return Redirect(
                    $"https://paymanfintech.in/PayMan/PayStatus" +
                    $"?IsSuccess={res.IsSuccess}" +
                    $"&Amount={Uri.EscapeDataString(res.Amount.ToString())}" +
                    $"&TransactionId={Uri.EscapeDataString(res.TransactionId ?? "FAILED")}"
                );
            }
            catch (Exception ex)
            {
                // 🛑 Optional logging
                // _logger.LogError(ex, "Receipt processing failed");

                return Redirect(
                    $"https://paymanfintech.in/PayMan/PayStatus" +
                    $"?IsSuccess=false" +
                    $"&Amount=0" +
                    $"&TransactionId=FAILED"
                );
            }
        }

        private string DecryptResponse(string encryptedResponse)
        {
            string merchantKey = _configuration["CoreVegaah:Secret"];

            byte[] keyBytes = HexStringToByteArray(merchantKey);
            byte[] encryptedBytes = Convert.FromBase64String(encryptedResponse);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] decryptedBytes = decryptor.TransformFinalBlock(
                encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
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


    }
}
