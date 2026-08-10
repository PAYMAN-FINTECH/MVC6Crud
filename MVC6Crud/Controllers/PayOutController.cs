using DocumentFormat.OpenXml.Wordprocessing;
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
using XAct;
using static WhatsAppApi.Parser.FMessage;

namespace MVC6Crud.Controllers
{
    public class PayOutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;
        public PayOutController(ApplicationDbContext context, IConfiguration configuration, DataUtils dataUtils)
        {
            _context = context;
            _configuration = configuration;
            _dataUtils = dataUtils;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> GetBeneficiaries([FromBody] GetBeneficiariesRequest request)
        {
            // Dummy data — in production, fetch from DB
            var beneficiaries = await _context.payManBeneficiaryAccounts.Where(t=>t.IsActive == true && t.UserPhone == request.Phone).OrderByDescending(b => b.CreatedDate)
         .Select(b => new Beneficiary
         {
             Id = b.Id,
             MobileNumber = b.MobileNumber,
             Name = b.ContactName,
             AccountNumber = b.AccountNo,
             IFSCCode = b.IfscCode,
             IsVerified = b.VerficationFlag == "Y"
         })
         .ToListAsync();

            // You can filter based on user phone if needed
            var response = new GetBeneficiariesResponse
            {
                Success = true,
                Beneficiaries = beneficiaries // You can filter this by request.Phone
            };

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyBeneficiary([FromBody] VerifyRequest request)
        {
            var status = await CashfreeBankAccountVerifaction(request.Id);

            if (status)
            {
                return Ok(new { success = true, message = "Account verified successfully" });
            }
            else
            {
                return BadRequest(new { success = false, message = "Account verification failed" });
            }
        }

        //[HttpPost]
        //public IActionResult PayToBeneficiary([FromBody] PayRequest request)
        //{
        //    //var beneficiary = dummyBeneficiaries
        //    //    .FirstOrDefault(b => b.MobileNumber == request.MobileNumber);

        //    //if (beneficiary == null || !beneficiary.IsVerified)
        //    //    return BadRequest(new { success = false, message = "Invalid or unverified beneficiary" });
        //    var name = "Jurra";

        //    // Simulate payment
        //    return Ok(new { success = true, message = $"Payment of ₹{request.Amount} to {name} successful" });
        //}

        [HttpGet]
        public IActionResult GetBanks()
        {
            var banks = _context.accountVerificationBanks
                .OrderBy(b => b.BankName)
                .Select(b => b.BankName)
                .ToList();

            return Ok(new
            {
                success = true,
                banks = banks
            });
        }


        [HttpPost]
        public async Task<IActionResult> AddBeneficiary([FromBody] Beneficiary model)
        {
            if (string.IsNullOrEmpty(model.MobileNumber) ||
                string.IsNullOrEmpty(model.Name) ||
                string.IsNullOrEmpty(model.AccountNumber) ||
                string.IsNullOrEmpty(model.TxnType) ||
                string.IsNullOrEmpty(model.BankName) ||
                string.IsNullOrEmpty(model.UserPhone))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "All fields are required."
                });
            }

            DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            var userDetails = await _context.payManUsers.FirstOrDefaultAsync(t => t.Phone == model.UserPhone);
            if (userDetails == null)
            {
                return BadRequest(new { success = false, message = "User not found with the provided phone number." });
            }

            var IfscCode = _context.accountVerificationBanks
                .Where(t => t.BankName == model.BankName)
                .Select(s => s.IFSCCode)
                .FirstOrDefault();

            int existingCount = await _context.payManBeneficiaryAccounts
                .CountAsync(b => b.MobileNumber == model.MobileNumber);

