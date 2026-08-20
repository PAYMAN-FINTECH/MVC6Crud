using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MVC6Crud.Controllers;
using MVC6Crud.Models;
using MVC6Crud.Models.App;
using MVC6Crud.Models.BBPS;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PaymanWeb;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using XAct.Library.Settings;
using XAct.Users;

namespace MVC6Crud.Data
{
    public class BBPSService
    {
        private readonly IConfiguration _configuration;

        private readonly ApplicationDbContext _context;
        private readonly DataUtils dataUtils;

        public BBPSService(IConfiguration configuration, ApplicationDbContext context, DataUtils dataUtils)
        {
            _configuration = configuration;
            _context = context;
            this.dataUtils = dataUtils;
        }
        public string GenerateRequestId()
        {
            return $"{Guid.NewGuid():N}{DateTime.Now:fff}".Substring(0, 35);
        }


        public async Task<BBPSFetchResponse> FetchBillAsync(FetchBillRequestDTO req)
        {
            try
            {
                string requestId = GenerateRequestId();

                var billerInfo = await _context.billAvenueCreditCardBillers
                    .FirstOrDefaultAsync(b => b.blr_id == req.BillerId);

                if (billerInfo == null)
                {
                    return new BBPSFetchResponse
                    {
                        Status = false,
                        Message = "Biller not found"
                    };
                }

                var accessCode = _configuration["BillAvenueKeys:accessCode"];
                var workingKey = _configuration["BillAvenueKeys:workingKey"];
                var instituteId = _configuration["BillAvenueKeys:instituteId"];
                var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];

                var userDetails = await _context.payManUsers
                    .FirstOrDefaultAsync(u => u.Phone == req.UserPhone);

                var inputParams = BuildInputParams(
                    billerInfo.blr_response,
                    req.Inputs
                );

    //            < agentId > CC01RP91MOBBAK024661 </ agentId >
    //< agentDeviceInfo >
    //    < app > PAYMAN </ app >
    //    < imei > 000000000000000 </ imei >
    //    < initChannel > MOB </ initChannel >
    //    < ip >{ endpointIp}</ ip >
    //    < os > android </ os >
    //</ agentDeviceInfo >

                string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<billFetchRequest>
<agentId>{userDetails.BillAvenueAgentId}</agentId>
                <agentDeviceInfo>
                  <ip>{endpointIp}</ip>
                  <initChannel>AGT</initChannel>
                  <mac>01-23-45-67-89-ab</mac>
                </agentDeviceInfo >

    
    <customerInfo>
        <customerMobile>{req.UserPhone}</customerMobile>
        <customerEmail>{userDetails?.Email}</customerEmail>
    </customerInfo>
    <billerId>{req.BillerId}</billerId>
    {inputParams}
</billFetchRequest>";

                string encryptedPayload = Encrypt(merchantData, workingKey);

                string url =
                    $"https://api.billavenue.com/billpay/extBillCntrl/billFetchRequest/xml" +
                    $"?accessCode={accessCode}&requestId={requestId}&ver=1.0&instituteId={instituteId}&encRequest={encryptedPayload}";

                using var client = new HttpClient();

                var response = await client.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    return new BBPSFetchResponse
                    {
                        Status = false,
                        Message = "Failed to connect Bill Avenue"
                    };
                }

                var encryptedResult = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(encryptedResult))
                {
                    return new BBPSFetchResponse
                    {
                        Status = false,
                        Message = "Empty response from Bill Avenue"
                    };
                }

                var decryptedXml = Decrypt(encryptedResult, workingKey);

                if (string.IsNullOrEmpty(decryptedXml))
                {
                    return new BBPSFetchResponse
                    {
                        Status = false,
                        Message = "Decryption failed"
                    };
                }

                var wrapper = ExtractBillerResponse(decryptedXml);

