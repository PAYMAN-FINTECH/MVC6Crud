using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MVC6Crud.Models.CashFree;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.DotNet.MSIdentity.Shared;

namespace MVC6Crud.Data
{
    public class CashfreeService
    {
        private readonly HttpClient _httpClient;
        private readonly CFConfig _cfConfig;

        public CashfreeService(HttpClient httpClient, IOptions<CFConfig> cfConfig)
        {
            _httpClient = httpClient;
            _cfConfig = cfConfig.Value;
        }

        public async Task<string> CreateOrderAsync(Models.CashFree.CFOrderRequest orderRequest)
        {
            var url = $"{_cfConfig.BaseUrl}/pg/orders";
            var requestContent = new StringContent(
                JsonConvert.SerializeObject(orderRequest),
                Encoding.UTF8,
                "application/json"
            );

            requestContent.Headers.Add("x-client-id", _cfConfig.ClientId);
            requestContent.Headers.Add("x-client-secret", _cfConfig.ClientSecret);
            requestContent.Headers.Add("x-api-version", _cfConfig.ApiVersion);

            var response = await _httpClient.PostAsync(url, requestContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var orderResponse = JsonConvert.DeserializeObject<CFOrderResponse>(responseContent);
                return orderResponse?.payment_session_id;
            }
            else
            {
                // Handle error response
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error creating order: {errorContent}");
            }
        }

        public async Task<string> VerifyPaymentAsync(string orderId)
        {
            var url = $"{_cfConfig.BaseUrl}/pg/orders/{orderId}"; // ✅ Correct endpoint format

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("x-client-id", _cfConfig.ClientId);
                request.Headers.Add("x-client-secret", _cfConfig.ClientSecret);
                request.Headers.Add("x-api-version", _cfConfig.ApiVersion);

                var response = await _httpClient.SendAsync(request);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // You can parse the response JSON if needed
                    return responseContent;
                }
                else
                {
                    throw new Exception($"Cashfree verification failed: {responseContent}");
                }
            }
        }

        //public async Task<CardBinDetailsResponse> GetBinDetailsAsync(string cardBin)
        //{
        //    if (string.IsNullOrWhiteSpace(cardBin))
        //        throw new ArgumentException("cardBin must be provided", nameof(cardBin));

        //    var url = $"{_cfConfig.BaseUrl}/pg/utilities/cardbin"; // per docs: POST to /utilities/cardbin :contentReference[oaicite:2]{index=2}

        //    using var request = new HttpRequestMessage(HttpMethod.Post, url);
        //    request.Headers.Add("x-client-id", _cfConfig.ClientId);
        //    request.Headers.Add("x-client-secret", _cfConfig.ClientSecret);
        //    request.Headers.Add("x-api-version", _cfConfig.ApiVersion);

        //    var payload = new { card_number = cardBin };
        //    var json = JsonConvert.SerializeObject(payload);
        //    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        //    var response = await _httpClient.SendAsync(request);
        //    var responseContent = await response.Content.ReadAsStringAsync();

        //    if (!response.IsSuccessStatusCode)
        //    {
        //        throw new Exception($"Cashfree Get-BIN failed: {responseContent}");
        //    }

        //    var result = JsonConvert.DeserializeObject<CardBinDetailsResponse>(responseContent);
        //    return result;
        //}


        public async Task<List<PaymentCashfree>> PaymentsForOrder(string orderId)
        {
            var requestUrl = $"{_cfConfig.BaseUrl}/pg/orders/{orderId}/payments";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("x-client-id", _cfConfig.ClientId);
            request.Headers.Add("x-client-secret", _cfConfig.ClientSecret);
            request.Headers.Add("x-api-version", "2025-01-01");

            using var response = await _httpClient.SendAsync(request);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var payments = JsonConvert.DeserializeObject<List<PaymentCashfree>>(responseContent);
                    return payments ?? new List<PaymentCashfree>();
                }
                catch (Exception ex)
                {
                    // Log exception if needed
                   // Console.WriteLine($"Deserialization error: {ex.Message}");
                    return new List<PaymentCashfree>();
                }
            }
            else
            {
                // Log or return specific error info
               // Console.WriteLine($"Cashfree API failed: {response.StatusCode} | {responseContent}");
                return new List<PaymentCashfree>();
            }
        }

    }
}