            if (existingCount >= 5)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "This mobile number is already linked to 5 accounts. Please use a different number."
                });
            }

            var existingAccount = await _context.payManBeneficiaryAccounts
                .FirstOrDefaultAsync(b =>
                    b.AccountNo == model.AccountNumber &&
                    b.MobileNumber == model.MobileNumber);

            if (existingAccount == null)
            {
                var newAccount = new PayManBeneficiaryAccounts
                {
                    UserId = userDetails.Id,
                    UserPhone = model.UserPhone,
                    ContactName = model.Name,
                    MobileNumber = model.MobileNumber,
                    IfscCode = IfscCode?.ToUpper(),
                    EmailId = userDetails.Email,
                    AccountNo = model.AccountNumber,
                    TxnType = model.TxnType,
                    IsActive = true,
                    CreatedDate = istDateTime,
                    ContactId = "",
                    BeneId = "",
                    VerficationFlag = ""
                };

                await _context.payManBeneficiaryAccounts.AddAsync(newAccount);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Beneficiary added." });
            }
            else
            {
                if (existingAccount.VerficationFlag != "Y")
                {
                    existingAccount.ContactName = model.Name;
                    existingAccount.MobileNumber = model.MobileNumber;
                    existingAccount.IfscCode = IfscCode?.ToUpper();
                    existingAccount.TxnType = model.TxnType;

                    _context.payManBeneficiaryAccounts.Update(existingAccount);
                    await _context.SaveChangesAsync();

                    return Ok(new { success = true, message = "Beneficiary updated." });
                }

                return BadRequest(new { success = false, message = "Beneficiary already exists and is verified." });
            }
        }

        public async Task<bool> CashfreeBankAccountVerifaction(Guid Id)
        {
            var objResult = await _context.payManBeneficiaryAccounts.FirstOrDefaultAsync(t => t.Id == Id);
            if (objResult == null)
                return false;

            var clientId = _configuration["Cashfree:ClientId"];
            var clientSecret = _configuration["Cashfree:ClientSecret"];

            bool status = false;

            using (HttpClient client = new HttpClient())
            {
                var requestUrl = "https://api.cashfree.com/verification/bank-account/sync";
                var requestData = new
                {
                    bank_account = objResult.AccountNo,
                    ifsc = objResult.IfscCode,
                    name = objResult.ContactName,
                    phone = objResult.MobileNumber
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-client-id", clientId);
                client.DefaultRequestHeaders.Add("x-client-secret", clientSecret);

                try
                {
                    var response = await client.PostAsync(requestUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();
                        var bankResponse = JsonConvert.DeserializeObject<CashfreeBankAccountVerificationResponse>(responseData);

                        if (bankResponse != null)
                        {
                            objResult.ContactId = bankResponse.reference_id.ToString();
                            objResult.ContactName = bankResponse.name_at_bank;
                            objResult.BeneId = bankResponse.reference_id.ToString();
                            objResult.VerficationFlag = bankResponse.account_status == "VALID" ? "Y" : "N";
                            objResult.VerificationComm = bankResponse.account_status == "VALID" ? 5 : 0;

                            _context.payManBeneficiaryAccounts.Update(objResult);
                            await _context.SaveChangesAsync();

                            status = bankResponse.account_status == "VALID";
                        }
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        // optionally log the error
                    }
                }
                catch (Exception ex)
                {
                    // optionally log the exception
                }
            }

            return status;
        }

        [HttpPost]
        public async Task<JsonResult> PayToBeneficiary([FromBody] PayOutRequestApp payOutRequest)
        {
            if (payOutRequest.amount <= 0)
            {
                return Json(new { Success = false, objRoot = new { message = "InvalidAmount", OrderRefNo = "" } });
            }

            var amount = Convert.ToInt32(payOutRequest.amount * 100);

            var userDetails = await _context.payManUsers.FirstOrDefaultAsync(t => t.Phone == payOutRequest.UserPhone);
            if (userDetails == null)
            {
                return Json(new { Success = false, objRoot = new { message = "UserNotFound", OrderRefNo = "" } });
            }

            var beneficiary = await _context.payManBeneficiaryAccounts.FirstOrDefaultAsync(t => t.Id == payOutRequest.Id);
            if (beneficiary == null)
            {
                return Json(new { Success = false, objRoot = new { message = "BeneficiaryNotFound", OrderRefNo = "" } });
            }

            var userWalletBalance = await _dataUtils.GetUserWalletAmount(payOutRequest.UserPhone);

            if (Convert.ToDecimal(payOutRequest.amount) > Convert.ToDecimal(userWalletBalance))
            {
                return Json(new
                {
                    Success = false,
                    objRoot = new
                    {
                        message = $"Insufficient wallet balance",
                        OrderRefNo = payOutRequest.amount,
                        accoutHolderName = ""
                    }
                });
            }


            var payoutConfig = await _context.payOutConfics.FirstOrDefaultAsync();

            string status = "";
            string orderRefNo = "";

            try
            {
                // Get first letter of the name
                string firstLetter = userDetails.FirstName.Substring(0, 1);

                // Get last 4 digits of the phone number
                string lastFourDigits = userDetails.Phone.Substring(userDetails.Phone.Length - 4);

                // Combine the results
                string result = firstLetter + lastFourDigits;


                var token = new JWT_Generator().GenerateToken();
                var apiUrl = "https://api.pluralonline.com/payouts/v2/payments/banks";

                var payoutRequest = new PInelabsPayoutTransactionRequest
                {
                    clientReferenceId = Guid.NewGuid().ToString(),
                    payeeName = beneficiary.ContactName?.Replace(".", "") ?? "Unknown",
                    accountNumber = beneficiary.AccountNo,
                    branchCode = beneficiary.IfscCode,
                    email = beneficiary.EmailId,
                    phone = beneficiary.MobileNumber,
                    amount = new PInelabsPayoutAmount
                    {
                        currency = "INR",
                        value = amount
                    },
                    mode = "IMPS",
                    remarks = result
                };

                var jsonContent = JsonConvert.SerializeObject(payoutRequest);

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await client.PostAsync(apiUrl, new StringContent(jsonContent, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { Success = false, objRoot = new { status = "Failed", OrderRefNo = "" } });
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var payoutResponse = JsonConvert.DeserializeObject<PInelabsPayoutResponse>(responseBody);

                // saving part
                DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                var transaction = new PayManPayOut
                {
                    UserId = userDetails.Id,
                    UserPhone = payOutRequest.UserPhone,
                    PayOutId = payoutResponse.clientReferenceId,
                    RefId = payoutResponse.paymentReferenceId,
                    AccountHolderName = payoutResponse.payeeName ?? "Unknown",
                    AccountNo = payoutResponse.accountNumber,
                    IfscCode = beneficiary.IfscCode,
                    Amount = payoutResponse.amount.value / 100.0m,
                    PayoutCommission = 15,
                    BeneId = beneficiary.BeneId,
                    PayOutType = "Pine Labs",
                    TxnType = "IMPS",
                    Email = userDetails.Email,
                    Status = true,
                    Result = "Sucess",
                    DateTime = istDateTime,
                    Device = "Mobile"
                };

                _context.payManPayOuts.Add(transaction);
                await _context.SaveChangesAsync();

                var userWalletAmount = await GetUserWalletAmount(payOutRequest.UserPhone);

                var payInHistory = new PayManHistory
                {
                    UserId = userDetails.Id,
                    UserPhone = payOutRequest.UserPhone,
                    TxnId = payoutResponse.paymentReferenceId,
                    Amount = payoutResponse.amount.value / 100.0m,
                    CardNumber = payoutResponse.accountNumber,
                    Mode = "PayOut",
                    Status = true,
                    Created = istDateTime,
                    AvlBalance = Convert.ToDecimal(userWalletAmount),
                    PayInId = transaction.Id
                };
                _context.payManHistories.Add(payInHistory);
                await _context.SaveChangesAsync();


                // Retry logic: check status up to 5 times
                string payoutStatus = "";
                int retryCount = 0;
                int maxRetries = 3;

                while (retryCount < maxRetries)
                {
                    payoutStatus = await PinelabsStatus(token, "https://api.pluralonline.com/payouts/v2/payments", payoutResponse.paymentReferenceId);

                    if (payoutStatus == "PROCESSED")
                    {
                        status = "Success";
                        orderRefNo = payoutResponse.paymentReferenceId;
                        break;
                    }
                    else if (payoutStatus == "FAILED" || payoutStatus == "REJECTED")
                    {
                        status = payoutStatus;
                        break;
                    }

                    await Task.Delay(3000); // 3-second wait
                    retryCount++;
                }

                if (status != "Success")
                {
                    status = payoutStatus;
                }

                var getPayOut = await _context.payManPayOuts
                    .FirstOrDefaultAsync(t => t.RefId == payoutResponse.paymentReferenceId);

                var getPayOuthistorie = await _context.payManHistories
                    .FirstOrDefaultAsync(t => t.TxnId == payoutResponse.paymentReferenceId);

                if (getPayOut != null)
                {
                    getPayOut.Result = payoutStatus;
                   // getPayOut.Status = payoutStatus == "PROCESSED";

                    _context.payManPayOuts.Update(getPayOut);
                }

                //if (getPayOuthistorie != null)
                //{
                //    getPayOuthistorie.Status = payoutStatus == "PROCESSED";
                //    _context.payManHistories.Update(getPayOuthistorie);
                //}

                // Save only if any changes are made
                if (getPayOut != null || getPayOuthistorie != null)
                {
                    await _context.SaveChangesAsync();
                }


                return Json(new
                {
                    Success = true,
                    objRoot = new
                    {
                        message = $"Payment of ₹{amount / 100.0m} to {payoutResponse.payeeName} successful",
                        OrderRefNo = payInHistory.Amount,
                        accoutHolderName = transaction.AccountHolderName
                    }
                });
            }
            catch (Exception ex)
            {
                // Log error if needed
                return Json(new { Success = false, objRoot = new { message = "Error", OrderRefNo = "" } });
            }
        }



        public async Task<string> PinelabsStatus(string token, string url, string txnId)
        {
            var fullUrl = $"{url}?paymentReferenceId={txnId}";

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    HttpResponseMessage response = await client.GetAsync(fullUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();

                        var objStatus = JsonConvert.DeserializeObject<PineLabsPaoutStatusRootObject>(responseContent);

                        if (objStatus?.payments != null && objStatus.payments.Any())
                        {
                            return objStatus.payments[0].status ?? "UNKNOWN";
                        }
                        else
                        {
                            // Log: No payments data
                            return "NO_PAYMENTS_FOUND";
                        }
                    }
                    else
                    {
                        // Log: StatusCode error
                        return $"HTTP_ERROR_{(int)response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                // Log: Exception
                return $"EXCEPTION: {ex.Message}";
            }
        }

        [HttpPost]
        public IActionResult ValidatePin([FromBody] PinRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Pin))
            {
                return BadRequest(new { success = false, message = "Phone and PIN are required" });
            }

            // First check in payManUsers table
            var user = _context.payManUsers
                .FirstOrDefault(u => u.Phone == request.Phone);

            string storedPin = null;

            if (user != null)
            {
                storedPin = user.UserPin;
            }
            else
            {
                // If not found, check in pMUsers table
                var user1 = _context.pMUsers
                    .FirstOrDefault(u => u.Phone == request.Phone);

                if (user1 == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                storedPin = user1.PinHash;
            }

            // Validate PIN
            if (storedPin != request.Pin)
            {
                return Unauthorized(new { success = false, message = "Invalid PIN" });
            }

            return Ok(new { success = true, message = "PIN validated" });
        }


        [HttpPost]
        public IActionResult DeleteBeneficiary([FromBody] DeleteRequest request)
        {
            var beneficiary = _context.payManBeneficiaryAccounts.FirstOrDefault(b => b.Id == request.Id);

            if (beneficiary == null)
            {
                return NotFound(new { success = false, message = "Beneficiary not found" });
            }

            beneficiary.IsActive = false;

            _context.payManBeneficiaryAccounts.Update(beneficiary);
            _context.SaveChanges();

            return Ok(new { success = true, message = "Beneficiary deleted successfully" });
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


        [HttpPost]
        public async Task<IActionResult> PayoutWebhook([FromBody] WebhookPayload payload)
        {
            if (payload?.Data == null || string.IsNullOrEmpty(payload.Data.PaymentReferenceId))
                return BadRequest("Invalid payload");

            var paymentRefId = payload.Data.PaymentReferenceId;

            var transaction = await _context.payManPayOuts
                .FirstOrDefaultAsync(t => t.RefId == paymentRefId);

            if (transaction == null)
            {
                return NotFound("Transaction not found");
            }

            var jsonContent = JsonConvert.SerializeObject(payload);

            transaction.Result = payload.Data.Status;
           // transaction.Device = jsonContent;
            if(payload.Data.Status == "SUCCESS" || payload.Data.Status == "SCHEDULED"  || payload.Data.Status == "PROCESSED" || payload.Data.Status == "PROCESSING" || payload.Data.Status == "PENDING")
            { 
                transaction.Status = true;

                if(transaction.Result == "FAILED")
                {
                    DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                    var userDetails = await _context.payManUsers.FirstOrDefaultAsync(t => t.Phone == transaction.UserPhone);
                    var userWalletAmount = await GetUserWalletAmount(transaction.UserPhone);

                    var payInHistory = new PayManHistory
                    {
                        UserId = userDetails.Id,
                        UserPhone = userDetails.Phone,
                        TxnId = payload.Data.PaymentReferenceId,
                        Amount = payload.Data.Amount.Value / 100.0m,
                        CardNumber = transaction.AccountNo,
                        Mode = "PayOut-Refund",
                        Status = payload.Data.Status == "SUCCESS",
                        Created = istDateTime,
                        AvlBalance = Convert.ToDecimal(userWalletAmount),
                        PayInId = transaction.Id
                    };
                    _context.payManHistories.Add(payInHistory);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                transaction.Status = false;
            }

            await _context.SaveChangesAsync();


            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> PayToBeneficiaryWeb(Guid Id, decimal amount)
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");

            if (string.IsNullOrEmpty(appPhone))
            {
                return Json(new { success = false, objRoot = new { message = "UserNotLoggedIn", orderRefNo = "" } });
            }

            if (amount <= 0)
            {
                return Json(new { success = false, objRoot = new { message = "InvalidAmount", orderRefNo = "" } });
            }

            var userDetails = await _context.payManUsers.FirstOrDefaultAsync(t => t.Phone == appPhone);
            if (userDetails == null)
            {
                return Json(new { success = false, objRoot = new { message = "UserNotFound", orderRefNo = "" } });
            }

            var beneficiary = await _context.payManBeneficiaryAccounts.FirstOrDefaultAsync(t => t.Id == Id);
            if (beneficiary == null)
            {
                return Json(new { success = false, objRoot = new { message = "BeneficiaryNotFound", orderRefNo = "" } });
            }

            int amountInPaise = (int)(amount * 100);
            string status = "";
            string orderRefNo = "";

            try
            {
                string remarks = userDetails.FirstName.Substring(0, 1) + userDetails.Phone[^4..];

                var token = new JWT_Generator().GenerateToken();
                var apiUrl = "https://api.pluralonline.com/payouts/v2/payments/banks";

                var payoutRequest = new PInelabsPayoutTransactionRequest
                {
                    clientReferenceId = Guid.NewGuid().ToString(),
                    payeeName = beneficiary.ContactName?.Replace(".", "") ?? "Unknown",
                    accountNumber = beneficiary.AccountNo,
                    branchCode = beneficiary.IfscCode,
                    email = beneficiary.EmailId,
                    phone = beneficiary.MobileNumber,
                    amount = new PInelabsPayoutAmount
                    {
                        currency = "INR",
                        value = amountInPaise
                    },
                    mode = "IMPS",
                    remarks = remarks
                };

                var jsonContent = JsonConvert.SerializeObject(payoutRequest);
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await client.PostAsync(apiUrl, new StringContent(jsonContent, Encoding.UTF8, "application/json"));

                //if (!response.IsSuccessStatusCode)
                //{
                //    return Json(new { success = false, objRoot = new { message = "RequestFailed", orderRefNo = "" } });
                //}

                var responseBody = await response.Content.ReadAsStringAsync();
                var payoutResponse = JsonConvert.DeserializeObject<PInelabsPayoutResponse>(responseBody);

                var istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                var transaction = new PayManPayOut
                {
                    UserId = userDetails.Id,
                    UserPhone = appPhone,
                    PayOutId = payoutResponse.clientReferenceId,
                    RefId = payoutResponse.paymentReferenceId,
                    AccountHolderName = payoutResponse.payeeName ?? "Unknown",
                    AccountNo = payoutResponse.accountNumber,
                    IfscCode = beneficiary.IfscCode,
                    Amount = payoutResponse.amount.value / 100.0m,
                    PayoutCommission = 15,
                    BeneId = beneficiary.BeneId,
                    PayOutType = "Pine Labs",
                    TxnType = "IMPS",
                    Email = userDetails.Email,
                    Status = true,
                    Result = "Initiated",
                    DateTime = istDateTime,
                    Device = "Web"
                };

                _context.payManPayOuts.Add(transaction);
                await _context.SaveChangesAsync();

                var userWalletAmount = await GetUserWalletAmount(appPhone);
                var payInHistory = new PayManHistory
                {
                    UserId = userDetails.Id,
                    UserPhone = appPhone,
                    TxnId = payoutResponse.paymentReferenceId,
                    Amount = payoutResponse.amount.value / 100.0m,
                    CardNumber = payoutResponse.accountNumber,
                    Mode = "PayOut",
                    Status = false,
                    Created = istDateTime,
                    AvlBalance = Convert.ToDecimal(userWalletAmount),
                    PayInId = transaction.Id
                };

                _context.payManHistories.Add(payInHistory);
                await _context.SaveChangesAsync();

                // Retry payout status
                string payoutStatus = "";
                int retryCount = 0;
                int maxRetries = 3;

                while (retryCount < maxRetries)
                {
                    payoutStatus = await PinelabsStatus(token, "https://api.pluralonline.com/payouts/v2/payments", payoutResponse.paymentReferenceId);

                    if (payoutStatus == "PROCESSED")
                    {
                        status = "Success";
                        orderRefNo = payoutResponse.paymentReferenceId;
                        break;
                    }
                    else if (payoutStatus == "FAILED" || payoutStatus == "REJECTED")
                    {
                        status = payoutStatus;
                        break;
                    }

                    await Task.Delay(3000);
                    retryCount++;
                }

                if (status != "Success")
                {
                    status = payoutStatus;
                }

                var getPayOut = await _context.payManPayOuts.FirstOrDefaultAsync(t => t.RefId == payoutResponse.paymentReferenceId);
               // var getHistory = await _context.payManHistories.FirstOrDefaultAsync(t => t.TxnId == payoutResponse.paymentReferenceId);

                if (getPayOut != null)
                {
                    getPayOut.Result = status;
                    _context.payManPayOuts.Update(getPayOut);
                }

                //if (getHistory != null)
                //{
                //    getHistory.Status = status == "PROCESSED";
                //    _context.payManHistories.Update(getHistory);
                //}

                await _context.SaveChangesAsync();

                var model = new PaymentStatusViewModel
                {
                    IsSuccess = status == "PROCESSED",
                    Amount = Convert.ToInt32( payoutResponse.amount.value / 100.0m),
                    TransactionId = payoutResponse.paymentReferenceId,
                    CardNumber = payoutResponse.payeeName
                };

                return View("PayStatus", model);
            }
            catch (Exception ex)
            {

                var model = new PaymentStatusViewModel
                {
                    IsSuccess = false,
                    Amount = 0,
                    TransactionId = "",
                    CardNumber = ""
                };

                return View("PayStatus", model);
            }
        }






        [HttpGet]
        public IActionResult GetInvoiceDetails([FromQuery] string txn)
        {
            if (string.IsNullOrWhiteSpace(txn))
            {
                return NotFound(new { Message = "Invoice not found" });
            }

            var address = "";

            var payoutDetails = _context.payManPayOuts
                .FirstOrDefault(t => t.RefId == txn || t.PayOutId == txn);

            if (payoutDetails == null)
            {
                return NotFound(new { Message = "Invoice not found" });
            }

            var rse = _context.aadharDetails
                .FirstOrDefault(t => t.Phone == payoutDetails.UserPhone);

            if (rse != null)
            {
                address = rse.Address;
            }

            // Only time part for invoice suffix
            var timeOnly = DateTime.Now.ToString("HHmmss"); // HHmmss for only time

            // Calculate taxes
            decimal commission = payoutDetails.PayoutCommission ?? 0;
            decimal cgst = commission * 0.09m; // 8%
            decimal sgst = commission * 0.09m; // 8%
            decimal total = commission + cgst + sgst;

            var invoice = new
            {
                InvoiceNo = payoutDetails.DateTime.HasValue ? $"PM/INV/{payoutDetails.DateTime.Value:yy}/{payoutDetails.DateTime.Value:MMddHHmmss}" : null,
                Date = payoutDetails.DateTime.HasValue ? payoutDetails.DateTime.Value.ToString("o") : null,
                TransactionId = txn.ToUpper(),
                From = new
                {
                    Name = "PayMan Fintech Solutions Pvt. Ltd.",
                    Address = "10-100 Hmt Nagar Stnumber 10, Nacharam, Hyderabad, Telangana, 500076",
                    GSTIN = "36AAOCP3061H1Z9"
                },
                To = new
                {
                    Name = rse.Name,
                    Address = address
                },
                LineItems = new[] {
            new {
                SNo = 1,
                Service = "Credit Card Bill Payment Fee",
                HsnSac = "997158",
                Qty = 1,
                UnitPrice = commission,
                Amount = commission
            }
        },
                CGST = cgst,
                SGST = sgst,
                Total = total
            };

            return Ok(invoice);
        }


        [HttpGet]
        public IActionResult ViewInvoice([FromQuery] string txn, [FromQuery] string InvoiceNo = null)
        {
            if (string.IsNullOrWhiteSpace(txn))
                return BadRequest(new { Message = "Invalid transaction reference" });

            var payoutDetails = _context.payManPayOuts
                .FirstOrDefault(t => t.RefId == txn || t.PayOutId == txn);

            if (payoutDetails == null)
                return NotFound(new { Message = "Invoice not found" });

            var userInfo = _context.aadharDetails
                .FirstOrDefault(t => t.Phone == payoutDetails.UserPhone);

            string address = userInfo?.Address ?? "Not Available";

            // Commission calculation
            decimal commission = payoutDetails.PayoutCommission ?? 0;
            decimal cgst = commission * 0.09m; // 9%
            decimal sgst = commission * 0.09m; // 9%
            decimal total = commission + cgst + sgst;

            var model = new InvoiceViewModel
            {
                InvoiceNo = payoutDetails.DateTime.HasValue ? $"PM/INV/{payoutDetails.DateTime.Value:yy}/{payoutDetails.DateTime.Value:MMddHHmmss}" : null,
                TransactionId = txn,
                Date = payoutDetails.DateTime.HasValue ? payoutDetails.DateTime.Value.ToString("o") : null,
                From = new PartyInfo
                {
                    Name = "PayMan Fintech Solutions Pvt. Ltd.",
                    Address = "10-100 Hmt Nagar, St Number 10, Nacharam, Hyderabad, Telangana, 500076",
                    Gstin = "36AAOCP3061H1Z9"
                },
                To = new PartyInfo
                {
                    Name = userInfo.Name,
                    Address = address
                },
                LineItems = new List<InvoiceLineItem>
        {
            new InvoiceLineItem
            {
                SNo = 1,
                Service =  "Credit Card Bill Payment",
                HsnSac = "9985",
                Qty = 1,
                UnitPrice = commission,
                Amount = commission
            }
        },
                Cgst = cgst,
                Sgst = sgst,
                Total = total
            };

            return View(model);
        }



        [HttpGet]
        public IActionResult GetInvoiceDetailsAgent([FromQuery] string txn)
        {
            if (string.IsNullOrWhiteSpace(txn))
            {
                return NotFound(new { Message = "Invoice not found" });
            }

            var address = "";

            var payoutDetails = _context.payManPayOuts
                .FirstOrDefault(t => t.RefId == txn || t.PayOutId == txn);

            if (payoutDetails == null)
            {
                return NotFound(new { Message = "Invoice not found" });
            }

            var rse = _context.aadharDetails
                .FirstOrDefault(t => t.Phone == payoutDetails.UserPhone);

            if (rse != null)
            {
                address = rse.Address;
            }

            // Only time part for invoice suffix
            var timeOnly = DateTime.Now.ToString("HHmmss"); // HHmmss for only time

            // Calculate taxes
            decimal commission = payoutDetails.PayoutCommission ?? 0;
            decimal cgst = commission * 0.09m; // 8%
            decimal sgst = commission * 0.09m; // 8%
            decimal total = commission + cgst + sgst;

            var invoice = new
            {
              
                Date = payoutDetails.DateTime.HasValue ? payoutDetails.DateTime.Value.ToString("o") : null,
                InvoiceNo = payoutDetails.DateTime.HasValue? $"PM/INV/{payoutDetails.DateTime.Value:yy}/{payoutDetails.DateTime.Value:MMddHHmmss}": null,
                TransactionId = txn.ToUpper(),
                From = new
                {
                    Name = "PayMan Fintech Solutions Pvt. Ltd.",
                    Address = "10-100 Hmt Nagar Stnumber 10, Nacharam, Hyderabad, Telangana, 500076",
                    GSTIN = "36AAOCP3061H1Z9"
                },
                To = new
                {
                    Name = rse.Name,
                    Address = address
                },
                LineItems = new[] {
            new {
                SNo = 1,
                Service = "Credit Card Bill Payment Fee",
                HsnSac = "997158",
                Qty = 1,
                UnitPrice = payoutDetails.PayoutCommission,
                Amount = payoutDetails.Amount
            }
        },
                CGST = cgst,
                SGST = sgst,
                Total = total
            };

            return Ok(invoice);
        }



    }
}
