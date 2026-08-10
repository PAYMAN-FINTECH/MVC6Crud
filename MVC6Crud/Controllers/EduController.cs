using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.CashFree;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PaymanWeb;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Razorpay.Api;
using System.Text;
using XAct.Users;
using static WhatsAppApi.Parser.FMessage;

namespace MVC6Crud.Controllers
{
    public class EduController : Controller
    {
        private readonly CashfreeService _paymentService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;

        public EduController(CashfreeService paymentService, ApplicationDbContext context, DataUtils dataUtils,IConfiguration configuration)
        {
            _paymentService = paymentService;
            _context = context;
            _dataUtils = dataUtils;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Aboutus()
        {
            return View();
        }
        public IActionResult python()
        {
            return View();
        }

        public IActionResult devops()
        {
            return View();
        }

        public IActionResult clanguage()
        {
            return View();
        }
        public IActionResult contact()
        {
            return View();
        }
        public IActionResult privacy1()
        {
            return View();

        }
        public IActionResult Refound()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EnrollCourse(string Name, string Email, string Phone, string Course)
        {
            // TODO: Save to DB or send email
            TempData["Message"] = $"Thanks {Name}, you have enrolled in {Course}!";
            return RedirectToAction("Index"); // or wherever you want
        }
        public IActionResult termsandconditions()
        {
            string salt = "XXXXXX";
            string Key = "XXXXXX";
            string env = "test";    // test for testing env and prod for production use wisely
                                    // Easebuzz t = new Easebuzz(salt, Key, env);


            return View();
        }
        [HttpPost]
        public async Task<JsonResult> IntiatePayment(int amount, string cardnumber, string selectedgateway = null)
        {
            var key = "rzp_test_lGsdswJA3Wc7jY";
            var secrate = "a8NHWgCI8mvxEyOa1Y9vq7p8";
            RazorpayClient razorpayClient = new RazorpayClient(key, secrate);
            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("amount", Convert.ToDecimal(amount) * 100);
            data.Add("currency", "INR");
            Order order = razorpayClient.Order.Create(data);
            ViewBag.orderId = order["id"].ToString();
            return Json(new { Success = true, OrderId = ViewBag.orderId });
        }
        [HttpGet]
        public async Task<JsonResult> ConfirmPayment(string razorpay_payment_id, string razorpay_order_id, string razorpay_signature, string amount, string enterCustomerNumber, string selectedgateway)
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            attributes.Add("razorpay_payment_id", razorpay_payment_id);
            attributes.Add("razorpay_order_id", razorpay_order_id);
            attributes.Add("razorpay_signature", razorpay_signature);
            //return Ok();
            try
            {
                var key = "rzp_test_lGsdswJA3Wc7jY";
                var secrate = "a8NHWgCI8mvxEyOa1Y9vq7p8";
                RazorpayClient _razorpayClient = new RazorpayClient(key, secrate);

                Utils.verifyPaymentSignature(attributes);
                var order = _razorpayClient.Order.Fetch(razorpay_order_id);
                var payment = _razorpayClient.Payment.Fetch(razorpay_payment_id);
                var razorPayCard = _razorpayClient.Card.FetchCardDetails(razorpay_payment_id);
                var dhsds = payment["status"];
                var dsjf = razorPayCard["issuer"];
                var hdsj = razorPayCard["network"];
                var hcc = razorPayCard["sub_type"] == null ? null : razorPayCard["sub_type"].Value;
                dhsds = dhsds.ToString();
                dsjf = dsjf.ToString();
                hdsj = hdsj.ToString();


                return Json(new { Success = true, paymentid = razorpay_payment_id, orderdd = razorpay_order_id, paymentstatus = dhsds, issuerbank = dsjf, issuecard = hdsj, selectedgat = selectedgateway, aamount = amount, eenterCustomerNumber = enterCustomerNumber, cardtype = hcc });
            }
            catch (Exception ex)
            {
                //  TempData["PaymentSucess"] = "You have " + amount + " failed transection.";
                return Json(new { Success = false, Paymentid = razorpay_payment_id });
            }


        }





        public IActionResult GetAI(string courseName, string amount = null)
        {
            // Default course name
            ViewBag.CourseName = courseName ?? "Generative AI & Machine Learning Engineer";

            // Convert amount to decimal
            decimal courseFee = 0;
            if (!string.IsNullOrEmpty(amount))
            {
                decimal.TryParse(amount, out courseFee);
            }

            // Apply 10% discount
            decimal discount = (courseFee * 10) / 100;
            decimal discountedAmount = courseFee - discount;

            //// Apply GST 18%
            //decimal gst = (discountedAmount * 18) / 100;

            // Final amount
            decimal finalAmount = discountedAmount;//+ gst;

            // Pass to View
            ViewBag.CourseFee = courseFee;
            ViewBag.Discount = discount;
            ViewBag.DiscountedAmount = discountedAmount;
           // ViewBag.GST = gst;
            ViewBag.FinalAmount = finalAmount;

            return View();
        }


