using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PaymanWeb;
using Newtonsoft.Json;
using System.Security;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MVC6Crud.Controllers
{
    public class ElectricityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;
        public ElectricityController(ApplicationDbContext context, IConfiguration configuration, DataUtils dataUtils)
        {
            _context = context;
            _configuration = configuration;
            _dataUtils = dataUtils;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ElectricityPayBill()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ElectricityBillers(string searchTerm = "")
        {
            var query = _context.billAvenueCreditCardBillers
                .Where(t => t.blr_category_name == "Electricity");

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query
                    .Where(b => b.blr_name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            var filteredBillers = query
                .Select(x => new
                {
                    billerId = x.blr_id,
                    billerName = x.blr_name,
                    category = x.blr_category_name,
                    iconUrl = "",   // because you said you don't have icon
                })
                .ToList();

            var result = new
            {
                availableAmount = "0.00",
                instantPayBalance = "0.00",
                billAvenue = true,
                billers = filteredBillers
            };

            return Ok(result);
        }


        [HttpGet]
        public async Task<IActionResult> CheckBillerCategory([FromQuery] string billerId)
        {
            if (string.IsNullOrEmpty(billerId))
                return BadRequest(new { message = "BillerId is required" });

            billerId = billerId.Trim().ToUpper();
            try
            {
                var biller1 = await _context.billAvenueCreditCardBillers
               .Where(b => b.blr_id.Trim().ToUpper() == billerId)
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
            catch (Exception ex)
            {
            }
            return Ok();

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
            string requestId = _dataUtils.GenerateRequestId();
            //string workingKey = "2EEFEE24E33539706EA1E532D60461C7";
            string ver = "1.0";
            //string instituteId = "RP91";
            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<billerInfoRequest>
  <billerId>{billerId}</billerId>
</billerInfoRequest>";

            string apiUrl = $"https://api.billavenue.com/billpay/extMdmCntrl/mdmRequestNew/xml" +
                            $"?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}";

            string encryptedPayload = _dataUtils.Encrypt(merchantData, workingKey);

            using var client = new HttpClient();

            try
            {
                var content = new StringContent(encryptedPayload, Encoding.UTF8, "text/plain");
                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    string decryptedResponse = _dataUtils.Decrypt(result, workingKey);

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
                }
            }
            catch (Exception ex)
            {

            }

            return Ok();
        }

        public async Task<BillResponseApp> BillAvenueFetchBill(BillRequestApp request)
        {

            // headers setup...
            var accessCode = _configuration["BillAvenueKeys:accessCode"];
            var workingKey = _configuration["BillAvenueKeys:workingKey"];
            var instituteId = _configuration["BillAvenueKeys:instituteId"];
            var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];


            string requestId = _dataUtils.GenerateRequestId();
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
                if (item.ParamName.Contains("Unique", StringComparison.OrdinalIgnoreCase))
                {
                    value = request.RegisteredMobile;
                }

                if (item.ParamName.Contains("Consumer", StringComparison.OrdinalIgnoreCase))
                {
                    value = request.RegisteredMobile;
                }

                inputParams.Add(
                    new XElement("input",
                        new XElement("paramName", item.ParamName),
                        new XElement("paramValue", value)
                    )
                );
            }

            // Convert to string
            string additionalInfo = inputParams.ToString();

            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?> 
<billFetchRequest>
    <agentId>CC01RP91MOBBAK024661</agentId>
    <agentDeviceInfo> 
          <app>tripozo</app> 
          <imei>000000000000000</imei> 
          <initChannel>MOB</initChannel> 
           <ip>{endpointIp}</ip> 
           <os>android</os> 
   </agentDeviceInfo>
    <customerInfo>
        <customerMobile>{request.CustomerMobile}</customerMobile>
        <customerEmail>jurrajanardhan@gmail.com</customerEmail>
        <customerAdhaar>519255500386</customerAdhaar>
        <customerPan>BKSPJ1156M</customerPan>
    </customerInfo>
    <billerId>{request.BillerId}</billerId>
{additionalInfo}
</billFetchRequest>";

            string encryptedPayload = _dataUtils.Encrypt(merchantData, workingKey);

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

                string decryptedResponse = _dataUtils.Decrypt(result, workingKey);

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
                        Message = "Invalid response format"
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
                    BillDate = wrapper == null ? "" : wrapper.BillerResponse,
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
            var billFetchResponse = doc.SelectSingleNode("//inputParams");

            var wrapper = new BillFetchWrapper
            {
                BillerResponse = node?.OuterXml,
                AdddditionalInfo = additionalInfo?.OuterXml,
                BillFetchResponse = billFetchResponse?.OuterXml
            };

            return wrapper;
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
                return Ok(_dataUtils.GenerateFailureResponse("Invalid input request."));
            }

            var userDetails = await _context.payManUsers
                                           .FirstOrDefaultAsync(t => t.Phone == request.Phone);

            var aadharDetails = await _context.userDocuments.Where(t => t.Phone == request.Phone).Select(t => t.PanCardNumber).FirstOrDefaultAsync();

            var aadharDetails1 = await _context.aadharDetails.FirstOrDefaultAsync(t => t.Phone == request.Phone);

            if (userDetails == null || aadharDetails == null)
            {
                return Ok(_dataUtils.GenerateFailureResponse("User or Aadhar details not found."));
            }



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
                    return Ok(_dataUtils.GenerateFailureResponse("Biller details not found."));
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
                if (availableModes.Contains("cash1"))
                {
                    PaymentMode = "Cash";
                    additionalInfo = @"
    <info>
        <infoName>Remarks</infoName>
        <infoValue>CashPayment</infoValue>
    </info>";
                }
                else if (availableModes.Contains("wallet1"))
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
                    additionalInfo = @"
<info>
        <infoName>VPA</infoName>
        <infoValue>9652724937@kotak</infoValue> 
    </info>";
                }


                //For AGT

                //              < agentId >{ userDetails.BillAvenueAgentId}</ agentId >
                //< agentDeviceInfo >
                //  < ip >{ endpointIp}</ ip >
                //  < initChannel > AGT </ initChannel >
                //  < mac > 01 - 23 - 45 - 67 - 89 - ab </ mac >
                //</ agentDeviceInfo >

                string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<billPaymentRequest>
  <agentId>CC01RP91MOBBAK024661</agentId>
   <agentDeviceInfo> 
          <app>tripozo</app> 
          <imei>000000000000000</imei> 
          <initChannel>MOB</initChannel> 
           <ip>{endpointIp}</ip> 
           <os>android</os> 
   </agentDeviceInfo>
  <customerInfo>
    <REMITTER_NAME>{aadharDetails1.Name}</REMITTER_NAME>
    <customerMobile>{userDetails.Phone}</customerMobile>
    <customerEmail>{userDetails.Email}</customerEmail>
    <customerAdhaar>{aadharDetails1.AadharNo}</customerAdhaar>
    <customerPan>{aadharDetails}</customerPan>
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
    <paymentMode>{PaymentMode}</paymentMode>
    <quickPay>N</quickPay>
    <splitPay>N</splitPay>
  </paymentMethod>
  <paymentInfo>
    {additionalInfo}
  </paymentInfo>
