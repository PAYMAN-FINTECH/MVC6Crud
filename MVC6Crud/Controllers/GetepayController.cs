using Microsoft.AspNetCore.Mvc;
using MVC6Crud.Models.PaymanApp;
using Newtonsoft.Json;
using System.Text;

namespace MVC6Crud.Controllers
{
    public class GetepayController : Controller
    {
        // GET: Payment Page
        private GetepayConfig GetConfig()
        {
            return new GetepayConfig
            {
                mid = "108",
                terminalId = "Getepay.merchant61062@icici",
                key = "JoYPd+qso9s7T+Ebj8pi4Wl8i+AHLv+5UNJxA3JkDgY=",
                iv = "hlnuyA9b4YxDq6oJSZFl8g==",
                url = "https://pay1.getepay.in:8443/getepayPortal/pg/generateInvoice"
            };
        }

        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult GenerateInvoice([FromBody] GatePayPaymentRequestModel model)
        {
            var config = GetConfig();
            var request = new GetepayRequest
            {
                terminalId = config.terminalId,
                udf2 = model.Email,
                amount = model.Amount,
                mid = config.mid,
                merchantTransactionId = "Sample" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                transactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ru = "https://localhost:44384/Getepay/PaymentResponse",
                currency = "INR",
                paymentMode = "ALL",
                txnType = "single",
                productType = "IPG",
                txnNote = "Test Txn",
                vpa = config.terminalId
            };

            var orderResponse = Getepay.generateRequest(config, request);

            if (orderResponse != null && !string.IsNullOrEmpty(orderResponse.paymentUrl))
                return Json(new { paymentUrl = orderResponse.paymentUrl });

            return BadRequest("Unable to generate payment URL.");
        }

        [HttpPost]
        public IActionResult PaymentResponse()
        {
            string responseString = Request.Form["response"];
            if (!string.IsNullOrEmpty(responseString))
            {
                var paymentResponse = Getepay.getepayResponse(GetConfig(), responseString);

                // Automatically verify transaction status using Requery
                var requeryRequest = new GetepayRequery
                {
                    paymentId = paymentResponse.getepayTxnId,
                    mid = GetConfig().mid,
                    terminalId = GetConfig().terminalId
                };

                var requeryResponse = Getepay.requeryRequest(GetConfig(), requeryRequest);

                return Content("Payment Status: " + requeryResponse.txnStatus +
                               "\nFull Response: " + Newtonsoft.Json.JsonConvert.SerializeObject(requeryResponse));
            }

            return Content("No response received.");
        }

        // Optional: Manual Requery API for testing
        [HttpGet]
        public IActionResult Requery(string paymentId)
        {
            if (string.IsNullOrEmpty(paymentId))
                return BadRequest("PaymentId is required");

            var config = GetConfig();

            var requeryRequest = new GetepayRequery
            {
                paymentId = paymentId,
                mid = config.mid,
                terminalId = config.terminalId
            };

            var requeryResponse = Getepay.requeryRequest(config, requeryRequest);

            return Json(requeryResponse);
        }
    }
}
