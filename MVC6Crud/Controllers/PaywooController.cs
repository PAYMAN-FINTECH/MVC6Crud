using Microsoft.AspNetCore.Mvc;

namespace MVC6Crud.Controllers
{
    public class PaywooController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
            {
                return View();
            }
    
            public IActionResult Privacy()
            {
                return View();
        }

        public IActionResult Services()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }
        public IActionResult RefundPolicy()
        {
            return View();
        }

        public IActionResult QuoteModal()
        {
            return PartialView("_QuoteModal");
        }

        [HttpGet]
        public IActionResult GetQuoteForm()
        {
            // Return the partial view with form state
            ViewBag.ShowSuccess = false;
            return PartialView("_QuoteModal");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuote(QuoteRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                // Return form with errors (simplified: just return form again)
                ViewBag.ShowSuccess = false;
                return PartialView("_QuoteModal");
            }

            // Process the quote request (save to DB, send email, etc.)
            // For demo, generate a random booking ID
            var bookingId = "TP" + new Random().Next(1000, 9999).ToString();

            // Set success data
            ViewBag.ShowSuccess = true;
            ViewBag.BookingId = bookingId;
            ViewBag.Name = model.Name;
            ViewBag.FromLocation = model.FromLocation;
            ViewBag.ToLocation = model.ToLocation;

            return PartialView("_QuoteModal");
        }
    }

    public class QuoteRequestModel
    {
        public string Name { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string FromLocation { get; set; }
        public string ToLocation { get; set; }
        public string Message { get; set; }
    }
}
