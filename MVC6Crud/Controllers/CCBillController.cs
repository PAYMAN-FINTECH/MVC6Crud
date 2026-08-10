using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.BillPaymentsModel;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PaymanWeb;
using MVC6Crud.Models.PineLab;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Crypto;
using Razorpay.Api;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using WhatsAppApi.Helper;
using XAct;
using XAct.Library.Settings;
using static WhatsAppApi.Parser.FMessage;

namespace MVC6Crud.Controllers
{
    public class CCBillController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;
        public CCBillController(ApplicationDbContext context, IConfiguration configuration, DataUtils dataUtils)
        {
            _context = context;
            _configuration = configuration;
            _dataUtils = dataUtils;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CreditCardBillers(string searchTerm = "")
        {
            var userId = HttpContext.Session.GetString("UserId");
            //if (string.IsNullOrEmpty(userId))
            //{
            //    return Unauthorized(new { message = "User not authenticated" });
            //}

            //var balanceStr = await BlanceCheck();
            //var useBillAvenue = await _context.PayManGateways.FirstOrDefaultAsync();
            ////var availableAmount = await GetAvailableAmountAsync(userId, true);

            //using (HttpClient client = new HttpClient())
            //{
            //    client.DefaultRequestHeaders.Add("Accept", "application/json");
            //    client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
            //    client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
            //    client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
            //    client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "13.200.194.39");
            //    client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", "490007");

            //    string url = "https://api.instantpay.in/marketplace/utilityPayments/billers";
            //    var requestData = new
            //    {
            //        pagination = new { pageNumber = 1, recordsPerPage = 100 },
            //        filters = new { categoryKey = "C15", updatedAfterDate = "" }
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


            //    var query = _context.billAvenueCreditCardBillers
            //    .Where(t => t.blr_category_name.Trim().ToLower() == billerListModel.BillerName.Trim().ToLower());

            var query = _context.billAvenueCreditCardBillers
               .Where(t => t.blr_category_name.Trim().ToLower() == "Credit Card" && t.blr_alias_name == "B2B");



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
                    BillAvenue = true,
                    Billers = filteredBillers1 //.Where(t=>t.BillerName.Contains("ICICI"))
                };

                return Ok(result);
           // }
        }


        //[HttpPost]
        //public IActionResult FetchCreditCardBill([FromBody] BillRequestApp request)
        //{
        //    // Validate inputs
        //    if (string.IsNullOrEmpty(request.RegisteredMobile) ||
        //        string.IsNullOrEmpty(request.CreditCardLast4) ||
        //        string.IsNullOrEmpty(request.CustomerMobile) ||
        //        string.IsNullOrEmpty(request.BillerId))
        //    {
        //        return BadRequest(new { success = false, message = "All fields are required." });
        //    }

        //    // Simulated bill fetch logic (replace with actual DB or service call)
        //    var fakeBill = new BillResponseApp
        //    {
        //        Success = true,
        //        Message = "Bill fetched successfully.",
        //        ConsumerName = "John Doe",
        //        BillNumber = "CC123456789",
        //        BillDate = DateTime.Now.ToString("yyyy-MM-dd"),
        //        DueDate = DateTime.Now.AddDays(15).ToString("yyyy-MM-dd"),
        //        TotalAmount = 11234.56m,
        //        MinPayable = 1500.00m
        //    };

        //    return Ok(fakeBill);
        //}

        [HttpPost]
        public IActionResult SubmitCreditCardPayment([FromBody] PaymentRequestApp req)
        {
            if (req.Amount > 0)
            {
                return Ok(new { success = true, message = "Payment successful" });
            }

            return BadRequest(new { success = false, message = "Invalid payment request" });
        }

        public async Task<string> BlanceCheck()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Set up headers
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
                    client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "13.200.194.39");
                    client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", "490007");

                    var requestData = new
                    {
                        bankProfileId = "0",
                        accountNumber = "9100748033",
                        externalRef = "PROD1981",
                        latitude = "20.126",
                        longitude = "78.3228"
                    };

                    string jsonData = System.Text.Json.JsonSerializer.Serialize(requestData);
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("https://api.instantpay.in/accounts/balance", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        return "0.00";  // Return a default balance on failure
                    }

                    string responseContent = await response.Content.ReadAsStringAsync();
                    var balanceResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<BalanceResponse>(responseContent);

                    // Ensure `Data` and `Balance` exist before accessing them
                    return balanceResponse?.Data?.Balance?.Available ?? "0.00";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching balance: {ex.Message}");
                return "0.00"; // Return default balance if an error occurs
            }
        }

        public async Task<string> BillAvanueBalance()
        {
            var accessCode = _configuration["BillAvenueKeys:accessCode"];
            var workingKey = _configuration["BillAvenueKeys:workingKey"];
            var instituteId = _configuration["BillAvenueKeys:instituteId"];
            var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];

            string requestId = GenerateRequestId();
            string ver = "1.0";
            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<depositDetailsRequest> 
 <fromDate>2025-08-20</fromDate> 
 <toDate>2025-08-24</toDate> 
 <transType>CR</transType> 
 <agents> 
  <agentId>CC01RP91AGTBAA919897</agentId> 
  <agentId>CC01RP91AGTBAA919896</agentId> 
  <agentId>CC01RP91AGTBAA919898</agentId> 
 </agents> 
