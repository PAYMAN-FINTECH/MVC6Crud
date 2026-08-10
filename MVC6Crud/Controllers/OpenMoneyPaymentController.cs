using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PaymanWeb;
using MVC6Crud.Models.PineLab;
using Newtonsoft.Json;
using Razorpay.Api;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static WhatsAppApi.Parser.FMessage;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MVC6Crud.Controllers
{
    public class OpenMoneyPaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;

        private string _accessKey = "";   // replace with your Access Key
        private string _secretKey = "";   // replace with your Secret Key
        private string _publicKey = "";   // ✅ frontend uses AccessKey as PublicKey
        //private readonly string _baseUrl = "https://api.zwitch.io/v1/pg/sandbox";
        private string _baseUrl = "";

        public OpenMoneyPaymentController(ApplicationDbContext context, IConfiguration configuration,DataUtils dataUtils)
        {
            _context = context;
            _accessKey = configuration["Zwitch:AccessCode"];
            _secretKey = configuration["Zwitch:SecretKey"];
            _baseUrl = configuration["Zwitch:BaseUrl"];
            _dataUtils = dataUtils;
        }

        public ActionResult Checkout()
        {
            ViewBag.PublicKey = _publicKey;
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> CreatePaymentToken(OpenMoneyPaymentResponseModel model)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    // ✅ Use Bearer instead of Basic + Base64
                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", $"{_accessKey}:{_secretKey}");

                    var payload = new
                    {
                        amount = model.Amount,
                        currency = "INR",
                        email_id = model.Email,
                        contact_number = model.mobile,
                        mtx= "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    };

                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync($"{_baseUrl}/payment_token", content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return Json(new { success = false, message = responseContent });
                    }

                    var paymentToken = JsonSerializer.Deserialize<OpenMoneyPaymentTokenResponse>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

                    return Json(new { success = true, payment = paymentToken, accesstoken = _accessKey });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult> CheckStatus( string paymentId, string paymentTokenId, string cardNumber, string cardHolderNumber, string cardHolderName)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    // Authorization header
                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", $"{_accessKey}:{_secretKey}");

                    // Call OpenMoney API
                    var response = await httpClient.GetAsync($"{_baseUrl}/payment_token/{paymentTokenId}/payment");
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        var model11 = new PaymentStatusViewModel
                        {
                            IsSuccess = false,
                            Amount = 0,
                            TransactionId = "",
                            CardNumber = cardNumber
                        };

                        return RedirectToAction("Response", model11);
                    }

                    // Deserialize OpenMoney response
                    var response1 = JsonSerializer.Deserialize<OpenMoneyPaymentResponse>(responseContent);

                    if (response1 != null)
                    {
                        bool isCaptured = response1.status?.ToLower() == "captured";  // ✅ Validate status

                        var appPhone = HttpContext.Session.GetString("AppPhone");
                        if (string.IsNullOrEmpty(appPhone))
                        {
                            return RedirectToAction("WebLogout", "Login");
                        }

                        var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == appPhone);
                        if (user == null)
                        {
                            return RedirectToAction("WebLogout", "Login");
                        }

                        // Get gateway config
                        var easebuzzGateway = await _context.PayManGateways.FirstOrDefaultAsync(t => t.GatewayName == "Easebuzz");

                        // Check for idempotency
                        var existing = await _context.payManPayIns
                            .FirstOrDefaultAsync(t => t.EasePayId == response1.id);

                        if (existing == null)
                        {
                            var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                             TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                            decimal margin = Convert.ToDecimal(user.Margin ?? "0");

                            if (response1.payment_instrument.type_id == 1)
                                margin = user.CarporateCardMarigin ?? margin;
                            else if (response1.payment_instrument.name?.ToLower() == "mastercard")
                                margin = user.MasterMarigin ?? margin;
                            else if (!string.IsNullOrEmpty(response1.payment_instrument.type_name) &&
                                     response1.payment_instrument.type_name.ToLower().Contains("hdfc"))
                                margin = user.HdfcMargin ?? margin;

                            var payInApp = new PayManPayIn
                            {
                                UserId = user.Id,
                                UserPhone = appPhone,
                                TxnId = response1.payment_token.id,
                                EasePayId = response1.id,
                                Email = response1.customer.email_id,
                                CardNumber = cardNumber,
                                EaseCardNum = user.Email,
                                Amount = Convert.ToDecimal(response1.amount),
                                Gateway = "Open Money",
                                BankName = "",
                                CardBrand = response1.payment_instrument.name,
                                IsCorporate = "",
                                PayInCommission = Convert.ToDecimal(response1.amount) * margin / 100,
                                PaymanCommission = Convert.ToDecimal(response1.amount) * (easebuzzGateway?.PaymanComm ?? 0) / 100,
                                Created = istTime,
                                Status = isCaptured,
                                Result = response1.status == "captured" ? "success" : response1.status,
                                Device = "Web",
                                CreditCardHolderNum = cardNumber,
                                CreditCardHolderName = cardHolderName,
                                CardholderMobileNo = cardHolderNumber
                            };

                            _context.payManPayIns.Add(payInApp);
                            await _context.SaveChangesAsync();

                            var avlAmount = await this._dataUtils.GetUserWalletAmount(appPhone);

                            var payInHistory = new PayManHistory
                            {
                                UserId = user.Id,
                                UserPhone = appPhone,
                                TxnId = response1.id,
                                Amount = Convert.ToDecimal(response1.amount),
                                CardNumber = cardNumber,
                                Mode = "PayIn",
                                Status = isCaptured,
                                Created = istTime,
                                AvlBalance = Convert.ToDecimal(avlAmount),
                                PayInId = payInApp.Id
                            };

                            _context.payManHistories.Add(payInHistory);
                            await _context.SaveChangesAsync();
                        }

                        // ✅ Build ViewModel with correct status
                        var model22 = new PaymentStatusViewModel
                        {
                            IsSuccess = isCaptured,
                            Amount = Convert.ToDouble(response1.amount ?? "0"),
                            TransactionId = response1.id,
                            CardNumber = cardNumber
                        };

                        return RedirectToAction("Response", model22);
                    }
                }
            }
            catch (Exception ex)
            {
                var model1 = new PaymentStatusViewModel
                {
                    IsSuccess = false,
                    Amount = 0,
                    TransactionId = "",
                    CardNumber = cardNumber
                };

                return RedirectToAction("Response", model1);
            }

            var model = new PaymentStatusViewModel
            {
                IsSuccess = false,
                Amount = 0,
                TransactionId = "",
                CardNumber = cardNumber
            };

            return RedirectToAction("Response", model);
        }

        public ActionResult Response(PaymentStatusViewModel model)
        {
            return View(model);
        }

        public ActionResult Failure()
        {
            var fail = new PaymentStatusViewModel();
            fail.IsSuccess = false;
            return View(fail);
        }

        public ActionResult Cancelled()
        {
            var fail = new PaymentStatusViewModel();
            fail.IsSuccess = false;
            return View(fail);
        }

    }
}
