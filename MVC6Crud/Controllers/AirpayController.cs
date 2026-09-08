using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.Airpay;
using MVC6Crud.Models.PaymanApp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XAct.Library.Settings;
using XAct.Users;

namespace MVC6Crud.Controllers
{
    public class AirpayController : Controller
    {

        private readonly AirpayService _airpay;

        private readonly ILogger<AirpayController>
            _logger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;

        public AirpayController(
            AirpayService airpay,
            ILogger<AirpayController> logger, ApplicationDbContext db, DataUtils dataUtils)
        {
            _airpay = airpay;
            _logger = logger;
            _context = db;
            _dataUtils = dataUtils;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> AirpayInit(decimal amount, string userPhone, string CName, string CMobile, string CCard, string Cemail, string gateway, string divice)
        {
            string orderId =
                    GenerateOrderId();

            // IST time
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == userPhone);

            var existing = await _context.payManPayIns
                   .FirstOrDefaultAsync(x => x.TxnId == orderId);

            if (existing == null)
            {
                var payIn = new PayManPayIn
                {
                    UserId = user.Id,
                    UserPhone = userPhone,
                    TxnId = orderId,              // 🔑 orderId
                    Amount = amount,
                    Gateway = gateway,
                    Created = istTime,
                    Email =  Cemail,
                    Status = false,
                    Result = "PENDING",
                    Device = divice,
                    CreditCardHolderNum = CCard,
                    CreditCardHolderName = CName,
                    CardholderMobileNo = CMobile
                };

                _context.payManPayIns.Add(payIn);
                await _context.SaveChangesAsync();
            }

            var model = new AirpaySessionModel
            {
                Amount = amount,
                CMobile = CMobile,
                Cemail = Cemail,
                CName = CName,
                userPhone = userPhone,
                CCard = CCard,
                gateway = gateway,
                orderId = orderId
            };

            return View(model);
        }

        // =====================================================
        // AJAX PAYMENT INITIATION
        // POST /Airpay/Initiate
        // =====================================================