</depositDetailsRequest>";


            string encryptedPayload = Encrypt(merchantData, workingKey);
            string apiUrl = $"https://api.billavenue.com/billpay/enquireDeposit/fetchDetails/xml" +
                            $"?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}&encRequest={encryptedPayload}";

            // If required by BillAvenue, add endpoint IP also
            //if (!string.IsNullOrEmpty(endpointIp))
            //    apiUrl += $"&endIP={endpointIp}";



            using var client = new HttpClient();

            try
            {
                var response = await client.PostAsync(apiUrl, null);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    string decryptedResponse = Decrypt(result, workingKey);

                    // Deserialize XML to object
                    var serializer = new XmlSerializer(typeof(DepositEnquiryResponse));
                    DepositEnquiryResponse billResponse;
                    using (var reader = new StringReader(decryptedResponse))
                    {
                        billResponse = (DepositEnquiryResponse)serializer.Deserialize(reader);
                    }
                    return billResponse.CurrentBalance.ToString();
                }

                return "Error: " + response.StatusCode;
            }
            catch (Exception ex)
            {
                // Log ex
                return "Exception: " + ex.Message;
            }
        }



        [HttpPost]
        public async Task<IActionResult> FetchCreditCardBill([FromBody] BillRequestApp request)
        {
            if (string.IsNullOrWhiteSpace(request.BillerId) ||
                string.IsNullOrWhiteSpace(request.CreditCardLast4) ||
                string.IsNullOrWhiteSpace(request.RegisteredMobile))
            {
                return BadRequest(new { message = "Invalid request. Please provide all required fields." });
            }

            string mobile = request.RegisteredMobile.Length == 10 ? request.RegisteredMobile : request.CreditCardLast4;
            string lastfour = request.CreditCardLast4.Length == 4 ? request.CreditCardLast4 : request.RegisteredMobile;
            string paymentMode = "UPI"; // default fallback
            BillInstantPayResponse prePaymentResponse = null;

            try
            {
                // ✅ Decide provider (BillAvenue or InstantPay) 
                // Example: based on billerId prefix or config
                var useBillAvenue = await _context.PayManGateways.FirstOrDefaultAsync();

                if (useBillAvenue.BillAvenue == true)
                {
                    var prePaymentResponse1 = await BillAvenueFetchBill(request);

                    if (prePaymentResponse1 == null)
                        return NotFound(new { Success = false, Message = "Bill fetch failed (BillAvenue)." });

                    return Ok(new BillResponseApp
                    {
                        Success = prePaymentResponse1.Success,
                        Message = prePaymentResponse1.Message,
                        ConsumerName = prePaymentResponse1.ConsumerName,
                        BillNumber = prePaymentResponse1.ConsumerName,
                        BillDate = prePaymentResponse1.BillDate,
                        DueDate = prePaymentResponse1.DueDate,
                        TotalAmount = Convert.ToDecimal(prePaymentResponse1.TotalAmount/100),
                        MinPayable = prePaymentResponse1.MinPayable,
                        PaymentMode = paymentMode,
                        Param1 = prePaymentResponse1.Param1,
                        Param2 = prePaymentResponse1.Param2,
                        EnquiryReferenceId = prePaymentResponse1.EnquiryReferenceId,
                        BillerId = request.BillerId,
                        CuurentOutStanding = prePaymentResponse1.CuurentOutStanding,

                        BillerResponse = prePaymentResponse1.BillerResponse,
                        AdddditionalInfo = prePaymentResponse1.AdddditionalInfo,
                        BillFetchResponse = prePaymentResponse1.BillFetchResponse

                    });
                }
                else
                {
                    string param11 = "";
                    string param22 = "";
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

                         param11 = billerDetails.Data.Parameters[0].MaxLength == 10 ? mobile : lastfour;
                         param22 = billerDetails.Data.Parameters[1].MaxLength == 10 ? mobile : lastfour;

                        // 2. PrePayment enquiry
                        var enquiryPayload = new
                        {
                            billerId = request.BillerId,
                            initChannel = "AGT",
                            externalRef = "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                            inputParameters = new  { param1 = param11, param2 = param22 },
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

                    if (prePaymentResponse?.Data?.CustomerName != null &&
                        prePaymentResponse.Data.BillAmount != null &&
                        prePaymentResponse.Data.BillDueDate != null)
                    {
                        return Ok(new BillResponseApp
                        {
                            Success = true,
                            Message = "Bill fetched successfully.",
                            ConsumerName = prePaymentResponse.Data.CustomerName,
                            BillNumber = prePaymentResponse.Data.CustomerName,
                            BillDate = prePaymentResponse.Data.BillDueDate,
                            DueDate = prePaymentResponse.Data.BillDueDate,
                            TotalAmount = Convert.ToDecimal(prePaymentResponse.Data.BillAmount),
                            MinPayable = Convert.ToDecimal(prePaymentResponse.Data.BillAmount),
                            PaymentMode = paymentMode,
                            Param1 = param11,
                            Param2 = param22,
                            EnquiryReferenceId = prePaymentResponse.Data.EnquiryReferenceId
                        });
                    }

                    return NotFound(new { Success = false, Message = "Bill fetch failed or incomplete data." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Exception occurred.", Details = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequestApp request)
        {
            var useBillAvenue = await _context.PayManGateways.FirstOrDefaultAsync();

            var appPhone = HttpContext.Session.GetString("AppPhone");
            if (string.IsNullOrEmpty(appPhone) && request.Device == "Web")
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }

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

            var userWalletBalance = await _dataUtils.GetUserWalletAmount(request.Phone);

            if (Convert.ToDecimal(request.Amount) > Convert.ToDecimal(userWalletBalance))
            {
                DateTime istDateTime1 = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                await LogFailedTransactionAsync(userDetails, request, istDateTime1, "Insufficient balance", "BalanceCheck");
                return Ok(GenerateFailureResponse("Insufficient balance. Bill payment failed.", "failed"));
            }


            var pymentrefId= "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss");
            if (useBillAvenue.BillAvenue == true)
            {
                var accessCode = _configuration["BillAvenueKeys:accessCode"];
                var workingKey = _configuration["BillAvenueKeys:workingKey"];
                var instituteId = _configuration["BillAvenueKeys:instituteId"];
                var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];

                if (userDetails == null || aadharDetails1 == null || aadharDetails == null)
                    return BadRequest(new { message = "User details not found" });

                var billerDetails = await _context.billAvenueCreditCardBillers.FirstOrDefaultAsync(t => t.blr_id == request.BillerId);

                if (billerDetails == null)
                {
                    return Ok(GenerateFailureResponse("Biller details not found."));
                }


                var mode = billerDetails.PaymentModes;
                string PaymentMode = "UPI"; // default

                // Split the string into individual modes
                var availableModes = mode
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim().ToLower()) // normalize to lowercase
                    .ToList();

                string additionalInfo = "";

                // Priority check
                if (availableModes.Contains("cash"))  // cash1 -- for mob
                {
                    PaymentMode = "Cash";
                    additionalInfo = @"
    <info>
        <infoName>Remarks</infoName>
        <infoValue>CashPayment</infoValue>
    </info>";
                }
                else if (availableModes.Contains("wallet")) // wallet1 for MOB
                {
                    PaymentMode = "Wallet";
                    additionalInfo = @"
    <info>
        <infoName>WalletName</infoName>
        <infoValue>Forpay</infoValue>
    </info>
<info>
        <infoName>MobileNo</infoName>
        <infoValue>7286887024</infoValue> 
    </info>";
                }
                else
                {
                    if(request.Amount > 49999)
                    {
                        additionalInfo = @"
<info>
        <infoName>Payment Account Info</infoName>
        <infoValue>9652724937@kotak</infoValue> 
    </info>";
                    }
                    else
                    {
                        additionalInfo = @"
<info>
        <infoName>VPA</infoName>
        <infoValue>9652724937@kotak</infoValue> 
    </info>";
                    }
                }


                //For AGT

                //              < agentId >{ userDetails.BillAvenueAgentId}</ agentId >
                //< agentDeviceInfo >
                //  < ip >{ endpointIp}</ ip >
                //  < initChannel > AGT </ initChannel >
                //  < mac > 01 - 23 - 45 - 67 - 89 - ab </ mac >
                //</ agentDeviceInfo >

                // for MOB

   //             < agentId > CC01RP91MOBBAK024661 </ agentId >
   //< agentDeviceInfo >
   //       < app > tripozo </ app >
   //       < imei > 000000000000000 </ imei >
   //       < initChannel > MOB </ initChannel >
   //        < ip >{ endpointIp}</ ip >
   //        < os > android </ os >
   //</ agentDeviceInfo >

                string customerKycXml = "";

                if (request.BillerId != "SBIC00000NATDN")
                {
                    customerKycXml = $@"
    <customerAdhaar>{aadharDetails1.AadharNo}</customerAdhaar>
    <customerPan>{aadharDetails}</customerPan>";
                }

                string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<billPaymentRequest>
   <agentId>{userDetails.BillAvenueAgentId}</agentId>
                <agentDeviceInfo>
                  <ip>{endpointIp}</ip>
                  <initChannel>AGT</initChannel>
                  <mac>01-23-45-67-89-ab</mac>
                </agentDeviceInfo >
  <customerInfo>
    <REMITTER_NAME>{aadharDetails1.Name}</REMITTER_NAME>
    <customerMobile>{userDetails.Phone}</customerMobile>
    <customerEmail>{userDetails.Email}</customerEmail>
    {customerKycXml}
  </customerInfo>
  <billerId>{request.BillerId}</billerId>
{request.BillFetchResponse}
{request.BillerResponse}
{request.AdddditionalInfo}
<paymentRefId>{pymentrefId}</paymentRefId>
  <amountInfo>
    <amount>{request.Amount * 100}</amount>
    <currency>356</currency>
    <custConvFee>0</custConvFee>
  </amountInfo>
  <paymentMethod>
    <paymentMode>{PaymentMode}</paymentMode>
    <quickPay>N</quickPay>
    <splitPay>N</splitPay>
  </paymentMethod>
  <paymentInfo>
    {additionalInfo}
  </paymentInfo>
</billPaymentRequest>";

                // Encrypt payload
                var encryptedPayload = Encrypt(merchantData, workingKey);
                var ver = "1.0";
                var requestId = SecurityElement.Escape(request.EnquiryReferenceId ?? Guid.NewGuid().ToString("N"));
                DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));



                var amountInPaise = Convert.ToInt64(Math.Round(request.Amount * 100));
                var url = $"https://api.billavenue.com/billpay/extBillPayCntrl/billPayRequest/xml?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}&encRequest={HttpUtility.UrlEncode(encryptedPayload)}";

                // Check instant balance before calling API
                var instAvlBalance = await _dataUtils.GetInstantPayAmount();
                if (request.Amount > Convert.ToDouble(instAvlBalance))
                {
                    await LogFailedTransactionAsync(userDetails, request, istDateTime, "Insufficient balance", "BalanceCheck");
                    return Ok(GenerateFailureResponse("Insufficient balance. Bill payment failed.", "failed"));
                }

                ExtBillPayResponse billResponse = null;
                string decryptedResponse = null;
                bool apiCallSuccess = false;


                using var client = new HttpClient();

                try
                {
                    //request.EnquiryReferenceId = "76d47305fe68494c99a5b92cb01f2d42351";
                    // Send request
                    var httpResponse = await client.PostAsync(url, null);
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();


                    if (httpResponse.IsSuccessStatusCode && !string.IsNullOrEmpty(responseContent))
                    {
                        // Some providers return "encResponse=xxx". handle same as in FetchTransactionStatus
                        string enc = responseContent.Trim();
                        if (enc.StartsWith("encResponse=", StringComparison.OrdinalIgnoreCase))
                            enc = enc.Substring("encResponse=".Length).Trim();

                        string decrypted;
                        try
                        {
                            decrypted = Decrypt(enc, workingKey)?.Trim() ?? "";
                        }
                        catch (Exception ex)
                        {
                            await LogDebugAsync("Initial decrypt failed: " + ex.Message);
                            decrypted = "";
                        }

                        await LogDebugAsync("Initial API decrypted: " + decrypted);

                        if (!string.IsNullOrWhiteSpace(decrypted) && decrypted.TrimStart().StartsWith("<"))
                        {
                            try
                            {
                                var serializer = new XmlSerializer(typeof(ExtBillPayResponse));
                                using var reader = new StringReader(decrypted);
                                billResponse = (ExtBillPayResponse)serializer.Deserialize(reader);
                                apiCallSuccess = true;
                            }
                            catch (Exception ex)
                            {
                                await LogDebugAsync("Initial API XML deserialize failed: " + ex.Message);
                                await LogDebugAsync("Payload: " + decrypted);
                            }
                        }
                        else
                        {
                            await LogDebugAsync("Initial API decryption produced no XML.");
                            await LogFailedTransactionAsync(userDetails, request, istDateTime, "Initial Decrypt/Deserialize failed", "InitialAPI",true);
                        }
                    }
                    else
                    {
                        await LogFailedTransactionAsync(userDetails, request, istDateTime, $"Initial API HTTP {(int)httpResponse.StatusCode}", "InitialAPI");
                    }
                }
                catch (Exception ex)
                {
                    await LogFailedTransactionAsync(userDetails, request, istDateTime, "Initial API Exception: " + ex.Message, "InitialAPIException");
                }

                // ---------- Start DB transaction with retry strategy ----------
                var strategy = _context.Database.CreateExecutionStrategy();

                return await strategy.ExecuteAsync(async () =>
                {
                    await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // Determine initial result/status
                        var initialResultMsg = !string.IsNullOrWhiteSpace(billResponse?.ResponseReason)
                            ? billResponse.ResponseReason
                            : "INITIATED";

                        var initialTxnId = !string.IsNullOrWhiteSpace(billResponse?.TxnRefId)
                            ? billResponse.TxnRefId
                            : (!string.IsNullOrWhiteSpace(request.EnquiryReferenceId)
                                ? request.EnquiryReferenceId
                                : Guid.NewGuid().ToString("N"));

                        bool initialIsSuccess = string.Equals(
                            initialResultMsg,
                            "Successful",
                            StringComparison.OrdinalIgnoreCase
                        );

                        // Check duplicate before insert
                        var alreadyExists = await _context.payManPayOuts
                            .AnyAsync(x => x.PayOutId == initialTxnId);

                        if (!alreadyExists)
                        {
                            // Insert payout
                            Guid payOutDbId = await LogPayOutAsync(
                                userDetails,
                                request,
                                istDateTime,
                                initialResultMsg,
                                initialTxnId,
                                initialIsSuccess
                            );

                            // Insert payin history
                            await LogPayInHistoryAsync(
                                userDetails,
                                request,
                                istDateTime,
                                initialTxnId,
                                initialIsSuccess,
                                billResponse?.RespAmount ?? amountInPaise,
                                payOutDbId
                            );

                            // Save inserts
                            await _context.SaveChangesAsync();
                        }

                        // If initial success → commit and return
                        if (initialIsSuccess)
                        {
                            await dbTransaction.CommitAsync();

                            return Ok(new PaymentResponseProcess
                            {
                                Success = true,
                                Amount = request.Amount.ToString(),
                                OrderId = billResponse?.TxnRefId ?? initialTxnId,
                                ReferenceId = billResponse?.ApprovalRefNumber ?? "NA",
                                Category = "Credit Card",
                                BillerName = billerDetails?.blr_name ?? "Unknown",
                                Status = "SUCCESS"
                            });
                        }

                        // ---------- Status check ----------
                        string finalStatus = "Under Process";
                        ExtBillPayResponse111? statusResponse = null;

                        for (int attempt = 0; attempt < 3; attempt++)
                        {
                            statusResponse = await FetchTransactionStatusAsync(
                                request.EnquiryReferenceId,
                                accessCode,
                                instituteId,
                                workingKey
                            );

                            var txnStatus = statusResponse?.TxnList?.TxnStatus
                                ?.Trim()
                                ?.ToLower();

                            if (txnStatus == "success")
                            {
                                finalStatus = "SUCCESS";
                                break;
                            }

                            if (txnStatus == "failed" || txnStatus == "rejected")
                            {
                                finalStatus = txnStatus.ToUpper();
                                break;
                            }

                            await Task.Delay(TimeSpan.FromSeconds(3));
                        }

                        // Fetch existing inserted rows
                        var getPayOut = await _context.payManPayOuts
                            .FirstOrDefaultAsync(x => x.RefId == request.EnquiryReferenceId);

                        var getPayInHistory = await _context.payManHistories
                            .FirstOrDefaultAsync(x => x.TxnId == request.EnquiryReferenceId);

                        if (getPayOut != null)
                        {
                            getPayOut.Result = finalStatus;

                            var txnList = statusResponse?.TxnList;

                            if (txnList != null)
                            {
                                getPayOut.PayOutId =
                                    txnList.TxnReferenceId
                                    ?? txnList.ApprovalRefNumber
                                    ?? getPayOut.PayOutId;

                                getPayOut.AccountNo =
                                    txnList.Mobile
                                    ?? getPayOut.AccountNo;

                                var ip = txnList.InputParams ?? new List<InputParams11>();

                                var primaryLast4 = ip.FirstOrDefault(x =>
                                    !string.IsNullOrEmpty(x.ParamName) &&
                                    (
                                        x.ParamName.IndexOf("last 4",
                                            StringComparison.OrdinalIgnoreCase) >= 0
                                        ||
                                        x.ParamName.IndexOf("primary",
                                            StringComparison.OrdinalIgnoreCase) >= 0
                                    )
                                )?.ParamValue;

                                var regMobile = ip.FirstOrDefault(x =>
                                    !string.IsNullOrEmpty(x.ParamName) &&
                                    x.ParamName.IndexOf("registered",
                                        StringComparison.OrdinalIgnoreCase) >= 0
                                )?.ParamValue;

                                getPayOut.TxnType =
                                    primaryLast4 ?? getPayOut.TxnType;

                                getPayOut.IfscCode =
                                    regMobile ?? getPayOut.IfscCode;

                                getPayOut.AccountHolderName =
                                    txnList.RespCustomerName
                                    ?? getPayOut.AccountHolderName;
                            }

                            getPayOut.Status =
                                !string.Equals(finalStatus,
                                    "FAILURE",
                                    StringComparison.OrdinalIgnoreCase);

                            _context.payManPayOuts.Update(getPayOut);
                        }

                        if (getPayInHistory != null)
                        {
                            getPayInHistory.Status =
                                !string.Equals(finalStatus,
                                    "FAILURE",
                                    StringComparison.OrdinalIgnoreCase);

                            _context.payManHistories.Update(getPayInHistory);
                        }

                        // Save updates
                        await _context.SaveChangesAsync();

                        await dbTransaction.CommitAsync();

                        return Ok(new PaymentResponseProcess
                        {
                            Success = !string.Equals(
                                finalStatus,
                                "FAILURE",
                                StringComparison.OrdinalIgnoreCase
                            ),
                            Amount = request.Amount.ToString(),
                            OrderId =
                                statusResponse?.TxnList?.TxnReferenceId
                                ?? billResponse?.TxnRefId
                                ?? request.EnquiryReferenceId,
                            ReferenceId =
                                statusResponse?.TxnList?.ApprovalRefNumber
                                ?? billResponse?.ApprovalRefNumber
                                ?? "NA",
                            Category = "Credit Card",
                            BillerName = billerDetails?.blr_name ?? "Unknown",
                            Status = finalStatus
                        });
                    }
                    catch (Exception dbEx)
                    {
                        await dbTransaction.RollbackAsync();

                        var msg = $"DB save error: {dbEx.Message}";

                        Console.WriteLine(msg);

                        try
                        {
                            await LogFailedTransactionAsync(
                                userDetails,
                                request,
                                istDateTime,
                                msg,
                                "DB"
                            );
                        }
                        catch
                        {
                        }

                        return StatusCode(500, new
                        {
                            Success = false,
                            message = "Failed while saving transaction",
                            error = dbEx.Message
                        });
                    }
                });
            }

            else
            {
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
                            inputParameters = new { param1 = request.Param1, param2 = request.Param2, param3 = cardholderNum, param4 = cardholderName },
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
                            AccountNo = cardholderNum,
                            IfscCode = request.CustomerMobile,
                            Amount = Convert.ToDecimal(request.Amount),
                            PayoutCommission = 15,
                            BeneId = request.BillerId,
                            PayOutType = "CC BILL",
                            TxnType = request.LastFourDigits,
                            Email = userDetails.Email,
                            Status = stus,
                            DateTime = istDateTime,
                            Result = transactionResponse?.Status,
                            Device = "I - "+ request.Device
                        };

                        _context.payManPayOuts.Add(trandetails);
                        await _context.SaveChangesAsync();

                        var userWalletAmount = await GetUserWalletAmount(request.Phone);

                        var payInHistory = new PayManHistory
                        {
                            UserId = userDetails.Id,
                            UserPhone = request.Phone,
                            TxnId = transactionResponse.Data.TxnReferenceId,
                            Amount = Convert.ToDecimal(request.Amount),
                            CardNumber = request.CustomerMobile,
                            Mode = "CC Bill",
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
            }      
        }




        // Helper method to generate failure responses
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
                Status = status,
                Message = message
            };
        }

        public string BuildTransactionStatusXml(string trackValue)
        {
            // Use TRANS_REF_ID (BillAvenue expects TRANS_REF_ID / BILL_RESP_ID etc.)
            return $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<transactionStatusReq>
    <trackType>REQUEST_ID</trackType>
    <trackValue>{SecurityElement.Escape(trackValue ?? "")}</trackValue>
</transactionStatusReq>";
        }

        private async Task<ExtBillPayResponse111?> FetchTransactionStatusAsync(
            string trackValue,
            string accessCode,
            string instituteId,
            string workingKey)
        {
            try
            {
                string xmlData = BuildTransactionStatusXml(trackValue);
                string encryptedPayload = Encrypt(xmlData, workingKey);
                string requestId = "REQ" + DateTime.Now.ToString("yyyyMMddHHmmss");
                string ver = "1.0";

                string endpoint = "https://api.billavenue.com/billpay/transactionStatus/fetchInfo/xml";

                string url = endpoint +
                             $"?accessCode={Uri.EscapeDataString(accessCode)}" +
                             $"&requestId={Uri.EscapeDataString(requestId)}" +
                             $"&ver={Uri.EscapeDataString(ver)}" +
                             $"&instituteId={Uri.EscapeDataString(instituteId)}" +
                             $"&encRequest={Uri.EscapeDataString(encryptedPayload)}";

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(40) };

                var httpResponse = await client.PostAsync(url, null);
                var raw = await httpResponse.Content.ReadAsStringAsync();

                // Log raw HTTP response for debugging
                await LogDebugAsync($"Status API HTTP {(int)httpResponse.StatusCode} RAW: {raw}");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    await LogDebugAsync($"Status API returned HTTP {(int)httpResponse.StatusCode}");
                    return null;
                }

                // Response typically comes as: encResponse=xxxxx
                var trimmed = (raw ?? "").Trim();
                string encryptedResponse;
                if (trimmed.StartsWith("encResponse=", StringComparison.OrdinalIgnoreCase))
                    encryptedResponse = trimmed.Substring("encResponse=".Length).Trim();
                else
                    encryptedResponse = trimmed; // allow either

                if (string.IsNullOrWhiteSpace(encryptedResponse))
                {
                    await LogDebugAsync("Status API returned empty encResponse");
                    return null;
                }

                string decryptedXml;
                try
                {
                    decryptedXml = Decrypt(encryptedResponse, workingKey)?.Trim() ?? "";
                }
                catch (Exception ex)
                {
                    await LogDebugAsync($"Decrypt failed: {ex.Message}");
                    return null;
                }

                // Log decrypted xml for debugging
                await LogDebugAsync("Decrypted Status XML: " + decryptedXml);

                if (string.IsNullOrWhiteSpace(decryptedXml) || !decryptedXml.TrimStart().StartsWith("<"))
                {
                    await LogDebugAsync("Decrypted content is not valid XML.");
                    return null;
                }

                try
                {
                    var serializer = new XmlSerializer(typeof(ExtBillPayResponse111));
                    using var reader = new StringReader(decryptedXml);
                    var result = (ExtBillPayResponse111)serializer.Deserialize(reader);
                    return result;
                }
                catch (Exception ex)
                {
                    await LogDebugAsync("XML Deserialization failed for status response: " + ex.Message);
                    // optionally save decryptedXml to logs for manual inspection
                    await LogDebugAsync("Bad XML payload: " + decryptedXml);
                    return null;
                }
            }
            catch (Exception ex)
            {
                await LogDebugAsync("FetchTransactionStatusAsync exception: " + ex.Message);
                return null;
            }
        }

        // Logging helper used in above method - change implementation to use your logger
        private Task LogDebugAsync(string message)
        {
            var error = new ErrorModel
            {
                agId = message,
                payload = "",
                reqTime = "",
                requestId = "",
                respTime = "",
                uid = "",
                jsonBody = "",
                statuscode = false,

            };
            _context.errorModels.Add(error);
            _context.SaveChanges();
            return Task.CompletedTask;
        }

        // Fix LogPayOutAsync - DO NOT call SaveChanges here. Just add to context.
        private Task<Guid> LogPayOutAsync(PayManUsers user, PaymentRequestApp request,
            DateTime dateTime, string result, string txnId, bool status)
        {
            var payout = new PayManPayOut
            {
                UserId = user.Id,
                UserPhone = request.Phone,
                PayOutId = txnId,
                RefId = request.EnquiryReferenceId,
                AccountHolderName = request.customerName ?? "Unknown",
                AccountNo = request.holderMobile,
                IfscCode = request.CustomerMobile,
                Amount = Convert.ToDecimal(request.Amount), // ensure request.Amount valid
                PayoutCommission = 15,
                BeneId = request.BillerId,
                PayOutType = "CC BILL",
                TxnType = request.LastFourDigits,
                Email = user.Email,
                Status = status,
                DateTime = dateTime,
                Result = result,
                Device = "B - " + request.Device
            };

            _context.payManPayOuts.Add(payout);
            return Task.FromResult(payout.Id);
        }

        // Fix LogPayInHistoryAsync - DO NOT call SaveChanges here. keep same amount logic (amount param is paise)
        private async Task LogPayInHistoryAsync(PayManUsers user, PaymentRequestApp request,
            DateTime dateTime, string txnId, bool status, decimal amountInPaise, Guid payOutDbId)
        {
            var userWalletAmount = await GetUserWalletAmount(request.Phone);

            var history = new PayManHistory
            {
                UserId = user.Id,
                UserPhone = request.Phone,
                TxnId = txnId,
                Amount = amountInPaise / 100m,   // keep previous behavior
                CardNumber = request.holderMobile,
                Mode = "CC Bill",
                Status = status,
                Created = dateTime,
                AvlBalance = Convert.ToDecimal(userWalletAmount) - amountInPaise / 100m,
                PayInId = payOutDbId
            };

            _context.payManHistories.Add(history);
        }

        // Fix LogFailedTransactionAsync - avoid swallowing exceptions
        private async Task LogFailedTransactionAsync(PayManUsers user, PaymentRequestApp request,
            DateTime dateTime, string reason, string type, bool status= false)
        {
            try
            {
                // create unique ids for failed logs
                var failId = type + "_" + Guid.NewGuid().ToString("N");
                Guid payOutDbId =  await LogPayOutAsync(user, request, dateTime, reason, failId, status);
                await LogPayInHistoryAsync(user, request, dateTime, failId, status, (decimal)(request.Amount * 100), payOutDbId); // pass paise
                                                                                                                     // Persist the failed log entries immediately (if you want them saved regardless):
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Do not swallow: log to console / logger so you can find the real issue.
                Console.WriteLine("LogFailedTransactionAsync failed: " + ex);
                // rethrow or swallow based on your policy. I will rethrow so caller knows.
                throw;
            }
        }


        public async Task<string> GetUserWalletAmount(string phone)
        {
            var userDetails = await _context.payManUsers
                                            .FirstOrDefaultAsync(t => t.Phone == phone);
            if (userDetails == null)
                return "User not found";

            var payIns = await _context.payManPayIns.Where(t => t.UserPhone == phone && t.Status == true).ToListAsync();
            var accountVerifications = await _context.payManBeneficiaryAccounts.Where(t => t.UserPhone == phone && t.VerficationFlag == "Y").ToListAsync();
            var payOuts = await _context.payManPayOuts.Where(t => t.UserPhone == phone && t.Status == true).ToListAsync();

            decimal totalPayIn = payIns.Sum(t => t.Amount ?? 0);
            decimal payInCommission = payIns.Sum(t => t.PayInCommission ?? 0);
            decimal accountCommission = accountVerifications.Sum(t => t.VerificationComm ?? 0);
            decimal payOutCommission = payOuts.Sum(t => t.PayoutCommission ?? 0);
            decimal totalPayOut = payOuts.Sum(t => t.Amount ?? 0);

            decimal totalDeductions = payInCommission + accountCommission + payOutCommission + totalPayOut;
            decimal walletAmount = totalPayIn - totalDeductions;

            return walletAmount.ToString("0.00");
        }

        public async Task<IActionResult> FastTrackPay()
        {      
            return View();
        }

        [HttpPost]
        public IActionResult FetchBill([FromBody] BillFetchRequest request)
        {
            if (string.IsNullOrEmpty(request.BillerId))
                return BadRequest(new { message = "Invalid biller selected" });

            var bill = new
            {
                amount = "1101.30",
                dueDate = "12/06/2025",
                customerName = "KUSA AJAY KUSA AJAY"
            };

            return Ok(bill);
        }

        [HttpPost]
        public IActionResult MakePayment([FromBody] BillPaymentRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.BillerId))
            {
                return BadRequest(new { success = false, message = "Invalid payment data." });
            }

            // Dummy logic: always succeed if amount > 0
            if (decimal.TryParse(request.Amount, out var amt) && amt > 0)
            {
                return Ok(new { success = true, message = "Payment successful!" });
            }

            return Ok(new { success = false, message = "Payment failed. Please try again." });
        }


        [HttpPost]
        public async Task<IActionResult> ProcessPaymentWeb([FromBody] BillRequest request)
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");
            if (string.IsNullOrEmpty(appPhone))
            {
                return RedirectToAction("LogOut", "LogIn");
            }

            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);
            var documents = _context.userDocuments.FirstOrDefault(t => t.Phone == appPhone);

            if (request == null || string.IsNullOrEmpty(request.LastFourDigits) ||
                string.IsNullOrEmpty(request.Mobile) || string.IsNullOrEmpty(request.CustomerMobile) ||
                string.IsNullOrEmpty(request.BillerId) || request.Amount <= 0)
            {
                return Ok(new PaymentResponseProcess
                {
                    Success = false,
                    Amount = "",
                    OrderId = "",
                    ReferenceId = "",
                    Category = "Credit Card",
                    BillerName = "ICICI Credit Card"
                });
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
                    client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "13.200.194.39");
                    client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", "490007");

                    object paymentInfo = request.Wallet == "UPI"
                        ? new { Remarks = "VPA", VPA = "9652724937@kotak" }
                        : new { WalletName = "Forpay", MobileNo = "7286887024" };

                    var externalRefId = "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss");

                    var requestData = new
                    {
                        billerId = request.BillerId,
                        externalRef = externalRefId,
                        enquiryReferenceId = request.EnquiryReferenceId,
                        telecomCircle = "",
                        inputParameters = new { param1 = request.Param1, param2 = request.Param2 },
                        initChannel = "AGT",
                        deviceInfo = new
                        {
                            terminalId = "1998",
                            mobile = "7286887024",
                            postalCode = "505402",
                            geoCode = "28.6326,77.2175"
                        },
                        paymentMode = request.Wallet,
                        paymentInfo = paymentInfo,
                        remarks = new { param1 = request.Mobile, param2 = request.LastFourDigits },
                        transactionAmount = request.Amount,
                        customerPan = documents?.PanCardNumber ?? ""
                    };

                    string json = System.Text.Json.JsonSerializer.Serialize(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response1 = await client.PostAsync("https://api.instantpay.in/marketplace/utilityPayments/payment", content);
                    string responseContent = await response1.Content.ReadAsStringAsync();

                    var transactionResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<TransactionResponse>(responseContent);

                    if (transactionResponse == null || transactionResponse.Data == null || transactionResponse.Data.Pool == null)
                    {
                        return Ok(new PaymentResponseProcess
                        {
                            Success = false,
                            Amount = "",
                            OrderId = "",
                            ReferenceId = "",
                            Category = "Credit Card",
                            BillerName = "ICICI Credit Card",
                            Status = transactionResponse?.Status ?? "Unknown"
                        });
                    }

                    var istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                    using var txn = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var trandetails = new PayManPayOut
                        {
                            UserId = user.Id,
                            UserPhone = appPhone,
                            PayOutId = transactionResponse.Data.TxnReferenceId.ToUpper(),
                            RefId = transactionResponse.Data.PoolReferenceId,
                            AccountHolderName = transactionResponse.Data.BillDetails?.CustomerName ?? "Unknown",
                            AccountNo = request.CustomerMobile,
                            IfscCode = "",
                            Amount = Convert.ToDecimal(request.Amount),
                            PayoutCommission = 15,
                            BeneId = transactionResponse?.Status,
                            PayOutType = "CC BILL",
                            TxnType = "IMPS",
                            Email = user.Email,
                            Status = transactionResponse?.Status == "Transaction Successful",
                            DateTime = istDateTime,
                            Result = transactionResponse?.Status,
                            Device = "web"
                        };

                        _context.payManPayOuts.Add(trandetails);
                        await _context.SaveChangesAsync();

                        var userWalletAmount = await GetUserWalletAmount(appPhone);

                        var payInHistory = new PayManHistory
                        {
                            UserId = user.Id,
                            UserPhone = appPhone,
                            TxnId = transactionResponse.Data.TxnReferenceId.ToUpper(),
                            Amount = Convert.ToDecimal(request.Amount),
                            CardNumber = request.CustomerMobile,
                            Mode = "CC Bill",
                            Status = transactionResponse?.Status == "Transaction Successful",
                            Created = istDateTime,
                            AvlBalance = Convert.ToDecimal(userWalletAmount),
                            PayInId = trandetails.Id
                        };

                        _context.payManHistories.Add(payInHistory);
                        await _context.SaveChangesAsync();

                        await txn.CommitAsync();

                        var model = new PaymentStatusViewModel
                        {
                            IsSuccess = transactionResponse?.Status == "Transaction Successful",
                            Amount = Convert.ToInt32(transactionResponse.Data.BillDetails?.BillAmount ?? "0"),
                            TransactionId = transactionResponse.Data.TxnReferenceId.ToUpper(),
                            CardNumber = request.CustomerMobile
                        };

                        return View("PayStatus", model);
                    }
                    catch
                    {
                        await txn.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(new PaymentResponseProcess
                {
                    Success = false,
                    Status = "Error: " + ex.Message,
                    Amount = "",
                    OrderId = "",
                    ReferenceId = "",
                    Category = "Credit Card",
                    BillerName = "ICICI Credit Card"
                });
            }
        }


        public async Task<IActionResult> GetBankCategories()
        {
            // Fetch billers from DB (async)
            var bankCategories = await _context.billAvenueCreditCardBillers.ToListAsync();

            List<BillerRecord> billerRecords = new List<BillerRecord>();

            if (bankCategories.Any())
            {
                foreach (var bankCategory in bankCategories)
                {
                    BillerRecord bb = new BillerRecord
                    {
                        BillerId = bankCategory.blr_id,
                        BillerName = bankCategory.blr_name
                    };
                    billerRecords.Add(bb);
                }
            }

            var result = new
            {
                AvailableAmount = "0.00", // replace later with actual availableAmount.Amount
                InstantPayBalance = "0.00", // replace later with actual balanceStr
                Billers = billerRecords //.Where(t => t.BillerName.Contains("ICICI"))
            };

            // Return JSON response
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> CheckBillerCategory([FromQuery] string billerId)
        {
            if (string.IsNullOrEmpty(billerId))
                return BadRequest(new { message = "BillerId is required" });

            var biller1 = await _context.billAvenueCreditCardBillers
                .Where(b => b.blr_id == billerId)
                .FirstOrDefaultAsync();

            if (biller1 == null)
                return NotFound(new { message = "Biller not found" });

            if (biller1.blr_response == null || biller1.IsActive == false)
            {
                var status = await GetBillers(biller1.blr_id);
                if (!status)
                {
                    return NotFound(new { message = "Biller not found" });
                }
            }

            var biller = await _context.billAvenueCreditCardBillers
               .Where(b => b.blr_id == billerId)
               .Select(b => new BillerRecord
               {
                   BillerId = b.blr_id,
                   BillerName = b.blr_name,
                   CategoryName = b.blr_category_name,
                   CategoryKey = b.blr_response,
                   IsAvailable = b.IsActive
               })
               .FirstOrDefaultAsync();

            return Ok(biller);
        }

        public async Task<bool> GetBillers(string billerId)
        {
            if (string.IsNullOrEmpty(billerId))
                return false;

            // headers setup...
            var accessCode = _configuration["BillAvenueKeys:accessCode"];
            var workingKey = _configuration["BillAvenueKeys:workingKey"];
            var instituteId = _configuration["BillAvenueKeys:instituteId"];
            var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];

           // string accessCode = "AVCY20UZ93QR58GOXV";
            string requestId = GenerateRequestId();
            //string workingKey = "2EEFEE24E33539706EA1E532D60461C7";
            string ver = "1.0";
            //string instituteId = "RP91";
            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<billerInfoRequest>
  <billerId>{billerId}</billerId>
</billerInfoRequest>";

            string apiUrl = $"https://api.billavenue.com/billpay/extMdmCntrl/mdmRequestNew/xml" +
                            $"?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}";

            string encryptedPayload = Encrypt(merchantData, workingKey);

            using var client = new HttpClient();

            try
            {
                var content = new StringContent(encryptedPayload, Encoding.UTF8, "text/plain");
                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    string decryptedResponse = Decrypt(result, workingKey);

                    var xdoc = XDocument.Parse(decryptedResponse);

                    // Get billerStatus
                    var billerStatus = xdoc.Root
                        .Element("biller")?
                        .Element("billerStatus")?.Value;

                    // Get payment modes
                    var paymentModes = xdoc.Root
                        .Element("biller")?
                        .Element("billerPaymentModes")?
                        .Elements("paymentModeInfo")
                        .Select(pm => pm.Element("paymentMode")?.Value)
                        .Where(pm => !string.IsNullOrEmpty(pm))
                        .ToList();

                    // Join payment modes as a comma-separated string
                    var paymentModesStr = string.Join(",", paymentModes);

                    var biller = await _context.billAvenueCreditCardBillers
                        .Where(b => b.blr_id == billerId)
                        .FirstOrDefaultAsync();

                    if (biller != null)
                    {
                        biller.IsActive = billerStatus == "ACTIVE";
                        biller.PaymentModes = paymentModesStr;
                        biller.blr_response = decryptedResponse;
                        _context.billAvenueCreditCardBillers.Update(biller);
                        await _context.SaveChangesAsync();
                    }

                    return true;
                }

                return false; // request failed
            }
            catch (Exception ex)
            {
                // Log ex.Message here
                return false;
            }
        }


        public async Task<BillResponseApp> BillAvenueFetchBill(BillRequestApp request)
        {

            // headers setup...
            var accessCode = _configuration["BillAvenueKeys:accessCode"];
            var workingKey = _configuration["BillAvenueKeys:workingKey"];
            var instituteId = _configuration["BillAvenueKeys:instituteId"];
            var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];


            string requestId = GenerateRequestId();
            string ver = "1.0";

            var billerInfo = await _context.billAvenueCreditCardBillers
    .FirstOrDefaultAsync(t => t.blr_id == request.BillerId);
            if (billerInfo.blr_response == null)
            {
                return new BillResponseApp
                {
                    Success = false,
                    Message = "Bill fetch failed"
                };
            }

            XDocument doc = XDocument.Parse(billerInfo.blr_response);

            var inputs = doc.Descendants("billerInputParams")
                            .Descendants("paramInfo")
                            .Select(x => new
                            {
                                ParamName = (string)x.Element("paramName"),
                                MaxLength = (string)x.Element("maxLength")
                            })
                            .ToList();

            string mobile = request.RegisteredMobile.Length == 10 ? request.RegisteredMobile : request.CreditCardLast4;
            string lastfour = request.CreditCardLast4.Length == 4 ? request.CreditCardLast4 : request.RegisteredMobile;

            // Build <inputParams> dynamically
            XElement inputParams = new XElement("inputParams");

            foreach (var item in inputs)
            {
                string value = string.Empty;

                // Map ParamName → request value
                if (item.ParamName.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
                {
                    value = mobile.Trim();
                }
                else if (item.ParamName.Contains("credit", StringComparison.OrdinalIgnoreCase))
                {
                    value = lastfour.Trim();
                }
                // 👇 you can extend here for other param types as needed
                else
                {
                    value = ""; // default empty
                }

                inputParams.Add(
                    new XElement("input",
                        new XElement("paramName", item.ParamName),
                        new XElement("paramValue", value)
                    )
                );
            }

            // for MOB

   //         < agentId > CC01RP91MOBBAK024661 </ agentId >
   // < agentDeviceInfo >
   //       < app > tripozo </ app >
   //       < imei > 000000000000000 </ imei >
   //       < initChannel > MOB </ initChannel >
   //        < ip >{ endpointIp}</ ip >
   //        < os > android </ os >
   //</ agentDeviceInfo >

            // Convert to string
            string additionalInfo = inputParams.ToString();

            var userDetails = _context.payManUsers.Where(t => t.Phone == request.CustomerMobile).FirstOrDefault();

            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?> 
<billFetchRequest>
    <agentId>{userDetails.BillAvenueAgentId}</agentId>
                <agentDeviceInfo>
                  <ip>{endpointIp}</ip>
                  <initChannel>AGT</initChannel>
                  <mac>01-23-45-67-89-ab</mac>
                </agentDeviceInfo >
    <customerInfo>
        <customerMobile>{request.CustomerMobile}</customerMobile>
    </customerInfo>
    <billerId>{request.BillerId}</billerId>
{additionalInfo}
</billFetchRequest>";

            string encryptedPayload = Encrypt(merchantData, workingKey);

            string url = $"https://api.billavenue.com/billpay/extBillCntrl/billFetchRequest/xml" +
                         $"?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}&encRequest={encryptedPayload}";

            using var client = new HttpClient();
            try
            {
                var response = await client.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    return new BillResponseApp
                    {
                        Success = false,
                        Message = "Bill fetch failed"
                    };
                }

                var result = await response.Content.ReadAsStringAsync();

                string decryptedResponse = Decrypt(result, workingKey);

                // Deserialize XML to object
                var serializer = new XmlSerializer(typeof(BillAvenueBillFetchResponse));
                BillAvenueBillFetchResponse billResponse;
                using (var reader = new StringReader(decryptedResponse))
                {
                    billResponse = (BillAvenueBillFetchResponse)serializer.Deserialize(reader);
                }

                var wrapper = ExtractBillerResponse(decryptedResponse);

                if (billResponse?.billerResponse == null)
                {
                    return new BillResponseApp
                    {
                        Success = false,
                        Message = billResponse.errorInfo.Error.ErrorMessage ?? "Invalid response format"
                    };
                }

                // Map to BillResponseApp  Current Outstanding
                var minPayable = billResponse.additionalInfo?
                    .FirstOrDefault(i => i.infoName.Contains("Minimum Amount", StringComparison.OrdinalIgnoreCase))?.infoValue;

                var currentOutStanding = billResponse.additionalInfo?
                    .FirstOrDefault(i => i.infoName.Contains("Current Outstanding", StringComparison.OrdinalIgnoreCase))?.infoValue;


                return new BillResponseApp
                {
                    Success = billResponse.responseCode == "000",
                    Message = billResponse.responseCode == "000" ? "Bill fetched successfully" : "Failed to fetch bill",
                    ConsumerName = billResponse.billerResponse.customerName,
                    BillNumber = billResponse.billerResponse.customerName, // you might want a better unique ref
                    BillDate = wrapper== null? "": wrapper.BillerResponse,
                    DueDate = billResponse.billerResponse.dueDate,
                    TotalAmount = Convert.ToDecimal(billResponse.billerResponse.billAmount),
                    MinPayable = string.IsNullOrEmpty(minPayable) ? Convert.ToDecimal(billResponse.billerResponse.billAmount) : Convert.ToDecimal(minPayable),
                    CuurentOutStanding = string.IsNullOrEmpty(currentOutStanding) ? Convert.ToDecimal(billResponse.billerResponse.billAmount) : Convert.ToDecimal(currentOutStanding),
                    Param1 = mobile.Trim(),
                    Param2 = lastfour.Trim(),
                    EnquiryReferenceId = requestId,


                    BillerResponse = wrapper.BillerResponse,
                    AdddditionalInfo = wrapper.AdddditionalInfo,
                    BillFetchResponse = wrapper.BillFetchResponse
                };
            }
            catch (Exception ex)
            {
                return new BillResponseApp
                {
                    Success = false,
                    Message = "Exception: " + ex.Message
                };
            }
        }

        public static BillFetchWrapper ExtractBillerResponse(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var node = doc.SelectSingleNode("//billerResponse");
            var additionalInfo = doc.SelectSingleNode("//additionalInfo");
            var billFetchResponse= doc.SelectSingleNode("//inputParams");

            var wrapper = new BillFetchWrapper
            {
                BillerResponse = node?.OuterXml,
                AdddditionalInfo = additionalInfo?.OuterXml,
                BillFetchResponse = billFetchResponse?.OuterXml
            };

            return wrapper;
        }


        public async Task<IActionResult> BillPayment(PaymentRequestApp request)
        {
            var accessCode = _configuration["BillAvenueKeys:accessCode"];
            var workingKey = _configuration["BillAvenueKeys:workingKey"];
            var instituteId = _configuration["BillAvenueKeys:instituteId"];
            var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];

            var userDetails = await _context.payManUsers.FirstOrDefaultAsync(t => t.Phone == request.Phone);
            var userdocuments = await _context.userDocuments.FirstOrDefaultAsync(t => t.Phone == request.Phone);
            var aadharDetails = await _context.aadharDetails.FirstOrDefaultAsync(t => t.Phone == request.Phone);

            if (userDetails == null || userdocuments == null || aadharDetails == null)
                return BadRequest(new { message = "User details not found" });

            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<billPaymentRequest>
  <agentId>CC01RP91AGTBAA919897</agentId>
  <agentDeviceInfo>
    <ip>{endpointIp}</ip>
    <initChannel>AGT</initChannel>
    <mac>01-23-45-67-89-ab</mac>
  </agentDeviceInfo>
  <customerInfo>
    <REMITTER_NAME>{aadharDetails.Name}</REMITTER_NAME>
    <customerMobile>{userDetails.Phone}</customerMobile>
    <customerEmail>{userDetails.Email}</customerEmail>
    <customerAdhaar>{aadharDetails.AadharNo}</customerAdhaar>
    <customerPan>{userdocuments.PanCardNumber}</customerPan>
  </customerInfo>
  <billerId>{request.BillerId}</billerId>
{request.BillFetchResponse}
{request.BillerResponse}
{request.AdddditionalInfo}
  <amountInfo>
    <amount>{request.Amount * 100}</amount>
    <currency>356</currency>
    <custConvFee>0</custConvFee>
  </amountInfo>
  <paymentMethod>
    <paymentMode>Cash</paymentMode>
    <quickPay>N</quickPay>
    <splitPay>N</splitPay>
  </paymentMethod>
  <paymentInfo>
    <info>
      <infoName>Remarks</infoName>
      <infoValue>Received</infoValue>
    </info>
  </paymentInfo>
