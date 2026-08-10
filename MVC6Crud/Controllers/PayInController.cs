using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PaymanWeb;
using Newtonsoft.Json;
using Razorpay.Api;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MVC6Crud.Controllers
{
    public class PayInController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly DataUtils _dataUtils;
        private readonly IConfiguration _configuration;

        public PayInController( ApplicationDbContext context, DataUtils dataUtils, IConfiguration configuration)
        {
            _context = context;
            _dataUtils = dataUtils;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        

        [HttpPost]
        public IActionResult PayIn([FromBody] PayInRequest request)
        {
            if (string.IsNullOrEmpty(request.CardNumber) ||
                string.IsNullOrEmpty(request.MobileNumber) ||
                string.IsNullOrEmpty(request.GatewayType))
            {
                return BadRequest(new { message = "All fields are required!" });
            }

            // Dummy response (Replace with actual payment gateway integration)
            var transactionId = Guid.NewGuid().ToString();
            return Ok(new
            {
                message = "Payment initiated successfully",
                transactionId,
                gateway = request.GatewayType
            });
        }

        public IActionResult RazorpayInit([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Error");

            var json = HttpContext.Session.GetString(token);

            if (json == null)
                return RedirectToAction("Error");

            var model = JsonConvert.DeserializeObject<RazorpaySessionModel>(json);

            return View(model);
        }


        [HttpPost]
        public IActionResult CreateRazorpaySession(RazorpaySessionModel model)
        {
            var token = Guid.NewGuid().ToString("N");

            HttpContext.Session.SetString(
                token,
                JsonConvert.SerializeObject(model)
            );

            return Json(new { token });
        }

        //[HttpGet]
        //public IActionResult GetInvoiceDetails([FromQuery] string txn)
        //{
        //    var transaction = _context.payManPayIns.FirstOrDefault(t => t.TxnId == txn || t.EasePayId == txn);
        //    if (transaction == null) return NotFound(new { success = false, message = "Transaction not found" });

        //    var transactionDate = transaction.Created ?? DateTime.Now;
        //    var amount = transaction.Amount;
        //    var cgst = amount * 0.09m;
        //    var sgst = amount * 0.09m;
        //    var total = amount + cgst + sgst;

        //    // Generate invoice number (simple example)
        //    var invoiceNo = $"PM/INV/{DateTime.Now:yyyyMMdd}"; // using ID as sequence

        //    var invoice = new
        //    {
        //        InvoiceNo = invoiceNo,
        //        Date = transactionDate.ToString("dd-MMM-yyyy"),
        //        TransactionId = txn,
        //        From = new
        //        {
        //            Name = "Payman Fintech Solutions Pvt. Ltd.",
        //            Address = "Hmt Nagar Stnumber 10, Nacharam, Hyderabad, Telangana, 500076",
        //            GSTIN = "36AAOCP3061H1Z9"
        //        },
        //        To = new
        //        {
        //            Name = transaction.CreditCardHolderName,
        //            Address = "3-45/1, Narsapuram, Mandal Siddipet, Mittapally, Siddipet, Medak, Mittapalle, Telangana, India, 502375", // TODO: fetch from user profile
        //            GSTIN = "" // optional
        //        },
        //        LineItems = new[]
        //        {
        //    new
        //    {
        //        SNo = 1,
        //        Service = "Credit Card Bill Payment Fee",
        //        HsnSac = "997158",
        //        Qty = 1,
        //        UnitPrice = amount,
        //        Amount = amount
        //    }
        //},
        //        CGST = cgst,
        //        SGST = sgst,
        //        Total = total
        //    };

        //    return Ok(invoice);
        //}


        //[HttpGet]
        //public IActionResult ViewInvoice(string txn, string invoiceNo)
        //{
        //    var transaction = _context.payManPayIns.FirstOrDefault(t => t.TxnId == txn);
        //    if (transaction == null) return NotFound("Transaction not found");
        //    var transactionDate = transaction.Created ?? DateTime.Now;

        //    var amount = transaction.Amount;
        //    var cgst = amount * 0.09m;
        //    var sgst = amount * 0.09m;
        //    var total = amount + cgst + sgst;

        //    // Generate invoice number (should match the one used in GetInvoiceDetails)
        //    var generatedInvoiceNo = $"PM/INV/{DateTime.Now:yyyyMMdd}/{transaction.Id}";
        //    // But we might want to use the one passed from QR code; for now use generated
        //    // For consistency, you might store invoice number in database.
        //    // For simplicity, we'll ignore invoiceNo param and use generated.

        //    var invoice = new
        //    {
        //        InvoiceNo = generatedInvoiceNo,
        //        Date = transactionDate.ToString("dd-MMM-yyyy"),
        //        TransactionId = txn,
        //        From = new
        //        {
        //            Name = "XYZ Payment Solutions Pvt. Ltd.",
        //            Address = "123, MG Road, Bengaluru - 560001",
        //            GSTIN = "29ABCDE1234F1Z5"
        //        },
        //        To = new
        //        {
        //            Name = transaction.CreditCardHolderName,
        //            Address = "Vavilapally, Karimnagar, Telangana – 505001",
        //            GSTIN = ""
        //        },
        //        LineItems = new[]
        //        {
        //    new
        //    {
        //        SNo = 1,
        //        Service = "Credit Card Bill Payment Fee",
        //        HsnSac = "997158",
        //        Qty = 1,
        //        UnitPrice = amount,
        //        Amount = amount
        //    }
        //},
        //        CGST = cgst,
        //        SGST = sgst,
        //        Total = total
        //    };

        //    return View(invoice);
        //}

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

            var key = req.key;
            var secret = req.workingkey;
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
                        Gateway = "Razorpay",
                        BankName = cardIssuer ?? "NA",
                        CardBrand = cardNetwork ?? "NA",
                        IsCorporate = cardType,
                        PayInCommission = amount * margin / 100,
                        PaymanCommission = amount * (easebuzzGateway?.PaymanComm ?? 0) / 100, // update if needed
                        Created = istTime,
                        CreditCardHolderName = req.CardHoderName,
                        CreditCardHolderNum =  req.CardNum,
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

        private List<CoursePurchase> GetCoursePurchases()
        {
            // Static data (GrossAmount is total paid inclusive of 18% GST)
            var rawData = new[]
            {
                new { MerchantRefNo = "PAYMAN20260604041001912", AmountPaid = 46157.80m, TxnCharges = 10132.20m, GrossAmount = 56290.00m, Service = "ASP.NET Core Advanced Courses", MobileNumber = "9912238196", CustomerEmail = "venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate ="2026-06-04 09:42:01" },
                new { MerchantRefNo = "PAYMAN20260604035811994", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "Azure for .NET Developers", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate ="2026-06-04 09:30:25" },
                new { MerchantRefNo = "PAYMAN20260604034602768", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "Docker Courses", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate = "2026-06-04 09:21:53" },
                new { MerchantRefNo = "PAYMAN20260604034336999", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "Kubernetes Courses", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate = "2026-06-04 09:15:42" },


                new { MerchantRefNo = "PAYMAN20260603022521782", AmountPaid = 54111.80m, TxnCharges = 11878.20m, GrossAmount = 65990.00m, Service = "PHASE 1 — Full Stack Development", MobileNumber = "9912238196", CustomerEmail = "venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate="2026-06-03 08:05:39" },
                new { MerchantRefNo = "PAYMAN20260603022940518", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "PHASE 2 — Python + AI Foundations", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate = "2026-06-03 08:02:15" },
                new { MerchantRefNo = "PAYMAN20260603023353925", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "PHASE 3 — Machine Learning", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate = "2026-06-03 07:59:08" },
                new { MerchantRefNo = "PAYMAN20260603021701483", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "PHASE 4 — Generative AI", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate = "2026-06-03 07:49:04" },

                new { MerchantRefNo = "PAYMAN20260601032812805", AmountPaid = 56580.00m, TxnCharges = 12420.00m, GrossAmount = 69000.00m, Service = "Python for Data Science & Machine Learning Bootcamp", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301",CreatedDate="2026-06-01 08:59:19" },
                new { MerchantRefNo = "PAYMAN20260601032527501", AmountPaid = 57318.00m, TxnCharges = 12582.00m, GrossAmount = 69900.00m, Service = "Machine Learning A-Z", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate = "2026-06-01 08:56:27" },


                new { MerchantRefNo = "PAYMAN20260604000212543", AmountPaid = 81180.00m, TxnCharges = 17820.00m, GrossAmount = 99000.00m, Service = "Machine Learning Specialization — Andrew Ng", MobileNumber = "9963811121", CustomerEmail = "praveenkumar.emmadi@gmail.com", HSN = "999293", Name = "Emmadi Praveen Kumar", Address = "1-8, bachannapet, Kesireddypalli, Bachannapet, Warangal, Kesireddipalle, Telangana, India, 506221",CreatedDate="2026-06-04 05:33:41" },
                new { MerchantRefNo = "PAYMAN20260602000328270", AmountPaid = 49200.00m, TxnCharges = 10800.00m, GrossAmount = 60000.00m, Service = "Machine Learning Professional Certificate", MobileNumber = "9963811121", CustomerEmail = "praveenkumar.emmadi@gmail.com", HSN = "999293", Name = "Emmadi Praveen Kumar", Address = "1-8, bachannapet, Kesireddypalli, Bachannapet, Warangal, Kesireddipalle, Telangana, India, 506221", CreatedDate = "2026-06-02 05:34:29" },
                new { MerchantRefNo = "PAYMAN20260601015553526", AmountPaid = 81180.00m, TxnCharges = 17820.00m, GrossAmount = 99000.00m, Service = "Machine Learning Crash Course", MobileNumber = "9963811121", CustomerEmail = "praveenkumar.emmadi@gmail.com", HSN = "999293", Name = "Emmadi Praveen Kumar", Address = "1-8, bachannapet, Kesireddypalli, Bachannapet, Warangal, Kesireddipalle, Telangana, India, 506221", CreatedDate = "2026-06-01 07:27:10" },

                new { MerchantRefNo = "PAYMAN20260603023647609", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "AI Agent Development", MobileNumber = "9032682950", CustomerEmail = "siddharthavovoldas2@gmail.com", HSN = "999293", Name = "vovoladas Siddhartha", Address = "3-1-16/116/3/3/2A/C, S V Nagar, Mallapur, Rangareddi, KAPRA, Andhra Pradesh, India, 500076", CreatedDate = "2026-06-03 08:09:36" },
                new { MerchantRefNo = "PAYMAN20260603002115842", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "Cloud Computing", MobileNumber = "9398958120", CustomerEmail = "samatha@gmail.com", HSN = "999293", Name = "Samatha", Address = "3-45/1, Narsapuram, Mandal Siddipet, Mittapally, Siddipet, Medak, Mittapalle, Telangana, India, 502375", CreatedDate = "2026-06-01 07:27:10" },
            };

            var purchases = new List<CoursePurchase>();
            int id = 1;
            foreach (var item in rawData)
            {
                purchases.Add(new CoursePurchase
                {
                    Id = id++,
                    MerchantRefNo = item.MerchantRefNo,
                    AmountPaid = item.AmountPaid,
                    TxnCharges = item.TxnCharges,
                    GrossAmount = item.GrossAmount,
                    Service = item.Service,
                    MobileNumber = item.MobileNumber,
                    CustomerEmail = item.CustomerEmail,
                    HSN = item.HSN,
                    Name = item.Name,
                    Address = item.Address,   // hardcoded address for all
                    CreatedDate = ParseCreatedDate(item.CreatedDate)
                });
            }
            return purchases;
        }

        private DateTime ParseCreatedDate(string dateTimePart)
        {
            if (DateTime.TryParseExact(
                    dateTimePart,
                    "yyyy-dd-MM HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dt))
            {
                return dt;
            }

            return DateTime.Now;
        }


        [HttpGet]
        public IActionResult GetInvoiceDetails([FromQuery] string txn)
        {

            var purchases = GetCoursePurchases();
            var invoices = new List<object>();

            foreach (var purchase in purchases)
            {

                var gross = purchase.GrossAmount; // 95000

                var cgst = Math.Round(gross * 0.09m, 2);
                var sgst = Math.Round(gross * 0.09m, 2);

                var totalGst = cgst + sgst; // 17100
                var net = gross - totalGst;

                var invoice = new
                {
                    InvoiceNo = $"INV/{purchase.MerchantRefNo}",
                    Date = purchase.CreatedDate.ToString("dd-MMM-yyyy"),
                    TransactionId = purchase.MerchantRefNo,
                    createdDate = purchase.CreatedDate.ToString("dd-MMM-yyyy hh:mm tt"),
                    From = new
                    {
                        Name = "Payman Fintech Solutions Pvt. Ltd.",
                        Address = "9-12 Hmt Nagar Stnumber 01, Nacharam, Hyderabad, Telangana, 500076",
                        GSTIN = "36AAOCP3061H1Z9"
                    },
                    To = new
                    {
                        Name = purchase.Name,
                        Address = purchase.Address,
                        Email = purchase.CustomerEmail,
                        Mobile = purchase.MobileNumber,
                        GSTIN = ""
                    },
                    LineItems = new[]
                    {
                new
                {
                    SNo = 1,
                    Service = purchase.Service,
                    HsnSac = purchase.HSN,
                    Qty = 1,
                    UnitPrice = Math.Round(net, 2),
                    Amount = gross
                }
            },
                    CGST = cgst,
                    SGST = sgst,
                    Total = gross
                };

                invoices.Add(invoice);
            }

            return Ok(invoices);
        }


        //[HttpGet]
        //public IActionResult GetInvoiceDetails([FromQuery] string txn)
        //{

        //    var purchases = GetCoursePurchases();


        //    var purchase = GetCoursePurchases().FirstOrDefault(p => p.MerchantRefNo == "PAYMAN20260326005826465");
        //    if (purchase == null)
        //        return NotFound(new { success = false, message = "Transaction not found" });

        //    // GrossAmount is total paid inclusive of 18% GST
            

        //    var gross = purchase.GrossAmount; // 95000

        //    var cgst = Math.Round(gross * 0.09m, 2);
        //    var sgst = Math.Round(gross * 0.09m, 2);

        //    var totalGst = cgst + sgst; // 17100
        //    var net = gross - totalGst;

        //    var invoice = new
        //    {
        //        InvoiceNo = $"INV/{purchase.MerchantRefNo}",
        //        Date = purchase.CreatedDate.ToString("dd-MMM-yyyy"),
        //        TransactionId = purchase.MerchantRefNo,
        //        From = new
        //        {
        //            Name = "Payman Fintech Solutions Pvt. Ltd.",
        //            Address = "9-12 Hmt Nagar Stnumber 01, Nacharam, Hyderabad, Telangana, 500076",
        //            GSTIN = "36AAOCP3061H1Z9"
        //        },
        //        To = new
        //        {
        //            Name = purchase.Name,
        //            Address = purchase.Address,
        //            Email = purchase.CustomerEmail,
        //            Mobile = purchase.MobileNumber,
        //            GSTIN = ""
        //        },
        //        LineItems = new[]
        //        {
        //            new
        //            {
        //                SNo = 1,
        //                Service = purchase.Service,
        //                HsnSac = purchase.HSN,
        //                Qty = 1,
        //                UnitPrice = Math.Round(net, 2),
        //                Amount = gross
        //            }
        //        },
        //        CGST = cgst,
        //        SGST = sgst,
        //        Total = gross
        //    };

        //    return Ok(invoice);
        //}

        [HttpGet]
        public IActionResult ViewInvoice(string txn, string invoiceNo)
        {
            var purchase = GetCoursePurchases().FirstOrDefault(p => p.MerchantRefNo == txn);
            if (purchase == null)
                return NotFound("Transaction not found");


            var gross = purchase.GrossAmount; // 95000

            var cgst = Math.Round(gross * 0.09m, 2);
            var sgst = Math.Round(gross * 0.09m, 2);

            var totalGst = cgst + sgst; // 17100
            var net = gross - totalGst;

            var invoice = new
            {
                InvoiceNo = $"INV/{purchase.MerchantRefNo}",
                Date = purchase.CreatedDate.ToString("dd-MMM-yyyy"),
                TransactionId = purchase.MerchantRefNo,
                From = new
                {
                    Name = "Payman Fintech Solutions Pvt. Ltd.",
                    Address = "9-12 Hmt Nagar Stnumber 01, Nacharam, Hyderabad, Telangana, 500076",
                    GSTIN = "36AAOCP3061H1Z9"
                },
                To = new
                {
                    Name = purchase.Name,
                    Address = purchase.Address,
                    Email = purchase.CustomerEmail,
                    Mobile = purchase.MobileNumber,
                    GSTIN = ""
                },
                LineItems = new[]
                {
                    new
                    {
                        SNo = 1,
                        Service = purchase.Service,
                        HsnSac = purchase.HSN,
                        Qty = 1,
                        UnitPrice = Math.Round(net, 2),
                        Amount = gross
                    }
                },
                CGST = cgst,
                SGST = sgst,
                Total = gross
            };

            return View(invoice);
        }
    
    }

    public class CoursePurchase
    {
        public int Id { get; set; }
        public string MerchantRefNo { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal TxnCharges { get; set; }
        public decimal GrossAmount { get; set; }   // total paid (incl. 18% GST)
        public string Service { get; set; }
        public string MobileNumber { get; set; }
        public string CustomerEmail { get; set; }
        public string HSN { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }         // customer address
        public DateTime CreatedDate { get; set; }
    }

}