</billPaymentRequest>";

                // Encrypt payload
                var encryptedPayload = _dataUtils.Encrypt(merchantData, workingKey);
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
                    await _dataUtils.LogFailedTransactionAsync(userDetails, request, istDateTime, "Insufficient balance", "BalanceCheck");
                    return Ok(_dataUtils.GenerateFailureResponse("Insufficient balance. Bill payment failed.", "failed"));
                }

                ExtBillPayResponse billResponse = null;
                string decryptedResponse = null;
                bool apiCallSuccess = false;


                using var client = new HttpClient();

                try
                {
                    // Send request
                    var httpResponse = await client.PostAsync(url, null);
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();


                    if (httpResponse.IsSuccessStatusCode && !string.IsNullOrEmpty(responseContent))
                    {
                        // Decrypt and deserialize
                        decryptedResponse = _dataUtils.Decrypt(responseContent, workingKey)?.Trim();
                        if (!string.IsNullOrWhiteSpace(decryptedResponse))
                        {
                            var serializer = new XmlSerializer(typeof(ExtBillPayResponse));
                            using var reader = new StringReader(decryptedResponse);
                            billResponse = (ExtBillPayResponse)serializer.Deserialize(reader);
                            apiCallSuccess = true;
                        }
                        else
                        {
                            // Decryption produced empty -> log
                            await _dataUtils.LogFailedTransactionAsync(userDetails, request, istDateTime, "Decryption returned empty", "Decrypt");
                        }
                    }
                    else
                    {
                        await _dataUtils.LogFailedTransactionAsync(userDetails, request, istDateTime, $"BillAvenue API returned HTTP {(int)httpResponse.StatusCode}", "API");
                    }
                }
                catch (Exception ex)
                {
                    await _dataUtils.LogFailedTransactionAsync(userDetails, request, istDateTime, $"API Exception: {ex.Message}", "Exception");
                }

                // Start DB transaction: INSERT initial records then attempt final status update within same transaction
                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Determine initial result/status
                    var initialResultMsg = billResponse?.ResponseReason ?? "INITIATED";
                    var initialTxnId = billResponse?.TxnRefId ?? request.EnquiryReferenceId ?? Guid.NewGuid().ToString("N");
                    var cdsh = true;
                    var initialIsSuccess = string.Equals(initialResultMsg, "Successful", StringComparison.OrdinalIgnoreCase);
                    //|| string.Equals(billResponse?.TxnList?.TxnStatus, "success", StringComparison.OrdinalIgnoreCase);

                    // Save initial PayOut and PayInHistory (use your existing helpers but mark as Initiated)
                    //await _dataUtils.LogPayOutAsync(userDetails, request, istDateTime, initialResultMsg, initialTxnId, initialIsSuccess);

                    //await _dataUtils.LogPayInHistoryAsync(userDetails, request, istDateTime, initialTxnId, initialIsSuccess, billResponse?.RespAmount ?? (decimal)amountInPaise);

                    // If initial response already success -> commit and return
                    if (initialIsSuccess)
                    {
                        await dbTransaction.CommitAsync();

                        return Ok(new PaymentResponseProcess
                        {
                            Success = true,
                            Amount = (billResponse?.RespAmount ?? (decimal)request.Amount).ToString(),
                            OrderId = billResponse?.TxnRefId ?? initialTxnId,
                            ReferenceId = billResponse?.ApprovalRefNumber ?? "NA",
                            Category = "Credit Card",
                            BillerName = billerDetails?.blr_name ?? "Unknown",
                            Status = "SUCCESS"
                        });
                    }

                    // If not immediate success, perform retries (status check) and update records before commit
                    string finalStatus = "PENDING";
                    ExtBillPayResponse11 statusResponse = null;

                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        statusResponse = await _dataUtils.FetchTransactionStatusAsync(request.EnquiryReferenceId, accessCode, instituteId, workingKey);

                        var txnStatus = statusResponse?.TxnList?.TxnStatus?.Trim()?.ToLower();
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

                        // wait before next attempt
                        await Task.Delay(TimeSpan.FromSeconds(3));
                    }

                    // If still pending but we have a billResponse with some info, keep PENDING.
                    // Map fields safely when updating
                    var getPayOut = await _context.payManPayOuts.FirstOrDefaultAsync(x => x.RefId == request.EnquiryReferenceId);
                    var getPayOuthistory = await _context.payManHistories.FirstOrDefaultAsync(x => x.TxnId == request.EnquiryReferenceId);

                    if (getPayOut != null)
                    {
                        getPayOut.Result = finalStatus;

                        if (statusResponse?.TxnList != null)
                        {
                            getPayOut.PayOutId = statusResponse.TxnList.TxnReferenceId ?? statusResponse.TxnList.ApprovalRefNumber ?? getPayOut.PayOutId;
                            getPayOut.AccountNo = statusResponse.TxnList.Mobile ?? getPayOut.AccountNo;

                            // safe mapping of input params
                            var ip = statusResponse.TxnList.InputParams ?? new List<InputParams11>();
                            var primaryLast4 = ip.FirstOrDefault(x => x.ParamName?.IndexOf("last 4", StringComparison.OrdinalIgnoreCase) >= 0
                                                                     || x.ParamName?.IndexOf("primary", StringComparison.OrdinalIgnoreCase) >= 0)
                                                ?.ParamValue;
                            var regMobile = ip.FirstOrDefault(x => x.ParamName?.IndexOf("registered", StringComparison.OrdinalIgnoreCase) >= 0)
                                           ?.ParamValue;

                            getPayOut.TxnType = primaryLast4 ?? getPayOut.TxnType;
                            getPayOut.IfscCode = regMobile ?? getPayOut.IfscCode;
                            getPayOut.AccountHolderName = statusResponse.TxnList.RespCustomerName ?? getPayOut.AccountHolderName;
                        }

                        // Final boolean status (FAILED -> false, everything else false->pending treated false as well)
                        getPayOut.Status = string.Equals(finalStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase);
                        _context.payManPayOuts.Update(getPayOut);
                    }

                    if (getPayOuthistory != null)
                    {
                        getPayOuthistory.Status = string.Equals(finalStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase);
                        _context.payManHistories.Update(getPayOuthistory);
                    }

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    // Build response using finalStatus
                    return Ok(new PaymentResponseProcess
                    {
                        Success = string.Equals(finalStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase),
                        Amount = (billResponse?.RespAmount ?? (decimal)request.Amount).ToString(),
                        OrderId = statusResponse?.TxnList?.TxnReferenceId ?? billResponse?.TxnRefId ?? request.EnquiryReferenceId,
                        ReferenceId = statusResponse?.TxnList?.ApprovalRefNumber ?? billResponse?.ApprovalRefNumber ?? "NA",
                        Category = "Credit Card",
                        BillerName = billerDetails?.blr_name ?? "Unknown",
                        Status = finalStatus
                    });
                }
                catch (Exception dbEx)
                {
                    await dbTransaction.RollbackAsync();
                    await _dataUtils.LogFailedTransactionAsync(userDetails, request, istDateTime, $"DB save error: {dbEx.Message}", "DB");
                    return StatusCode(500, new { Success = false, message = "Failed while saving transaction", error = dbEx.Message });
                }

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
                            return Ok(_dataUtils.GenerateFailureResponse("API request failed."));
                        }

                        var transactionResponse = JsonConvert.DeserializeObject<TransactionResponse>(responseContent);
                        if (transactionResponse?.Data?.Pool == null)
                        {
                            return Ok(_dataUtils.GenerateFailureResponse("Transaction pool is null.", transactionResponse?.Status ?? "FAILED"));
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
                            Device = "I - " + request.Device
                        };

                        _context.payManPayOuts.Add(trandetails);
                        await _context.SaveChangesAsync();

                        var userWalletAmount = await _dataUtils.GetUserWalletAmount(request.Phone);

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
                    return Ok(_dataUtils.GenerateFailureResponse("Exception occurred while processing payment."));
                }
            }
        }


    }
}
