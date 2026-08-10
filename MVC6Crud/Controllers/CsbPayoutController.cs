using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.CSBPayOut;
using MVC6Crud.Models.PaymanApp;
using System.Text.Json;

namespace MVC6Crud.Controllers
{
    public class CsbPayoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;

        public CsbPayoutController( ApplicationDbContext context, IConfiguration configuration, DataUtils dataUtils)
        {
            _context = context;
            _configuration = configuration;
            _dataUtils = dataUtils;
        }
        public IActionResult Index()
        {
            return View();
        }

        //string responseJson = await _dataUtils.ProcessRTGSAsync(token, CSB_KEY);
        //string responseJson = await _dataUtils.ProcessNEFTAsync(token, CSB_KEY);

        // string responseJson = await _dataUtils.TransactionInquiryAsync(token, "PAYMAN20251223160629","NEFT", CSB_KEY);
        //string responseJson = await _dataUtils.GetAccountMiniStatementAsync(token, "PAYMAN20251223160629", "NEFT","", CSB_KEY);


        public async Task<IActionResult> PayToBeneficiary([FromBody] PayOutRequestApp payOutRequest)
        {
            try
            {
                var clientid = _configuration["CSBPayOut:client_id"];
                var CSB_KEY = _configuration["CSBPayOut:client_secret"];
                var Scope = _configuration["CSBPayOut:scope"];
                var Grant_type = _configuration["CSBPayOut:grant_type"];
                var AccountNo = _configuration["CSBPayOut:accountId"];

                if (payOutRequest == null || payOutRequest.amount <= 0)
                {
                    return Json(new { Success = false, objRoot = new { message = "InvalidAmount", OrderRefNo = "" } });
                }

                var amount = Convert.ToDecimal(payOutRequest.amount);

                var userDetails = await _context.payManUsers
                    .FirstOrDefaultAsync(t => t.Phone == payOutRequest.UserPhone);

                if (userDetails == null)
                {
                    return Json(new { Success = false, objRoot = new { message = "UserNotFound", OrderRefNo = "" } });
                }

                var beneficiary = await _context.payManBeneficiaryAccounts
                    .FirstOrDefaultAsync(t => t.Id == payOutRequest.Id);

                if (beneficiary == null)
                {
                    return Json(new { Success = false, objRoot = new { message = "BeneficiaryNotFound", OrderRefNo = "" } });
                }

                var userWalletBalance = await _dataUtils.GetPinelabsAmount(payOutRequest.UserPhone);

                if (amount > Convert.ToDecimal(userWalletBalance))
                {
                    return Json(new
                    {
                        Success = false,
                        objRoot = new
                        {
                            message = "Insufficient wallet balance",
                            OrderRefNo = "",
                            accoutHolderName = ""
                        }
                    });
                }

                // ✅ Get Token
                var tokenRes = await _dataUtils.GetAccessTokenAsync(clientid, CSB_KEY, Scope, Grant_type);
                string token = tokenRes?.access_token ?? "";

                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { Success = false, objRoot = new { message = "TokenGenerationFailed", OrderRefNo = "" } });
                }

                // 📅 IST Time
                DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                // 🔹 Insert PENDING before calling bank
                var pendingRequest = new PaymentRequestApp
                {
                    Phone = payOutRequest.UserPhone,
                    EnquiryReferenceId = "",
                    customerName = beneficiary.ContactName ?? "",
                    holderMobile = beneficiary.AccountNo ?? "",
                    BillerResponse = beneficiary.IfscCode ?? "",
                    Amount = Convert.ToDouble(amount),
                    Device = "web",
                    BillerId = beneficiary.Id.ToString(),
                    LastFourDigits = ""
                };

                Guid payOutDbId = await _dataUtils.LogForPayOutAsync(
                    userDetails,
                    pendingRequest,
                    istDateTime,
                    "PENDING",
                    "",
                    false);

                await _context.SaveChangesAsync();

                // ✅ Call IMPS
                string responseJson = "";
                string transferMode = "";

                // Select payout mode
                if (amount <= 100000)
                {
                    transferMode = "IMPS";

                    responseJson = await _dataUtils.ProcessImpsAsync(
                        token,
                        CSB_KEY,
                        beneficiary,
                        amount,
                        AccountNo);
                }
                else if (amount <= 150000)
                {
                    transferMode = "NEFT";

                    responseJson = await _dataUtils.ProcessNEFTAsync(
                        token,
                        CSB_KEY,
                        beneficiary,
                        amount,
                        AccountNo);
                }
                else
                {
                    transferMode = "RTGS";

                    responseJson = await _dataUtils.ProcessRTGSAsync(
                        token,
                        CSB_KEY,
                        beneficiary,
                        amount,
                        AccountNo);
                }

                if (string.IsNullOrEmpty(responseJson))
                    throw new Exception($"Empty {transferMode} response");

                var bankResponse = JsonSerializer.Deserialize<BankTransferResponse>(responseJson);

                if (bankResponse == null)
                    throw new Exception($"Invalid {transferMode} response format");

                bool isSuccess =
                    !string.Equals(
                        bankResponse.MSGSTATUS,
                        "FAILED",
                        StringComparison.OrdinalIgnoreCase);

                // Update payout
                var payoutRecord = await _context.payManPayOuts
                    .FirstOrDefaultAsync(x => x.Id == payOutDbId);

                if (payoutRecord != null)
                {
                    payoutRecord.PayOutId =
                        bankResponse.TXNID ?? "";

                    payoutRecord.RefId =
                        bankResponse.sourceRefNo
                        ?? bankResponse.txnRefNo
                        ?? bankResponse.txnrefno
                        ?? "";

                    payoutRecord.TxnType = transferMode;

                    payoutRecord.Result =
                        bankResponse.MSGSTATUS ?? "UNKNOWN";

                    payoutRecord.Status = isSuccess;

                    payoutRecord.DateTime = istDateTime;
                }

                if (isSuccess)
                {
                    await _dataUtils.LogForPaoutPayInHistoryAsync(
                        userDetails,
                        pendingRequest,
                        istDateTime,
                        bankResponse.TXNID,
                        true,
                        amount,
                        payOutDbId);
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    Success = isSuccess,
                    objRoot = new
                    {
                        message = isSuccess
                            ? "Transaction Successful"
                            : "Transaction Failed",

                        TransferMode = transferMode,

                        OrderRefNo = bankResponse.TXNID ?? "",

                        UTR =
                            bankResponse.utrno
                            ?? "",

                        accoutHolderName =
                            bankResponse.benName
                            ?? beneficiary.ContactName
                    }
                });
            }
            catch (Exception ex)
            {
                _context.errorModels.Add(new ErrorModel
                {
                    payload = "PayToBeneficiary Exception",
                    agId = "",
                    reqTime = ex.Message,
                    respTime = ex.StackTrace ?? "",
                    statuscode = false,
                    jsonBody = ""
                });

                await _context.SaveChangesAsync();

                return Json(new
                {
                    Success = false,
                    objRoot = new
                    {
                        message = "Something went wrong",
                        OrderRefNo = "",
                        accoutHolderName = ""
                    }
                });
            }
        }
    }
}
