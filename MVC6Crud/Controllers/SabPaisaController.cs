using Microsoft.AspNetCore.Mvc;
using MVC6Crud.Data;
using MVC6Crud.Models.SabPaisa;

namespace MVC6Crud.Controllers
{
    public class SabPaisaController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        private readonly SabPaisaService _sabPaisaService;
        private readonly ILogger<SabPaisaController> _logger;

        public SabPaisaController(
            SabPaisaService sabPaisaService,
            ILogger<SabPaisaController> logger)
        {
            _sabPaisaService = sabPaisaService;
            _logger = logger;
        }


        // =========================================================
        // CREATE PAYMENT
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> CreatePayment(
            [FromBody] CreatePaymentInput input)
        {
            try
            {
                if (input.Amount <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid amount."
                    });
                }

                // Generate unique PAYMAN Order ID
                string merchantTxnId =
                    $"PM{DateTime.Now:yyMMddHHmmss}" +
                    $"{Random.Shared.Next(1000, 9999)}";


                // =================================================
                // SAVE PENDING PAYMENT IN DATABASE
                // =================================================

                /*
                var payin = new payManPayIn
                {
                    OrderId = merchantTxnId,
                    Amount = input.Amount,
                    Status = "PENDING",
                    Created = DateTime.Now
                };

                _context.payManPayIns.Add(payin);

                await _context.SaveChangesAsync();
                */


                // =================================================
                // CREATE SABPAISA PAYMENT
                // =================================================

                var result =
                    await _sabPaisaService
                        .CreatePaymentAsync(
                            merchantTxnId,
                            input.Amount,
                            input.CustomerName,
                            input.CustomerEmail,
                            input.CustomerPhone,
                            $"PAYMAN Payment {merchantTxnId}");


                if (!result.Success ||
                    string.IsNullOrWhiteSpace(
                        result.CheckoutUrl))
                {
                    return BadRequest(new
                    {
                        success = false,

                        message =
                            result.Message ??
                            "Unable to create SabPaisa payment."
                    });
                }


                // Save SabPaisa PaymentId if required

                /*
                var payin = await _context.payManPayIns
                    .FirstOrDefaultAsync(x =>
                        x.OrderId == merchantTxnId);

                if (payin != null)
                {
                    payin.GatewayTransactionId =
                        result.PaymentId;

                    await _context.SaveChangesAsync();
                }
                */


                return Ok(new
                {
                    success = true,

                    merchantTxnId = merchantTxnId,

                    paymentId = result.PaymentId,

                    checkoutUrl = result.CheckoutUrl,

                    clientSecret = result.ClientSecret
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SabPaisa Create Payment Error");

                return StatusCode(500,
                    new
                    {
                        success = false,
                        message =
                            "Internal server error."
                    });
            }
        }


        // =========================================================
        // PAYMENT RETURN URL
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Return(
            [FromQuery(Name = "transaction_id")]
            string? transactionId,

            [FromQuery(Name = "merchant_txn_id")]
            string? merchantTxnId,

            [FromQuery(Name = "status")]
            string? status,

            [FromQuery(Name = "amount")]
            string? amount,

            [FromQuery(Name = "paid_amount")]
            string? paidAmount,

            [FromQuery(Name = "payment_mode")]
            string? paymentMode,

            [FromQuery(Name = "timestamp")]
            string? timestamp,

            [FromQuery(Name = "signature")]
            string? signature)
        {
            var request =
                new SabPaisaReturnRequest
                {
                    TransactionId = transactionId,
                    MerchantTxnId = merchantTxnId,
                    Status = status,
                    Amount = amount,
                    PaidAmount = paidAmount,
                    PaymentMode = paymentMode,
                    Timestamp = timestamp,
                    Signature = signature
                };


            // =====================================================
            // VERIFY SABPAISA SIGNATURE
            // =====================================================

            bool isValidSignature =
                _sabPaisaService
                    .VerifyReturnSignature(
                        request);

            if (!isValidSignature)
            {
                _logger.LogWarning(
                    "Invalid SabPaisa Signature. OrderId: {OrderId}",
                    merchantTxnId);

                return View(
                    "Failed",
                    new
                    {
                        Message =
                            "Invalid payment response."
                    });
            }


            // =====================================================
            // CHECK PAYMENT STATUS
            // =====================================================

            if (string.Equals(
                status,
                "SUCCESS",
                StringComparison.OrdinalIgnoreCase))
            {
                // IMPORTANT:
                // UPDATE DATABASE ONLY AFTER
                // ADDITIONAL SERVER-SIDE VERIFICATION
                // IF AVAILABLE FOR YOUR SABPAISA ACCOUNT.

                /*
                var payment =
                    await _context.payManPayIns
                    .FirstOrDefaultAsync(
                        x => x.OrderId == merchantTxnId);

                if (payment != null)
                {
                    payment.Status = "SUCCESS";

                    payment.GatewayTransactionId =
                        transactionId;

                    payment.PaymentMode =
                        paymentMode;

                    payment.PaidAmount =
                        Convert.ToDecimal(paidAmount);

                    payment.Completed =
                        DateTime.Now;

                    await _context.SaveChangesAsync();
                }
                */

                ViewBag.OrderId =
                    merchantTxnId;

                ViewBag.TransactionId =
                    transactionId;

                ViewBag.Amount =
                    paidAmount;

                ViewBag.PaymentMode =
                    paymentMode;

                return View("Success");
            }


            // =====================================================
            // PAYMENT FAILED
            // =====================================================

            ViewBag.OrderId =
                merchantTxnId;

            ViewBag.Status =
                status;

            return View("Failed");
        }
    }

    public class CreatePaymentInput
    {
        public decimal Amount { get; set; }

        public string CustomerName { get; set; } = "";

        public string CustomerEmail { get; set; } = "";

        public string CustomerPhone { get; set; } = "";
    }

}
