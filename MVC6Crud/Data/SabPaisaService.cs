using Microsoft.Extensions.Options;
using MVC6Crud.Models.SabPaisa;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MVC6Crud.Data
{
    public class SabPaisaService
    {
        private readonly HttpClient _httpClient;
        private readonly SabPaisaOptions _options;
        private readonly ILogger<SabPaisaService> _logger;

        public SabPaisaService(
            HttpClient httpClient,
            IOptions<SabPaisaOptions> options,
            ILogger<SabPaisaService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<SabPaisaPaymentResponse> CreatePaymentAsync(
            string merchantTxnId,
            decimal amountInRupees,
            string customerName,
            string customerEmail,
            string customerPhone,
            string? description = null)
        {
            try
            {
                // ₹100.50 => 10050 paise
                long amountInPaise =
                    Convert.ToInt64(
                        Math.Round(
                            amountInRupees * 100,
                            MidpointRounding.AwayFromZero));

                long timestamp =
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                string currency = "INR";

                string checksum = GenerateChecksum(
                    _options.MerchantId,
                    merchantTxnId,
                    amountInPaise,
                    currency,
                    timestamp);

                var request = new CreateSabPaisaPaymentRequest
                {
                    MerchantId = _options.MerchantId,
                    MerchantTxnId = merchantTxnId,
                    Amount = amountInPaise,
                    Currency = currency,

                    CustomerName = customerName,
                    CustomerEmail = customerEmail,
                    CustomerPhone = customerPhone,

                    ReturnUrl = _options.ReturnUrl,

                    Description = description,

                    Timestamp = timestamp,
                    Checksum = checksum
                };

                string json = JsonSerializer.Serialize(
                    request,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy =
                            JsonNamingPolicy.CamelCase
                    });

                using var content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                var url =
                    $"{_options.BaseUrl.TrimEnd('/')}/api/v2/payments";

                using var httpRequest =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        url);

                httpRequest.Headers.Add(
                    "X-Api-Key",
                    _options.ApiKey);

                httpRequest.Content = content;

                _logger.LogInformation(
                    "SabPaisa Create Payment Request. MerchantTxnId: {MerchantTxnId}",
                    merchantTxnId);

                using var response =
                    await _httpClient.SendAsync(httpRequest);

                string responseBody =
                    await response.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "SabPaisa Response: {Response}",
                    responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "SabPaisa API Error: {StatusCode} {Response}",
                        response.StatusCode,
                        responseBody);

                    return new SabPaisaPaymentResponse
                    {
                        Success = false,
                        Message =
                            $"SabPaisa API Error: {response.StatusCode}"
                    };
                }

                var result =
                    JsonSerializer.Deserialize<SabPaisaPaymentResponse>(
                        responseBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (result == null)
                {
                    return new SabPaisaPaymentResponse
                    {
                        Success = false,
                        Message =
                            "Invalid response received from SabPaisa."
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SabPaisa payment creation failed.");

                return new SabPaisaPaymentResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }


        public string GenerateChecksum(
            string merchantId,
            string merchantTxnId,
            long amountInPaise,
            string currency,
            long timestamp)
        {
            string message =
                $"{merchantId}|{merchantTxnId}|{amountInPaise}|{currency}|{timestamp}";

            using var hmac =
                new HMACSHA256(
                    Encoding.UTF8.GetBytes(
                        _options.SecretKey));

            byte[] hash =
                hmac.ComputeHash(
                    Encoding.UTF8.GetBytes(
                        message));

            return Convert.ToHexString(hash)
                .ToLowerInvariant();
        }


        public bool VerifyReturnSignature(
            SabPaisaReturnRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Signature))
                    return false;

                var parameters =
                    new SortedDictionary<string, string>(
                        StringComparer.Ordinal)
                    {
                        ["amount"] =
                            request.Amount ?? "",

                        ["merchant_txn_id"] =
                            request.MerchantTxnId ?? "",

                        ["paid_amount"] =
                            request.PaidAmount ?? "",

                        ["payment_mode"] =
                            request.PaymentMode ?? "",

                        ["status"] =
                            request.Status ?? "",

                        ["timestamp"] =
                            request.Timestamp ?? "",

                        ["transaction_id"] =
                            request.TransactionId ?? ""
                    };

                string message =
                    string.Join(
                        "|",
                        parameters.Select(
                            x => $"{x.Key}={x.Value}"));

                using var hmac =
                    new HMACSHA256(
                        Encoding.UTF8.GetBytes(
                            _options.SecretKey));

                byte[] hash =
                    hmac.ComputeHash(
                        Encoding.UTF8.GetBytes(
                            message));

                string calculatedSignature =
                    Convert.ToHexString(hash)
                        .ToLowerInvariant();

                return CryptographicOperations
                    .FixedTimeEquals(
                        Encoding.UTF8.GetBytes(
                            calculatedSignature),
                        Encoding.UTF8.GetBytes(
                            request.Signature
                                .ToLowerInvariant()));
            }
            catch
            {
                return false;
            }
        }
    }
}
