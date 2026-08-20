using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.App;
using MVC6Crud.Models.BBPS;
using MVC6Crud.Models.BillPaymentsModel;
using MVC6Crud.Models.PaymanApp;
using NuGet.Configuration;

namespace MVC6Crud.Controllers
{
    public class BillPaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;
        private readonly BBPSService bBPSService;
        public BillPaymentsController(ApplicationDbContext context, IConfiguration configuration, DataUtils dataUtils, BBPSService bBPSService)
        {
            _context = context;
            _configuration = configuration;
            _dataUtils = dataUtils;
            this.bBPSService = bBPSService;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> BillersList([FromQuery] BillerListModel billerListModel)
        {
            var query = _context.billAvenueCreditCardBillers
                .Where(t => t.blr_category_name.Trim().ToLower() == billerListModel.BillerName.Trim().ToLower() && t.blr_alias_name == "B2B").OrderByDescending(t=>t.blr_name);

            //if (!string.IsNullOrWhiteSpace(billerListModel.SearchTerm))
            //{
            //    string searchLower = billerListModel.SearchTerm.Trim().ToLower();

            //    query = query
            //        .Where(b => b.blr_name.ToLower().Contains(searchLower));s
            //}

            var filteredBillers = await query
                .Select(x => new
                {
                    billerId = x.blr_id,
                    billerName = x.blr_name,
                    category = x.blr_category_name,
                    iconUrl = x.iconUrl
                })
                .ToListAsync();

            var result = new
            {
                availableAmount = "0.00",
                instantPayBalance = "0.00",
                billAvenue = true,
                billers = filteredBillers,
                billerName = billerListModel.BillerName,
                userPhone = billerListModel.UserPhone,
            };

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> CheckBillerCategory([FromQuery] BillerListModel billerListModel)
        {
            if (string.IsNullOrEmpty(billerListModel.BillerId))
                return BadRequest(new { message = "BillerId is required" });

            billerListModel.BillerId = billerListModel.BillerId.Trim().ToUpper();
            try
            {
                var res = await _dataUtils.GetBillerCategoryAsync(billerListModel.BillerId, billerListModel.UserPhone);
                return Ok(res);
            }
            catch (Exception ex)
            {
            }
            return Ok();

        }


        [HttpPost]
        public async Task<IActionResult> FetchBill([FromBody] BillRequestApp request)
        {
            if (request == null)
                return BadRequest(new { message = "Invalid Request" });

            try
            {
                var gatewayConfig = await _context.PayManGateways.FirstOrDefaultAsync();

                if (gatewayConfig == null)
                    return BadRequest(new { message = "Gateway configuration not found." });

                string requestId = _dataUtils.GenerateRequestId();

                // BILL AVENUE FLOW
                if (gatewayConfig.BillAvenue == true)
                {
                    var result = await _dataUtils.FetchBillAsync(
                        request.BillerId,
                        requestId,
                        request.ServiceNumber,
                        request.CreditCardLast4,
                        request.CustomerMobile,
                        request.UserPhone,
                        request.Category
                    );

                    result.paymentMode = "UPI";
                    result.EnquiryReferenceId = requestId;

                    return Ok(result);
                }

                return Ok(new
                {
                    status = "FAIL",
                    message = "BillAvenue disabled. InstantPay bill fetch not implemented."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "ERROR", message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> ProcessBill([FromBody] PaymentRequestApp request)
        {
            var result = await _dataUtils.ProcessBillPaymentAsync(request);
            return Ok(result);
        }


        // request methods

        [HttpPost]
        public async Task<IActionResult> BBPSFetchBill([FromBody] FetchBillRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrEmpty(req.BillerId))
                {
                    return BadRequest(new BBPSFetchResponse
                    {
                        Status = false,
                        Message = "Invalid Request"
                    });
                }

                var dto = new FetchBillRequestDTO
                {
                    BillerId = req.BillerId,
                    UserPhone = req.UserPhone,
                    Category = req.Category,
                    Inputs = req.Inputs?.Select(i => new BBPSInputParamDTO
                    {
                        ParamName = i.ParamName,
                        ParamValue = i.ParamValue
                    }).ToList()
                };

                var result = await this.bBPSService.FetchBillAsync(dto);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new BBPSFetchResponse
                {
                    Status = false,
                    Message = "Server Error: " + ex.Message
                });
            }
        }

        // ✅ 3. PROCESS PAYMENT
        [HttpPost]
        public async Task<IActionResult> BBPSPayBill([FromBody] BBPSPaymentRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrEmpty(req.BillerId))
                    return BadRequest(new { status = false, message = "Invalid Request" });


                var result = await this.bBPSService.ProcessBillPaymentAsync(req);


                string jsonData = System.Text.Json.JsonSerializer.Serialize(result);

                var user1 = new ErrorModel
                {
                    payload = "Bill pay2",
                    agId = jsonData,
                    reqTime = "",
                    respTime = "",
                    requestId = "",
                    uid = "",
                    statuscode = true,
                    jsonBody = ""
                };

                _context.errorModels.Add(user1);
                await _context.SaveChangesAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = false,
                    message = "API Error",
                    error = ex.Message
                });
            }
        }

       

