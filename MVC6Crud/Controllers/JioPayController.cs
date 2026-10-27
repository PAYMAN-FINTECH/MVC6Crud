using Irony.Parsing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.App;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PaymanWeb;
using MVC6Crud.Models.PineLab;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XAct.Library.Settings;

namespace MVC6Crud.Controllers
{
    public class JioPayController : Controller
    {
        private readonly IConfiguration _cfg;
        private readonly JiopayService _ps;
        private readonly ApplicationDbContext _db;
        private readonly DataUtils _dataUtils;
        private readonly IHttpClientFactory _httpClientFactory;

        public JioPayController(IConfiguration cfg,  JiopayService ps, ApplicationDbContext db, DataUtils dataUtils, IHttpClientFactory  httpClientFactory)
        {
            _cfg = cfg;  _ps = ps; _db = db; _dataUtils = dataUtils; _httpClientFactory = httpClientFactory;
        }
        private string PaymentsUrl = "https://api.payu.in/v2/payments";
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult JiopayInit(decimal amount, string userPhone, string CName, string CMobile, string CCard, string Cemail,string gateway, string divice)
        {
            var model = new JiopaySessionModel
            {
                Amount = amount,
                CMobile = CMobile,
                Cemail = Cemail,
                CName = CName,
                userPhone = userPhone,
                CCard = CCard,
                gateway = gateway,
            };

            return View(model);
        }

        // ============================================================
        // CREATE PAYMENT
        // POST: api/PayUPayment/CreatePayment
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreatePayment(decimal amount, string userPhone, string CName, string CMobile, string CCard, string Cemail, string divice, string loadGateways)
        {
            try
            {

                if (amount < 1)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Invalid amount"
                    });
                }

                var user = await _db.payManUsers
                    .FirstOrDefaultAsync(t => t.Phone == userPhone);

