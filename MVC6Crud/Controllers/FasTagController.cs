using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PaymanWeb;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using XAct;
using XAct.Users;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MVC6Crud.Controllers
{
    public class FasTagController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;
        private readonly PaymentService _ps;
        public FasTagController(ApplicationDbContext context, IConfiguration configuration, DataUtils utils, PaymentService ps)
        {
            _context = context;
            _configuration = configuration;
            _dataUtils = utils;
            _ps = ps;
        }
        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("UserName");
           var AppPhone = HttpContext.Session.GetString("AppPhone");
            //// Get user from session

            //if (string.IsNullOrEmpty(appPhone))
            //{
            //    return RedirectToAction("WebLogout", "Login"); // or show error view
            //}

            FastTagRechargeModel fastTagRechargeModel = new FastTagRechargeModel();
            fastTagRechargeModel.Phone = AppPhone;
            return View(fastTagRechargeModel);
        }

        public IActionResult WebLogout()
        {
            HttpContext.Session.Clear(); // Clear session
            return RedirectToAction("Index", "FasTag"); // ✅ Correct syntax
        }

        public IActionResult About()
        {
            return View();
        }
        public IActionResult PrivacyPolicy()
        {
            return View();
        }
        public IActionResult RefundPolicy()
        {
            return View();
        }
        public IActionResult Terms()
        {
            return View();
        }
        public IActionResult ContactUs()
        {
            return View();
        }
        public IActionResult PayStatus(PaymentStatusViewModel paymentStatusViewModel)
        {
            return View(paymentStatusViewModel);
        }
        [HttpPost]
        public IActionResult Contact(ContactFormModel model)
        {    
            // Process form submission:
            // e.g. send an email, or save to database

            TempData["SuccessMessage"] = "Thank you for contacting us. We will get back to you soon.";
            return RedirectToAction("Contact");
        }



        [HttpGet]
        public IActionResult Recharge()
        {
            // Example: You can fetch providers from API
            var providers = new List<SelectListItem>
        {
            new SelectListItem { Value = "1", Text = "HDFC FASTag" },
            new SelectListItem { Value = "2", Text = "ICICI FASTag" },
            new SelectListItem { Value = "3", Text = "Paytm FASTag" }
        };

            ViewBag.Providers = providers;
            return View();
        }

        [HttpGet]
        public IActionResult History()
        {

            var appPhone = HttpContext.Session.GetString("AppPhone");
            // Get user from session

            var history = _context.payManPayOuts
                .Where(t => t.PayOutType == "FasTrack" && t.UserPhone == appPhone)
                .Select(t => new FastTagHistory
                {
                    Amount = t.Amount,                                   // keep as decimal
                    Status = t.Status == true ? "Success" : "Failed",            // true/false to text
                    RefId = t.RefId,
                    AccountHolderName = t.AccountHolderName,
                    AccountNo = t.AccountNo,
                    DateTime = t.DateTime,                             // keep as DateTime
                })
                .OrderByDescending(h => h.DateTime)                      // now works correctly
                .ToList();

            return Json(history);
        }

        [HttpGet]
        public async Task<IActionResult> FastTrackPay(bool isFastTag = false)
        {
           // bool isValid = await _dataUtils.CheckBinAsync("797706", "Juarraaaaoijja");
            BeneficiaryListModel beneficiaryListModel = new BeneficiaryListModel();
            var appPhone = HttpContext.Session.GetString("AppPhone");
            // Get user from session

            if (string.IsNullOrEmpty(appPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }
            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == appPhone);
            var gateWayDetails = _context.PayManGateways.FirstOrDefault();
            if (user == null)
            {
                return RedirectToAction("WebLogout", "Login");
            }

            var instantPayAmount = await this._dataUtils.GetInstantPayAmount();
            var userWalletAmount = await this._dataUtils.GetUserWalletAmount(appPhone);

            beneficiaryListModel.InstatntPay = Convert.ToInt32(Convert.ToDecimal(instantPayAmount));
            beneficiaryListModel.UserAvlAmount = userWalletAmount;
            beneficiaryListModel.Phone = appPhone;
            beneficiaryListModel.PayOutMinAmount = gateWayDetails.PayOutMinAmount;
            beneficiaryListModel.PayOutMaxAmount = gateWayDetails.PayOutMaxAmount;
            beneficiaryListModel.MinBalanceAvl = gateWayDetails.MinBalanceAvl;
            beneficiaryListModel.isFastTag = isFastTag;

            return View(beneficiaryListModel);
        }

        [HttpGet]
        public async Task<IActionResult> FastTagBillers(string searchTerm = "")
        {
            var userId = HttpContext.Session.GetString("UserId");
            //if (string.IsNullOrEmpty(userId))
            //{
            //    return Unauthorized(new { message = "User not authenticated" });
            //}

            //var balanceStr = await this._dataUtils.BlanceCheck();
            //var useBillAvenue = await _context.PayManGateways.FirstOrDefaultAsync();
            ////var availableAmount = await GetAvailableAmountAsync(userId, true);

            //using (HttpClient client = new HttpClient())
            //{
            //    var clientId = _configuration["Ipay:ClientId"];
            //    var clientSecret = _configuration["Ipay:ClientSecret"];
            //    var outletId = _configuration["Ipay:OutletId"];
            //    var endpointIp = _configuration["Ipay:EndpointIp"];
            //    var macAddress = _configuration["DeviceInfo:Mac"];
            //    var ipAddress = _configuration["DeviceInfo:Ip"];

            //    client.DefaultRequestHeaders.Add("Accept", "application/json");
            //    client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
            //    client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", clientId);
            //    client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", clientSecret);
            //    client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", endpointIp);
            //    client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", outletId);

            //    string url = "https://api.instantpay.in/marketplace/utilityPayments/billers";
            //    var requestData = new
            //    {
            //        pagination = new { pageNumber = 1, recordsPerPage = 100 },
            //        filters = new { categoryKey = "C10", updatedAfterDate = "" }
            //    };

            //    string json = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
            //    var content = new StringContent(json, Encoding.UTF8, "application/json");
            //    HttpResponseMessage response = await client.PostAsync(url, content);

            //    if (!response.IsSuccessStatusCode)
            //    {
            //        return StatusCode((int)response.StatusCode, new { message = "Failed to fetch billers" });
            //    }

            //    var responseContent = await response.Content.ReadAsStringAsync();
            //    var billerResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<BillerResponse>(responseContent);

            //    var filteredBillers = string.IsNullOrWhiteSpace(searchTerm)
            //        ? billerResponse.Data.Records
            //        : billerResponse.Data.Records
            //            .Where(b => b.BillerName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            //            .ToList();

            var query = _context.billAvenueCreditCardBillers
              .Where(t => t.blr_category_name.Trim().ToLower() == "Fastag" && t.blr_alias_name == "B2B");



            var filteredBillers1 = await query
     .Select(x => new
     {
         billerId = x.blr_id,
         billerName = x.blr_name,
         category = x.blr_category_name,
         iconUrl = string.IsNullOrEmpty(x.iconUrl)
             ? "https://paymanfintech.in/images/default-biller.png"
             : x.iconUrl
     })
     .ToListAsync();

            var result = new
                {
                    AvailableAmount = "0.00",//availableAmount.Amount,
                    InstantPayBalance = "0.00",//balanceStr,
                    BillAvenue = true, //useBillAvenue.BillAvenue,
                    Billers = filteredBillers1 //.Where(t=>t.BillerName.Contains("ICICI"))
                };

                return Ok(result);
            //}
        }

        [HttpPost]
        public async Task<IActionResult> FetchCreditCardBill([FromBody] BillRequestApp request)
        {
            if (string.IsNullOrWhiteSpace(request.BillerId) ||
                string.IsNullOrWhiteSpace(request.CreditCardLast4))
            {
                return BadRequest(new { message = "Invalid request. Please provide all required fields." });
            }

            string requestId = _dataUtils.GenerateRequestId();
            //var appPhone = HttpContext.Session.GetString("AppPhone");
            //// Get user from session

            //if (string.IsNullOrEmpty(appPhone))
            //{
            //    return RedirectToAction("WebLogout", "Login"); // or show error view
            //}
            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == "9652724937");

            string paymentMode = "UPI"; // default fallback
            BillInstantPayResponse prePaymentResponse = null;

            try
            {
                // ✅ Decide provider (BillAvenue or InstantPay) 
                // Example: based on billerId prefix or config
                var useBillAvenue = await _context.PayManGateways.FirstOrDefaultAsync();

                if (useBillAvenue.BillAvenue == true)
                {
                    //request.UserPhone = appPhone;

                    var prePaymentResponse1 = await this._dataUtils.FetchBillAsync(request.BillerId, requestId,request.CreditCardLast4,"",request.CustomerMobile,user.Phone, "Fastag");

                    prePaymentResponse1.paymentMode = "UPI";
                    prePaymentResponse1.EnquiryReferenceId = requestId;

                    var minPayable = prePaymentResponse1.additionalInfo?.FirstOrDefault(i => i.infoName.Contains("Maximum", StringComparison.OrdinalIgnoreCase))?.infoValue;
                    var Balance = prePaymentResponse1.additionalInfo?.FirstOrDefault(i => i.infoName.Contains("Balance", StringComparison.OrdinalIgnoreCase))?.infoValue;


                    return Ok(new BillResponseApp
                    {
                        Success = true,
                        Message = "Bill fetched successfully.",
                        ConsumerName = prePaymentResponse1.billerResponse.customerName,
                        BillNumber = prePaymentResponse1.billerResponse.customerName,
                        BillDate = "",
                        DueDate = "",
                        TotalAmount = Convert.ToDecimal(Balance),
                        MinPayable = Convert.ToDecimal(minPayable),
                        PaymentMode = paymentMode,
                        Param1 = request.CreditCardLast4,
                        Param2 = "",
                        EnquiryReferenceId = prePaymentResponse1.EnquiryReferenceId,
                        CustomerType = "new"
                    });
                }
                else
                {
                    using (HttpClient client = new HttpClient())
                    {
                        // headers setup...
                        var clientId = _configuration["Ipay:ClientId"];
                        var clientSecret = _configuration["Ipay:ClientSecret"];
                        var outletId = _configuration["Ipay:OutletId"];
                        var endpointIp = _configuration["Ipay:EndpointIp"];
                        var macAddress = _configuration["DeviceInfo:Mac"];
                        var ipAddress = _configuration["DeviceInfo:Ip"];

                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                        client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                        client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", clientId);
                        client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", clientSecret);
                        client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", endpointIp);
                        client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", outletId);

                        // 1. Get biller details
                        var billerDetailsBody = new { billerId = request.BillerId };
                        var billerDetailsContent = new StringContent(JsonConvert.SerializeObject(billerDetailsBody), Encoding.UTF8, "application/json");

                        var billerDetailsResponse = await client.PostAsync("https://api.instantpay.in/marketplace/utilityPayments/billerDetails", billerDetailsContent);
                        if (!billerDetailsResponse.IsSuccessStatusCode)
                            return StatusCode(500, new { Success = false, Message = "Failed to fetch biller details." });

                        var billerDetailsJson = await billerDetailsResponse.Content.ReadAsStringAsync();
                        var billerDetails = JsonConvert.DeserializeObject<BillerResponse11>(billerDetailsJson);

                        if (billerDetails?.Data?.PaymentModes?.Any() == true)
                        {
                            var mode = billerDetails.Data.PaymentModes.FirstOrDefault(t => t.Name == "Wallet");
                            var modeCash = billerDetails.Data.PaymentModes.FirstOrDefault(t => t.Name == "Cash");

                            paymentMode = mode?.Name ?? modeCash?.Name ?? "UPI";
                        }

                        // 2. PrePayment enquiry
                        var enquiryPayload = new
                        {
                            billerId = request.BillerId,
                            initChannel = "AGT",
                            externalRef = "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                            inputParameters = new { param1 = request.CreditCardLast4 },
                            deviceInfo = new { mac = macAddress, ip = ipAddress },
                            remarks = new { param1 = "9849800697" },
                            transactionAmount = 10
                        };

                        var enquiryContent = new StringContent(JsonConvert.SerializeObject(enquiryPayload), Encoding.UTF8, "application/json");
                        var enquiryResponse = await client.PostAsync("https://api.instantpay.in/marketplace/utilityPayments/prePaymentEnquiry", enquiryContent);

                        if (!enquiryResponse.IsSuccessStatusCode)
                            return StatusCode(500, new { Success = false, Message = "PrePayment enquiry failed." });

                        var enquiryJson = await enquiryResponse.Content.ReadAsStringAsync();
                        prePaymentResponse = JsonConvert.DeserializeObject<BillInstantPayResponse>(enquiryJson);
                    }

                    var minPayable = prePaymentResponse.Data.AdditionalDetails?.FirstOrDefault(i => i.Name.Contains("Maximum", StringComparison.OrdinalIgnoreCase))?.Value;
                    var Balance = prePaymentResponse.Data.AdditionalDetails?.FirstOrDefault(i => i.Name.Contains("Balance", StringComparison.OrdinalIgnoreCase))?.Value;


                    if (prePaymentResponse?.Data?.CustomerName != null &&
                        prePaymentResponse.Data.AdditionalDetails[1].Value != null)
                    {
                        return Ok(new BillResponseApp
                        {
                            Success = true,
                            Message = "Bill fetched successfully.",
                            ConsumerName = prePaymentResponse.Data.CustomerName,
                            BillNumber = prePaymentResponse.Data.CustomerName,
                            BillDate = "",
                            DueDate = "",
                            TotalAmount = Convert.ToDecimal(Balance),
                            MinPayable = Convert.ToDecimal(minPayable),
                            PaymentMode = paymentMode,
                            Param1 = request.CreditCardLast4,
                            Param2 = "",
                            EnquiryReferenceId = prePaymentResponse.Data.EnquiryReferenceId,
                            CustomerType = "new"
                        });
                    }

                    return Ok(new { Success = false, Message = prePaymentResponse.Status });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = prePaymentResponse.Status, Details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequestApp request)
        {
            var useBillAvenue = await _context.PayManGateways.FirstOrDefaultAsync();

            if (request == null ||
                string.IsNullOrEmpty(request.CustomerMobile) ||
                string.IsNullOrEmpty(request.EnquiryReferenceId) ||
                request.Amount <= 0)
            {
                return Ok(GenerateFailureResponse("Invalid input request."));
            }

            var userDetails = await _context.payManUsers
                                           .FirstOrDefaultAsync(t => t.Phone == request.Phone);

            var aadharDetails = await _context.userDocuments.Where(t => t.Phone == request.Phone).Select(t => t.PanCardNumber).FirstOrDefaultAsync();

            var aadharDetails1 = await _context.aadharDetails.FirstOrDefaultAsync(t => t.Phone == request.Phone);

            if (userDetails == null || aadharDetails == null)
            {
                return Ok(GenerateFailureResponse("User or Aadhar details not found."));
            }



//            if (useBillAvenue.BillAvenue == true)
//            {
//                var accessCode = _configuration["BillAvenueKeys:accessCode"];
//                var workingKey = _configuration["BillAvenueKeys:workingKey"];
//                var instituteId = _configuration["BillAvenueKeys:instituteId"];
//                var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];

//                if (userDetails == null || aadharDetails1 == null || aadharDetails == null)
//                    return BadRequest(new { message = "User details not found" });

//                var billerDetails = await _context.billAvenueCreditCardBillers.FirstOrDefaultAsync(t => t.blr_id == request.BillerId);

//                if (billerDetails == null)
//                {
//                    return Ok(GenerateFailureResponse("Biller details not found."));
//                }


//                var mode = billerDetails.PaymentModes;
//                string PaymentMode = "UPI"; // default

//                // Split the string into individual modes
//                var availableModes = mode
//                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
//                    .Select(m => m.Trim().ToLower()) // normalize to lowercase
//                    .ToList();

//                string additionalInfo = "";

//                // Priority check
//                if (availableModes.Contains("cash"))
//                {
//                    PaymentMode = "Cash";
//                    additionalInfo = @"
//    <info>
//        <infoName>Remarks</infoName>
//        <infoValue>CashPayment</infoValue>
//    </info>";
//                }
//                else if (availableModes.Contains("wallet"))
//                {
//                    PaymentMode = "Wallet";
//                    additionalInfo = @"
//    <info>
//        <infoName>WalletName</infoName>
//        <infoValue>Forpay</infoValue>
//    </info>
//<info>
//        <infoName>MobileNo</infoName>
//        <infoValue>7286887024</infoValue> 
//    </info>";
//                }
//                else
//                {
//                    additionalInfo = @"
//    <info>
//        <infoName>Remarks</infoName>
//        <infoValue>VPA</infoValue>
//    </info>
//<info>
//        <infoName>VPA</infoName>
//        <infoValue>9652724937@kotak</infoValue> 
//    </info>";
//                }

//                string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
//<billPaymentRequest>
//  <agentId>{userDetails.BillAvenueAgentId}</agentId>
//  <agentDeviceInfo>
//    <ip>{endpointIp}</ip>
//    <initChannel>AGT</initChannel>
//    <mac>01-23-45-67-89-ab</mac>
//  </agentDeviceInfo>
//  <customerInfo>
//    <REMITTER_NAME>{aadharDetails1.Name}</REMITTER_NAME>
//    <customerMobile>{userDetails.Phone}</customerMobile>
//    <customerEmail>{userDetails.Email}</customerEmail>
//    <customerAdhaar>{aadharDetails1.AadharNo}</customerAdhaar>
//    <customerPan>{aadharDetails}</customerPan>
//  </customerInfo>
//  <billerId>{request.BillerId}</billerId>
//{request.BillFetchResponse}
//{request.BillerResponse}
//{request.AdddditionalInfo}
//  <amountInfo>
//    <amount>{request.Amount * 100}</amount>
//    <currency>356</currency>
//    <custConvFee>0</custConvFee>
//  </amountInfo>
//  <paymentMethod>
//    <paymentMode>{PaymentMode}</paymentMode>
//    <quickPay>N</quickPay>
//    <splitPay>N</splitPay>
//  </paymentMethod>
//  <paymentInfo>
//    {additionalInfo}
//  </paymentInfo>
//</billPaymentRequest>";

//                string ver = "1.0";
//                string encryptedPayload = this._dataUtils.Encrypt(merchantData, workingKey);

//                string url = $"https://api.billavenue.com/billpay/extBillPayCntrl/billPayRequest/xml" +
//                             $"?accessCode={accessCode}&requestId={request.EnquiryReferenceId}&ver={ver}&instituteId={instituteId}&encRequest={encryptedPayload}";

//                using var client = new HttpClient();
//                try
//                {
//                    var response = await client.PostAsync(url, null);

//                    if (!response.IsSuccessStatusCode)
//                    {
//                        var err = await response.Content.ReadAsStringAsync();
//                        return StatusCode((int)response.StatusCode, new { Success = false, message = "Payment failed", error = err});
//                    }

//                    var result = await response.Content.ReadAsStringAsync();
//                    string decryptedResponse = this._dataUtils.Decrypt(result, workingKey);

//                    var serializer = new XmlSerializer(typeof(ExtBillPayResponse));
//                    ExtBillPayResponse billResponse;
//                    using (var reader = new StringReader(decryptedResponse))
//                    {
//                        billResponse = (ExtBillPayResponse)serializer.Deserialize(reader);
//                    }

//                    if (response == null || billResponse.TxnRefId == null)
//                    {
//                        return Ok(GenerateFailureResponse("Bill payment failed.", await response.Content.ReadAsStringAsync()));
//                    }

//                    DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
//                                                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

//                    // bool stus = response.responseCode == "000"; // success check

//                    var trandetails = new PayManPayOut
//                    {
//                        UserId = userDetails.Id,
//                        UserPhone = request.Phone,
//                        PayOutId = billResponse.TxnRefId,
//                        RefId = billResponse.ApprovalRefNumber,
//                        AccountHolderName = billResponse.RespCustomerName ?? "Unknown",
//                        AccountNo = request.LastFourDigits,
//                        IfscCode = request.CustomerMobile,
//                        Amount = Convert.ToDecimal(billResponse.RespAmount / 100),
//                        PayoutCommission = 15,
//                        BeneId = request.BillerId,
//                        PayOutType = "FasTag",
//                        TxnType = request.LastFourDigits,
//                        Email = userDetails.Email,
//                        Status = true,
//                        DateTime = istDateTime,
//                        Result = billResponse.ResponseReason,
//                        Device = "B - " + request.Device
//                    };

//                    _context.payManPayOuts.Add(trandetails);
//                    await _context.SaveChangesAsync();

//                    var userWalletAmount = await  this._dataUtils.GetUserWalletAmount(request.Phone);

//                    var payInHistory = new PayManHistory
//                    {
//                        UserId = userDetails.Id,
//                        UserPhone = request.Phone,
//                        TxnId = billResponse.TxnRefId,
//                        Amount = Convert.ToDecimal(billResponse.RespAmount / 100),
//                        CardNumber = request.holderMobile,
//                        Mode = "CC Bill",
//                        Status = true,
//                        Created = istDateTime,
//                        AvlBalance = Convert.ToDecimal(userWalletAmount),
//                        PayInId = trandetails.Id
//                    };

//                    _context.payManHistories.Add(payInHistory);
//                    await _context.SaveChangesAsync();

//                    return Ok(new PaymentResponseProcess
//                    {
//                        Success = true,
//                        Amount = billResponse.RespAmount.ToString(),
//                        OrderId = billResponse.TxnRefId?.ToUpper(),
//                        ReferenceId = billResponse.ApprovalRefNumber?.ToUpper(),
//                        Category = "Credit Card",
//                        BillerName = "ICICI Credit Card",
//                        Status = billResponse.ResponseReason
//                    });
//                }
//                catch (Exception ex)
//                {
//                    return StatusCode(500, new { Success = false, message = "Exception occurred", error = ex.Message });
//                }
//            }

//            else
//            {
                // Load sensitive values from configuration
                var clientId = _configuration["Ipay:ClientId"];
                var clientSecret = _configuration["Ipay:ClientSecret"];
                var outletId = _configuration["Ipay:OutletId"];
                var endpointIp = _configuration["Ipay:EndpointIp"];

                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                        client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                        client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", clientId);
                        client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", clientSecret);
                        client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", endpointIp);
                        client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", outletId);

                        object paymentInfo;
                        switch (request.PaymentMode.Trim().ToUpperInvariant())
                        {
                            case "CASH":
                                paymentInfo = new { Remarks = "CashPayment" };
                                break;
                            case "UPI":
                                paymentInfo = new { Remarks = "VPA", VPA = "9652724937@kotak" };
                                break;
                            default:
                                paymentInfo = new { WalletName = "Forpay", MobileNo = "7286887024" };
                                break;
                        }


                        var cardholderNum = request.holderMobile == null ? userDetails.Phone : request.holderMobile;
                        var cardholderName = request.customerName == null ? userDetails.FirstName : request.customerName;

                        var requestData = new
                        {
                            billerId = request.BillerId,
                            externalRef = "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                            enquiryReferenceId = request.EnquiryReferenceId,
                            telecomCircle = "",
                            inputParameters = new { param1 = request.Param1 },
                            initChannel = "AGT",
                            remitterDetails = cardholderName,
                            deviceInfo = new
                            {
                                terminalId = "1998",
                                mobile = "7286887024",
                                postalCode = "505402",
                                geoCode = "28.6326,77.2175"
                            },
                            paymentMode = request.PaymentMode,
                            paymentInfo = paymentInfo,
                            remarks = new { param1 = request.CustomerMobile },
                            transactionAmount = request.Amount,
                            customerPan = aadharDetails
                        };

                        string json = JsonConvert.SerializeObject(requestData);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await client.PostAsync("https://api.instantpay.in/marketplace/utilityPayments/payment", content);
                        string responseContent = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            return Ok(GenerateFailureResponse("API request failed."));
                        }

                        var transactionResponse = JsonConvert.DeserializeObject<TransactionResponse>(responseContent);
                        if (transactionResponse?.Data?.Pool == null)
                        {
                            return Ok(GenerateFailureResponse("Transaction pool is null.", transactionResponse?.Status ?? "FAILED"));
                        }

                        DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                        bool stus = false;
                        if (transactionResponse?.Status == "Transaction Under Process" || transactionResponse?.Status == "Transaction Successful")
                        {
                            stus = true;
                        }

                        var trandetails = new PayManPayOut
                        {
                            UserId = userDetails.Id,
                            UserPhone = request.Phone,
                            PayOutId = transactionResponse.Data.TxnReferenceId,
                            RefId = transactionResponse.Data.PoolReferenceId,
                            AccountHolderName = transactionResponse.Data.BillDetails?.CustomerName ?? "Unknown",
                            AccountNo = request.Param1,
                            IfscCode = request.CustomerMobile,
                            Amount = Convert.ToDecimal(request.Amount),
                            PayoutCommission = 15,
                            BeneId = request.BillerId,
                            PayOutType = "FasTrack",
                            TxnType = request.LastFourDigits,
                            Email = userDetails.Email,
                            Status = stus,
                            DateTime = istDateTime,
                            Result = transactionResponse?.Status,
                            Device = "I - " + request.Device
                        };

                        _context.payManPayOuts.Add(trandetails);
                        await _context.SaveChangesAsync();

                        var userWalletAmount = await this._dataUtils.GetUserWalletAmount(request.Phone);

                        var payInHistory = new PayManHistory
                        {
                            UserId = userDetails.Id,
                            UserPhone = request.Phone,
                            TxnId = transactionResponse.Data.TxnReferenceId,
                            Amount = Convert.ToDecimal(request.Amount),
                            CardNumber = request.CustomerMobile,
                            Mode = "FasTrack",
                            Status = stus,
                            Created = istDateTime,
                            AvlBalance = Convert.ToDecimal(userWalletAmount),
                            PayInId = trandetails.Id
                        };
                        _context.payManHistories.Add(payInHistory);
                        await _context.SaveChangesAsync();



                        return Ok(new PaymentResponseProcess
                        {
                            Success = true,
                            Amount = transactionResponse.Data.BillDetails.BillAmount,
                            OrderId = transactionResponse.Data.TxnReferenceId.ToUpper(),
                            ReferenceId = transactionResponse.Data.ExternalRef.ToUpper(),
                            Category = "Credit Card",
                            BillerName = "ICICI Credit Card",
                            Status = transactionResponse.Status
                        });
                    }
                }
                catch (Exception ex)
                {
                    return Ok(GenerateFailureResponse("Exception occurred while processing payment."));
                }
            //}
        }

        private PaymentResponseProcess GenerateFailureResponse(string message, string status = "FAILED")
        {
            return new PaymentResponseProcess
            {
                Success = false,
                Amount = "",
                OrderId = "",
                ReferenceId = "",
                Category = "Credit Card",
                BillerName = "ICICI Credit Card",
                Status = status
            };
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

                var paymentResponse =
                    JsonConvert.DeserializeObject<PaymentDecryptResponse>(decryptedJson);

                paymentResponse.Gatewayname = "Vegaah";

                var res = await _dataUtils.PayInDbCall(paymentResponse);

                if(res.Gateway == "fastag")
                {
                    // ✅ Safe redirect with encoding
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
                    // ✅ Safe redirect with encoding
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


        //[HttpPost]
        //public IActionResult Receipt(IFormCollection form)
        //{
        //    try
        //    {
        //        Request.EnableBuffering();

        //        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        //        string rawBody = reader.ReadToEnd();
        //        Request.Body.Position = 0;

        //        var encData = form["encData"].ToString();

        //        var errorLog = new ErrorModel
        //        {
        //            agId = "VEGAAH_CALLBACK",
        //            payload = rawBody,        // ✅ full callback
        //            jsonBody = encData,       // encrypted data
        //            reqTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        //            respTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        //            statuscode = true
        //        };

        //        _context.errorModels.Add(errorLog);
        //        _context.SaveChanges();

        //        // 1️⃣ Decrypt
        //        var decryptedJson = DecryptResponse(encData);

        //        // 2️⃣ Deserialize
        //        var response = JsonSerializer.Deserialize<VegaahFinalResponse>(
        //            decryptedJson,
        //            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        //        );

        //        // 4️⃣ Save callback (ALWAYS save)
        //        var error1 = new ErrorModel
        //        {
        //            agId = "VEGAAH_CALLBACK",
        //            payload = decryptedJson,        // decrypted JSON
        //            jsonBody = encData,             // encrypted raw data
        //            requestId = response.transactionId,
        //            uid = response.orderDetails?.orderId,
        //            reqTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        //            respTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        //            statuscode = response.result == "SUCCESS"
        //        };
        //        _context.errorModels.Add(error1);
        //        _context.SaveChanges();


        //        // 3️⃣ Validate Signature (MANDATORY)
        //        bool isValid = ValidateResponseSignature(
        //            response, _configuration["Vegaah:Secret"]
        //        );

        //        // 4️⃣ Save callback (ALWAYS save)
        //        var error = new ErrorModel
        //        {
        //            agId = "VEGAAH_CALLBACK",
        //            payload = decryptedJson,        // decrypted JSON
        //            jsonBody = encData,             // encrypted raw data
        //            requestId = response.transactionId,
        //            uid = response.orderDetails?.orderId,
        //            reqTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        //            respTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        //            statuscode = isValid && response.result == "SUCCESS"
        //        };

        //        _context.errorModels.Add(error);
        //        _context.SaveChanges();

        //        // 5️⃣ Reject if signature invalid
        //        if (!isValid)
        //            return BadRequest("Invalid signature");

        //        // 6️⃣ Business logic
        //        if (response.result == "SUCCESS")
        //        {
        //            // Update order as PAID
        //        }
        //        else
        //        {
        //            // Mark FAILED / TIMEOUT
        //        }

        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        // log ex
        //        return BadRequest();
        //    }
        //}

        public async Task<IActionResult> ProcessPostRequest()
        {
            using var reader = new StreamReader(Request.Body);
            string rawData = await reader.ReadToEndAsync();

            var keyValuePairs = System.Web.HttpUtility.ParseQueryString(rawData);
            string encryptedData = keyValuePairs["data"];

            

            encryptedData = encryptedData.Replace(" ", "+");

            // 🔐 Decrypt
            string decryptedJson = DecryptResponse(encryptedData);

            var paymentResponse =
                JsonConvert.DeserializeObject<PaymentDecryptResponse>(decryptedJson);
            

            var res = await _dataUtils.PayInDbCall(paymentResponse);

            return View();

            
        }




        private string DecryptResponse(string encryptedResponse)
        {
            string merchantKey = _configuration["Vegaah:Secret"];

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

        public static bool ValidateResponseSignature(
        VegaahFinalResponse response,
        string secret)
        {
            if (response == null)
                return false;

            if (string.IsNullOrWhiteSpace(response.signature))
                return false;

            // 1️⃣ Build signature string (ORDER MATTERS)
            string signatureString =
                $"{response.orderDetails?.orderId}|" +
                $"{response.transactionId}|" +
                $"{response.amount:0.00}|" +
                $"{response.currency}|" +
                $"{response.result}|" +
                $"{secret}";

            // 2️⃣ Generate SHA256 hash
            string generatedSignature = GenerateSHA256(signatureString);

            // 3️⃣ Compare signatures (case-insensitive)
            return string.Equals(
                generatedSignature,
                response.signature,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string GenerateSHA256(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(bytes);

            var sb = new StringBuilder();
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString().ToUpperInvariant();
        }

        [HttpPost]
        public async Task<IActionResult> Initiate(string orderId, string amount, string actionType, string email, string phone, string custmobile, string custname, string custcard, string divice)
        {
            try
            {
                var hosted = _configuration["Vegaah:TerminalId"];
                var password = _configuration["Vegaah:Password"];
                var secret = _configuration["Vegaah:Secret"];
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
                        description = "toll and fastag bills"
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
                        Gateway = custcard == "fastag" ? "fastag": "Vegaah",
                        Created = istTime,
                        Status = false,
                        Result = "PENDING",
                        Device = divice,
                        CreditCardHolderNum= custcard,
                        CreditCardHolderName= custname,
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

        public IActionResult PayStatus1(PaymentStatusViewModel paymentStatusViewModel)
        {
            return View(paymentStatusViewModel);
        }

    }
}
