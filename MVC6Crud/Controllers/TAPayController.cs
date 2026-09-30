using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.PaymanApp;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MVC6Crud.Controllers
{
    public class TAPayController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DataUtils _dataUtils;
        private readonly ApplicationDbContext _context;

        public TAPayController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory, ApplicationDbContext db, DataUtils dataUtils)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _dataUtils = dataUtils;
            _context = db; 

        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TApayInit(decimal amount, string userPhone, string CName, string CMobile, string CCard, string Cemail, string gateway, string divice)
        {
            var orderId = $"PAYMAN{DateTime.UtcNow:yyyyMMddHHmmssfff}{Random.Shared.Next(1000, 9999)}";

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
                    Email = Cemail,
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

            var model = new TaPaySessionModel
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

        [HttpPost]
        public async Task<IActionResult> CreatePayment(
            [FromBody] PaymentRequest request)
        {
            try
            {
                var apiKey =
                    _configuration["TAPaymentGateway:ApiKey"];

                var salt =
                    _configuration["TAPaymentGateway:Salt"];

                var apiUrl =
                    _configuration["TAPaymentGateway:ApiUrl"];

                if (string.IsNullOrWhiteSpace(apiKey) ||
                    string.IsNullOrWhiteSpace(salt) ||
                    string.IsNullOrWhiteSpace(apiUrl))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Payment gateway configuration missing"
                    });
                }

                // Generate unique order ID
                

                var returnUrl = "https://edu.paymanfintech.in/TAPay/Success";

                var failureUrl = "https://edu.paymanfintech.in/TAPay/Failure";

                var cancelUrl = "https://edu.paymanfintech.in/TAPay/Cancel";

                var parameters =
                    new Dictionary<string, string>
                    {
                        ["amount"] = request.Amount.ToString("0.00"),
                        ["api_key"] = apiKey,
                        ["currency"] = "INR",
                        ["description"] = "Education fee payments",
                        ["email"] = request.Email ?? "",
                        ["mode"] = "PROD",
                        ["name"] = request.Name ?? "",
                        ["order_id"] = request.orderId ?? "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                        ["phone"] = request.Phone ?? "",
                        ["city"] = "hyd",
                        ["country"] = "ind",
                        ["zip_code"] = "500050",
                        ["return_url"] = returnUrl,
                        ["return_url_cancel"] = cancelUrl,
                        ["return_url_failure"] = failureUrl
                    };

                // Gateway hash
                var hash = GenerateHash(
                    parameters,
                    salt);

                parameters["hash"] = hash;

                var client =
                    _httpClientFactory.CreateClient();

                using var content =
                    new FormUrlEncodedContent(parameters);

                var response =
                    await client.PostAsync(
                        $"{apiUrl}/v2/getpaymentrequesturl",
                        content);

                var responseText =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Gateway request failed",
                        response = responseText
                    });
                }

                using var json =
                    JsonDocument.Parse(responseText);

                var root = json.RootElement;

                string? paymentUrl = null;

                if (root.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty(
                        "url",
                        out var urlProperty))
                    {
                        paymentUrl =
                            urlProperty.GetString();
                    }
                }

                if (string.IsNullOrWhiteSpace(paymentUrl))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Payment URL not received",
                        response = responseText
                    });
                }

                return Json(new
                {
                    success = true,
                    paymentUrl = paymentUrl,
                    orderId = request.orderId
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        private async Task<PaymentStatusResult?> CheckPaymentStatusAsync(
    string orderId,
    string? transactionId = null)
        {
            var apiKey =
                   _configuration["TAPaymentGateway:ApiKey"];

            var salt =
                _configuration["TAPaymentGateway:Salt"];

            var apiUrl =
                _configuration["TAPaymentGateway:ApiUrl"];

            var parameters = new Dictionary<string, string>
    {
        { "api_key", apiKey! },
        { "order_id", orderId }
    };

            // If gateway returned transaction_id, send it too
            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                parameters.Add("transaction_id", transactionId);
            }

            // Generate gateway hash
            parameters["hash"] = GenerateHash(parameters, salt!);

            using var client = _httpClientFactory.CreateClient();

            var content = new FormUrlEncodedContent(parameters);

            var response = await client.PostAsync(
                $"{apiUrl}/v2/paymentstatus",
                content);

            var responseJson = await response.Content.ReadAsStringAsync();

            // Log actual gateway response
            var responseLog = new ErrorModel
            {
                payload = "TA Pay Status response",
                agId = "",
                reqTime = "",
                respTime = responseJson,
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(responseLog);
            await _context.SaveChangesAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentStatusResult
                {
                    Success = false,
                    Message = $"Gateway HTTP Error: {response.StatusCode}",
                    RawResponse = responseJson
                };
            }

            try
            {
                var result = JsonSerializer.Deserialize<PaymentStatusResult>(
                    responseJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result == null)
                {
                    return new PaymentStatusResult
                    {
                        Success = false,
                        Message = "Invalid gateway response.",
                        RawResponse = responseJson
                    };
                }

                result.RawResponse = responseJson;

                return result;
            }
            catch
            {
                return new PaymentStatusResult
                {
                    Success = false,
                    Message = "Unable to parse gateway response.",
                    RawResponse = responseJson
                };
            }
        }

        private static string GenerateHash(
            Dictionary<string, string> parameters,
            string salt)
        {
            var hashData = new StringBuilder();

            hashData.Append(salt);

            foreach (var item in parameters
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .OrderBy(x => x.Key))
            {
                hashData.Append("|");
                hashData.Append(item.Value.Trim());
            }

            using var sha512 =
                SHA512.Create();

            var bytes =
                Encoding.UTF8.GetBytes(
                    hashData.ToString());

            var hash =
                sha512.ComputeHash(bytes);

            return Convert
                .ToHexString(hash)
                .ToUpperInvariant();
        }
        [HttpPost]
        public async Task<IActionResult> Success()
        {
            // Log that callback was reached
            var initialLog = new ErrorModel
            {
                payload = "TA Pay response - callback reached",
                agId = "",
                reqTime = "",
                respTime = "",
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(initialLog);
            await _context.SaveChangesAsync();


            // Read gateway POST data
            var form = Request.HasFormContentType
                ? Request.Form
                : null;

            var response = form?.ToDictionary(
                x => x.Key,
                x => x.Value.ToString()
            ) ?? new Dictionary<string, string>();


            // Convert to JSON
            var responseJson = JsonSerializer.Serialize(response);

            // Log actual gateway response
            var responseLog = new ErrorModel
            {
                payload = "TA Pay response",
                agId = "",
                reqTime = "",
                respTime = responseJson,
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(responseLog);
            await _context.SaveChangesAsync();


            var model = JsonSerializer.Deserialize<TAPayResponse>(
    responseJson,
    new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

            // FIRST: Check actual status from gateway
            var status = await CheckPaymentStatusAsync(
                model.order_id,
                model.transaction_id);

            model.bank_code = status?.Data?.bank_code ?? string.Empty;


            var res = await _dataUtils.TAPayInDbCall(model);

            if (res.IsSuccess)
            {
                return Redirect(
                       $"https://paymanfintech.in/PayMan/PayStatus" +
                       $"?IsSuccess={res.IsSuccess}" +
                       $"&Amount={Uri.EscapeDataString(res.Amount.ToString())}" +
                       $"&TransactionId={Uri.EscapeDataString(res.TransactionId ?? "FAILED")}"
                   );
            }
            else
            {
                return Redirect(
                   $"https://paymanfintech.in/PayMan/PayStatus" +
                   $"?IsSuccess=false" +
                   $"&Amount=0" +
                   $"&TransactionId=FAILED"
               );
            }
        }



        [HttpPost]
        public async Task<IActionResult> Failure()
        {
            // Read gateway POST data
            var form = Request.HasFormContentType
                ? Request.Form
                : null;

            var response = form?.ToDictionary(
                x => x.Key,
                x => x.Value.ToString()
            ) ?? new Dictionary<string, string>();


            // Convert to JSON
            var responseJson = JsonSerializer.Serialize(response);

            var responseLog = new ErrorModel
            {
                payload = "TA Pay Failed response",
                agId = "",
                reqTime = "",
                respTime = responseJson,
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(responseLog);
            await _context.SaveChangesAsync();

            return Redirect(
                  $"https://paymanfintech.in/PayMan/PayStatus" +
                  $"?IsSuccess=false" +
                  $"&Amount=0" +
                  $"&TransactionId=FAILED"
              );
        }

        [HttpPost]
        public IActionResult Cancel()
        {
            return Redirect(
                  $"https://paymanfintech.in/PayMan/PayStatus" +
                  $"?IsSuccess=false" +
                  $"&Amount=0" +
                  $"&TransactionId=FAILED"
              );
        }

    }

    public class PaymentRequest
    {
        public decimal Amount { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }
        public string? orderId { get; set; }
    }

    public class PaymentStatusResult
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public string? RawResponse { get; set; }

        public PaymentStatusData? Data { get; set; }

        public PaymentStatusError? Error { get; set; }
    }

    public class PaymentStatusData
    {
        public string? Transaction_Id { get; set; }
        public string? bank_code { get; set; }

        public string? Order_Id { get; set; }

        public string? Amount { get; set; }

        public string? Currency { get; set; }

        public string? Response_Code { get; set; }

        public string? Response_Message { get; set; }

        public string? Payment_Mode { get; set; }

        public string? Payment_Channel { get; set; }

        public string? Payment_Datetime { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }
    }

    public class PaymentStatusError
    {
        public string? Code { get; set; }

        public string? Message { get; set; }
    }

    public class TaPaySessionModel
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

    public class TAPayResponse
    {
        public string? order_id { get; set; }
        public string? amount { get; set; }
        public string? currency { get; set; }
        public string? description { get; set; }
        public string? name { get; set; }
        public string? email { get; set; }
        public string? phone { get; set; }

        public string? address_line_1 { get; set; }
        public string? address_line_2 { get; set; }

        public string? city { get; set; }
        public string? state { get; set; }
        public string? country { get; set; }
        public string? zip_code { get; set; }

        public string? udf1 { get; set; }
        public string? udf2 { get; set; }
        public string? udf3 { get; set; }
        public string? udf4 { get; set; }
        public string? udf5 { get; set; }

        public string? transaction_id { get; set; }
        public string? bank_code { get; set; }
        public string? payment_mode { get; set; }
        public string? payment_channel { get; set; }
        public string? payment_datetime { get; set; }

        public string? response_code { get; set; }
        public string? response_message { get; set; }
        public string? error_desc { get; set; }

        public string? cardmasked { get; set; }

        public string? hash { get; set; }
    }


}