                return new BBPSFetchResponse
                {
                    Status = true,
                    Message = "Success",
                    EnquiryReferenceId = requestId,
                    BillerResponse = decryptedXml,
                    BillerResponse1 = wrapper.BillerResponse,
                    AdditionalInfo = wrapper.AdddditionalInfo,
                    BillFetchResponse = wrapper.BillFetchResponse
                };
            }
            catch (Exception ex)
            {
                return new BBPSFetchResponse
                {
                    Status = false,
                    Message = "Exception: " + ex.Message
                };
            }
        }


        public async Task<dynamic> ProcessBillPaymentAsync(BBPSPaymentRequest  request)
        {
            // ---- Load Keys ----
            var accessCode = _configuration["BillAvenueKeys:accessCode"];
            var workingKey = _configuration["BillAvenueKeys:workingKey"];
            var instituteId = _configuration["BillAvenueKeys:instituteId"];
            var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];

            // ---- Load User Details ----
            var userDetails = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == request.UserPhone);
            var panCardNumber = await _context.userDocuments.Where(t => t.Phone == request.UserPhone).Select(t => t.PanCardNumber).FirstOrDefaultAsync();

            var aadharDetails = await _context.aadharDetails.FirstOrDefaultAsync(a => a.Phone == request.UserPhone);

            if (userDetails == null || aadharDetails == null || panCardNumber == null)
            {
                return new PaymentResponseProcess
                {
                    Success = false,
                    Status = "FAILED",
                    Message = "User details not found",
                    UserPhone = request.UserPhone,
                    UserName = ""
                };
            }

            // ---- Get Biller ----
            var billerDetails = await _context.billAvenueCreditCardBillers
                .FirstOrDefaultAsync(t => t.blr_id == request.BillerId);

            if (billerDetails == null)
            {
                return new PaymentResponseProcess
                {
                    Success = false,
                    Status = "FAILED",
                    Message = "Biller details not found",
                    UserPhone = request.UserPhone,
                    UserName = ""
                };
            }

            // ---- Determine Payment Mode ----
            string paymentMode, additionalInfo;
            GetPaymentModeAndInfo(billerDetails.PaymentModes, out paymentMode, out additionalInfo);

            // ---- Build XML ----
            string merchantData = BuildPaymentRequestXml(request, userDetails, aadharDetails, panCardNumber, billerDetails, endpointIp, paymentMode, additionalInfo);

            // ---- Encrypt ----
            var encryptedPayload = Encrypt(merchantData, workingKey);
            var requestId = SecurityElement.Escape(request.EnquiryReferenceId ?? Guid.NewGuid().ToString("N"));

            var url =
                $"https://api.billavenue.com/billpay/extBillPayCntrl/billPayRequest/xml?accessCode={accessCode}" +
                $"&requestId={requestId}&ver=1.0&instituteId={instituteId}&encRequest={HttpUtility.UrlEncode(encryptedPayload)}";

            // ---- Balance Check ----
            //var instBalance = await this.dataUtils.GetInstantPayAmount();
            //if (request.Amount > Convert.ToDouble(instBalance))
            //{
            //    return new PaymentResponseProcess
            //    {
            //        Success = false,
            //        Status = "FAILED",
            //        Message = "Insufficient Balance",
            //        UserPhone = request.UserPhone,
            //        UserName = ""
            //    };
            //}

            // ---- Call BillPay API ----
            var initialResponse = await CallBillPayApiAsync(url, workingKey);

            // ---- Save INITIATED logs ----
            //var initialStatus = await SaveInitialLogsAsync(initialResponse, request, userDetails, accessCode, instituteId, workingKey);

            // if initial success, return directly
            //if (initialStatus.Success)
            //{
            //    return initialStatus;
            //}

            if (initialResponse != null)
            {

                var amount = initialResponse.RespAmount / 100;
                var log = new BbpsTransaction
                {
                    UserPhone = userDetails.Phone,
                    ResponseCode = initialResponse.ResponseCode,
                    ResponseReason = initialResponse.ResponseReason,
                    TxnRefId = initialResponse.TxnRefId,
                    ApprovalRefNumber = initialResponse.ApprovalRefNumber,
                    TxnRespType = initialResponse.TxnRespType,
                    CustConvFee = initialResponse.CustConvFee,
                    RespAmount = amount,
                    RespBillDate = initialResponse.RespBillDate,
                    RespCustomerName = initialResponse.RespCustomerName,
                    RespDueDate = initialResponse.RespDueDate,
                    InputParamsJson = initialResponse.InputParams != null
                                          ? JsonSerializer.Serialize(initialResponse.InputParams)
                                          : null,
                    CreatedAt = DateTime.Now,
                    Status = true,
                    StatusCode= initialResponse.ResponseReason
                };
                _context.bbpsTransactions.Add(log);
                await _context.SaveChangesAsync();
            }

            var hh= new PaymentResponseProcess
            {
                Success = initialResponse.ResponseReason == "Successful",
                Status = initialResponse.ResponseReason.ToUpper() ?? "FAILED",
                Amount = request.Amount.ToString(),
                OrderId = initialResponse.TxnRefId,
                BillerName = "",
                UserPhone = request.UserPhone,
                UserName = ""
            };

            string jsonData = System.Text.Json.JsonSerializer.Serialize(hh);

            var user1 = new ErrorModel
            {
                payload = "Bill pay1",
                agId = jsonData,
                reqTime =  "",
                respTime = "",
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(user1);
            await _context.SaveChangesAsync();

            // ---- Do Status Check (3 attempts) ----
            //var finalResult = await PerformStatusCheckAsync(
            //    request.EnquiryReferenceId,
            //    initialResponse,
            //    accessCode,
            //    instituteId,
            //    workingKey,
            //    request,
            //    userDetails,
            //    billerDetails
            //);

            // return finalResult;
            return hh;
        }

        /* ----------------------------------------------------
         * Below here: helper methods (reusable)
         * ----------------------------------------------------
         */

        private static void GetPaymentModeAndInfo(string modeString, out string pmode, out string info)
        {
            var modes = modeString.Split(',')
                .Select(m => m.Trim().ToLower())
                .ToList();

            pmode = "UPI";
            info = @"<info><infoName>VPA</infoName><infoValue>9652724937@kotak</infoValue></info>";

            if (modes.Contains("cash")) // cash1 for mobile
            {
                pmode = "Cash";
                info = @"<info><infoName>Remarks</infoName><infoValue>CashPayment</infoValue></info>";
            }
            else if (modes.Contains("wallet")) // wallet1 for mobile
            {
                pmode = "Wallet";
                info = @"
                <info><infoName>WalletName</infoName><infoValue>Forpay</infoValue></info>
                <info><infoName>MobileNo</infoName><infoValue>7286887024</infoValue></info>
            ";
            }
        }



        // for mobile
  //      <agentId>CC01RP91MOBBAK024661</agentId>
  //<agentDeviceInfo>
  //  <app>tripozo</app>
  //  <imei>000000000000000</imei>
  //  <initChannel>MOB</initChannel>
  //  <ip>{endpointIp
  //  }</ip>
  //  <os>android</os>
  //</agentDeviceInfo>
        private string BuildPaymentRequestXml(
            BBPSPaymentRequest request,
            PayManUsers user,
            AadharDetails aadhar,
            string panNumber,
            BillAvenueCreditCardBillers biller,
            string endpointIp,
            string paymentMode,
            string extraInfo)
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<billPaymentRequest>
<agentId>{user.BillAvenueAgentId}</agentId>
                <agentDeviceInfo>
                  <ip>{endpointIp}</ip>
                  <initChannel>AGT</initChannel>
                  <mac>01-23-45-67-89-ab</mac>
                </agentDeviceInfo >

  

  <customerInfo>
    <REMITTER_NAME>{aadhar.Name}</REMITTER_NAME>
    <customerMobile>{user.Phone}</customerMobile>
    <customerEmail>{user.Email}</customerEmail>
    <customerAdhaar>{aadhar.AadharNo}</customerAdhaar>
    <customerPan>{panNumber}</customerPan>
  </customerInfo>

  <billerId>{request.BillerId}</billerId>
  {request.BillFetchResponse}
  {request.BillerResponse}
  {request.AdditionalInfo}

  <amountInfo>
    <amount>{request.Amount * 100}</amount>
    <currency>356</currency>
    <custConvFee>0</custConvFee>
  </amountInfo>

  <paymentMethod>
    <paymentMode>{paymentMode}</paymentMode>
    <quickPay>N</quickPay>
    <splitPay>N</splitPay>
  </paymentMethod>

  <paymentInfo>{extraInfo}</paymentInfo>
