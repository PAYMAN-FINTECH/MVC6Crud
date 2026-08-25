using Microsoft.AspNetCore.Mvc;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.Airpay;
using Newtonsoft.Json.Linq;

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
            ILogger<AirpayController> logger)
        {
            _airpay = airpay;
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
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


                /*
                 * =============================================
                 * IMPORTANT PAYMAN SECTION
                 * =============================================
                 *
                 * In your real Payman application,
                 * DON'T trust request.Amount.
                 *
                 * Get the amount from your database.
                 *
                 * Example:
                 *
                 * var payin = await _db.fPayIns
                 *     .FirstOrDefaultAsync(x =>
                 *         x.TaxNumber == request.OrderId);
                 *
                 * decimal actualAmount = payin.Amount;
                 *
                 * Also retrieve your logged-in user here.
                 *
                 * For now this example uses the AJAX amount
                 * so that the Airpay module can run independently.
                 */


                string orderId =
                    GenerateOrderId();


                /*
                 * =============================================
                 * CUSTOMER INFORMATION
                 * =============================================
                 *
                 * Replace these values with your fUsers data.
                 */

                string buyerPhone =
                    User.FindFirst(
                        "UserPhone")?.Value
                    ?? "9999999999";

                string buyerEmail =
                    User.FindFirst(
                        "Email")?.Value
                    ?? "customer@example.com";

                string buyerFirstName =
                    User.FindFirst(
                        "FirstName")?.Value
                    ?? "Payman";

                string buyerLastName =
                    User.FindFirst(
                        "LastName")?.Value
                    ?? "Customer";


                var paymentRequest =
                    new AirpayPaymentRequest
                    {
                        BuyerEmail =
                            buyerEmail,

                        BuyerPhone =
                            buyerPhone,

                        BuyerFirstName =
                            buyerFirstName,

                        BuyerLastName =
                            buyerLastName,

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
                            orderId,

                        Amount =
                            request.Amount,

                        CurrencyCode =
                            "356",

                        IsoCurrency =
                            "INR",

                        CustomVar =
                            orderId,

                        /*
                         * Leave blank to show
                         * all enabled payment methods.
                         *
                         * Or use "upi", "pg", etc.
                         */
                        Chmod =
                            "",

                        TxnSubType =
                            ""
                    };


                /*
                 * Call Airpay
                 */
                AirpayPaymentResult result =
                    await _airpay
                        .CreatePaymentAsync(
                            paymentRequest);


                /*
                 * =============================================
                 * SAVE INITIATED TRANSACTION HERE
                 * =============================================
                 *
                 * Connect this to your Payman DB.
                 */


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

        public async Task<IActionResult> Success()
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
}
