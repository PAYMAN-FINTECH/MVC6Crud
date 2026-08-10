using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models.PaymanApp;

namespace MVC6Crud.Controllers
{
    public class PaytmController : Controller
    {

        private readonly IConfiguration _cfg;
        private readonly PaytmService _ps;
        private readonly ApplicationDbContext _db;
        private readonly DataUtils _dataUtils;

        public PaytmController(IConfiguration cfg, PaytmService ps, ApplicationDbContext db, DataUtils dataUtils)
        {
            _cfg = cfg; _ps = ps; _db = db; _dataUtils = dataUtils;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] PaytmRequest req)

        {
            // IST time
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            var user = await _db.payManUsers
                .FirstOrDefaultAsync(u => u.Phone == req.userPhone);

            if (user == null)
                return Json(new { success = false, message = "User not found" });

            // ❗ Prevent duplicate orderId
            var existing = await _db.payManPayIns
                .FirstOrDefaultAsync(x => x.TxnId == req.OrderId);

            if (existing == null)
            {
                var payIn = new PayManPayIn
                {
                    UserId = user.Id,
                    UserPhone = req.userPhone,
                    TxnId = req.OrderId,              // 🔑 orderId
                    Amount = req.Amount,
                    Gateway = "PaytmEdu",
                    Created = istTime,
                    Status = false,
                    Result = "PENDING",
                    Device = req.divice,
                    CreditCardHolderNum = req.CCard,
                    CreditCardHolderName = req.CName,
                    CardholderMobileNo = req.CMobile,
                    Email = req.CEmail
                };

                _db.payManPayIns.Add(payIn);
                await _db.SaveChangesAsync();
            }


            var result = await _ps.CreateTransaction(
                req.OrderId,
                req.Amount,
                "CUST_" + DateTime.Now.Ticks
            );

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Callback()
        {
            var form = Request.Form;
            try
            {
                var dict = Request.Form.Keys.ToDictionary(k => k, k => Request.Form[k].ToString());

                var payment = new PaymentTransaction
                {
                    OrderId = dict.GetValueOrDefault("ORDERID"),
                    TxnId = dict.GetValueOrDefault("TXNID"),
                    BankTxnId = dict.GetValueOrDefault("BANKTXNID"),
                    Status = dict.GetValueOrDefault("STATUS"),
                    ResponseCode = dict.GetValueOrDefault("RESPCODE"),
                    ResponseMessage = dict.GetValueOrDefault("RESPMSG"),
                    PaymentMode = dict.GetValueOrDefault("PAYMENTMODE"),
                    BankName = dict.GetValueOrDefault("BANKNAME"),
                    Gateway = dict.GetValueOrDefault("GATEWAYNAME"),
                    Amount = Convert.ToDecimal(dict.GetValueOrDefault("TXNAMOUNT")),
                    TransactionDate = DateTime.Parse(dict.GetValueOrDefault("TXNDATE"))
                };

                var res = await _dataUtils.PaytmInDbCall(payment);


                // Save to DB
                //_db.PaymentTransactions.Add(payment);
                //_db.SaveChanges();
                return Redirect(
                        $"https://paymanfintech.in/PayMan/PayStatus" +
                        $"?IsSuccess={res.IsSuccess}" +
                        $"&Amount={Uri.EscapeDataString(res.Amount.ToString())}" +
                        $"&TransactionId={Uri.EscapeDataString(res.TransactionId ?? "FAILED")}"
                    );
            }
            catch (Exception ex)
            {
                return Redirect(
                    $"https://paymanfintech.in/PayMan/PayStatus" +
                    $"?IsSuccess=false" +
                    $"&Amount=0" +
                    $"&TransactionId=FAILED"
                );
            }

           
        }

        
    }

    public class PaytmRequest
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public string userPhone { get; set; }
        public string divice { get; set; }
        public string CCard { get; set; }
        public string CName { get; set; }
        public string CMobile { get; set; }
        public string CEmail { get; set; }
    }
    public class PaymentTransaction
    {
        public string OrderId { get; set; }
        public string TxnId { get; set; }
        public string BankTxnId { get; set; }
        public string Status { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public string PaymentMode { get; set; }
        public string BankName { get; set; }
        public string Gateway { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}