        public async Task<IActionResult> Initiate(
            [FromBody] AirpayInitiateRequest request)
        {
            try
            {
                var existingPayIn1 = await _context.payManPayIns
               .FirstOrDefaultAsync(t =>
                   t.TxnId == request.orderId);

                if (request == null)
                {
                    return BadRequest(
                        new
                        {
                            success = false,
                            message =
                                "Invalid payment request."
                        });
                }


                if (request.Amount <= 0)
                {
                    return BadRequest(
                        new
                        {
                            success = false,
                            message =
                                "Invalid payment amount."
                        });
                }

                string buyerPhone = existingPayIn1.CardholderMobileNo?? "9849800697";

                string buyerEmail = existingPayIn1.Email ?? "jurrjanardhan@gmail.com";

                string buyerFirstName = existingPayIn1.CreditCardHolderName?? "jurra";

                //string buyerLastName =
                //    User.FindFirst(
                //        "LastName")?.Value
                //    ?? "janardhan";


                var paymentRequest =
                    new AirpayPaymentRequest
                    {
                        BuyerEmail =
                            buyerEmail,

                        BuyerPhone =
                            buyerPhone,

                        BuyerFirstName =
                            buyerFirstName,

                        //BuyerLastName =
                        //    buyerLastName,

                        BuyerAddress =
                            "India",

                        BuyerCity =
                            "Hyderabad",

                        BuyerState =
                            "Telangana",

                        BuyerCountry =
                            "India",

                        BuyerPinCode =
                            "500001",

                        OrderId =
                            request.orderId,

                        Amount =
                            request.Amount,

                        CurrencyCode =
                            "356",

                        IsoCurrency =
                            "INR",

                        CustomVar =
                            request.orderId,

                        /*
                         * Leave blank to show
                         * all enabled payment methods.
                         *
                         * Or use "upi", "pg", etc.
                         */
                        Chmod =
                            "",

                        TxnSubType =
                            "",
                        ReturnUrl =
            "https://edu.paymanfintech.in/Airpay/Success"
                    };


                /*
                 * Call Airpay
                 */
                AirpayPaymentResult result =
                    await _airpay
                        .CreatePaymentAsync(
                            paymentRequest);


                return Ok(
                    new
                    {
                        success = true,

                        orderId =
                            result.OrderId,

                        paymentUrl =
                            result.PaymentUrl,

                        merchantId =
                            result.MerchantId,

                        privateKey =
                            result.PrivateKey,

                        encData =
                            result.EncData,

                        checksum =
                            result.Checksum
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Airpay payment initiation failed.");

                return StatusCode(
                    500,
                    new
                    {
                        success = false,

                        message =
                            "Unable to initiate Airpay payment.",

                        error =
                            ex.Message
                    });
            }
        }


        // =====================================================
        // AIRPAY CALLBACK
        // POST /Airpay/Response
        // =====================================================

        public async Task<IActionResult> Response()
        {
            try
            {
                var user1 = new ErrorModel
                {
                    payload = "Airpay",
                    agId = "",
                    reqTime = "",
                    respTime = "",
                    requestId = "",
                    uid = "",
                    statuscode = true,
                    jsonBody = ""
                };

                _context.errorModels.Add(user1);
                await _context.SaveChangesAsync();

                string encryptedResponse =
                    Request.Form["response"]
                        .ToString();

                var user11 = new ErrorModel
                {
                    payload = "Airpay1",
                    agId = encryptedResponse,
                    reqTime = "",
                    respTime = "",
                    requestId = "",
                    uid = "",
                    statuscode = true,
                    jsonBody = ""
                };

                _context.errorModels.Add(user11);
                await _context.SaveChangesAsync();

                string customerVpa =
                    Request.Form["CUSTOMERVPA"]
                        .ToString();


                if (string.IsNullOrWhiteSpace(
                    encryptedResponse))
                {
                    ViewBag.Message =
                        "Empty Airpay response.";

                    return View("Error");
                }


                /*
                 * Decrypt Airpay response
                 */

                string decrypted =
                    _airpay.DecryptResponse(
                        encryptedResponse);
                var user111 = new ErrorModel
                {
                    payload = "Airpay11",
                    agId = decrypted,
                    reqTime = "",
                    respTime = "",
                    requestId = "",
                    uid = "",
                    statuscode = true,
                    jsonBody = ""
                };

                _context.errorModels.Add(user111);
                await _context.SaveChangesAsync();


                _logger.LogInformation(
                    "Airpay callback decrypted.");


                JObject root =
                    JObject.Parse(
                        decrypted);


                JObject? data =
                    root["data"] as JObject;

                var user1111 = new ErrorModel
                {
                    payload = "Airpay111",
                    agId = data.ToString(),
                    reqTime = "",
                    respTime = "",
                    requestId = "",
                    uid = "",
                    statuscode = true,
                    jsonBody = ""
                };

                _context.errorModels.Add(user1111);
                await _context.SaveChangesAsync();


                if (data == null)
                {
                    ViewBag.Message =
                        "Invalid Airpay response.";

                    return View("Error");
                }


                string orderId =
                    data["orderid"]?
                        .ToString()
                    ?? "";

                string airpayTransactionId =
                    data["ap_transactionid"]?
                        .ToString()
                    ?? "";

                string amount =
                    data["amount"]?
                        .ToString()
                    ?? "";

                string transactionStatus =
                    data["transaction_status"]?
                        .ToString()
                    ?? "";

                string message =
                    data["message"]?
                        .ToString()
                    ?? "";

                string secureHash =
                    data["ap_SecureHash"]?
                        .ToString()
                    ?? data["ap_securehash"]?
                        .ToString()
                    ?? "";

                string customVar =
                    data["custom_var"]?
                        .ToString()
                    ?? data["customvar"]?
                        .ToString()
                    ?? "";

                string chmod =
                    data["chmod"]?
                        .ToString()
                    ?? "";


                /*
                 * Validate mandatory data.
                 */

                if (string.IsNullOrWhiteSpace(orderId) ||
                    string.IsNullOrWhiteSpace(
                        airpayTransactionId) ||
                    string.IsNullOrWhiteSpace(amount) ||
                    string.IsNullOrWhiteSpace(
                        transactionStatus) ||
                    string.IsNullOrWhiteSpace(
                        secureHash))
                {
                    ViewBag.Message =
                        "Incomplete Airpay response.";

                    return View("Error");
                }


                /*
                 * Airpay uses 402 for cancelled/
                 * not processed payment.
                 */

                if (transactionStatus == "402")
                {
                    message =
                        "TRANSACTION CANCELLED";
                }


                /*
                 * =============================================
                 * CRC32 VALIDATION
                 * =============================================
                 */

                bool hashValid =
                    _airpay.ValidateResponseHash(
                        orderId,
                        airpayTransactionId,
                        amount,
                        transactionStatus,
                        message,
                        secureHash,
                        chmod.Equals(
                            "upi",
                            StringComparison.OrdinalIgnoreCase)
                            ? customerVpa
                            : "");


                if (!hashValid)
                {
                    _logger.LogWarning(
                        "Airpay secure hash mismatch for {OrderId}",
                        orderId);

                    ViewBag.OrderId =
                        orderId;

                    ViewBag.Message =
                        "SECURE HASH MISMATCH";

                    ViewBag.Status =
                        "FAILED";

                    return View("Error");
                }


                /*
                 * =============================================
                 * SUCCESS
                 * =============================================
                 */

                if (transactionStatus == "200")
                {
                    /*
                     * =========================================
                     * PAYMAN DB UPDATE GOES HERE
                     * =========================================
                     *
                     * 1. Find transaction using orderId.
                     *
                     * 2. Verify amount.
                     *
                     * 3. Check transaction isn't already SUCCESS.
                     *
                     * 4. Save Airpay transaction ID.
                     *
                     * 5. Set SUCCESS.
                     *
                     * 6. Credit wallet/pay-in only once.
                     */


                    ViewBag.OrderId =
                        orderId;

                    ViewBag.AirpayTransactionId =
                        airpayTransactionId;

                    ViewBag.Amount =
                        amount;

                    ViewBag.Message =
                        message;

                    ViewBag.Status =
                        "SUCCESS";

                    ViewBag.CustomVar =
                        customVar;

                    return View("Result");
                }


                /*
                 * =============================================
                 * FAILED / PENDING / CANCELLED
                 * =============================================
                 */

                ViewBag.OrderId =
                    orderId;

                ViewBag.AirpayTransactionId =
                    airpayTransactionId;

                ViewBag.Amount =
                    amount;

                ViewBag.Message =
                    message;

                ViewBag.Status =
                    transactionStatus;

                ViewBag.CustomVar =
                    customVar;


                return View("Result");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Airpay callback processing failed.");

                ViewBag.Message =
                    "Unable to process Airpay response.";

                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Success()
        {
            try
            {

                string encryptedResponse =
                    Request.Form["response"]
                        .ToString();


                ///*
                // * Decrypt Airpay response
                // */

                string decrypted =
                    _airpay.DecryptResponse(
                        encryptedResponse);
                var user111 = new ErrorModel
                {
                    payload = "Airpay11",
                    agId = decrypted,
                    reqTime = "",
                    respTime = "",
                    requestId = "",
                    uid = "",
                    statuscode = true,
                    jsonBody = ""
                };

                _context.errorModels.Add(user111);
                await _context.SaveChangesAsync();


                _logger.LogInformation(
                    "Airpay callback decrypted.");


                JObject root =
                    JObject.Parse(
                        decrypted);


                JObject? data =
                    root["data"] as JObject;

                var user1111 = new ErrorModel
                {
                    payload = "Airpay111",
                    agId = data.ToString(),
                    reqTime = "",
                    respTime = "",
                    requestId = "",
                    uid = "",
                    statuscode = true,
                    jsonBody = ""
                };

                _context.errorModels.Add(user1111);
                await _context.SaveChangesAsync();

                //   var data = "{    \"merchant_id\": \"369932\",    \"orderid\": \"PM2608311418117929\",    \"ap_transactionid\": \"2062624719\",    \"txn_mode\": \"LIVE\",    \"chmod\": \"upi\",    \"amount\": \"1.00\",    \"currency_code\": \"356\",    \"transaction_status\": 200,    \"transaction_payment_status\": \"SUCCESS\",    \"message\": \"Success\",    \"customer_name\": \"JURRA JANARDHAN\",    \"customer_phone\": \"9849800697\",    \"customer_email\": \"JURRJANARDHAN@GMAIL.COM\",    \"transaction_type\": 320,    \"risk\": \"0\",    \"customvar\": \"PM2608311418117929\",    \"transaction_time\": \"31-08-2026 19:48:51\",    \"bank_response_msg\": \"SUCCESS\",    \"customer_vpa\": \"janardhan.jurra@axl\",    \"charge_type\": \"SAVINGS\",    \"ap_securehash\": \"4182621015\"  }";


                AirpayResponseModel11 airpayResponse =
        JsonConvert.DeserializeObject<AirpayResponseModel11>(
            data.ToString());


                var res = await _dataUtils.AirPayInDbCall(airpayResponse);



                string jsonData1 = System.Text.Json.JsonSerializer.Serialize(res);

                var user1a11 = new ErrorModel
                {
                    payload = "air Pay response5",
                    agId = "",
                    reqTime = "",
                    respTime = jsonData1,
                    requestId = "",
                    uid = "",
                    statuscode = true,
                    jsonBody = ""
                };

                _context.errorModels.Add(user1a11);
                await _context.SaveChangesAsync();

                return Redirect(
                       $"https://paymanfintech.in/PayMan/PayStatus" +
                       $"?IsSuccess={res.IsSuccess}" +
                       $"&Amount={Uri.EscapeDataString(res.Amount.ToString())}" +
                       $"&TransactionId={Uri.EscapeDataString(res.TransactionId ?? "FAILED")}"
                   );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Airpay callback processing failed.");

                ViewBag.Message =
                    "Unable to process Airpay response.";

                return View("Error");
            }

        }
private string GenerateOrderId()
        {
            /*
             * Airpay order ID max length is documented
             * as 30 alphanumeric characters.
             */

            return
                "PM" +
                DateTime.UtcNow.ToString(
                    "yyMMddHHmmss") +
                Random.Shared.Next(
                    1000,
                    9999);
        }
    }

    public class AirpaySessionModel
    {
        public decimal Amount { get; set; }

        public string userPhone { get; set; }

        public string Cemail { get; set; }

        public string CName { get; set; }

        // Mask or last 4 digits only (PCI safe)
        public string CCard { get; set; }

        public string CMobile { get; set; }
        public string divice { get; set; }
        public string gateway { get; set; }
        public string orderId { get; set; }
    }

    public class AirpayResponseModel11
    {
        // =====================================================
        // COMMON AIRPAY FIELDS
        // =====================================================

        [JsonProperty("merchant_id")]
        public string? MerchantId { get; set; }

        [JsonProperty("orderid")]
        public string? OrderId { get; set; }

        [JsonProperty("ap_transactionid")]
        public string? AirpayTransactionId { get; set; }

        [JsonProperty("txn_mode")]
        public string? TxnMode { get; set; }

        [JsonProperty("chmod")]
        public string? Chmod { get; set; }

        [JsonProperty("amount")]
        public string? Amount { get; set; }

        [JsonProperty("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonProperty("transaction_status")]
        public int TransactionStatus { get; set; }

        [JsonProperty("transaction_payment_status")]
        public string? TransactionPaymentStatus { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("customer_name")]
        public string? CustomerName { get; set; }

        [JsonProperty("customer_phone")]
        public string? CustomerPhone { get; set; }

        [JsonProperty("customer_email")]
        public string? CustomerEmail { get; set; }

        [JsonProperty("transaction_type")]
        public int TransactionType { get; set; }

        [JsonProperty("risk")]
        public string? Risk { get; set; }

        [JsonProperty("customvar")]
        public string? CustomVar { get; set; }

        [JsonProperty("transaction_time")]
        public string? TransactionTime { get; set; }


        // =====================================================
        // UPI FIELDS
        // =====================================================

        [JsonProperty("customer_vpa")]
        public string? CustomerVpa { get; set; }

        [JsonProperty("charge_type")]
        public string? ChargeType { get; set; }


        // =====================================================
        // CARD / PG FIELDS
        // =====================================================

        [JsonProperty("card_scheme")]
        public string? CardScheme { get; set; }

        [JsonProperty("card_number")]
        public string? CardNumber { get; set; }

        [JsonProperty("card_country")]
        public string? CardCountry { get; set; }

        [JsonProperty("card_type")]
        public string? CardType { get; set; }

        [JsonProperty("bank_name")]
        public string? BankName { get; set; }


        // =====================================================
        // BANK RESPONSE
        // =====================================================

        [JsonProperty("bank_response_msg")]
        public string? BankResponseMessage { get; set; }


        // =====================================================
        // AIRPAY SECURE HASH
        // =====================================================

        [JsonProperty("ap_securehash")]
        public string? ApSecureHash { get; set; }
    }
}
