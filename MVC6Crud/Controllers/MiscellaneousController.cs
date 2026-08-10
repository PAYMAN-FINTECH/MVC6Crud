using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.App;
using MVC6Crud.Models.PaymanApp;
using Org.BouncyCastle.Crypto.Generators;
using XAct;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MVC6Crud.Controllers
{
    public class MiscellaneousController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;

        public MiscellaneousController(ApplicationDbContext context, IConfiguration configuration, DataUtils dataUtils)
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
        public  async Task<ActionResult<object>> GetPayoutConfigurationDetails(string userPhone)
        {

            var payoutBankAmount = await _dataUtils.GetPinelabsAmount(userPhone);
            var billAvenueAmount = await _dataUtils.GetInstantPayAmount();


            var payOutMaxAmount = 200000;
            var payOutMinAmount = 100;
            var minBalanceAvl = 100;

            var billAvenueOutMaxAmount = 300000;



            return Ok(new { payoutBankAmount , payOutMaxAmount, payOutMinAmount, minBalanceAvl, billAvenueOutMaxAmount , billAvenueAmount });
        }

        // GET: api/Wallet/Balance?userPhone=XXXXXXXXXX
        [HttpGet]
        public async Task<ActionResult<object>> GetBalance(string userPhone)
        {
            var balance = await _dataUtils.GetUserWalletAmount(userPhone);
            return Ok(new { balance });
        }


        // POST: api/Wallet/PayInTransactions
        [HttpPost]
        public async Task<ActionResult> GetPayInTransactions([FromBody] PayInTransectionRequestModel request)
        {
            var start = request.StartDate.Date;
            var end = request.EndDate.Date.AddDays(1).AddTicks(-1);

            var transactions = await _context.payManPayIns
        .Where(t =>
            t.UserPhone == request.UserPhone &&
            EF.Functions.DateDiffDay(start, t.Created) >= 0 &&
            EF.Functions.DateDiffDay(t.Created, end) >= 0)
        .OrderByDescending(t => t.Created)
                .Select(t => new
                {
                    Amount= t.Amount,
                    TransectionId = t.EasePayId,
                    CardNumber = t.CardNumber,
                    UserPhone= t.UserPhone,
                    Commission= t.PayInCommission,
                    TransectionDate = t.Created.HasValue ? t.Created.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "NA",
                    Status= t.Status,
                    Result = t.Result
                })
                .ToListAsync();

            return Ok(transactions);
        }
        [HttpPost]
        public async Task<ActionResult> GetPayOutTransactions([FromBody] PayInTransectionRequestModel request)
        {
            var startDate = request.StartDate.Date;
            var endDate = request.EndDate.Date;

            var transactions = _context.payManPayOuts
                .Where(t =>
                    t.UserPhone == request.UserPhone &&
            EF.Functions.DateDiffDay(startDate, t.DateTime) >= 0 &&
            EF.Functions.DateDiffDay(t.DateTime, endDate) >= 0).OrderByDescending(t => t.DateTime)
                .Select(t => new
                {
                    Amount =t.Amount,
                    TransectionId =t.RefId,
                    AccountNo= t.AccountNo,
                    UserPhone= t.UserPhone,
                    AccountHolder =t.AccountHolderName,
                    Commission =t.PayoutCommission,
                    TransectionDate = t.DateTime.HasValue ? t.DateTime.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "NA",
                    Status=t.Status,
                    Result =t.Result,
                    PayOutType =t.PayOutType
                })
                .ToList();

            return Ok(transactions);
        }


        [HttpPost]
        public async Task<IActionResult> GetPassbook([FromBody] PayInTransectionRequestModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserPhone))
            {
                return BadRequest("UserPhone is required");
            }

            // 🔸 Transaction list
            var allTransactions = await GetTransactionsForPhone(request.UserPhone);

            // 🔸 Date filtering
            DateTime? start = request.StartDate.Date;
            DateTime? end = request.EndDate.Date;

            var filtered = allTransactions
                .Where(t =>
                    (!start.HasValue || t.DateTime?.Date >= start.Value) &&
                    (!end.HasValue || t.DateTime?.Date <= end.Value))
                .OrderByDescending(t => t.DateTime)
                .ToList();

            // 🔸 Totals
            decimal totalPaid = filtered
                .Where(t => !t.IsCredit)
                .Sum(t => t.Amount ?? 0);

            decimal totalReceived = filtered
                .Where(t => t.IsCredit)
                .Sum(t => t.Amount ?? 0);

            int paidCount = filtered.Count(t => !t.IsCredit);
            int receivedCount = filtered.Count(t => t.IsCredit);

            return Ok(new
            {
                transactions = filtered,
                totalPaid,
                totalReceived,
                totalPaidCount = paidCount,
                totalReceivedCount = receivedCount
            });
        }

        private async Task<List<PayManPassBook>> GetTransactionsForPhone(string phone)
        {
            var payinTransections = await _context.payManPayIns
                .Where(t => t.UserPhone == phone && t.Status == true)
                .ToListAsync();

            var payOutTransections = await _context.payManPayOuts
                .Where(t => t.UserPhone == phone && t.Status == true)
                .ToListAsync();

            var histories = await _context.payManHistories
                .Where(t => t.UserPhone == phone && t.Status == true)
                .OrderByDescending(t => t.Created)
                .ToListAsync();

            var payOutTransections1 = await _context.payManPayOuts
                .Where(t => t.UserPhone == phone)
                .ToListAsync();

            var payinlist = histories.Select(t =>
            {
                var isCredit = t.Mode == "PayIn" || t.Mode == "Refund";

                var payIn = payinTransections
                    .FirstOrDefault(p => p.EasePayId == t.TxnId);

                var payOut = t.Mode == "CC Bill"
                    ? payOutTransections.FirstOrDefault(p => p.PayOutId == t.TxnId)
                    : payOutTransections.FirstOrDefault(p => p.RefId == t.TxnId);

                string status = "";

                if (t.Mode == "PayIn")
                {
                    status = payIn?.Result ?? "";
                }
                else
                {
                    if (t.TxnId == "00")
                    {
                        var ndbjs = payOutTransections1
                            .FirstOrDefault(p => p.Id == t.PayInId);

                        status = ndbjs?.Result ?? "";
                    }
                    else
                    {
                        status = payOut?.Result ?? "";
                    }
                }

                return new PayManPassBook
                {
                    DateTime = t.Created,
                    Amount = t.Amount,
                    Details = t.Mode == "PayIn"
                        ? "PAYMAN"
                        : payOut?.AccountHolderName ?? "",

                    UpiRef = t.TxnId,

                    AccountName = t.Mode == "PayIn"
                        ? payIn?.BankName ?? ""
                        : payOut?.AccountNo ?? "",

                    Tag = t.Mode,
                    IsCredit = isCredit,
                    AvailableBalance = t.AvlBalance,

                    Comm = t.Mode == "PayIn"
                        ? payIn?.PayInCommission ?? 0
                        : payOut?.PayoutCommission ?? 0,

                    Status = status
                };
            }).ToList();

            return payinlist;
        }


        // GET: api/User/GetProfile?userPhone=XXXXXXXXXX
        [HttpGet]
        public async Task<ActionResult<object>> GetProfile(string userPhone)
        {
            try
            {
                var user = await _context.pMUsers.FirstOrDefaultAsync(u => u.Phone == userPhone);

                if (user == null)
                {
                    // First-time user – return default values
                    return Ok(new
                    {
                        name = "",
                        email = "",
                        hasPin = false,
                        kycStatus = "Pending",
                        isNewUser = true,
                        isPanVerified = false,
                        isAadhaarVerified = false,
                        UserType = "B2B"
                    });
                }

                var panAlreadyVerified =
                   await _context.panDetails.AnyAsync(x =>
                       x.UserPhone == userPhone &&
                       x.IsVerified);

                var aadhaarAlreadyVerified =
                    await _context.aadharKycDetails.AnyAsync(x =>
                        x.UserPhone == userPhone &&
                        x.IsVerified);
                var aadharr = await _context.aadharDetails.AnyAsync(x =>
                x.Phone == userPhone && x.Status == true);

                return Ok(new
                {
                    name = user.Name ?? "",
                    email = user.Email ?? "",
                    hasPin = !string.IsNullOrEmpty(user.PinHash),
                    kycStatus = user.IsKycCompleted == true ? "Verified" : "Pending",
                    isNewUser = false,
                    isPanVerified = true,//panAlreadyVerified,
                    isAadhaarVerified = aadharr,//aadhaarAlreadyVerified,
                    UserType = user.UserType ?? "B2B"
                });
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return StatusCode(500, new { success = false, message = "An error occurred while fetching the profile" });

            }
        }

        // POST: api/User/SetPin
        [HttpPost]
        public async Task<IActionResult> SetPin([FromBody] SetPinRequest request)
        {
            var user = await _context.pMUsers.FirstOrDefaultAsync(u => u.Phone == request.UserPhone);
            if (user == null)
                return BadRequest(new { success = false, message = "User not found" });

            // PIN should be 6 digits
            if (request.Pin.Length != 6 || !request.Pin.All(char.IsDigit))
                return BadRequest(new { success = false, message = "PIN must be 6 digits" });   
            
                user.PinHash = request.Pin;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "PIN set successfully" });
        }


        // POST: api/User/ChangePin
        [HttpPost]
        public async Task<IActionResult> ChangePin([FromBody] ChangePinRequest request)
        {
            var user = await _context.pMUsers.FirstOrDefaultAsync(u => u.Phone == request.UserPhone);
            if (user == null)
                return BadRequest(new { success = false, message = "User not found" });

            // Verify old PIN
            if (request.OldPin != user.PinHash)
                return BadRequest(new { success = false, message = "Invalid current PIN" });

            // New PIN validation
            if (request.NewPin.Length != 6 || !request.NewPin.All(char.IsDigit))
                return BadRequest(new { success = false, message = "New PIN must be 6 digits" });

            user.PinHash = request.NewPin;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "PIN changed successfully" });
        }



        // POST: api/User/SendForgotPinOtp
        [HttpPost]
        public async Task<IActionResult> SendForgotPinOtp([FromBody] ForgotPinOtpRequest request)
        {
            var user = await _context.pMUsers.FirstOrDefaultAsync(u => u.Phone == request.UserPhone);
            if (user == null)
                return BadRequest(new { success = false, message = "User not found" });

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            // Store OTP with expiry (e.g., 5 minutes)
            user.PinResetOtp = otp;
            user.PinResetOtpExpiry = DateTime.UtcNow.AddMinutes(5);
            await _context.SaveChangesAsync();

            // Send OTP via SMS (integrate with your SMS provider)
            // await _smsService.SendSmsAsync(request.UserPhone, $"Your OTP to reset PIN is {otp}");

            string message = $"{otp} is your OTP from PAYMAN to reset PIN. Valid for 5 minutes. Never share your OTP or account details with anyone. Regards, PAYMAN";


            var smsResponse = await _dataUtils.SendDLTSms(request.UserPhone, message);

            if (smsResponse != null && smsResponse.@return == true)
            {
                return Ok(new
                {
                    success = true,
                    message = "OTP sent successfully"
                    // otp = otp // Remove in production
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to send OTP"
                });
            }
        }

        // POST: api/User/ResetPin
        [HttpPost]
        public async Task<IActionResult> ResetPin([FromBody] ResetPinRequest request)
        {
            var user = await _context.pMUsers.FirstOrDefaultAsync(u => u.Phone == request.UserPhone);
            if (user == null)
                return BadRequest(new { success = false, message = "User not found" });

            // Validate OTP
            if (user.PinResetOtp != request.Otp || user.PinResetOtpExpiry < DateTime.UtcNow)
                return BadRequest(new { success = false, message = "Invalid or expired OTP" });

            // New PIN validation
            if (request.NewPin.Length != 6 || !request.NewPin.All(char.IsDigit))
                return BadRequest(new { success = false, message = "PIN must be 6 digits" });

            user.PinHash = request.NewPin;
            user.PinResetOtp = null;
            user.PinResetOtpExpiry = null;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "PIN reset successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var user = await _context.pMUsers.FirstOrDefaultAsync(u => u.Phone == request.UserPhone);
            if (user == null)
            {
                // Create new profile record
                user = new PMUsers
                {
                    Id = Guid.NewGuid(),
                    Phone = request.UserPhone,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true,
                    IsKycCompleted = false
                };
                _context.pMUsers.Add(user);
            }

            if (!string.IsNullOrEmpty(request.Name))
                user.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Email))
                user.Email = request.Email;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Profile updated successfully" });
        }

    }

    public class PayInTransectionModel
    {
        public string UserPhone { get; set; }
        public string TransectionId { get; set; }
        public DateTime TransectionDate { get; set; }
        public decimal Amount { get; set; }
        public string CardNumber { get; set; }
        public string CardHolderNumber { get; set; }
        public decimal Commission { get; set; }
        public bool Status { get; set; }

    }

    public class PayInTransectionRequestModel
    {
        public string UserPhone { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class PayManPassBook
    {
        public DateTime? DateTime { get; set; }
        public string? Details { get; set; }
        public string? UpiRef { get; set; }
        public string? Tag { get; set; }
        public string? AccountName { get; set; }
        public decimal? Amount { get; set; }
        public bool IsCredit { get; set; }
        public decimal? AvailableBalance { get; set; }
        public decimal? Comm { get; set; }
        public string? Status { get; set; }
    }

    // Request/Response models
    public class GetProfileResponse
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public bool HasPin { get; set; }
        public string KycStatus { get; set; } // "Pending", "Verified", "Rejected"
    }

    public class SetPinRequest
    {
        public string UserPhone { get; set; }
        public string Pin { get; set; }
    }

    public class ChangePinRequest
    {
        public string UserPhone { get; set; }
        public string OldPin { get; set; }
        public string NewPin { get; set; }
    }

    public class ForgotPinOtpRequest
    {
        public string UserPhone { get; set; }
    }

    public class ResetPinRequest
    {
        public string UserPhone { get; set; }
        public string Otp { get; set; }
        public string NewPin { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string UserPhone { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