</billPaymentRequest>";
        }

        private async Task<ExtBillPayResponse> CallBillPayApiAsync(string url, string workingKey)
        {
            using var client = new HttpClient();
            var httpResponse = await client.PostAsync(url, null);
            var responseContent = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                return null;

            string enc = responseContent.Replace("encResponse=", "");
            string decrypted = Decrypt(enc, workingKey);

            //string jsonData = System.Text.Json.JsonSerializer.Serialize(result);

            var user1 = new ErrorModel
            {
                payload = "Bill pay",
                agId = decrypted,
                reqTime = "",
                respTime = "",
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(user1);
            await _context.SaveChangesAsync();

            var serializer = new XmlSerializer(typeof(ExtBillPayResponse));
            using var reader = new StringReader(decrypted);
            return (ExtBillPayResponse)serializer.Deserialize(reader);
        }

        private async Task<PaymentResponseProcess> SaveInitialLogsAsync(
            ExtBillPayResponse billResponse,
            BBPSPaymentRequest request,
            PayManUsers user, string accessCode, string instituteId, string workingKey)
        {
            // Save your payout and history logs here (same as your existing logic)
            // ...
            PaymentResponseProcess jlkj = new PaymentResponseProcess();
            try
            {
                var rsu = await ProcessCreditCardTransactionAsync(billResponse, request, user, accessCode, instituteId, workingKey);

                return new PaymentResponseProcess
                {
                    Success = rsu.Success,
                    Status = rsu.Status,
                    Amount = request.Amount.ToString(),
                    OrderId = "",//request.EnquiryReferenceId
                    UserPhone = request.UserPhone,
                    UserName = ""
                };
            }
            catch (Exception ex)
            {

            }
            return jlkj;


        }

        private async Task<PaymentResponseProcess> PerformStatusCheckAsync(
            string enquiryRef,
            ExtBillPayResponse initialResponse,
            string accessCode,
            string instituteId,
            string workingKey,
            BBPSPaymentRequest request,
            PayManUsers user,
            BillAvenueCreditCardBillers biller)
        {
            // 3 attempts loop
            ExtBillPayResponse11? finalStatus = null;

            for (int i = 0; i < 3; i++)
            {
                finalStatus = await FetchTransactionStatusAsync(enquiryRef, accessCode, instituteId, workingKey);
                if (finalStatus?.TxnList?.TxnStatus?.ToLower() == "success") break;
                await Task.Delay(3000);
            }

            return new PaymentResponseProcess
            {
                Success = finalStatus?.TxnList?.TxnStatus?.ToLower() == "success",
                Status = finalStatus?.TxnList?.TxnStatus?.ToUpper() ?? "FAILED",
                Amount = request.Amount.ToString(),
                OrderId = enquiryRef,
                BillerName = biller.blr_name,
                UserPhone = request.UserPhone,
                UserName = ""
            };
        }

        public async Task<ExtBillPayResponse11?> FetchTransactionStatusAsync(
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
                // await LogDebugAsync($"Status API HTTP {(int)httpResponse.StatusCode} RAW: {raw}");

                if (!httpResponse.IsSuccessStatusCode)
                {
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
                    return null;
                }

                string decryptedXml;
                try
                {
                    decryptedXml = Decrypt(encryptedResponse, workingKey)?.Trim() ?? "";
                }
                catch (Exception ex)
                {
                    return null;
                }


                if (string.IsNullOrWhiteSpace(decryptedXml) || !decryptedXml.TrimStart().StartsWith("<"))
                {
                    return null;
                }

                try
                {
                    var serializer = new XmlSerializer(typeof(ExtBillPayResponse11));
                    using var reader = new StringReader(decryptedXml);
                    var result = (ExtBillPayResponse11)serializer.Deserialize(reader);
                    return result;
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public BBPSParsedBill ParseBillerResponse(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);

                var biller = doc.Descendants("billerResponse").FirstOrDefault();

                if (biller == null) return new BBPSParsedBill();

                return new BBPSParsedBill
                {
                    CustomerName = biller.Element("customerName")?.Value,
                    BillAmount = biller.Element("billAmount")?.Value,
                    DueDate = biller.Element("dueDate")?.Value
                };
            }
            catch
            {
                return new BBPSParsedBill();
            }
        }

        private async Task<Guid> LogPayOutAsync(
    PayManUsers user,
    BBPSPaymentRequest request,
    DateTime dateTime,
    string result,
    string txnId,
    bool status)
        {
            /// 🔥 PARSE XML HERE
            var parsed = ParseBillerResponse(request.BillerResponse);
            decimal payoutCommission = request.Amount / 100 > 100000d ? 30m : 15m;

            var payout = new PayManPayOut
            {
                UserId = user.Id,
                UserPhone = request.UserPhone,

                PayOutId = txnId,
                RefId = request.EnquiryReferenceId,

                /// ✅ FROM XML
                AccountHolderName = parsed.CustomerName ?? "Unknown",
                Amount = decimal.TryParse(parsed.BillAmount, out var amt)
                            ? amt/100m
                            : Convert.ToDecimal(request.Amount/100),

                /// ✅ FROM INPUTS
                AccountNo = request.Inputs?
                    .FirstOrDefault(x => x.ParamName.ToLower().Contains("mobile")
                                      || x.ParamName.ToLower().Contains("vehicle")
                                      || x.ParamName.ToLower().Contains("service"))
                    ?.ParamValue,

                IfscCode = request.UserPhone,
                PayoutCommission = payoutCommission,

                BeneId = request.BillerId,
                PayOutType = request.Category ?? "BBPS",
                TxnType = request.Inputs?
                    .FirstOrDefault(x => x.ParamName.ToLower().Contains("last4"))
                    ?.ParamValue,

                Email = user.Email,
                Status = status,
                DateTime = dateTime,
                Result = result,
                Device = request.Device
            };

            _context.payManPayOuts.Add(payout);
            await _context.SaveChangesAsync();

            return payout.Id;
        }
        // Fix LogPayInHistoryAsync - DO NOT call SaveChanges here. keep same amount logic (amount param is paise)
        private async Task LogPayInHistoryAsync(PayManUsers user, BBPSPaymentRequest request,
            DateTime dateTime, string txnId, bool status, decimal amountInPaise, Guid payOutDbId)
        {
            var userWalletAmount = await this.dataUtils.GetUserWalletAmount(request.UserPhone);

            var history = new PayManHistory
            {
                UserId = user.Id,
                UserPhone = request.UserPhone,
                TxnId = txnId,
                Amount = amountInPaise / 100m,   // keep previous behavior
                CardNumber = "",
                Mode = request.Category,
                Status = status,
                Created = dateTime,
                AvlBalance = Convert.ToDecimal(userWalletAmount) - amountInPaise / 100m,
                PayInId = payOutDbId
            };

            _context.payManHistories.Add(history);
        }





        public async Task<PaymentResponseProcess> ProcessCreditCardTransactionAsync(
            ExtBillPayResponse billResponse,
             BBPSPaymentRequest request,
    PayManUsers userDetails, string accessCode, string instituteId, string workingKey)
        {

            IDbContextTransaction? dbTransaction = null;

            DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                              TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
            var billerDetails = await _context.billAvenueCreditCardBillers.FirstOrDefaultAsync(t => t.blr_id == request.BillerId);

            try
            {

                dbTransaction = await _context.Database.BeginTransactionAsync();


                // -----------------------------
                // 1. INITIAL LOGGING
                // -----------------------------
                var initialResultMsg = !string.IsNullOrWhiteSpace(billResponse?.ResponseReason)
                    ? billResponse.ResponseReason
                    : "INITIATED";

                var initialTxnId = billResponse?.TxnRefId
                    ?? request.EnquiryReferenceId
                    ?? Guid.NewGuid().ToString("N");

                var amountInPaise = Convert.ToInt64(Math.Round(request.Amount * 100));


                bool initialIsSuccess = string.Equals(initialResultMsg, "Successful", StringComparison.OrdinalIgnoreCase);

                Guid payOutDbId = await LogPayOutAsync(userDetails, request, istDateTime, initialResultMsg, initialTxnId, initialIsSuccess);
                await LogPayInHistoryAsync(userDetails, request, istDateTime, initialTxnId, initialIsSuccess, billResponse?.RespAmount ?? amountInPaise, payOutDbId);

                await _context.SaveChangesAsync();


                // -----------------------------
                // 2. EARLY SUCCESS
                // -----------------------------
                if (initialIsSuccess)
                {
                    await dbTransaction.CommitAsync();

                    return new PaymentResponseProcess
                    {
                        Success = true,
                        Amount = request.Amount.ToString(),
                        OrderId = billResponse?.TxnRefId ?? initialTxnId,
                        ReferenceId = billResponse?.ApprovalRefNumber ?? "NA",
                        Category = request.Category,
                        BillerName = billerDetails?.blr_name ?? "Unknown",
                        Status = "SUCCESS"
                    };
                }


                // -----------------------------
                // 3. STATUS CHECK (3 attempts)
                // -----------------------------
                string finalStatus = "PENDING";
                ExtBillPayResponse11? statusResponse = null;

                for (int attempt = 0; attempt < 3; attempt++)
                {
                    statusResponse = await FetchTransactionStatusAsync(
                        request.EnquiryReferenceId,
                        accessCode,
                        instituteId,
                        workingKey);

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

                    await Task.Delay(TimeSpan.FromSeconds(3));  // retry delay
                }


                // -----------------------------
                // 4. UPDATE EXISTING LOG RECORDS
                // -----------------------------
                var getPayOut = await _context.payManPayOuts
                    .FirstOrDefaultAsync(x => x.RefId == request.EnquiryReferenceId);

                var getPayOuthistory = await _context.payManHistories
                    .FirstOrDefaultAsync(x => x.TxnId == request.EnquiryReferenceId);

                if (getPayOut != null)
                {
                    getPayOut.Result = finalStatus;
                    getPayOut.Status = finalStatus == "SUCCESS";

                    var txn = statusResponse?.TxnList;

                    if (txn != null)
                    {
                        getPayOut.PayOutId = txn.TxnReferenceId ?? txn.ApprovalRefNumber ?? getPayOut.PayOutId;
                        getPayOut.AccountNo = txn.Mobile ?? getPayOut.AccountNo;
                        getPayOut.AccountHolderName = txn.RespCustomerName ?? getPayOut.AccountHolderName;

                        var inputParams = txn.InputParams ?? new List<InputParams11>();

                        getPayOut.TxnType = inputParams.FirstOrDefault(x =>
                            x.ParamName?.ToLower().Contains("last 4") == true ||
                            x.ParamName?.ToLower().Contains("primary") == true)?.ParamValue
                            ?? getPayOut.TxnType;

                        getPayOut.IfscCode = inputParams.FirstOrDefault(x =>
                            x.ParamName?.ToLower().Contains("registered") == true)?.ParamValue
                            ?? getPayOut.IfscCode;
                    }

                    _context.payManPayOuts.Update(getPayOut);
                }

                if (getPayOuthistory != null)
                {
                    getPayOuthistory.Status = finalStatus == "SUCCESS";
                    _context.payManHistories.Update(getPayOuthistory);
                }

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();


                // -----------------------------
                // 5. FINAL RETURN RESPONSE
                // -----------------------------
                return new PaymentResponseProcess
                {
                    Success = finalStatus == "SUCCESS",
                    Amount = request.Amount.ToString(),
                    OrderId = statusResponse?.TxnList?.TxnReferenceId
                        ?? billResponse?.TxnRefId
                        ?? request.EnquiryReferenceId,

                    ReferenceId = statusResponse?.TxnList?.ApprovalRefNumber
                        ?? billResponse?.ApprovalRefNumber
                        ?? "NA",

                    Category = "Credit Card",
                    BillerName = billerDetails?.blr_name ?? "Unknown",
                    Status = finalStatus
                };
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();

                var errMessage = $"DB save error: {ex.Message} - STACK: {ex.StackTrace}";
                Console.WriteLine(errMessage);

                try
                {
                    //await LogFailedTransactionAsync(userDetails, request, istDateTime, errMessage, "DB");
                }
                catch { }

                return new PaymentResponseProcess
                {
                    Success = false,
                    Status = "FAILED",
                    Amount = request.Amount.ToString(),
                    OrderId = request.EnquiryReferenceId,
                    ReferenceId = "NA",
                    Category = "Credit Card",
                    BillerName = billerDetails?.blr_name ?? "Unknown"
                };
            }
        }

       


        // reusable helper methods

        private dynamic ParseXmlToJson(string xml)
        {
            var doc = XDocument.Parse(xml);

            return new
            {
                customerName = doc.Descendants("customerName").FirstOrDefault()?.Value,
                billAmount = doc.Descendants("amount").FirstOrDefault()?.Value,
                dueDate = doc.Descendants("dueDate").FirstOrDefault()?.Value
            };
        }

        private string BuildInputParamsFromRequest(List<BBPSInputParamDTO> inputs)
        {
            XElement inputParams = new XElement("inputParams");

            foreach (var input in inputs)
            {
                inputParams.Add(new XElement("input",
                    new XElement("paramName", input.ParamName),
                    new XElement("paramValue", input.ParamValue)
                ));
            }

            return inputParams.ToString(SaveOptions.DisableFormatting);
        }

        private string BuildInputParams(string xml, List<BBPSInputParamDTO> userInputs)
        {
            XDocument doc = XDocument.Parse(xml);

            var requiredParams = doc.Descendants("billerInputParams")
                                    .Descendants("paramInfo")
                                    .Select(x => (string)x.Element("paramName"))
                                    .ToList();

            XElement inputParams = new XElement("inputParams");

            foreach (var paramName in requiredParams)
            {
                string normalizedParam = Normalize(paramName);

                var match = userInputs.FirstOrDefault(x =>
                    normalizedParam.Contains(Normalize(x.ParamName)) ||
                    Normalize(x.ParamName).Contains(normalizedParam)
                );

                string value = match?.ParamValue ?? "";

                inputParams.Add(new XElement("input",
                    new XElement("paramName", paramName),
                    new XElement("paramValue", value)
                ));
            }

            return inputParams.ToString(SaveOptions.DisableFormatting);
        }

        private string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            return input
                .ToLower()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Replace("number", "")
                .Replace("no", "")
                .Replace("digit", "")
                .Replace("of", "")
                .Trim();
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
        public string Encrypt(string plainText, string key)
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

        public string Decrypt(string encryptedHex, string key)
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

        public string MD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        public byte[] HexToBytes(string hex)
        {
            int length = hex.Length;
            byte[] result = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return result;
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


    }


}
