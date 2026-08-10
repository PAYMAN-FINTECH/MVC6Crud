using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Text;

namespace MVC6Crud.Data
{
    public class PaymentService
    {
        private readonly IConfiguration _cfg;
        private readonly IHttpClientFactory _http;
        public PaymentService(IConfiguration cfg, IHttpClientFactory http)
        {
            _cfg = cfg;
            _http = http;
        }

        public async Task<string> GetTargetUrlAsync(string jsonPayload)
        {
            var apiUrl = _cfg["Vegaah:ApiUrl"];
            var client = _http.CreateClient();
           // client.Timeout = TimeSpan.FromSeconds(int.Parse(_cfg["Vegaah:TimeoutSeconds"] ?? "60"));
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var resp = await client.PostAsync(apiUrl, content);
            var txt = await resp.Content.ReadAsStringAsync();
            //Logger.Info("Gateway Response: " + txt);

            try
            {
                var jobj = JObject.Parse(txt);
                var link = jobj.SelectToken("paymentLink.linkUrl")?.Value<string>()
                           ?? jobj.SelectToken("paymentLink")?.Value<string>()
                           ?? jobj.SelectToken("linkUrl")?.Value<string>();

                if (!string.IsNullOrEmpty(link))
                {
                    var paymentId = jobj.SelectToken("transactionId")?.Value<string>();
                    if (!string.IsNullOrEmpty(paymentId) && !link.Contains(paymentId))
                       // return link.TrimEnd('/') + (link.Contains("?") ? "&" : "?") + "paymentId=" + paymentId;
                    return link + paymentId;
                    return link;
                }
                return txt;
            }
            catch (Exception ex)
            {
               // Logger.Error("GetTargetUrlAsync parse error: " + ex.Message);
                return txt;
            }
        }
    }
}
