
using Microsoft.Extensions.Options;
using MVC6Crud.Controllers;
using MVC6Crud.Models.Airpay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;


namespace MVC6Crud.Data
{
    public class AirpayService
    {
        private readonly AirpaySettings _settings;
        private readonly AirpayCryptoService _crypto;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AirpayService> _logger;

        public AirpayService(
            IOptions<AirpaySettings> settings,
            AirpayCryptoService crypto,
            IHttpClientFactory httpClientFactory,
            ILogger<AirpayService> logger)
        {
            _settings = settings.Value;
            _crypto = crypto;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }


        public async Task<AirpayPaymentResult>
            CreatePaymentAsync(
                AirpayPaymentRequest request)
        {
            /*
             * ------------------------------------------------
             * STEP 1
             * Generate Airpay AES secret key
             * ------------------------------------------------
             */

            string secretKey =
                _crypto.GenerateSecretKey(
                    _settings.Username,
                    _settings.Password);


            /*
             * ------------------------------------------------
             * STEP 2
             * Airpay OAuth request
             * ------------------------------------------------
             */

            var oauthData =
                new Dictionary<string, string>
                {
                    ["client_id"] =
                        _settings.ClientId,

                    ["client_secret"] =
                        _settings.ClientSecret,

                    ["grant_type"] =
                        "client_credentials",

                    ["merchant_id"] =
                        _settings.MerchantId
                };


            /*
             * ------------------------------------------------
             * STEP 3
             * Convert OAuth data to JSON
             * ------------------------------------------------
             */

            string oauthJson =
                JsonConvert.SerializeObject(
                    oauthData);


            /*
             * ------------------------------------------------
             * STEP 4
             * Encrypt OAuth data
             * ------------------------------------------------
             */

            string encryptedOauth =
                _crypto.Encrypt(
                    oauthJson,
                    secretKey);


            /*
             * ------------------------------------------------
             * STEP 5
             * OAuth checksum
             * ------------------------------------------------
             */

            string oauthChecksum =
                _crypto.GenerateChecksum(
                    oauthData);


            /*
             * ------------------------------------------------
             * STEP 6
             * Send OAuth request
             * ------------------------------------------------
             */

            var oauthPost =
                new Dictionary<string, string>
                {
                    ["merchant_id"] =
                        _settings.MerchantId,

                    ["encdata"] =
                        encryptedOauth,

                    ["checksum"] =
                        oauthChecksum
                };


            string oauthResponse =
                await PostFormAsync(
                    _settings.TokenUrl,
                    oauthPost);


            _logger.LogInformation(
                "Airpay OAuth response received.");


            /*
             * ------------------------------------------------
             * STEP 7
             * Parse OAuth response
             * ------------------------------------------------
             */

            JObject oauthJsonResponse;

            try
            {
                oauthJsonResponse =
                    JObject.Parse(
                        oauthResponse);
            }
            catch
            {
                throw new Exception(
                    "Invalid response received from Airpay OAuth.");
            }


            string encryptedResponse =
                oauthJsonResponse[
                    "response"]?
                    .ToString() ?? "";


            if (string.IsNullOrWhiteSpace(
                encryptedResponse))
            {
                throw new Exception(
                    "Airpay OAuth response does not contain response.");
            }


            /*
             * ------------------------------------------------
             * STEP 8
             * Decrypt OAuth response
             * ------------------------------------------------
             */

            string decryptedOauth =
                _crypto.Decrypt(
                    encryptedResponse,
                    secretKey);


            JObject tokenObject;

            try
            {
                tokenObject =
                    JObject.Parse(
                        decryptedOauth);
            }
            catch
            {
                throw new Exception(
                    "Unable to decrypt Airpay OAuth response.");
            }


            string accessToken =
                tokenObject["data"]?[
                    "access_token"]?
                    .ToString() ?? "";


            if (string.IsNullOrWhiteSpace(
                accessToken))
            {
                throw new Exception(
                    "Airpay access token was not received.");
            }


            /*
             * ------------------------------------------------
             * STEP 9
             * Create payment data
             * ------------------------------------------------
             */

            var paymentData =
                new Dictionary<string, string>
                {
                    ["buyer_email"] =
                        request.BuyerEmail,

                    ["buyer_phone"] =
                        request.BuyerPhone,

                    ["buyer_firstname"] =
                        request.BuyerFirstName,

                    ["buyer_lastname"] =
                        request.BuyerLastName,

                    ["buyer_address"] =
                        request.BuyerAddress,

                    ["buyer_city"] =
                        request.BuyerCity,

                    ["buyer_state"] =
                        request.BuyerState,

                    ["buyer_country"] =
                        request.BuyerCountry,

                    ["buyer_pincode"] =
                        request.BuyerPinCode,

                    ["orderid"] =
                        request.OrderId,

                    ["amount"] =
                        request.Amount.ToString(
                            "0.00",
                            System.Globalization.CultureInfo.InvariantCulture),

                    ["currency_code"] =
                        request.CurrencyCode,

                    ["iso_currency"] =
                        request.IsoCurrency,

                    ["customvar"] =
                        request.CustomVar,

                    ["txnsubtype"] =
                        request.TxnSubType,

                    ["chmod"] =
                        request.Chmod
                };


            /*
             * Remove empty optional values.
             *
             * Required values remain.
             */

            paymentData =
                paymentData
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.Value))
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value);


            /*
             * ------------------------------------------------
             * STEP 10
             * Encrypt payment data
             * ------------------------------------------------
             */

            string paymentJson =
                JsonConvert.SerializeObject(
                    paymentData);


            string encryptedPayment =
                _crypto.Encrypt(
                    paymentJson,
                    secretKey);


            /*
             * ------------------------------------------------
             * STEP 11
             * Payment checksum
             * ------------------------------------------------
             */

            string paymentChecksum =
                _crypto.GenerateChecksum(
                    paymentData);


            /*
             * ------------------------------------------------
             * STEP 12
             * Generate private key
             * ------------------------------------------------
             */

            string privateKey =
                _crypto.GeneratePrivateKey(
                    _settings.Username,
                    _settings.Password,
                    _settings.Secret);


            /*
             * ------------------------------------------------
             * STEP 13
             * Build Airpay payment URL
             * ------------------------------------------------
             */

            string paymentUrl =
                _settings.PaymentUrl.TrimEnd('/') +
                "/?token=" +
                Uri.EscapeDataString(
                    accessToken);


            /*
             * ------------------------------------------------
             * Return everything needed by AJAX
             * ------------------------------------------------
             */

            return new AirpayPaymentResult
            {
                PaymentUrl =
                    paymentUrl,

                MerchantId =
                    _settings.MerchantId,

                PrivateKey =
                    privateKey,

                EncData =
                    encryptedPayment,

                Checksum =
                    paymentChecksum,

                OrderId =
                    request.OrderId
            };
        }


        private async Task<string>
            PostFormAsync(
                string url,
                Dictionary<string, string> data)
        {
            var client =
                _httpClientFactory
                    .CreateClient("Airpay");

            using var content =
                new FormUrlEncodedContent(
                    data);

            using HttpResponseMessage response =
                await client.PostAsync(
                    url,
                    content);

            string body =
                await response.Content
                    .ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Airpay HTTP {(int)response.StatusCode}: {body}");
            }

            return body;
        }


        /*
         * Decrypt callback response.
         */
        public string DecryptResponse(
            string response)
        {
            string secretKey =
                _crypto.GenerateSecretKey(
                    _settings.Username,
                    _settings.Password);

            return _crypto.Decrypt(
                response,
                secretKey);
        }


        /*
         * Validate Airpay private response hash.
         */
        public bool ValidateResponseHash(
            string transactionId,
            string airpayTransactionId,
            string amount,
            string transactionStatus,
            string message,
            string secureHash,
            string customerVpa = "")
        {
            string crcData =
                transactionId +
                ":" +
                airpayTransactionId +
                ":" +
                amount +
                ":" +
                transactionStatus +
                ":" +
                message +
                ":" +
                _settings.MerchantId +
                ":" +
                _settings.Username;

            if (!string.IsNullOrWhiteSpace(
                customerVpa))
            {
                crcData +=
                    ":" +
                    customerVpa;
            }

            var crc =
                new AirpayCrc32();

            return crc.Validate(
                crcData,
                secureHash);
        }
    }

}