</billPaymentRequest>";

            string ver = "1.0";
            string encryptedPayload = Encrypt(merchantData, workingKey);

            string url = $"https://api.billavenue.com/billpay/extBillPayCntrl/billPayRequest/xml" +
                         $"?accessCode={accessCode}&requestId={request.EnquiryReferenceId}&ver={ver}&instituteId={instituteId}&encRequest={encryptedPayload}";

            using var client = new HttpClient();
            try
            {
                var response = await client.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { message = "Payment failed", error = err });
                }

                var result = await response.Content.ReadAsStringAsync();
                string decryptedResponse = Decrypt(result, workingKey);

                var serializer = new XmlSerializer(typeof(ExtBillPayResponse));
                ExtBillPayResponse billResponse;
                using (var reader = new StringReader(decryptedResponse))
                {
                    billResponse = (ExtBillPayResponse)serializer.Deserialize(reader);
                }

                return Ok(new ExtBillPayResponseResult
                {
                    success = true,
                    extBillPayResponse = billResponse
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Exception occurred", error = ex.Message });
            }
        }


        private string GenerateRequestId()
        {
            return $"{Guid.NewGuid():N}{DateTime.Now:fff}".Substring(0, 35);
        }

        private string Encrypt(string plainText, string key)
        {
            byte[] keyBytes = HexToBytes(MD5Hash(key));
            byte[] iv = new byte[16] {
                0x00, 0x01, 0x02, 0x03,
                0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0a, 0x0b,
                0x0c, 0x0d, 0x0e, 0x0f
            };

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                ICryptoTransform encryptor = aes.CreateEncryptor();

                byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return BitConverter.ToString(encrypted).Replace("-", "").ToLower();
            }
        }

        private string Decrypt(string encryptedHex, string key)
        {
            byte[] keyBytes = HexToBytes(MD5Hash(key));
            byte[] iv = new byte[16] {
                0x00, 0x01, 0x02, 0x03,
                0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0a, 0x0b,
                0x0c, 0x0d, 0x0e, 0x0f
            };

            byte[] encryptedBytes = HexToBytes(encryptedHex);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
        }

        private string MD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private byte[] HexToBytes(string hex)
        {
            int length = hex.Length;
            byte[] result = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return result;
        }


        public IActionResult CreateRazorpayOrder([FromBody] OrderRequestRazorpay request)
        {
            try
            {
                string key = "rzp_live_2EIqs9bxOALurg";   // Replace with your Razorpay Key
                string secret = "KcADpSWx41UCr6tPjGDT10EU";       // Replace with your Razorpay Secret

                RazorpayClient client = new RazorpayClient(key, secret);

                Dictionary<string, object> options = new Dictionary<string, object>();
                options.Add("amount", request.Amount * 100); // amount in paise
                options.Add("currency", "INR");
                options.Add("receipt", "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                options.Add("payment_capture", 1);

                Order order = client.Order.Create(options);

                return Ok(new
                {
                    success = true,
                    orderId = order["id"].ToString(),
                    amount = order["amount"],
                    currency = order["currency"]
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
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

            var key = "rzp_live_2EIqs9bxOALurg";
            var secret = "KcADpSWx41UCr6tPjGDT10EU";
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

                    var payInApp = new PayManPayIn
                    {
                        UserId = user.Id,
                        UserPhone = req.Phone,
                        TxnId = req.RazorpayOrderId,
                        EasePayId = req.RazorpayOrderId,
                        Email = email,
                        Amount = amount,
                        Gateway = "Razorpay",
                        BankName = cardIssuer ?? "NA",
                        CardBrand = cardNetwork ?? "NA",
                        IsCorporate = cardType,
                        PayInCommission = amount * margin / 100,
                        PaymanCommission = amount * 0 / 100, // update if needed
                        Created = istTime,
                        Status = status == "captured",
                        Result = status,
                        Device = "Web"
                    };

                    _context.payManPayIns.Add(payInApp);
                    await _context.SaveChangesAsync();

                    var avlAmount = await GetUserWalletAmount(req.Phone);

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


        [HttpGet]
        public async Task<IActionResult> GetRecentPayments(string phone)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(phone))
                    return BadRequest(new { success = false, message = "Phone number is required." });

                // Query the database for last 10 credit card payments for this user
                // Adjust table/column names to match your actual schema
                var payments = await _context.payManPayOuts
                    .Where(p => p.UserPhone == phone && p.AccountHolderName != null && p.PayOutType == "CC BILL") // filter only CC payments
                    .OrderByDescending(p => p.DateTime)
                    .Take(10)
                    .Select(p => new
                    {
                        p.Id,
                        date = p.DateTime,                     // DateTime
                        amount = p.Amount,
                        cardLast4 = p.TxnType,    // Last 4 digits (or full card, adjust if needed)
                        name = p.AccountHolderName,
                        status = p.Status == true ? "success" : "failed", // or p.Status directly if string
                        billerId = p.BeneId,                 // make sure this column exists
                        holderMobile = p.AccountNo,
                        customerMobile = p.UserPhone      // or p.UserPhone?
                    })
                    .ToListAsync();

                return Ok(payments);
            }
            catch (Exception ex)
            {
                // Log exception
                return StatusCode(500, new { success = false, message = "An error occurred while fetching recent payments." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchPayments(string mobile, string name)
        {
            var query = _context.payManPayOuts
                .Where(g => g.UserPhone == mobile &&  g.PayOutType == "CC BILL");

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(g =>
                    EF.Functions.Like(g.TxnType, $"%{name}%") ||
                    EF.Functions.Like(g.AccountNo, $"%{name}%") ||
                    EF.Functions.Like(g.AccountHolderName, $"%{name}%")
                );
            }

            var gateways = await query
                .OrderByDescending(g => g.DateTime)
                .Take(50)
                .Select(p => new
                {
                    Id= p.Id,
                    date = p.DateTime,                     // DateTime
                    amount = p.Amount,
                    cardLast4 = p.TxnType,    // Last 4 digits (or full card, adjust if needed)
                    name = p.AccountHolderName,
                    status = p.Status == true ? "success" : "failed", // or p.Status directly if string
                    billerId = p.BeneId,                 // make sure this column exists
                    holderMobile = p.AccountNo,
                    customerMobile = p.UserPhone
                })
                .ToListAsync();

            return Ok(gateways);
        }

    }
}
