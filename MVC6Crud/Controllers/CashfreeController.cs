using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MVC6Crud.Data;
using MVC6Crud.Models.CashFree;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace MVC6Crud.Controllers
{
    public class CashfreeController : Controller
    {
        private readonly CashfreeService _paymentService;

        public CashfreeController(CashfreeService paymentService)
        {
            _paymentService = paymentService;
        }

        public IActionResult CreateOrder()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CFOrderRequest orderRequest)
        {
            orderRequest.order_id = "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss");
            if (ModelState.IsValid)
            {
                try
                {
                    // Set the return URL to the PaymentReturn action
                    //orderRequest.OrderMeta.ReturnUrl = Url.Action("PaymentReturn", "Payment", null, Request.Scheme);

                    var paymentSessionId = await _paymentService.CreateOrderAsync(orderRequest);

                    // Redirect to the Cashfree payment page
                    return Ok(new { paymentSessionId = paymentSessionId });
                }
                catch (Exception ex)
                {
                    // Handle error
                    ViewBag.ErrorMessage = ex.Message;
                    return View();
                }
            }
            return View();
        }

        public IActionResult PaymentReturn()
        {
            // Handle payment response
            return View();
        }

        //[HttpPost]
        //public async Task<IActionResult> PaymentNotify([FromBody] PaymentNotification notification)
        //{
        //    if (notification != null)
        //    {
        //        // Validate the notification (e.g., verify signature)
        //        if (IsValidNotification(notification))
        //        {
        //            // Process the payment status
        //            if (notification.PaymentStatus == "SUCCESS")
        //            {
        //                // Handle successful payment
        //                // Update order status, send confirmation emails, etc.
        //            }
        //            else
        //            {
        //                // Handle failed payment
        //                // Update order status, notify customer, etc.
        //            }
        //        }
        //        else
        //        {
        //            // Invalid notification
        //            return BadRequest("Invalid notification");
        //        }
        //    }
        //    return Ok();
        //}

        //private bool IsValidNotification(PaymentNotification notification)
        //{
        //    // Implement signature validation logic here
        //    return true;
        //}

    }
}
