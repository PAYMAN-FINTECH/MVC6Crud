using Newtonsoft.Json.Linq;

namespace MVC6Crud.Data
{
    public class OperationService
    {
        private readonly IConfiguration _cfg;
        public OperationService(IConfiguration cfg) {
            _cfg = cfg;
        }

        public string BuildSignedHash(string trackId, string hosted, string password, string secret, string amount, string currency)
        {
            //var pipe = $"{trackId}|{hosted}|{password}|{secret}|{amount}|{currency}";
            decimal amt = Convert.ToDecimal(amount);
            return CryptoHelperGateway.GenerateSignature(trackId,hosted,password,secret, amount, currency);
        }

        public JObject GeneratePurchaseOrPreAuth(string sTransId, string token, string cardToken, string country,
            string fName, string lName, string address, string city, string state, string zip, string phone, string email,
            string merchantTranId, string hosted, string password, string secret, string amount, string currency, string actionType,
            string strHash, string merchantIp, string paymentId, string iframe, string metadata)
        {
            var obj = new JObject
            {
                ["terminalId"] = hosted,
                ["password"] = password,
                ["signature"] = strHash,
                ["paymentType"] = actionType,
                ["amount"] = amount,
                ["currency"] = currency,
                ["order"] = new JObject { ["orderId"] = merchantTranId, ["description"] = $"Order {merchantTranId}" },
                ["customer"] = new JObject
                {
                    ["customerEmail"] = email,
                    ["billingAddressStreet"] = address,
                    ["billingAddressCity"] = city,
                    ["billingAddressState"] = state,
                    ["billingAddressPostalCode"] = zip,
                    ["billingAddressCountry"] = country
                },
            };

            if (!string.IsNullOrEmpty(cardToken))
                obj["tokenization"] = new JObject { ["cardToken"] = cardToken };

            if (!string.IsNullOrEmpty(token))
                obj["token"] = token;

            return obj;
        }

        public JObject GenerateVoid(string referenceId, string country, string email, string hosted, string password, string amount, string currency, string merchantTranId, string actionType, string strHash, string merchantIp, string sTransId, string iframe, string metadata)
        {
            var obj = new JObject
            {
                ["referenceId"] = referenceId,
                ["terminalId"] = hosted,
                ["password"] = password,
                ["signature"] = strHash,
                ["paymentType"] = actionType,
                ["amount"] = amount,
                ["currency"] = currency,
                ["order"] = new JObject { ["orderId"] = merchantTranId }
            };
            return obj;
        }

        public JObject GenerateInquiry(string referenceId, string country, string email, string hosted, string password, string amount, string currency, string merchantTranId, string actionType, string strHash, string merchantIp, string sTransId, string iframe, string metadata)
        {
            var obj = new JObject
            {
                ["referenceId"] = referenceId,
                ["terminalId"] = hosted,
                ["password"] = password,
                ["signature"] = strHash,
                ["paymentType"] = actionType,
                ["order"] = new JObject { ["orderId"] = merchantTranId }
            };
            return obj;
        }

        public JObject GenerateTokenization(string merchantTranId, string cardToken, string zip, string address, string city, string state, string email, string mobile, string token, string country, string hosted, string password, string amount, string currency, string transId, string actionType, string strHash, string merchantIp, string sTransId, string iframe, string metadata)
        {
            var obj = new JObject
            {
                ["terminalId"] = hosted,
                ["password"] = password,
                ["signature"] = strHash,
                ["paymentType"] = actionType,
                ["amount"] = amount,
                ["currency"] = currency,
                ["order"] = new JObject { ["orderId"] = merchantTranId },
                ["tokenization"] = new JObject { ["operation"] = "A", ["cardToken"] = cardToken }
            };
            return obj;
        }
    }
}
