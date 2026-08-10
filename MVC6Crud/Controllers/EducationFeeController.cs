using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PaymanWeb;
using MVC6Crud.Models.PineLab;
using Newtonsoft.Json;
using System.Text;
using XAct.Users;

namespace MVC6Crud.Controllers
{
    public class EducationFeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;
        public EducationFeeController(ApplicationDbContext context, IConfiguration configuration, DataUtils utils)
        {
            _context = context;
            _configuration = configuration;
            _dataUtils = utils;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> EducationFeePay()
        {
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

            return View(beneficiaryListModel);
        }

        [HttpGet]
        public async Task<IActionResult> EducationFeeBillers(int page =1, int pageSize=10, string state = "")
        {
            var userId = HttpContext.Session.GetString("UserId");
            //if (string.IsNullOrEmpty(userId))
            //{
            //    return Unauthorized(new { message = "User not authenticated" });
            //}

            var balanceStr = await this._dataUtils.BlanceCheck();
            var useBillAvenue = await _context.PayManGateways.FirstOrDefaultAsync();
            //var availableAmount = await GetAvailableAmountAsync(userId, true);

            using (HttpClient client = new HttpClient())
            {
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




                string url = "https://api.instantpay.in/marketplace/utilityPayments/billers";

                // Build filters dynamically
                Dictionary<string, string> filters = new Dictionary<string, string>
{
    { "categoryKey", "C09" },
    { "updatedAfterDate", "" }
};

                if (!string.IsNullOrEmpty(state))
                {
                    filters.Add("coverageState", state.ToUpper().Replace(" ", ""));
                }

                var requestData = new
                {
                    pagination = new { pageNumber = page, recordsPerPage = 50 },
                    filters = filters
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { message = "Failed to fetch billers" });
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var billerResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<BillerResponse>(responseContent);

                var filteredBillers = string.IsNullOrWhiteSpace(state)
                    ? billerResponse.Data.Records
                    : billerResponse.Data.Records
                        .Where(b => b.CoverageState.Contains(state, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                var result = new
                {
                    AvailableAmount = "0.00",//availableAmount.Amount,
                    InstantPayBalance = "0.00",//balanceStr,
                    BillAvenue = useBillAvenue.BillAvenue,
                    totalcount = billerResponse.Data.Meta.TotalRecords,
                    Billers = filteredBillers //.Where(t=>t.BillerName.Contains("ICICI"))
                };

                return Ok(result);
            }
        }

        [HttpPost]
        public async Task<IActionResult> FetchCreditCardBill([FromBody] BillRequestApp request)
        {
            return Ok(new BillResponseApp
            {
                Success = true,
                Message = "Bill fetched successfully.",
                ConsumerName = "Jurra Aryansh",
                BillNumber = "Jurra Aryansh",
                BillDate = "",
                DueDate = "",
                TotalAmount = Convert.ToDecimal("63500"),
                MinPayable = Convert.ToDecimal("21000"),
                PaymentMode = "Cash",
                Param1 = request.CreditCardLast4,
                Param2 = "",
                EnquiryReferenceId = "Payman123",
                CustomerType = "new"
            });



            //if (string.IsNullOrWhiteSpace(request.BillerId) ||
            //    string.IsNullOrWhiteSpace(request.CreditCardLast4))
            //{
            //    return BadRequest(new { message = "Invalid request. Please provide all required fields." });
            //}

            //var appPhone = HttpContext.Session.GetString("AppPhone");
            //// Get user from session

            //if (string.IsNullOrEmpty(appPhone))
            //{
            //    return RedirectToAction("WebLogout", "Login"); // or show error view
            //}
            //var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == appPhone);

            //string paymentMode = "UPI"; // default fallback
            //BillInstantPayResponse prePaymentResponse = null;

            //try
            //{
            //    // ✅ Decide provider (BillAvenue or InstantPay) 
            //    // Example: based on billerId prefix or config
            //    var useBillAvenue = await _context.PayManGateways.FirstOrDefaultAsync();

            //    if (useBillAvenue.BillAvenue == true)
            //    {
            //        request.UserPhone = appPhone;

            //        var prePaymentResponse1 = await this._dataUtils.BillAvenueFetchBill(request);

            //        if (prePaymentResponse1 == null)
            //            return NotFound(new { Success = false, Message = "Bill fetch failed (BillAvenue)." });

            //        return Ok(new BillResponseApp
            //        {
            //            Success = true,
            //            Message = "Bill fetched successfully.",
            //            ConsumerName = prePaymentResponse1.ConsumerName,
            //            BillNumber = prePaymentResponse1.ConsumerName,
            //            BillDate = prePaymentResponse1.BillDate,
            //            DueDate = prePaymentResponse1.DueDate,
            //            TotalAmount = Convert.ToDecimal(prePaymentResponse1.TotalAmount / 100),
            //            MinPayable = prePaymentResponse1.MinPayable,
            //            PaymentMode = paymentMode,
            //            Param1 = prePaymentResponse1.Param1,
            //            Param2 = prePaymentResponse1.Param2,
            //            EnquiryReferenceId = prePaymentResponse1.EnquiryReferenceId,
            //            BillerId = request.BillerId,
            //            CuurentOutStanding = prePaymentResponse1.CuurentOutStanding,

            //            BillerResponse = prePaymentResponse1.BillerResponse,
            //            AdddditionalInfo = prePaymentResponse1.AdddditionalInfo,
            //            BillFetchResponse = prePaymentResponse1.BillFetchResponse

            //        });
            //    }
            //    else
            //    {
            //        using (HttpClient client = new HttpClient())
            //        {
            //            // headers setup...
            //            var clientId = _configuration["Ipay:ClientId"];
            //            var clientSecret = _configuration["Ipay:ClientSecret"];
            //            var outletId = _configuration["Ipay:OutletId"];
            //            var endpointIp = _configuration["Ipay:EndpointIp"];
            //            var macAddress = _configuration["DeviceInfo:Mac"];
            //            var ipAddress = _configuration["DeviceInfo:Ip"];

            //            client.DefaultRequestHeaders.Add("Accept", "application/json");
            //            client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
            //            client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", clientId);
            //            client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", clientSecret);
            //            client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", endpointIp);
            //            client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", outletId);

            //            // 1. Get biller details
            //            var billerDetailsBody = new { billerId = request.BillerId };
            //            var billerDetailsContent = new StringContent(JsonConvert.SerializeObject(billerDetailsBody), Encoding.UTF8, "application/json");

            //            var billerDetailsResponse = await client.PostAsync("https://api.instantpay.in/marketplace/utilityPayments/billerDetails", billerDetailsContent);
            //            if (!billerDetailsResponse.IsSuccessStatusCode)
            //                return StatusCode(500, new { Success = false, Message = "Failed to fetch biller details." });

            //            var billerDetailsJson = await billerDetailsResponse.Content.ReadAsStringAsync();
            //            var billerDetails = JsonConvert.DeserializeObject<BillerResponse11>(billerDetailsJson);

            //            if (billerDetails?.Data?.PaymentModes?.Any() == true)
            //            {
            //                var mode = billerDetails.Data.PaymentModes.FirstOrDefault(t => t.Name == "Wallet");
            //                var modeCash = billerDetails.Data.PaymentModes.FirstOrDefault(t => t.Name == "Cash");

            //                paymentMode = mode?.Name ?? modeCash?.Name ?? "UPI";
            //            }

            //            // 2. PrePayment enquiry
            //            var enquiryPayload = new
            //            {
            //                billerId = request.BillerId,
            //                initChannel = "AGT",
            //                externalRef = "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            //                inputParameters = new { param1 = request.CreditCardLast4 },
            //                deviceInfo = new { mac = macAddress, ip = ipAddress },
            //                remarks = new { param1 = "9849800697" },
            //                transactionAmount = 10
            //            };

            //            var enquiryContent = new StringContent(JsonConvert.SerializeObject(enquiryPayload), Encoding.UTF8, "application/json");
            //            var enquiryResponse = await client.PostAsync("https://api.instantpay.in/marketplace/utilityPayments/prePaymentEnquiry", enquiryContent);

            //            if (!enquiryResponse.IsSuccessStatusCode)
            //                return StatusCode(500, new { Success = false, Message = "PrePayment enquiry failed." });

            //            var enquiryJson = await enquiryResponse.Content.ReadAsStringAsync();
            //            prePaymentResponse = JsonConvert.DeserializeObject<BillInstantPayResponse>(enquiryJson);
            //        }

            //        if (prePaymentResponse?.Data?.CustomerName != null &&
            //            prePaymentResponse.Data.AdditionalDetails[1].Value != null)
            //        {
            //            return Ok(new BillResponseApp
            //            {
            //                Success = true,
            //                Message = "Bill fetched successfully.",
            //                ConsumerName = prePaymentResponse.Data.CustomerName,
            //                BillNumber = prePaymentResponse.Data.CustomerName,
            //                BillDate = "",
            //                DueDate = "",
            //                TotalAmount = Convert.ToDecimal(prePaymentResponse.Data.AdditionalDetails[1].Value),
            //                MinPayable = Convert.ToDecimal(prePaymentResponse.Data.AdditionalDetails[2].Value),
            //                PaymentMode = paymentMode,
            //                Param1 = request.CreditCardLast4,
            //                Param2 = "",
            //                EnquiryReferenceId = prePaymentResponse.Data.EnquiryReferenceId,
            //                CustomerType = user.CustomerType
            //            });
            //        }

            //        return NotFound(new { Success = false, Message = "Bill fetch failed or incomplete data." });
            //    }
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, new { Success = false, Message = "Exception occurred.", Details = ex.Message });
            //}
        }

    }
}