                if (user == null)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "User not found"
                    });
                }

                var orderId =
                    $"PAYMAN{DateTime.UtcNow:yyyyMMddHHmmssfff}{Random.Shared.Next(1000, 9999)}";

                var istNow = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));



                var encodedOrder = Uri.EscapeDataString(orderId);

                var payload = new
                {
                    accountId = "46yIgi",
                    txnId = orderId,
                    currency = "INR",
                    order = new
                    {
                        productInfo = "Education fee payments",
                        paymentChargeSpecification = new
                        {
                            price = amount
                        }
                    },
                    additionalInfo = new
                    {
                        txnFlow = "nonseamless",
                        createOrder = true,
                        orderId = orderId
                    },
                    callBackActions = new
                    {
                        successAction = $"https://edu.paymanfintech.in/JioPay/Success?orderId={encodedOrder}",
                        failureAction = $"https://edu.paymanfintech.in/JioPay/Failure?orderId={encodedOrder}",
                        cancelAction = $"https://edu.paymanfintech.in/JioPay/Cancel?orderId={encodedOrder}"
                    },
                    billingDetails = new
                    {
                        firstName = string.IsNullOrWhiteSpace(CName) ? "Customer" : CName,
                        phone = CMobile ?? "",
                        email = Cemail ?? user.Email ?? "",
                        address1 = "NA",
                        city = "NA",
                        state = "NA",
                        country = "India",
                        zipCode = "500050"
                    }
                };

                // The exact string that is hashed must be the string that is sent.
                var body = System.Text.Json.JsonSerializer.Serialize(payload);

              
                var date = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);

                using var message = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, PaymentsUrl)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                message.Headers.TryAddWithoutValidation("date", date);
                message.Headers.TryAddWithoutValidation(
                    "authorization", BuildAuthorization(body, date));

                var client = _httpClientFactory.CreateClient();
                var response = await client.SendAsync(message);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "PayU request failed",
                        response = responseText
                    });
                }

                using var json = JsonDocument.Parse(responseText);

                string? checkoutUrl = null;

                if (json.RootElement.TryGetProperty("result", out var result) &&
                    result.ValueKind == JsonValueKind.Object &&
                    result.TryGetProperty("checkoutUrl", out var url))
                {
                    checkoutUrl = url.GetString();
                }

                if (string.IsNullOrWhiteSpace(checkoutUrl))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "PayU checkout URL was not returned",
                        response = responseText
                    });
                }

                return Ok(new
                {
                    success = true,
                    orderId,
                    redirectUrl = checkoutUrl
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        private string BuildAuthorization(string body, string date)
        {
            var key = "46yIgi";
            var salt = "LVppnjLWrPGK67pOt2T2oOBw2oqDyshm";

            var bytes = SHA512.HashData(
                Encoding.UTF8.GetBytes($"{body}|{date}|{salt}"));

            var signature = Convert.ToHexString(bytes).ToLowerInvariant();

            return $"hmac username=\"{key}\", algorithm=\"sha512\", headers=\"date\", signature=\"{signature}\"";
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(decimal amount,string userPhone,string CName, string CMobile,string CCard, string Cemail, string divice, string loadGateways)
        {
            //var status = await CheckStatus("PAYMAN20260618062838202", "101.00", loadGateways);
           // var status = await InitiateRefund("PAYMAN20260618062838202", "101.00", loadGateways);

            try
            {
                string txnId = "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmssfff");


                // IST time
                var istTime = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                var user = await _db.payManUsers
                    .FirstOrDefaultAsync(u => u.Phone == userPhone);

                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // ❗ Prevent duplicate orderId
               
                if (divice == "mobile")
                {
                    var bbpsuser = await _db.pMUsers
                    .FirstOrDefaultAsync(u => u.Phone == userPhone);

                    Cemail = bbpsuser.Email;
                    CMobile = bbpsuser.Phone;

                    var existing = await _db.bbpsPayIns
                   .FirstOrDefaultAsync(x => x.TxnId == txnId);

                    

                    if (existing == null)
                    {
                        var payIn = new BbpsPayIn
                        {
                            UserId = bbpsuser.Id,
                            UserPhone = bbpsuser.Phone,
                            TxnId = txnId,              // 🔑 orderId
                            Amount = amount,
                            Created = istTime,
                            Status = false,
                            Device = divice,
                            Result = "PENDING",
                            CardNumber = "",
                            PaymentTxnId = "",
                            CustomerEmail ="",
                        };

                        _db.bbpsPayIns.Add(payIn);
                        await _db.SaveChangesAsync();
                    }
                }
                else
                {
                    var existing = await _db.payManPayIns
                   .FirstOrDefaultAsync(x => x.TxnId == txnId);

                    if (existing == null)
                    {
                        var payIn = new PayManPayIn
                        {
                            UserId = user.Id,
                            UserPhone = userPhone,
                            TxnId = txnId,              // 🔑 orderId
                            Amount = amount,
                            Gateway = loadGateways,
                            Created = istTime,
                            Status = false,
                            Result = "PENDING",
                            Device = divice,
                            CreditCardHolderNum = CCard,
                            CreditCardHolderName = CName,
                            CardholderMobileNo = CMobile
                        };

                        _db.payManPayIns.Add(payIn);
                        await _db.SaveChangesAsync();
                    }
                }
                

                //var redirectUrl1 = await _ps.GenerateDQR(amount, txnId, Cemail, CMobile, loadGateways);


                string redirectUrl = await _ps.CreatePayment(amount, txnId, Cemail,CMobile,loadGateways);

                return Json(new
                {
                    success = true,
                    redirectUrl = redirectUrl
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

        public async Task<JiopayCommandResponse> CheckStatus(string originalTxnNo, string amount, string loadGateways)
        {
            var merchantId = _cfg["JioPay:merchantId"];

            if (loadGateways == "JioPay")
            {
                merchantId = _cfg["JioPay:merchantId"];
            }
            else
            {
                merchantId = _cfg["JioPayFasTag:merchantId"];
            }

            var request = new JiopayCommandRequest
            {
                merchantID = merchantId,
                aggregatorID = "", // if applicable
                merchantTxnNo = originalTxnNo,
                originalTxnNo = originalTxnNo,
                amount = amount,
                transactionType = "STATUS",
                addlParam1 = "",
                addlParam2 = ""
            };

            // 🔐 Generate Hash
            request.secureHash = _ps.GenerateJiopayHash(request, loadGateways);

            // 🔥 Convert to Form Data (IMPORTANT)
            var formData = new Dictionary<string, string>
    {
        { "merchantID", request.merchantID },
        { "aggregatorID", request.aggregatorID ?? "" },
        { "merchantTxnNo", request.merchantTxnNo },
        { "originalTxnNo", request.originalTxnNo },
        { "amount", request.amount },
        { "transactionType", request.transactionType },
        { "addlParam1", request.addlParam1 ?? "" },
        { "addlParam2", request.addlParam2 ?? "" },
        { "secureHash", request.secureHash }
    };

            string jsonData = System.Text.Json.JsonSerializer.Serialize(formData);

            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(formData);

                var response = await client.PostAsync(
                    "https://uat.jiopay.co.in/tsp/pg/api/command",
                    content);

                //var response = await client.PostAsync(
                //    "https://jiopay.co.in/pg/api/command",
                //    content);

                var result = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<JiopayCommandResponse>(result);
            }
        }



        public async Task<JiopayCommandResponse> InitiateRefund(string originalTxnNo, string refundAmount, string loadGateways)
        {
            var merchantId = _cfg["JioPay:merchantId"];

            if (loadGateways == "JioPay")
            {
                merchantId = _cfg["JioPay:merchantId"];
            }
            else
            {
                merchantId = _cfg["JioPayFasTag:merchantId"];
            }

            // 🔥 Refund must have NEW unique merchantTxnNo
            string refundTxnNo = "REF" + DateTime.Now.Ticks;

            var request = new JiopayCommandRequest
            {
                merchantID = merchantId,
                aggregatorID = "", // if applicable
                merchantTxnNo = refundTxnNo,      // NEW reference
                originalTxnNo = originalTxnNo,    // Original transaction ref
                amount = refundAmount,            // Refund amount (≤ original)
                transactionType = "REFUND",
                addlParam1 = "",
                addlParam2 = ""
            };

            // 🔐 Generate Hash (Alphabetical sorting)
            request.secureHash = _ps.GenerateJiopayHash(request, loadGateways);

            var formData = new Dictionary<string, string>
    {
        { "merchantID", request.merchantID },
        { "aggregatorID", request.aggregatorID ?? "" },
        { "merchantTxnNo", request.merchantTxnNo },
        { "originalTxnNo", request.originalTxnNo },
        { "amount", request.amount },
        { "transactionType", request.transactionType },
        { "addlParam1", request.addlParam1 ?? "" },
        { "addlParam2", request.addlParam2 ?? "" },
        { "secureHash", request.secureHash }
    };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(formData);

            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(formData);

                var response = await client.PostAsync(
                    "https://uat.jiopay.co.in/tsp/pg/api/command",
                    content);

                var result = await response.Content.ReadAsStringAsync();

                var refundResponse = JsonConvert.DeserializeObject<JiopayCommandResponse>(result);

                // 🔐 OPTIONAL: Validate Response Hash
                string receivedHash = refundResponse.secureHash;
                refundResponse.secureHash = null;

                string generatedHash = _ps.GenerateJiopayHash(refundResponse, loadGateways);

                if (!string.Equals(receivedHash, generatedHash, StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Invalid response hash from Jiopay");

                return refundResponse;
            }
        }


        [HttpPost]
        public async Task<IActionResult> Success(string orderId)
        {
            return Redirect(
                       $"https://paymanfintech.in/PayMan/PayStatus" +
                       $"?IsSuccess=true" +
                       $"&Amount=100" +
                       $"&TransactionId=Sucess"
                   );
        }
            //  private readonly string secretKey = "41d2971e1fd444cca9364f44d4b5e3b6";

            // ===============================
            // 🔷 B2B RETURN URL
            // ===============================
            [HttpPost]
        public async Task<IActionResult> Return()
        {
            try
            {
                var user1 = new ErrorModel
                {
                    payload = "Jio Pay response",
                    agId = Request.Form["merchantId"].ToString() ?? "",
                    reqTime = Request.Form["paymentID"].ToString() ?? "",
                    respTime =  "",
                    requestId =  "",
                    uid =  "",
                    statuscode = true,
                    jsonBody = ""
                };

                _db.errorModels.Add(user1);
                await _db.SaveChangesAsync();

                var formData = Request.Form.ToDictionary(
          x => x.Key,
          x => x.Value.ToString()
      );

                var jsonData11 = System.Text.Json.JsonSerializer.Serialize(formData);

                var responseObj = new JiopayResponse
                {
                    secureHash = Request.Form["secureHash"].ToString(),
                    amount = Request.Form["amount"].ToString(),
                    customerEmailID = Request.Form["customerEmailID"].ToString(),
                    paymentMode = Request.Form["paymentMode"].ToString(),
                    respDescription = Request.Form["respDescription"].ToString(),
                    TransmissionDateTime = Request.Form["TransmissionDateTime"].ToString(),
                    oth_charge = Request.Form["oth_charge"].ToString(),
                    paymentInstId = Request.Form["paymentInstId"].ToString(),
                    customerMobileNo = Request.Form["customerMobileNo"].ToString(),
                    responseCode = Request.Form["responseCode"].ToString(),
                    acqName = Request.Form["acqName"].ToString(),
                    cardNetwork = Request.Form["cardNetwork"].ToString(),
                    paymentSubInstType = Request.Form["paymentSubInstType"].ToString(),
                    merchantId = Request.Form["merchantId"].ToString(),
                    paymentID = Request.Form["paymentID"].ToString(),
                    merchantTxnNo = Request.Form["merchantTxnNo"].ToString(),
                    paymentDateTime = Request.Form["paymentDateTime"].ToString(),
                    txnID = Request.Form["txnID"].ToString(),
                    reponse = jsonData11
                };

                string jsonData = System.Text.Json.JsonSerializer.Serialize(responseObj);

                var user11 = new ErrorModel
                {
                    payload = "Jio Pay response1",
                    agId = Request.Form["merchantId"].ToString() ?? "",
                    reqTime = Request.Form["paymentID"].ToString() ?? "",
                    respTime = jsonData,
                    requestId = "",
                    uid = "",
                    statuscode = true,
                    jsonBody = ""
                };

                _db.errorModels.Add(user11);
                await _db.SaveChangesAsync();

                // 🔥 Null safety (important for hash generation)
                foreach (var prop in typeof(JiopayResponse).GetProperties())
                {
                    if (prop.GetValue(responseObj) == null)
                        prop.SetValue(responseObj, "");
                }

                var res = await _dataUtils.JioPayInDbCall(responseObj);

                string jsonData1 = System.Text.Json.JsonSerializer.Serialize(res);

                var user1a11 = new ErrorModel
                {
                    payload = "Jio Pay response5",
                    agId = "",
                    reqTime = "",
                    respTime = jsonData1,
                    requestId = "",
                    uid = "",
                    statuscode = true,
                    jsonBody = ""
                };

                _db.errorModels.Add(user1a11);
                await _db.SaveChangesAsync();

                // var status = await CheckStatus(Request.Form["txnID"].ToString(), responseObj.amount,);

                if (res.Gateway == "edu")
                {
                    return Redirect(
                    $"https://paymanfintech.in/PayMan/PayStatus" +
                    $"?IsSuccess={res.IsSuccess}" +
                    $"&Amount={Uri.EscapeDataString(res.Amount.ToString())}" +
                    $"&TransactionId={Uri.EscapeDataString(res.TransactionId ?? "FAILED")}" +
                    $"&Gateway={Uri.EscapeDataString(res.Gateway ?? "")}"
                     );
                }
                else
                {
                    return Redirect(
                       $"https://paymanfintech.in/PayMan/PayStatus" +
                       $"?IsSuccess={res.IsSuccess}" +
                       $"&Amount={Uri.EscapeDataString(res.Amount.ToString())}" +
                       $"&TransactionId={Uri.EscapeDataString(res.TransactionId ?? "FAILED")}"
                   );
                }

                   
            }
            catch (Exception ex)
            {
                return Redirect(
                    $"https://paymanfintech.in/PayMan/PayStatus" +
                    $"?IsSuccess=false" +
                    $"&Amount=0" +
                    $"&TransactionId=FAILED"
                );
            }
        }


        // ===============================
        // 🔷 S2S WEBHOOK (FINAL STATUS)
        // ===============================
        [HttpPost]
        public async Task<IActionResult> Webhook([FromBody] JObject json)
        {
            try
            {
                if (json == null)
                    return BadRequest("Invalid Request");

                // 🔐 Hash Validation
                //var hashHelper = new JiopayHashHelper(secretKey);
                //bool isValid = hashHelper.IsValidHash(json);

                //if (!isValid)
                //    return BadRequest("Invalid Hash");

                // 🔥 Map to Model (Null Safe)
                var responseObj = new JiopayResponse
                {
                    secureHash = json["secureHash"]?.ToString() ?? "",
                    amount = json["amount"]?.ToString() ?? "",
                    customerEmailID = json["customerEmailID"]?.ToString() ?? "",
                    paymentMode = json["paymentMode"]?.ToString() ?? "",
                    respDescription = json["respDescription"]?.ToString() ?? "",
                    TransmissionDateTime = json["TransmissionDateTime"]?.ToString() ?? "",
                    oth_charge = json["oth_charge"]?.ToString() ?? "",
                    paymentInstId = json["paymentInstId"]?.ToString() ?? "",
                    customerMobileNo = json["customerMobileNo"]?.ToString() ?? "",
                    responseCode = json["responseCode"]?.ToString() ?? "",
                    acqName = json["acqName"]?.ToString() ?? "",
                    cardNetwork = json["cardNetwork"]?.ToString() ?? "",
                    paymentSubInstType = json["paymentSubInstType"]?.ToString() ?? "",
                    merchantId = json["merchantId"]?.ToString() ?? "",
                    paymentID = json["paymentID"]?.ToString() ?? "",
                    merchantTxnNo = json["merchantTxnNo"]?.ToString() ?? "",
                    paymentDateTime = json["paymentDateTime"]?.ToString() ?? "",
                    txnID = json["txnID"]?.ToString() ?? ""
                };

                // 🔥 FINAL STATUS UPDATE HERE
                if (responseObj.responseCode == "0000")
                {
                    var res = await _dataUtils.JioPayInDbCall(responseObj);
                    //UpdatePaymentStatus(responseObj.merchantTxnNo, "SUCCESS");
                }
                else
                {
                    UpdatePaymentStatus(responseObj.merchantTxnNo, "FAILED");
                }

                return Ok("Received");
            }
            catch (Exception)
            {
                return StatusCode(500, "Error processing webhook");
            }
        }


        private void UpdatePaymentStatus(string txnId, string status)
        {
            // TODO: Update database
            Console.WriteLine($"Transaction {txnId} updated to {status}");
        }
    }

    public class JiopayResponse
    {
        public string secureHash { get; set; }
        public string amount { get; set; }
        public string customerEmailID { get; set; }
        public string paymentMode { get; set; }
        public string respDescription { get; set; }
        public string TransmissionDateTime { get; set; }
        public string oth_charge { get; set; }
        public string paymentInstId { get; set; }
        public string customerMobileNo { get; set; }
        public string responseCode { get; set; }
        public string acqName { get; set; }
        public string cardNetwork { get; set; }
        public string paymentSubInstType { get; set; }
        public string merchantId { get; set; }
        public string paymentID { get; set; }
        public string merchantTxnNo { get; set; }
        public string paymentDateTime { get; set; }
        public string txnID { get; set; }
        public string reponse {  get; set; }
    }

    public class JiopayCommandRequest
    {
        public string merchantID { get; set; }
        public string aggregatorID { get; set; } // optional
        public string merchantTxnNo { get; set; }  // new refNo (same as original for STATUS)
        public string originalTxnNo { get; set; }
        public string amount { get; set; }
        public string transactionType { get; set; } // STATUS / REFUND / AUTH / VOID
        public string addlParam1 { get; set; }
        public string addlParam2 { get; set; }
        public string secureHash { get; set; }
    }


    public class JiopayCommandResponse
    {
        public string responseCode { get; set; }
        public string respDescription { get; set; }
        public string merchantId { get; set; }
        public string aggregatorID { get; set; }
        public string merchantTxnNo { get; set; }
        public string txnStatus { get; set; }
        public string txnResponseCode { get; set; }
        public string txnRespDescription { get; set; }
        public string txnId { get; set; }
        public string paymentDateTime { get; set; }
        public string txnAuthID { get; set; }
        public string secureHash { get; set; }
        public string oth_charge { get; set; }
    }
    public class JiopaySessionModel 
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
    }

}