//        private static readonly List<CardItem> Cards = new() {
//  new CardItem {
//    BankName = "State Bank of India",
//    CardNumber = "XXXX-XXXX-XXXX-4444",
//    Amount = "150.00",
//    DueDate = "2026-07-01",
//    IsOverdue = true,
//    SetupPending = true
//  },
//  new CardItem {
//    BankName = "HDFC Bank",
//    CardNumber = "XXXX-XXXX-XXXX-2446",
//    Amount = "17475.00",
//    DueDate = "2026-07-08",
//    IsOverdue = false,
//    SetupPending = true
//  }
//};
        // For Card Add
       

        public async Task<IActionResult> GetBanks()
        {
            var banks = await _context.billAvenueCreditCardBillers.Where(t=>t.blr_category_name == "Credit Card")
                .Select(b => new
                {
                    key = b.blr_id,
                    value = b.blr_name
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                banks
            });
        }

        [HttpPost]
        public async Task<IActionResult> GetCards([FromBody] CardRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserPhone))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "userPhone is required"
                });
            }

            var cards = _context.paymanCards
                .Where(x => x.UserPhone == request.UserPhone)
                .ToList();

            var cardItems = new List<CardItem>();

            foreach (var card in cards)
            {
                try
                {
                    var billRequest = new BillRequestApp
                    {
                        BillerId = card.BillerId,
                        RegisteredMobile = card.CardRegisterdPhone,
                        CreditCardLast4 = card.CardLastFourDigits,
                        CustomerMobile = card.CardRegisterdPhone,
                        UserPhone = request.UserPhone
                    };

                    var bank = _context.billAvenueCreditCardBillers
                        .FirstOrDefault(x => x.blr_id == card.BillerId);

                    var bill = await _dataUtils.BillAvenueFetchBillMOBCard(billRequest);

                    cardItems.Add(new CardItem
                    {
                        BankName = bank?.blr_name ?? "Unknown Bank",
                        CardNumber = $"XXXX XXXX XXXX {card.CardLastFourDigits}",
                        Amount = Convert.ToDecimal(bill?.TotalAmount) / 100,
                        DueDate = bill?.DueDate ?? "",
                        IsOverdue = false,
                        SetupPending = false
                    });
                }
                catch
                {
                    // Continue processing remaining cards
                }
            }

            return Ok(new
            {
                success = true,
                cards = cardItems
            });
        }

        [HttpPost]
        public IActionResult AddCard([FromBody] AddCardRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserPhone) ||
                string.IsNullOrWhiteSpace(request.MobileNumber) ||
                string.IsNullOrWhiteSpace(request.CardNumber))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "All fields are required"
                });
            }

            var card = new PaymanCards
            {
                BillerId = request.BankId,
                UserPhone = request.UserPhone,
                CardRegisterdPhone = request.MobileNumber,
                CardNumber = request.CardNumber,
                CardLastFourDigits = request.CardNumber.Length >= 4
                    ? request.CardNumber[^4..]
                    : request.CardNumber,
                Craeted = DateTime.Now
            };

            _context.paymanCards.Add(card);
            _context.SaveChanges();

            return Ok(new
            {
                success = true,
                message = "Card added successfully"
            });
        }

        [HttpPost]
        public async Task<IActionResult> GetReceipt(
            [FromBody] GetReceiptRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid request."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.UserPhone))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "User phone is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.TransactionId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Transaction ID is required."
                    });
                }

                var transaction = await _context.bbpsTransactions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.TxnRefId == request.TransactionId &&
                        x.UserPhone == request.UserPhone
                    );

                if (transaction == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Transaction not found."
                    });
                }

                return Ok(new
                {
                    success = true,

                    userName = transaction.RespCustomerName,

                    phone = transaction.UserPhone,

                    amount = transaction.RespAmount,

                    transactionId = transaction.TxnRefId,

                    status = transaction.Status,

                    date = transaction.CreatedAt,

                    customerType = ""
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Unable to get receipt.",
                        error = ex.Message
                    });
            }
        }

        public async Task<IActionResult> GetPaymentHistory(
           [FromBody] PaymentHistoryRequest request)
        {
            try
            {
                // ----------------------------------------------------
                // VALIDATION
                // ----------------------------------------------------

                if (request == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid request."
                    });
                }

                if (string.IsNullOrWhiteSpace(
                    request.UserPhone))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "User phone is required."
                    });
                }

                // ----------------------------------------------------
                // GET USER PAYMENT HISTORY
                // ----------------------------------------------------

                var history =
                    await _context.bbpsTransactions
                        .AsNoTracking()
                        .Where(x =>
                            x.UserPhone ==
                            request.UserPhone)
                        .OrderByDescending(x =>
                            x.CreatedAt)
                        .Select(x => new PaymentHistoryItem
                        {
                            TransactionId =
                                x.TxnRefId ?? string.Empty,

                            UserName =
                                x.RespCustomerName ?? string.Empty,

                            Phone =
                                x.UserPhone ?? string.Empty,

                            Amount =
                                x.RespAmount ,

                            Status =
                                x.ResponseReason ?? string.Empty,

                            CustomerType = "",

                            Date =
                                x.CreatedAt,

                            ServiceName =
                                "Payment"
                        })
                        .ToListAsync();

                // ----------------------------------------------------
                // NO DATA
                // ----------------------------------------------------

                if (history.Count == 0)
                {
                    return Ok(new
                    {
                        success = true,
                        message =
                            "No payment history found.",
                        data = new List<object>()
                    });
                }

                // ----------------------------------------------------
                // SUCCESS
                // ----------------------------------------------------

                return Ok(new
                {
                    success = true,

                    message =
                        "Payment history retrieved successfully.",

                    data = history
                });
            }
            catch (Exception ex)
            {
                // IMPORTANT:
                // Log ex using your logger here.
                // Do not return exception details to mobile app.

                Console.WriteLine(
                    $"GetPaymentHistory Error: {ex}");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,

                        message =
                            "Unable to retrieve payment history.",

                        data = new List<object>()
                    });
            }
        }


    }

    public class PaymentHistoryRequest
    {
        public string UserPhone { get; set; } = string.Empty;
    }

    public class PaymentHistoryResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public List<PaymentHistoryItem> Data { get; set; }
            = new();
    }

    public class PaymentHistoryItem
    {
        public string TransactionId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Status { get; set; } = string.Empty;

        public string CustomerType { get; set; } = string.Empty;

        public DateTime? Date { get; set; }

        public string ServiceName { get; set; } = string.Empty;
    }

    public class GetReceiptRequest
    {
        public string UserPhone { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
    }

    public class CardItem
    {
        public string BankName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; } 
        public string DueDate { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
        public bool SetupPending { get; set; }
    }

    public class CardRequest
    {
        public string UserPhone { get; set; } = string.Empty; 
    }

    public class AddCardRequest
    {
        public string UserPhone { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string BankId { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
    }

    public class BillerRequest
    {
        public string BillerName { get; set; }
        public string UserPhone { get; set; }
    }

    public class FetchBillRequest
    {
        public string BillerId { get; set; }
        public string UserPhone { get; set; }
        public string Category { get; set; }
        public List<BBPSInputParam> Inputs { get; set; }
    }

    public class BBPSInputParam
    {
        public string ParamName { get; set; }
        public string ParamValue { get; set; }
    }

    public class ProcessBillRequest
    {
        public string BillerId { get; set; }
        public string UserPhone { get; set; }
        public double Amount { get; set; }
        public List<BBPSInputParam> Inputs { get; set; }
        public string CustomerName { get; set; }
    }
}
