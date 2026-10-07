using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.App;
using MVC6Crud.Models.PaymanApp;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MVC6Crud.Controllers
{
    public class TAPayController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DataUtils _dataUtils;
        private readonly ApplicationDbContext _context;

        private const string BINLIST_URL =
           "https://raw.githubusercontent.com/venelinkochev/bin-list-data/refs/heads/master/bin-list-data.csv";

        private static List<BinData>? _cachedData;
        private static bool _isOnline;
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);

        public TAPayController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory, ApplicationDbContext db, DataUtils dataUtils)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _dataUtils = dataUtils;
            _context = db; 

        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TApayInit(decimal amount, string userPhone, string CName, string CMobile, string CCard, string Cemail, string gateway, string divice)
        {
            var orderId = $"PAYMAN{DateTime.UtcNow:yyyyMMddHHmmssfff}{Random.Shared.Next(1000, 9999)}";

            // IST time
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == userPhone);

            var existing = await _context.payManPayIns
                   .FirstOrDefaultAsync(x => x.TxnId == orderId);

            if (existing == null)
            {
                var payIn = new PayManPayIn
                {
                    UserId = user.Id,
                    UserPhone = userPhone,
                    TxnId = orderId,              // 🔑 orderId
                    Amount = amount,
                    Gateway = gateway,
                    Created = istTime,
                    Email = Cemail,
                    Status = false,
                    Result = "PENDING",
                    Device = divice,
                    CreditCardHolderNum = CCard,
                    CreditCardHolderName = CName,
                    CardholderMobileNo = CMobile
                };

                _context.payManPayIns.Add(payIn);
                await _context.SaveChangesAsync();
            }

            var model = new TaPaySessionModel
            {
                Amount = amount,
                CMobile = CMobile,
                Cemail = Cemail,
                CName = CName,
                userPhone = userPhone,
                CCard = CCard,
                gateway = gateway,
                orderId = orderId
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment(
            [FromBody] PaymentRequest request)
        {
            try
            {
                var apiKey =
                    _configuration["TAPaymentGateway:ApiKey"];

                var salt =
                    _configuration["TAPaymentGateway:Salt"];

                var apiUrl =
                    _configuration["TAPaymentGateway:ApiUrl"];

                if (string.IsNullOrWhiteSpace(apiKey) ||
                    string.IsNullOrWhiteSpace(salt) ||
                    string.IsNullOrWhiteSpace(apiUrl))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Payment gateway configuration missing"
                    });
                }

                // Generate unique order ID
                

                var returnUrl = "https://edu.paymanfintech.in/TAPay/Success";

                var failureUrl = "https://edu.paymanfintech.in/TAPay/Failure";

                var cancelUrl = "https://edu.paymanfintech.in/TAPay/Cancel";

                var parameters =
                    new Dictionary<string, string>
                    {
                        ["amount"] = request.Amount.ToString("0.00"),
                        ["api_key"] = apiKey,
                        ["currency"] = "INR",
                        ["description"] = "Education fee payments",
                        ["email"] = request.Email ?? "",
                        ["mode"] = "PROD",
                        ["name"] = request.Name ?? "",
                        ["order_id"] = request.orderId ?? "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                        ["phone"] = request.Phone ?? "",
                        ["city"] = "hyd",
                        ["country"] = "ind",
                        ["zip_code"] = "500050",
                        ["return_url"] = returnUrl,
                        ["return_url_cancel"] = cancelUrl,
                        ["return_url_failure"] = failureUrl
                    };

                // Gateway hash
                var hash = GenerateHash(
                    parameters,
                    salt);

                parameters["hash"] = hash;

                var client =
                    _httpClientFactory.CreateClient();

                using var content =
                    new FormUrlEncodedContent(parameters);

                var response =
                    await client.PostAsync(
                        $"{apiUrl}/v2/getpaymentrequesturl",
                        content);

                var responseText =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Gateway request failed",
                        response = responseText
                    });
                }

                using var json =
                    JsonDocument.Parse(responseText);

                var root = json.RootElement;

                string? paymentUrl = null;

                if (root.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty(
                        "url",
                        out var urlProperty))
                    {
                        paymentUrl =
                            urlProperty.GetString();
                    }
                }

                if (string.IsNullOrWhiteSpace(paymentUrl))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Payment URL not received",
                        response = responseText
                    });
                }

                return Json(new
                {
                    success = true,
                    paymentUrl = paymentUrl,
                    orderId = request.orderId
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        private async Task<PaymentStatusResult?> CheckPaymentStatusAsync(
    string orderId,
    string? transactionId = null)
        {
            var apiKey =
                   _configuration["TAPaymentGateway:ApiKey"];

            var salt =
                _configuration["TAPaymentGateway:Salt"];

            var apiUrl =
                _configuration["TAPaymentGateway:ApiUrl"];

            var parameters = new Dictionary<string, string>
    {
        { "api_key", apiKey! },
        { "order_id", orderId }
    };

            // If gateway returned transaction_id, send it too
            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                parameters.Add("transaction_id", transactionId);
            }

            // Generate gateway hash
            parameters["hash"] = GenerateHash(parameters, salt!);

            using var client = _httpClientFactory.CreateClient();

            var content = new FormUrlEncodedContent(parameters);

            var response = await client.PostAsync(
                $"{apiUrl}/v2/paymentstatus",
                content);

            var responseJson = await response.Content.ReadAsStringAsync();

            // Log actual gateway response
            var responseLog = new ErrorModel
            {
                payload = "TA Pay Status response",
                agId = "",
                reqTime = "",
                respTime = responseJson,
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(responseLog);
            await _context.SaveChangesAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentStatusResult
                {
                };

            }

            try
            {
                var result = JsonSerializer.Deserialize<PaymentStatusResult>(
                    responseJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result == null)
                {
                    return new PaymentStatusResult
                    {
                    };

                }

                //result.RawResponse = responseJson;

                return result;
            }
            catch
            {
                return new PaymentStatusResult
                {
                };
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckCardRepeatedToday(
    [FromBody] CardRepeatRequest request)
        {
            try
            {
                //if (request == null ||
                //    string.IsNullOrWhiteSpace(request.Last4) ||
                //    !Regex.IsMatch(request.Last4, @"^\d{4}$"))
                //{
                //    return BadRequest(new
                //    {
                //        success = false,
                //        message = "Invalid card number."
                //    });
                //}

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var repeated = await _context.payManPayIns
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.CreditCardHolderNum != null &&
                        x.CreditCardHolderNum == request.Last4 &&
                        x.Created >= today &&
                        x.Created < tomorrow &&
                        x.Status == true
                    );

                return Json(new
                {
                    success = true,
                    repeated = repeated,
                    message = repeated
                        ? "This card has already been used today."
                        : "Card is available."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to check card.",
                    error = ex.Message
                });
            }
        }

        private static string GenerateHash(
            Dictionary<string, string> parameters,
            string salt)
        {
            var hashData = new StringBuilder();

            hashData.Append(salt);

            foreach (var item in parameters
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .OrderBy(x => x.Key))
            {
                hashData.Append("|");
                hashData.Append(item.Value.Trim());
            }

            using var sha512 =
                SHA512.Create();

            var bytes =
                Encoding.UTF8.GetBytes(
                    hashData.ToString());

            var hash =
                sha512.ComputeHash(bytes);

            return Convert
                .ToHexString(hash)
                .ToUpperInvariant();
        }
        [HttpPost]
        public async Task<IActionResult> Success()
        {
            // Log that callback was reached
            var initialLog = new ErrorModel
            {
                payload = "TA Pay response - callback reached",
                agId = "",
                reqTime = "",
                respTime = "",
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(initialLog);
            await _context.SaveChangesAsync();


            // Read gateway POST data
            var form = Request.HasFormContentType
                ? Request.Form
                : null;

            var response = form?.ToDictionary(
                x => x.Key,
                x => x.Value.ToString()
            ) ?? new Dictionary<string, string>();


            // Convert to JSON
            var responseJson = JsonSerializer.Serialize(response);

            // Log actual gateway response
            var responseLog = new ErrorModel
            {
                payload = "TA Pay response",
                agId = "",
                reqTime = "",
                respTime = responseJson,
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(responseLog);
            await _context.SaveChangesAsync();


            var model = JsonSerializer.Deserialize<TAPayResponse>(
    responseJson,
    new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

            // FIRST: Check actual status from gateway
            var status = await CheckPaymentStatusAsync(
                model.order_id,
                model.transaction_id);

            model.bank_code = status?.Data[0].BankCode ?? string.Empty;


            var res = await _dataUtils.TAPayInDbCall(model);

            if (res.IsSuccess)
            {
                return Redirect(
                       $"https://paymanfintech.in/PayMan/PayStatus" +
                       $"?IsSuccess={res.IsSuccess}" +
                       $"&Amount={Uri.EscapeDataString(res.Amount.ToString())}" +
                       $"&TransactionId={Uri.EscapeDataString(res.TransactionId ?? "FAILED")}"
                   );
            }
            else
            {
                return Redirect(
                   $"https://paymanfintech.in/PayMan/PayStatus" +
                   $"?IsSuccess=false" +
                   $"&Amount=0" +
                   $"&TransactionId=FAILED"
               );
            }
        }



        [HttpPost]
        public async Task<IActionResult> Failure()
        {
            // Read gateway POST data
            var form = Request.HasFormContentType
                ? Request.Form
                : null;

            var response = form?.ToDictionary(
                x => x.Key,
                x => x.Value.ToString()
            ) ?? new Dictionary<string, string>();


            // Convert to JSON
            var responseJson = JsonSerializer.Serialize(response);

            var responseLog = new ErrorModel
            {
                payload = "TA Pay Failed response",
                agId = "",
                reqTime = "",
                respTime = responseJson,
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(responseLog);
            await _context.SaveChangesAsync();

            return Redirect(
                  $"https://paymanfintech.in/PayMan/PayStatus" +
                  $"?IsSuccess=false" +
                  $"&Amount=0" +
                  $"&TransactionId=FAILED"
              );
        }

        [HttpPost]
        public IActionResult Cancel()
        {
            return Redirect(
                  $"https://paymanfintech.in/PayMan/PayStatus" +
                  $"?IsSuccess=false" +
                  $"&Amount=0" +
                  $"&TransactionId=FAILED"
              );
        }



        [HttpPost]
        public async Task<IActionResult> CheckBin(
     [FromBody] CardBinRequest request,
     CancellationToken ct)
        {
            // ============================================
            // 1. Validate request
            // ============================================

            if (request == null || string.IsNullOrWhiteSpace(request.Bin))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "BIN is required."
                });
            }

            var bin = request.Bin.Trim();

            if (!Regex.IsMatch(bin, @"^\d{6,8}$"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "BIN must be 6 to 8 digits."
                });
            }

            try
            {
                // ============================================
                // 2. FIRST CHECK LOCAL DATABASE
                // ============================================

                var existingBin = await _context.pMBinCheckers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.CardNumber == bin,
                        ct);

                if (existingBin != null)
                {
                    return Ok(new
                    {
                        success = true,
                        source = "DATABASE",
                        message = "BIN found in local database.",
                        bin = existingBin.CardNumber,
                        bankName = existingBin.BankName,
                        brand = existingBin.brand,
                        isActive = existingBin.IsActive
                    });
                }

                // ============================================
                // 3. BIN NOT FOUND
                //    CALL INSTANTPAY
                // ============================================

                using var client = new HttpClient();

                client.DefaultRequestHeaders.Add(
                    "Accept",
                    "application/json");

                client.DefaultRequestHeaders.Add(
                    "X-Ipay-Auth-Code",
                    "1");

                client.DefaultRequestHeaders.Add(
                    "X-Ipay-Client-Id",
                    "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");

                client.DefaultRequestHeaders.Add(
                    "X-Ipay-Client-Secret",
                    "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");

                client.DefaultRequestHeaders.Add(
                    "X-Ipay-Endpoint-Ip",
                    "13.200.194.39");

                client.DefaultRequestHeaders.Add(
                    "X-Ipay-Outlet-Id",
                    "490007");

                client.Timeout = TimeSpan.FromSeconds(15);

                // ============================================
                // 4. GENERATE EXTERNAL REFERENCE
                // ============================================

                var refId =
                    $"PAYMAN{DateTime.UtcNow:yyyyMMddHHmmssfff}" +
                    $"{Random.Shared.Next(1000, 9999)}";

                // ============================================
                // 5. INSTANTPAY REQUEST
                // ============================================

                var requestData = new
                {
                    binNumber = bin,
                    latitude = "38.8951",
                    longitude = "-77.0364",
                    externalRef = refId
                };

                var jsonRequest =
                    JsonSerializer.Serialize(requestData);

                using var content = new StringContent(
                    jsonRequest,
                    Encoding.UTF8,
                    "application/json");

                // ============================================
                // 6. CALL INSTANTPAY
                // ============================================

                using var response = await client.PostAsync(
                    "https://api.instantpay.in/identity/binChecker",
                    content,
                    ct);

                var responseContent =
                    await response.Content.ReadAsStringAsync(ct);

                // ============================================
                // 7. API ERROR
                // ============================================

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        success = false,
                        source = "INSTANTPAY",
                        bin = bin,
                        message = "InstantPay BIN API failed.",
                        statusCode = (int)response.StatusCode,
                        response = responseContent
                    });
                }

                // ============================================
                // 8. DESERIALIZE INSTANTPAY RESPONSE
                // ============================================

                var instantPayResponse =
                    JsonSerializer.Deserialize<BinCheckResponse>(
                        responseContent,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (instantPayResponse == null)
                {
                    return StatusCode(502, new
                    {
                        success = false,
                        source = "INSTANTPAY",
                        bin = bin,
                        message = "Invalid InstantPay response."
                    });
                }

                // ============================================
                // 9. CHECK TRANSACTION STATUS
                // ============================================

                if (!string.Equals(
                        instantPayResponse.StatusCode,
                        "TXN",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new
                    {
                        success = false,
                        source = "INSTANTPAY",
                        bin = bin,
                        message = instantPayResponse.Status ??
                                  "Unable to fetch BIN details.",
                        statusCode = instantPayResponse.StatusCode,
                        response = responseContent
                    });
                }

                // ============================================
                // 10. GET BIN DETAILS
                // ============================================

                var binDetails =
                    instantPayResponse.Data?.BinDetails;

                if (binDetails == null)
                {
                    return StatusCode(502, new
                    {
                        success = false,
                        source = "INSTANTPAY",
                        bin = bin,
                        message = "BIN details not available.",
                        response = responseContent
                    });
                }

                var returnedBin =
                    binDetails.Bin?.Trim();

                var bankName =
                    binDetails.IssuerBank?.Trim();

                var brand =
                    binDetails.CardNetwork?.Trim();

                var cardType =
                    binDetails.CardType?.Trim();

                var cardLevel =
                    binDetails.CardLevel?.Trim();

                // ============================================
                // 11. BANK NAME VALIDATION
                // ============================================

                if (string.IsNullOrWhiteSpace(bankName))
                {
                    return Ok(new
                    {
                        success = false,
                        source = "INSTANTPAY",
                        bin = returnedBin ?? bin,
                        message = "BIN found but bank name is not available.",
                        bankName = "",
                        isActive = false
                    });
                }

                // ============================================
                // 12. CHECK AGAIN BEFORE INSERT
                //     Prevent duplicate records
                // ============================================

                var alreadyInserted =
                    await _context.pMBinCheckers
                        .FirstOrDefaultAsync(
                            x => x.CardNumber == (returnedBin ?? bin),
                            ct);

                if (alreadyInserted == null)
                {
                    // ========================================
                    // 13. INSERT BIN
                    // ========================================

                    var newBin = new PMBinChecker
                    {
                        // DO NOT SET Id
                        // SQL Server Identity will generate it

                        CardNumber = returnedBin ?? bin,
                        BankName = bankName,
                        brand = brand,
                        response = responseContent,
                        IsActive = true,
                        Created = DateTime.Now,
                        CardLevel = cardLevel,
                        CardType = cardType
                    };

                    _context.pMBinCheckers.Add(newBin);

                    await _context.SaveChangesAsync(ct);
                }

                // ============================================
                // 14. RETURN RESULT
                // ============================================

                return Ok(new
                {
                    success = true,
                    source = "INSTANTPAY",
                    message = "BIN found and saved successfully.",
                    bin = returnedBin ?? bin,
                    bankName = bankName,
                    cardNetwork = brand,
                    cardType = cardType,
                    cardLevel = cardLevel,
                    country = binDetails.IsoCountryName,
                    countryCode = binDetails.IsoCountryA2,
                    isActive = true
                });
            }
            catch (TaskCanceledException)
                when (!ct.IsCancellationRequested)
            {
                return StatusCode(504, new
                {
                    success = false,
                    bin = bin,
                    message = "InstantPay BIN API request timed out."
                });
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                return StatusCode(499, new
                {
                    success = false,
                    bin = bin,
                    message = "Request cancelled."
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new
                {
                    success = false,
                    bin = bin,
                    message = "Unable to connect to InstantPay BIN API.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    bin = bin,
                    message = "Unexpected error.",
                    error = ex.Message
                });
            }
        }

        //[HttpPost]
        //public async Task<IActionResult> CheckBin(
        //  [FromBody] CardBinRequest request,
        //  CancellationToken cancellationToken)
        //{
        //    if (string.IsNullOrWhiteSpace(request.Bin))
        //    {
        //        return BadRequest(new
        //        {
        //            error = "BIN parameter is required"
        //        });
        //    }

        //    if (!System.Text.RegularExpressions.Regex.IsMatch(request.Bin, @"^\d{6,8}$"))
        //    {
        //        return BadRequest(new
        //        {
        //            error = "BIN must be 6 to 8 digits"
        //        });
        //    }

        //    try
        //    {
        //        await LoadBinData(cancellationToken);

        //        var foundBin = _cachedData?
        //            .FirstOrDefault(x =>
        //                string.Equals(x.BIN, request.Bin, StringComparison.OrdinalIgnoreCase));

        //        if (foundBin == null)
        //        {
        //            return NotFound(new
        //            {
        //                error = "BIN not found"
        //            });
        //        }

        //        return Ok(new
        //        {
        //            success = true,
        //            source = "DATABASE",
        //            message = "BIN found in local database.",
        //            bin = foundBin.BIN,
        //            bankName = foundBin.Issuer,
        //            isActive = true
        //        });

        //        //return Ok(new
        //        //{
        //        //    foundBin.BIN,
        //        //    foundBin.Brand,
        //        //    foundBin.Type,
        //        //    foundBin.Category,
        //        //    foundBin.Issuer,
        //        //    foundBin.Country,
        //        //    foundBin.Currency,
        //        //    source = _isOnline ? "Online" : "Local"
        //        //});
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error processing BIN request: {ex}");

        //        return StatusCode(500, new
        //        {
        //            error = "Internal Server Error"
        //        });
        //    }
        //}

        private async Task LoadBinData(CancellationToken cancellationToken)
        {
            if (_cachedData != null && _cachedData.Count > 0)
                return;

            await _cacheLock.WaitAsync(cancellationToken);

            try
            {
                // Double-check after acquiring lock
                if (_cachedData != null && _cachedData.Count > 0)
                    return;

                List<BinData>? data = null;

                // ==========================================
                // 1. Try GitHub CSV
                // ==========================================
                try
                {
                    using var httpClient = new HttpClient();

                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    var csvText = await httpClient.GetStringAsync(
                        BINLIST_URL,
                        cancellationToken);

                    data = ParseCsv(csvText);

                    if (data.Count > 0)
                    {
                        _isOnline = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Online BIN fetch failed: {ex.Message}");
                }

                // ==========================================
                // 2. Local CSV fallback
                // ==========================================
                if (data == null || data.Count == 0)
                {
                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "src",
                        "data",
                        "offline-bin-list-data.csv");

                    if (!System.IO.File.Exists(filePath))
                    {
                        throw new FileNotFoundException(
                            "Local BIN CSV file not found.",
                            filePath);
                    }

                    var fileContent =
                        await System.IO.File.ReadAllTextAsync(
                            filePath,
                            cancellationToken);

                    data = ParseCsv(fileContent);

                    _isOnline = false;
                }

                _cachedData = data;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private static List<BinData> ParseCsv(string csv)
        {
            var result = new List<BinData>();

            using var reader = new StringReader(csv);

            var headerLine = reader.ReadLine();

            if (string.IsNullOrWhiteSpace(headerLine))
                return result;

            var headers = ParseCsvLine(headerLine);

            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var values = ParseCsvLine(line);

                if (values.Count == 0)
                    continue;

                var item = new BinData();

                for (int i = 0; i < headers.Count && i < values.Count; i++)
                {
                    var header = headers[i]
                        .Trim()
                        .ToLowerInvariant();

                    var value = values[i].Trim();

                    switch (header)
                    {
                        case "bin":
                            item.BIN = value;
                            break;

                        case "brand":
                            item.Brand = value;
                            break;

                        case "type":
                            item.Type = value;
                            break;

                        case "category":
                            item.Category = value;
                            break;

                        case "issuer":
                            item.Issuer = value;
                            break;

                        case "country":
                            item.Country = value;
                            break;

                        case "currency":
                            item.Currency = value;
                            break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(item.BIN))
                    result.Add(item);
            }

            return result;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = "";
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (insideQuotes &&
                        i + 1 < line.Length &&
                        line[i + 1] == '"')
                    {
                        current += '"';
                        i++;
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (c == ',' && !insideQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            result.Add(current);

            return result;
        }


    //    [HttpPost]
    //    public async Task<IActionResult> CheckBin(
    //[FromBody] CardBinRequest request,
    //CancellationToken ct)
    //    {
    //        // ============================================
    //        // 1. Validate request
    //        // ============================================

    //        if (request == null || string.IsNullOrWhiteSpace(request.Bin))
    //        {
    //            return BadRequest(new
    //            {
    //                success = false,
    //                message = "BIN is required."
    //            });
    //        }

    //        var bin = request.Bin.Trim();

    //        if (!Regex.IsMatch(bin, @"^\d{6,8}$"))
    //        {
    //            return BadRequest(new
    //            {
    //                success = false,
    //                message = "BIN must be 6 to 8 digits."
    //            });
    //        }

    //        try
    //        {
    //            // ============================================
    //            // 2. FIRST CHECK LOCAL DATABASE
    //            // ============================================

    //            var existingBin = await _context.pMBinCheckers
    //                .AsNoTracking()
    //                .FirstOrDefaultAsync(
    //                    x => x.CardNumber == bin,
    //                    ct);

    //            if (existingBin != null)
    //            {
    //                return Ok(new
    //                {
    //                    success = true,
    //                    source = "DATABASE",
    //                    message = "BIN found in local database.",
    //                    bin = bin,
    //                    bankName = existingBin.BankName,
    //                    isActive = true
    //                });
    //            }


    //            // ============================================
    //            // 3. BIN NOT FOUND IN DATABASE
    //            //    CALL BINLIST API
    //            // ============================================

    //            var client = _httpClientFactory.CreateClient();

    //            client.DefaultRequestHeaders.TryAddWithoutValidation(
    //                "Accept-Version",
    //                "3");

    //            client.Timeout = TimeSpan.FromSeconds(10);

    //            var apiUrl =
    //                $"https://lookup.binlist.net/{bin}";

    //            using var response = await client.GetAsync(
    //                apiUrl,
    //                ct);


    //            // ============================================
    //            // 4. BIN NOT FOUND FROM BINLIST
    //            // ============================================

    //            if (response.StatusCode == HttpStatusCode.NotFound)
    //            {
    //                return NotFound(new
    //                {
    //                    success = false,
    //                    source = "BINLIST",
    //                    bin = bin,
    //                    message = "BIN not found."
    //                });
    //            }


    //            // ============================================
    //            // 5. RATE LIMIT
    //            // ============================================

    //            if (response.StatusCode == HttpStatusCode.TooManyRequests)
    //            {
    //                return StatusCode(429, new
    //                {
    //                    success = false,
    //                    source = "BINLIST",
    //                    bin = bin,
    //                    message = "BIN API rate limit reached."
    //                });
    //            }


    //            // ============================================
    //            // 6. OTHER BINLIST ERROR
    //            // ============================================

    //            if (!response.IsSuccessStatusCode)
    //            {
    //                return StatusCode(502, new
    //                {
    //                    success = false,
    //                    source = "BINLIST",
    //                    bin = bin,
    //                    message = "BIN API failed.",
    //                    statusCode = (int)response.StatusCode
    //                });
    //            }


    //            // ============================================
    //            // 7. READ BINLIST JSON
    //            // ============================================

    //            var json =
    //                await response.Content.ReadAsStringAsync(ct);


    //            // ============================================
    //            // 8. DESERIALIZE
    //            // ============================================

    //            var binData =
    //                JsonSerializer.Deserialize<BinResponse>(
    //                    json,
    //                    new JsonSerializerOptions
    //                    {
    //                        PropertyNameCaseInsensitive = true
    //                    });

    //            if (binData == null)
    //            {
    //                return StatusCode(502, new
    //                {
    //                    success = false,
    //                    source = "BINLIST",
    //                    bin = bin,
    //                    message = "Invalid BIN API response."
    //                });
    //            }


    //            // ============================================
    //            // 9. GET BANK NAME
    //            // ============================================

    //            var bankName =
    //                binData.Bank?.Name?.Trim();
    //            var brand = binData.Brand;


    //            // ============================================
    //            // 10. BANK NAME VALIDATION
    //            // ============================================

    //            if (string.IsNullOrWhiteSpace(bankName))
    //            {
    //                return Ok(new
    //                {
    //                    success = false,
    //                    source = "BINLIST",
    //                    bin = bin,
    //                    message = "BIN found but bank name is not available.",
    //                    bankName = "",
    //                    isActive = false
    //                });
    //            }


    //            // ============================================
    //            // 11. CHECK AGAIN BEFORE INSERT
    //            //    Prevent duplicate records if two requests
    //            //    arrive at the same time.
    //            // ============================================

    //            var alreadyInserted =
    //                await _context.pMBinCheckers
    //                    .FirstOrDefaultAsync(
    //                        x => x.CardNumber == bin,
    //                        ct);

    //            if (alreadyInserted == null)
    //            {
    //                // ========================================
    //                // 12. INSERT BIN INTO DATABASE
    //                // ========================================

    //                var newBin = new PMBinChecker
    //                {
    //                    // DO NOT SET Id
    //                    // SQL Server will generate identity Id

    //                    CardNumber = bin,
    //                    BankName = bankName,
    //                    brand = brand,
    //                    response = json,
    //                    IsActive = true,
    //                    Created = DateTime.Now

    //                };

    //                _context.pMBinCheckers.Add(newBin);

    //                await _context.SaveChangesAsync(ct);
    //            }


    //            // ============================================
    //            // 13. RETURN BINLIST RESULT
    //            // ============================================

    //            return Ok(new
    //            {
    //                success = true,
    //                source = "BINLIST",
    //                message = "BIN found and saved successfully.",
    //                bin = bin,
    //                bankName = bankName,
    //                scheme = binData.Scheme,
    //                type = binData.Type,
    //                brand = binData.Brand,
    //                country = binData.Country?.Name,
    //                isActive = true
    //            });
    //        }
    //        catch (TaskCanceledException)
    //            when (!ct.IsCancellationRequested)
    //        {
    //            return StatusCode(504, new
    //            {
    //                success = false,
    //                bin = bin,
    //                message = "BIN API request timed out."
    //            });
    //        }
    //        catch (OperationCanceledException)
    //            when (ct.IsCancellationRequested)
    //        {
    //            return StatusCode(499, new
    //            {
    //                success = false,
    //                bin = bin,
    //                message = "Request cancelled."
    //            });
    //        }
    //        catch (HttpRequestException ex)
    //        {
    //            return StatusCode(502, new
    //            {
    //                success = false,
    //                bin = bin,
    //                message = "Unable to connect to BIN API.",
    //                error = ex.Message
    //            });
    //        }
    //        catch (Exception ex)
    //        {
    //            return StatusCode(500, new
    //            {
    //                success = false,
    //                bin = bin,
    //                message = "Unexpected error.",
    //                error = ex.Message
    //            });
    //        }
    //    }

        //[HttpPost]
        //public async Task<IActionResult> CheckBin([FromBody] CardBinRequest request)
        //{
        //    // Validate: only 6-8 digits
        //    if (string.IsNullOrWhiteSpace(request.Bin) ||
        //        !System.Text.RegularExpressions.Regex.IsMatch(request.Bin, @"^\d{6,8}$"))
        //    {
        //        return BadRequest(new
        //        {
        //            error = "Invalid BIN"
        //        });
        //    }

        //    try
        //    {
        //        var apiKey = "apv_55779472-18ff-4af7-b7c6-937a02e8df04";

        //        if (string.IsNullOrWhiteSpace(apiKey))
        //        {
        //            return StatusCode(500, new
        //            {
        //                error = "APIVerve API key is not configured"
        //            });
        //        }

        //        var client = _httpClientFactory.CreateClient();

        //        var url =
        //            $"https://api.apiverve.com/v1/binlookup?bin={request.Bin}";

        //        using var requestResult = new HttpRequestMessage(
        //            HttpMethod.Get,
        //            url);

        //        requestResult.Headers.Add("x-api-key", apiKey);

        //        using var response = await client.SendAsync(requestResult);

        //        var responseBody = await response.Content.ReadAsStringAsync();

        //        var result = JsonSerializer.Deserialize<ApiVerveBinResponse>(
        //    responseBody,
        //    new JsonSerializerOptions
        //    {
        //        PropertyNameCaseInsensitive = true
        //    });

        //        return Json(new
        //        {
        //            success = true,
        //            source = "DATABASE",
        //            message = "BIN found in local database.",
        //            bin = result.Data.Bin,
        //            bankName = result.Data.Issuer.Name,
        //            isActive = true
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(502, new
        //        {
        //            error = "Lookup failed",
        //            message = ex.Message
        //        });
        //    }
        //}


        //   [HttpPost]
        //   public async Task<IActionResult> CheckBin(
        //[FromBody] CardBinRequest request)
        //   {
        //       try
        //       {


        //           var apiKey1 = "apv_55779472-18ff-4af7-b7c6-937a02e8df04";

        //           if (string.IsNullOrWhiteSpace(apiKey1))
        //           {
        //               return StatusCode(500, new
        //               {
        //                   error = "APIVerve API key is not configured"
        //               });
        //           }

        //           var clientw = _httpClientFactory.CreateClient();

        //           var url =
        //               $"https://api.apiverve.com/v1/binlookup?bin={request.Bin}";

        //           using var requesta = new HttpRequestMessage(
        //               HttpMethod.Get,
        //               url);

        //           requesta.Headers.Add("x-api-key", apiKey1);

        //           using var response2 = await clientw.SendAsync(requesta);

        //           var responseBodyd = await response2.Content.ReadAsStringAsync();


        //           // ============================================
        //           // 1. Validate request
        //           // ============================================

        //           if (request == null || string.IsNullOrWhiteSpace(request.Bin))
        //           {
        //               return Json(new
        //               {
        //                   success = false,
        //                   message = "BIN is required."
        //               });
        //           }

        //           var bin = request.Bin.Trim();

        //           if (!bin.All(char.IsDigit) ||
        //               (bin.Length != 6 && bin.Length != 8))
        //           {
        //               return Json(new
        //               {
        //                   success = false,
        //                   message = "BIN must contain exactly 6 or 8 digits."
        //               });
        //           }


        //           // ============================================
        //           // 2. Convert to 8 digit BIN
        //           // ============================================
        //           //
        //           // Your table contains CardFirst8.
        //           //
        //           // If request contains 8 digits:
        //           //     use all 8.
        //           //
        //           // If request contains 6 digits:
        //           //     first check whether we have matching
        //           //     8-digit BINs starting with those 6 digits.
        //           //

        //           if (bin.Length == 8)
        //           {
        //               // ========================================
        //               // 3. Check local DB first
        //               // ========================================

        //               var localBin = await _context.pMBinCheckers.FirstOrDefaultAsync(x =>
        //                       x.CardNumber == bin &&
        //                       x.IsActive == true);

        //               if (localBin != null)
        //               {
        //                   return Json(new
        //                   {
        //                       success = true,
        //                       source = "DATABASE",
        //                       message = "BIN found in local database.",
        //                       bin = localBin.CardNumber,
        //                       bankName = localBin.BankName,
        //                       isActive = localBin.IsActive
        //                   });
        //               }
        //           }
        //           else
        //           {
        //               // ========================================
        //               // 3A. For 6 digit BIN:
        //               // Check whether we already have an
        //               // 8-digit BIN under this 6-digit range.
        //               // ========================================

        //               var localBins = await _context.pMBinCheckers
        //                   .AsNoTracking()
        //                   .Where(x =>
        //                       x.IsActive == true &&
        //                       x.CardNumber.StartsWith(bin))
        //                   .Select(x => new
        //                   {
        //                       x.CardNumber,
        //                       x.BankName,
        //                       x.IsActive
        //                   })
        //                   .ToListAsync();

        //               if (localBins.Count == 1)
        //               {
        //                   var localBin = localBins[0];

        //                   return Json(new
        //                   {
        //                       success = true,
        //                       source = "DATABASE",
        //                       message = "BIN found in local database.",
        //                       bin = localBin.CardNumber,
        //                       bankName = localBin.BankName,
        //                       isActive = localBin.IsActive
        //                   });
        //               }

        //               // If multiple 8-digit BINs exist under the
        //               // same 6-digit BIN, we cannot safely choose one.
        //               //
        //               // Therefore continue to external API.
        //           }


        //           // ============================================
        //           // 4. BIN not found locally
        //           //    Call TA BIN API
        //           // ============================================

        //           var apiKey =
        //               _configuration["TAPaymentGateway:ApiKey"];

        //           var apiSecret =
        //               _configuration["TAPaymentGateway:Salt"];

        //           if (string.IsNullOrWhiteSpace(apiKey) ||
        //               string.IsNullOrWhiteSpace(apiSecret))
        //           {
        //               return Json(new
        //               {
        //                   success = false,
        //                   message = "BIN API credentials are missing."
        //               });
        //           }


        //           // ============================================
        //           // 5. Unique reference ID
        //           // ============================================

        //           var clientRefId =
        //               $"PAYMANBIN{DateTime.UtcNow:yyyyMMddHHmmssfff}{Random.Shared.Next(1000, 9999)}";


        //           // ============================================
        //           // 6. Request payload
        //           // ============================================

        //           var payload = new
        //           {
        //               clientRefId = clientRefId,
        //               bin = bin,
        //               latitude = "12.9716",
        //               longitude = "77.5946"
        //           };

        //           var requestJson =
        //               JsonSerializer.Serialize(payload);


        //           // ============================================
        //           // 7. HTTP client
        //           // ============================================

        //           var client =
        //               _httpClientFactory.CreateClient();

        //           client.DefaultRequestHeaders.Remove("X-Api-Key");
        //           client.DefaultRequestHeaders.Remove("X-Api-Secret");

        //           client.DefaultRequestHeaders.Add(
        //               "X-Api-Key",
        //               apiKey);

        //           client.DefaultRequestHeaders.Add(
        //               "X-Api-Secret",
        //               apiSecret);


        //           // ============================================
        //           // 8. API URL
        //           // ============================================

        //           var apiUrl =
        //               _configuration["TAPaymentGateway:ApiUrl"];

        //           if (string.IsNullOrWhiteSpace(apiUrl))
        //           {
        //               return Json(new
        //               {
        //                   success = false,
        //                   message = "BIN API URL is missing."
        //               });
        //           }


        //           // ============================================
        //           // 9. Call external BIN API
        //           // ============================================

        //           using var content =
        //               new StringContent(
        //                   requestJson,
        //                   Encoding.UTF8,
        //                   "application/json");

        //           var response =
        //               await client.PostAsync(
        //                   $"{apiUrl.TrimEnd('/')}/api/v1/verify/bin-check",
        //                   content);

        //           var responseBody =
        //               await response.Content.ReadAsStringAsync();


        //           // ============================================
        //           // 10. Log API response
        //           // ============================================

        //           var responseLog = new ErrorModel
        //           {
        //               payload = "TA Pay BIN Check",
        //               agId = "",
        //               reqTime = requestJson,
        //               respTime = responseBody,
        //               requestId = clientRefId,
        //               uid = "",
        //               statuscode = response.IsSuccessStatusCode,
        //               jsonBody = ""
        //           };

        //           _context.errorModels.Add(responseLog);
        //           await _context.SaveChangesAsync();


        //           // ============================================
        //           // 11. Empty response
        //           // ============================================

        //           if (string.IsNullOrWhiteSpace(responseBody))
        //           {
        //               return Json(new
        //               {
        //                   success = false,
        //                   message = "Empty response from BIN API."
        //               });
        //           }


        //           // ============================================
        //           // 12. Return external API response
        //           // ============================================

        //           return Content(
        //               responseBody,
        //               "application/json");
        //       }
        //       catch (Exception ex)
        //       {
        //           return Json(new
        //           {
        //               success = false,
        //               message = ex.Message
        //           });
        //       }
        //   }
    }

    public class PaymentRequest
    {
        public decimal Amount { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }
        public string? orderId { get; set; }
    }

    public class PaymentStatusResult
    {
        [JsonPropertyName("data")]
        public List<PaymentStatusData>? Data { get; set; }

        [JsonPropertyName("hash")]
        public string? Hash { get; set; }
    }

    public class PaymentStatusData
    {
        [JsonPropertyName("transaction_id")]
        public string? TransactionId { get; set; }

        [JsonPropertyName("bank_code")]
        public string? BankCode { get; set; }

        [JsonPropertyName("payment_mode")]
        public string? PaymentMode { get; set; }

        [JsonPropertyName("payment_channel")]
        public string? PaymentChannel { get; set; }

        [JsonPropertyName("payment_datetime")]
        public string? PaymentDatetime { get; set; }

        [JsonPropertyName("response_code")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("response_message")]
        public string? ResponseMessage { get; set; }

        [JsonPropertyName("authorization_staus")]
        public string? AuthorizationStatus { get; set; }

        [JsonPropertyName("order_id")]
        public string? OrderId { get; set; }

        [JsonPropertyName("amount")]
        public string? Amount { get; set; }

        [JsonPropertyName("amount_orig")]
        public string? AmountOrig { get; set; }

        [JsonPropertyName("tdr_amount")]
        public decimal TdrAmount { get; set; }

        [JsonPropertyName("tax_on_tdr_amount")]
        public decimal TaxOnTdrAmount { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("error_desc")]
        public string? ErrorDesc { get; set; }

        [JsonPropertyName("customer_phone")]
        public string? CustomerPhone { get; set; }

        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("customer_email")]
        public string? CustomerEmail { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("cardmasked")]
        public string? CardMasked { get; set; }

        [JsonPropertyName("udf1")]
        public string? Udf1 { get; set; }

        [JsonPropertyName("udf2")]
        public string? Udf2 { get; set; }

        [JsonPropertyName("udf3")]
        public string? Udf3 { get; set; }

        [JsonPropertyName("udf4")]
        public string? Udf4 { get; set; }

        [JsonPropertyName("udf5")]
        public string? Udf5 { get; set; }

        [JsonPropertyName("bank_ref_id")]
        public string? BankRefId { get; set; }
    }

    public class PaymentStatusError
    {
        public string? Code { get; set; }

        public string? Message { get; set; }
    }

    public class TaPaySessionModel
    {
        public decimal Amount { get; set; }

        public string userPhone { get; set; }

        public string Cemail { get; set; }

        public string CName { get; set; }

        // Mask or last 4 digits only (PCI safe)
        public string CCard { get; set; }

        public string CMobile { get; set; }
        public string divice { get; set; }
        public string gateway { get; set; }
        public string orderId { get; set; }
    }

    public class CardBinRequest
    {
        public string Bin { get; set; } = "";
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
    }

    public class TAPayResponse
    {
        public string? order_id { get; set; }
        public string? amount { get; set; }
        public string? currency { get; set; }
        public string? description { get; set; }
        public string? name { get; set; }
        public string? email { get; set; }
        public string? phone { get; set; }

        public string? address_line_1 { get; set; }
        public string? address_line_2 { get; set; }

        public string? city { get; set; }
        public string? state { get; set; }
        public string? country { get; set; }
        public string? zip_code { get; set; }

        public string? udf1 { get; set; }
        public string? udf2 { get; set; }
        public string? udf3 { get; set; }
        public string? udf4 { get; set; }
        public string? udf5 { get; set; }

        public string? transaction_id { get; set; }
        public string? bank_code { get; set; }
        public string? payment_mode { get; set; }
        public string? payment_channel { get; set; }
        public string? payment_datetime { get; set; }

        public string? response_code { get; set; }
        public string? response_message { get; set; }
        public string? error_desc { get; set; }

        public string? cardmasked { get; set; }

        public string? hash { get; set; }
    }



    public class ApiVerveBinResponse
    {
        public string? Status { get; set; }
        public string? Error { get; set; }
        public ApiVerveBinData? Data { get; set; }
        public ApiVervePremium? Premium { get; set; }
    }

    public class ApiVerveBinData
    {
        public string? Bin { get; set; }
        public string? Brand { get; set; }
        public string? Type { get; set; }
        public string? Category { get; set; }
        public string? Country { get; set; }
        public ApiVerveIssuer? Issuer { get; set; }
        public ApiVerveLocation? Location { get; set; }
    }

    public class ApiVerveIssuer
    {
        public string? Name { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
    }

    public class ApiVerveLocation
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Alpha2 { get; set; }
        public string? Alpha3 { get; set; }
    }

    public class ApiVervePremium
    {
        public string? Message { get; set; }
        public string? Upgrade_Url { get; set; }
        public List<string>? Locked_Fields { get; set; }
    }


    public class BinResponse
    {
        [JsonPropertyName("number")]
        public BinNumber Number { get; set; }

        [JsonPropertyName("scheme")]
        public string Scheme { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("brand")]
        public string Brand { get; set; }

        [JsonPropertyName("country")]
        public BinCountry Country { get; set; }

        [JsonPropertyName("bank")]
        public BinBank Bank { get; set; }
    }
    public class BinData
    {
        public string? BIN { get; set; }
        public string? Brand { get; set; }
        public string? Type { get; set; }
        public string? Category { get; set; }
        public string? Issuer { get; set; }
        public string? Country { get; set; }
        public string? Currency { get; set; }
    }

    public class BinNumber
    {
        // Currently empty in the response: "number": {}
    }

    public class BinCountry
    {
        [JsonPropertyName("numeric")]
        public string Numeric { get; set; }

        [JsonPropertyName("alpha2")]
        public string Alpha2 { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("emoji")]
        public string Emoji { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("latitude")]
        public decimal Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public decimal Longitude { get; set; }
    }

    public class BinBank
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class CardRepeatRequest
    {
        public string Last4 { get; set; }
    }

    public class BinCheckResponse
    {
        [JsonPropertyName("statuscode")]
        public string? StatusCode { get; set; }

        [JsonPropertyName("actcode")]
        public string? ActCode { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("data")]
        public BinCheckData? Data { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("ipay_uuid")]
        public string? IpayUuid { get; set; }

        [JsonPropertyName("orderid")]
        public string? OrderId { get; set; }

        [JsonPropertyName("environment")]
        public string? Environment { get; set; }

        [JsonPropertyName("internalCode")]
        public string? InternalCode { get; set; }
    }

    public class BinCheckData
    {
        [JsonPropertyName("externalRef")]
        public string? ExternalRef { get; set; }

        [JsonPropertyName("poolReferenceId")]
        public string? PoolReferenceId { get; set; }

        [JsonPropertyName("pool")]
        public PoolDetails? Pool { get; set; }

        [JsonPropertyName("binDetails")]
        public BinDetails? BinDetails { get; set; }
    }

    public class PoolDetails
    {
        [JsonPropertyName("account")]
        public string? Account { get; set; }

        [JsonPropertyName("openingBal")]
        public string? OpeningBalance { get; set; }

        [JsonPropertyName("mode")]
        public string? Mode { get; set; }

        [JsonPropertyName("amount")]
        public string? Amount { get; set; }

        [JsonPropertyName("closingBal")]
        public string? ClosingBalance { get; set; }
    }

    public class BinDetails
    {
        [JsonPropertyName("bin")]
        public string? Bin { get; set; }

        [JsonPropertyName("cardNetwork")]
        public string? CardNetwork { get; set; }

        [JsonPropertyName("cardType")]
        public string? CardType { get; set; }

        [JsonPropertyName("cardLevel")]
        public string? CardLevel { get; set; }

        [JsonPropertyName("isoCountryName")]
        public string? IsoCountryName { get; set; }

        [JsonPropertyName("isoCountryA2")]
        public string? IsoCountryA2 { get; set; }

        [JsonPropertyName("issuerBank")]
        public string? IssuerBank { get; set; }

        [JsonPropertyName("issuerWebsite")]
        public string? IssuerWebsite { get; set; }

        [JsonPropertyName("issuerPhone")]
        public string? IssuerPhone { get; set; }

        [JsonPropertyName("cardTransfer")]
        public string? CardTransfer { get; set; }
    }
}