        [HttpPost]
        public IActionResult SubmitForm(string FullName, string Email, string WhatsAppNumber)
        {
            // You can save data to DB here if needed.

            // Pass success message
            TempData["SuccessMessage"] = "Your response has been submitted successfully! Our team will contact you shortly.";

            return RedirectToAction("Index", "Edu");
        }

        [HttpGet]
        public IActionResult PaymentStatus()
        {
            PaymentStatusViewModel pp = new PaymentStatusViewModel();
           
            pp.IsSuccess = true;
            return View(pp);
        }
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CFOrderRequest orderRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid order request.");

            try
            {
                var paymentSessionId = await _paymentService.CreateOrderAsync(orderRequest);
                return Ok(new { paymentSessionId });
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateOrder Error: " + ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }
        public IActionResult PaymentReturn()
        {
            // Handle payment response
            return View();
        }
        [HttpGet]
        public IActionResult StartCheckout(string sessionId)
        {
            ViewBag.SessionId = sessionId;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Return(string order_id, string loginmobile, string email, string cardnum, string holderphone, string holdername, string device)
        {
            try
            {
                var userDetails = await _context.payManUsers
                    .FirstOrDefaultAsync(t => t.Phone == loginmobile);

                if (userDetails == null)
                {
                    var model = new PaymentStatusViewModel
                    {
                        IsSuccess = false,
                        Amount = 0,
                        TransactionId = order_id,
                        CardNumber = "N/A"
                    };
                    return View("PayStatus", model);
                }

                DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                var res = await _paymentService.PaymentsForOrder(order_id);
                if (res == null || res.Count == 0)
                {
                    var model = new PaymentStatusViewModel
                    {
                        IsSuccess = false,
                        Amount = 0,
                        TransactionId = order_id,
                        CardNumber = "N/A"
                    };
                    return View("PayStatus", model);
                }

                var payment = res[0];
                var easebuzzGateway = await _context.PayManGateways.FirstOrDefaultAsync(t => t.GatewayName == "Easebuzz");
                var existing = await _context.payManPayIns.FirstOrDefaultAsync(t => t.EasePayId == payment.cf_payment_id);

                if (existing == null)
                {
                    var gatewayId = await _context.gateways
                   .Where(g => g.StoreName == "CashfreeEdu")
                   .Select(g => g.Id)
                   .FirstOrDefaultAsync();

                    var getMargin = await _context.userLookUps
                        .Where(m => m.GatewayId == gatewayId && m.UserPhone == loginmobile)
                        .Select(m => m.GatewayMargin)
                        .FirstOrDefaultAsync();

                    decimal margin = getMargin > 0 ? getMargin : 0;
                    string paymentGroup = payment.payment_group?.ToLower() ?? "";

                    var card = payment.payment_method?.card;
                    string cardSubType = card?.card_sub_type ?? "";
                    string cardNetwork = card?.card_network ?? "";
                    string bankName = card?.card_bank_name ?? "";

                    // Apply margin only for card transactions
                    if (paymentGroup == "credit_card" || paymentGroup == "debit_card")
                    {
                        if (cardSubType.Equals("C", StringComparison.OrdinalIgnoreCase))
                            margin = userDetails.CarporateCardMarigin ?? margin;
                        else if (cardNetwork.ToLower().Contains("master"))
                            margin = userDetails.MasterMarigin ?? margin;
                        else if (bankName.ToLower().Contains("hdfc"))
                            margin = userDetails.HdfcMargin ?? margin;
                    }

                    var payInApp = new PayManPayIn
                    {
                        UserId = userDetails.Id,
                        UserPhone = loginmobile,
                        TxnId = payment.order_id,
                        EasePayId = payment.cf_payment_id,
                        Email = email,
                        CardNumber = card?.card_number ?? "N/A",
                        EaseCardNum = userDetails.Email,
                        Amount = payment.order_amount,
                        Gateway = "CashfreeEdu",
                        BankName = bankName,
                        CardBrand = cardNetwork,
                        IsCorporate = cardSubType,
                        PayInCommission = payment.order_amount * margin / 100,
                        PaymanCommission = payment.order_amount * (easebuzzGateway?.PaymanComm ?? 0) / 100,
                        Created = istDateTime,
                        Status = payment.is_captured,
                        Result = payment.payment_status,
                        Device = device,
                        CreditCardHolderNum = cardnum,
                        CreditCardHolderName = holdername,
                        CardholderMobileNo = holderphone
                    };

                    _context.payManPayIns.Add(payInApp);
                    await _context.SaveChangesAsync();

                    var avlAmount = await _dataUtils.GetUserWalletAmount(loginmobile);

                    var payInHistory = new PayManHistory
                    {
                        UserId = userDetails.Id,
                        UserPhone = loginmobile,
                        TxnId = payment.cf_payment_id,
                        Amount = payment.order_amount,
                        CardNumber = card?.card_number ?? "N/A",
                        Mode = "PayIn",
                        Status = payment.is_captured,
                        Created = istDateTime,
                        AvlBalance = Convert.ToDecimal(avlAmount),
                        PayInId = payInApp.Id
                    };

                    _context.payManHistories.Add(payInHistory);
                    await _context.SaveChangesAsync();
                }

                var modelSuccess = new PaymentStatusViewModel
                {
                    IsSuccess = true,
                    Amount = Convert.ToDouble(payment.order_amount),
                    TransactionId = payment.cf_payment_id,
                    CardNumber = payment.payment_method?.card?.card_number ?? "UPI"
                };

                return Redirect(
                    $"https://paymanfintech.in/PayMan/PayStatus" +
                    $"?IsSuccess={modelSuccess.IsSuccess}" +
                    $"&Amount={Uri.EscapeDataString(modelSuccess.Amount.ToString())}" +
                    $"&TransactionId={Uri.EscapeDataString(modelSuccess.TransactionId ?? "FAILED")}"
                );
            }
            catch (Exception ex)
            {
                var model = new PaymentStatusViewModel
                {
                    IsSuccess = false,
                    Amount = 0,
                    TransactionId = order_id,
                    CardNumber = "N/A"
                };
                return Redirect(
                 $"https://paymanfintech.in/PayMan/PayStatus" +
                 $"?IsSuccess={model.IsSuccess}" +
                 $"&Amount={Uri.EscapeDataString(model.Amount.ToString())}" +
                 $"&TransactionId={Uri.EscapeDataString(model.TransactionId ?? "FAILED")}"
             );
            }
        }

        public IActionResult PayStatus(PaymentStatusViewModel paymentStatusViewModel)
        {
            return View(paymentStatusViewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Notify([FromBody] JObject payload)
        {
            try
            {
                if (payload == null)
                {
                    return BadRequest("No payload received from Cashfree.");
                }

                // Extract order_id
                string orderId = payload["order_id"]?.ToString();

                if (string.IsNullOrEmpty(orderId))
                {
                    return BadRequest("Missing order_id in notification payload.");
                }

                // ✅ Call service to verify payment again for double confirmation
                //var verifyResponse = await _cashfreeService.VerifyPaymentAsync(orderId);

                // ✅ Optionally log full payload + verify result for auditing
                //await _cashfreeService.SaveNotifyLogAsync(orderId, payload.ToString(), verifyResponse);

                // ✅ Respond with 200 OK (Cashfree expects 2xx to mark notify delivered)
                return Ok(new { message = "Notification received successfully." });
            }
            catch (Exception ex)
            {
                // Log exception and return 500 to indicate failure
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult RazorpayInit(decimal amount, string mobile, string email, string name, string logonphone,string card)
        {
            var model = new RazorpaySessionModel
            {
                Amount = amount,
                Mobile = mobile,
                Email = email,
                Name = name,
                logonphone = logonphone,
                Card = card
            };

            return View(model);
        }


        // ---------------- CREATE ORDER ----------------
        [HttpPost]
        public IActionResult CreateRazorpayOrder([FromBody] OrderRequestRazorpay request)
        {
            if (request == null || request.Amount <= 0)
                return BadRequest(new { success = false, message = "Invalid request" });

            var key = request.merchent == "Utility"
                ? _configuration["RozorPay:Ukey"]
                : _configuration["RozorPay:NUkey"];

            var secret = request.merchent == "Utility"
                ? _configuration["RozorPay:Usecrate"]
                : _configuration["RozorPay:NUsecrate"];

            try
            {
                RazorpayClient client = new RazorpayClient(key, secret);

                Dictionary<string, object> options = new()
            {
                { "amount", request.Amount * 100 },
                { "currency", "INR" },
                { "receipt", "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "payment_capture", 1 }
            };

                Order order = client.Order.Create(options);

                return Ok(new
                {
                    success = true,
                    orderId = order["id"].ToString(),
                    key,
                    workingKey = secret
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ---------------- VERIFY PAYMENT ----------------
        [HttpPost]
        public async Task<IActionResult> VerifyRazorPayPayment([FromBody] VerifyPaymentRequestRazor req)
        {
            if (string.IsNullOrWhiteSpace(req.RazorpayPaymentId) ||
                string.IsNullOrWhiteSpace(req.RazorpaySignature) ||
                string.IsNullOrWhiteSpace(req.RazorpayOrderId))
            {
                return BadRequest(new VerifyPaymentResponseRazor
                {
                    Success = false,
                    Message = "Invalid request"
                });
            }

            var key = req.Merchent == "Utility"
               ? _configuration["RozorPay:Ukey"]
               : _configuration["RozorPay:NUkey"];

            var secret = req.Merchent == "Utility"
                ? _configuration["RozorPay:Usecrate"]
                : _configuration["RozorPay:NUsecrate"];

            RazorpayClient client = new RazorpayClient(key, secret);

            var attributes = new Dictionary<string, string>
    {
        { "razorpay_payment_id", req.RazorpayPaymentId },
        { "razorpay_order_id", req.RazorpayOrderId },
        { "razorpay_signature", req.RazorpaySignature }
    };

            try
            {
                // Verify signature
                Utils.verifyPaymentSignature(attributes);

                // Fetch payment details
                var payment = client.Payment.Fetch(req.RazorpayPaymentId);
                string status = payment["status"];
                string method = payment["method"];
                string email = payment["email"];
                string contact = payment["contact"];
                decimal amount = Convert.ToDecimal(payment["amount"]) / 100; // Razorpay gives paise

                // Extract card info if card payment
                string cardType = "";
                string cardIssuer = "";
                string cardNetwork = "";

                if (method == "card")
                {
                    var card = payment["card"];
                    cardType = card["type"]?.ToString();
                    cardIssuer = card["issuer"]?.ToString();
                    cardNetwork = card["network"]?.ToString();
                }

                var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == req.Phone);
                if (user == null)
                {
                    return Unauthorized(new VerifyPaymentResponseRazor
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                // Check if already recorded
                var existing = await _context.payManPayIns
                    .FirstOrDefaultAsync(t => t.EasePayId == req.RazorpayOrderId);

                if (existing == null)
                {
                    var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                    decimal margin = Convert.ToDecimal(user.Margin ?? "0");

                    if (cardType?.ToLower() == "corporate")
                        margin = user.CarporateCardMarigin ?? margin;
                    else if (cardNetwork?.ToLower() == "mastercard")
                        margin = user.MasterMarigin ?? margin;
                    else if (cardIssuer?.ToLower().Contains("hdfc") == true)
                        margin = user.HdfcMargin ?? margin;

                    var easebuzzGateway = await _context.PayManGateways.FirstAsync();
                    var payInApp = new PayManPayIn
                    {
                        UserId = user.Id,
                        UserPhone = req.Phone,
                        TxnId = req.RazorpayOrderId,
                        EasePayId = req.RazorpayOrderId,
                        Email = user.Email,
                        EaseCardNum = email,
                        Amount = amount,
                        Gateway = "RazorpayEdu",
                        CardNumber = req.CardNum,
                        BankName = cardIssuer ?? "NA",
                        CardBrand = cardNetwork ?? "NA",
                        IsCorporate = cardType,
                        PayInCommission = amount * margin / 100,
                        PaymanCommission = amount * (easebuzzGateway?.PaymanComm ?? 0) / 100, // update if needed
                        Created = istTime,
                        CreditCardHolderName = req.CardHoderName,
                        CreditCardHolderNum = req.CardNum,
                        CardholderMobileNo = req.CustomerMobile,
                        Status = status == "captured",
                        Result = status,
                        Device = "Web"
                    };

                    _context.payManPayIns.Add(payInApp);
                    await _context.SaveChangesAsync();

                    var avlAmount = await _dataUtils.GetUserWalletAmount(req.Phone);

                    var payInHistory = new PayManHistory
                    {
                        UserId = user.Id,
                        UserPhone = req.Phone,
                        TxnId = req.RazorpayOrderId,
                        Amount = amount,
                        Mode = "PayIn",
                        Status = status == "captured",
                        Created = istTime,
                        AvlBalance = Convert.ToDecimal(avlAmount),
                        PayInId = payInApp.Id
                    };

                    _context.payManHistories.Add(payInHistory);
                    await _context.SaveChangesAsync();
                }

                if (status == "captured")
                {
                    return Ok(new VerifyPaymentResponseRazor
                    {
                        Success = true,
                        Message = "Payment successful",
                        TransactionId = req.RazorpayPaymentId
                    });
                }
                else
                {
                    return BadRequest(new VerifyPaymentResponseRazor
                    {
                        Success = false,
                        Message = $"Payment status: {status}"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new VerifyPaymentResponseRazor
                {
                    Success = false,
                    Message = "Payment verification failed"
                });
            }
        }


    }
}
