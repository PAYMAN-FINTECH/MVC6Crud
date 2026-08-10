using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace MVC6Crud.Controllers
{
    public class CourseInvoiceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        private List<CoursePurchase1> GetCoursePurchasesIn()
        {
            // Static data (GrossAmount is total paid inclusive of 18% GST)
            var rawData = new[]
          {
                new { MerchantRefNo = "PAYMAN20260604041001912", AmountPaid = 46157.80m, TxnCharges = 10132.20m, GrossAmount = 56290.00m, Service = "ASP.NET Core Advanced Courses", MobileNumber = "9912238196", CustomerEmail = "venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate ="2026-06-04 09:42:01" },
                new { MerchantRefNo = "PAYMAN20260604035811994", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "Azure for .NET Developers", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate ="2026-06-04 09:30:25" },
                new { MerchantRefNo = "PAYMAN20260604034602768", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "Docker Courses", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate = "2026-06-04 09:21:53" },
                new { MerchantRefNo = "PAYMAN20260604034336999", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "Kubernetes Courses", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate = "2026-06-04 09:15:42" },


                new { MerchantRefNo = "PAYMAN20260603022521782", AmountPaid = 54111.80m, TxnCharges = 11878.20m, GrossAmount = 65990.00m, Service = "PHASE 1 — Full Stack Development", MobileNumber = "9912238196", CustomerEmail = "venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate="2026-06-03 08:05:39" },
                new { MerchantRefNo = "PAYMAN20260603022940518", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "PHASE 2 — Python + AI Foundations", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate = "2026-06-03 08:02:15" },
                new { MerchantRefNo = "PAYMAN20260603023353925", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "PHASE 3 — Machine Learning", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate = "2026-06-03 07:59:08" },
                new { MerchantRefNo = "PAYMAN20260603021701483", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "PHASE 4 — Generative AI", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate = "2026-06-03 07:49:04" },

                new { MerchantRefNo = "PAYMAN20260601032812805", AmountPaid = 56580.00m, TxnCharges = 12420.00m, GrossAmount = 69000.00m, Service = "Python for Data Science & Machine Learning Bootcamp", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301",CreatedDate="2026-06-01 08:59:19" },
                new { MerchantRefNo = "PAYMAN20260601032527501", AmountPaid = 57318.00m, TxnCharges = 12582.00m, GrossAmount = 69900.00m, Service = "Machine Learning A-Z", MobileNumber = "9912238196", CustomerEmail = "Venkat@gmail.com", HSN = "999293", Name = "Venkat", Address = "11-7-46, p s nagar, Sirsilla, Sircilla, Karimnagar, Sircilla, Telangana, India, 505301", CreatedDate = "2026-06-01 08:56:27" },


                new { MerchantRefNo = "PAYMAN20260604000212543", AmountPaid = 81180.00m, TxnCharges = 17820.00m, GrossAmount = 99000.00m, Service = "Machine Learning Specialization — Andrew Ng", MobileNumber = "9963811121", CustomerEmail = "praveenkumar.emmadi@gmail.com", HSN = "999293", Name = "Emmadi Praveen Kumar", Address = "1-8, bachannapet, Kesireddypalli, Bachannapet, Warangal, Kesireddipalle, Telangana, India, 506221",CreatedDate="2026-06-04 05:33:41" },
                new { MerchantRefNo = "PAYMAN20260602000328270", AmountPaid = 49200.00m, TxnCharges = 10800.00m, GrossAmount = 60000.00m, Service = "Machine Learning Professional Certificate", MobileNumber = "9963811121", CustomerEmail = "praveenkumar.emmadi@gmail.com", HSN = "999293", Name = "Emmadi Praveen Kumar", Address = "1-8, bachannapet, Kesireddypalli, Bachannapet, Warangal, Kesireddipalle, Telangana, India, 506221", CreatedDate = "2026-06-02 05:34:29" },
                new { MerchantRefNo = "PAYMAN20260601015553526", AmountPaid = 81180.00m, TxnCharges = 17820.00m, GrossAmount = 99000.00m, Service = "Machine Learning Crash Course", MobileNumber = "9963811121", CustomerEmail = "praveenkumar.emmadi@gmail.com", HSN = "999293", Name = "Emmadi Praveen Kumar", Address = "1-8, bachannapet, Kesireddypalli, Bachannapet, Warangal, Kesireddipalle, Telangana, India, 506221", CreatedDate = "2026-06-01 07:27:10" },

                new { MerchantRefNo = "PAYMAN20260603023647609", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "AI Agent Development", MobileNumber = "9032682950", CustomerEmail = "siddharthavovoldas2@gmail.com", HSN = "999293", Name = "vovoladas Siddhartha", Address = "3-1-16/116/3/3/2A/C, S V Nagar, Mallapur, Rangareddi, KAPRA, Andhra Pradesh, India, 500076", CreatedDate = "2026-06-03 08:09:36" },
                new { MerchantRefNo = "PAYMAN20260603002115842", AmountPaid = 57400.00m, TxnCharges = 12600.00m, GrossAmount = 70000.00m, Service = "Cloud Computing", MobileNumber = "9398958120", CustomerEmail = "samatha@gmail.com", HSN = "999293", Name = "Samatha", Address = "3-45/1, Narsapuram, Mandal Siddipet, Mittapally, Siddipet, Medak, Mittapalle, Telangana, India, 502375", CreatedDate = "2026-06-01 07:27:10" },
            };


            var purchases = new List<CoursePurchase1>();
            int id = 1;
            foreach (var item in rawData)
            {
                purchases.Add(new CoursePurchase1
                {
                    Id = id++,
                    MerchantRefNo = item.MerchantRefNo,
                    AmountPaid = item.AmountPaid,
                    TxnCharges = item.TxnCharges,
                    GrossAmount = item.GrossAmount,
                    Service = item.Service,
                    MobileNumber = item.MobileNumber,
                    CustomerEmail = item.CustomerEmail,
                    HSN = item.HSN,
                    Name = item.Name,
                    Address = item.Address,  // hardcoded address for all
                    CreatedDate = ParseCreatedDate(item.CreatedDate)
                });
            }
            return purchases;
        }

        private DateTime ParseCreatedDate(string dateTimePart)
        {
            if (DateTime.TryParseExact(
                    dateTimePart,
                    "yyyy-dd-MM HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dt))
            {
                return dt;
            }

            return DateTime.Now;
        }

        [HttpGet]
        public IActionResult ViewInvoice(string txn, string invoiceNo)
        {
            var purchase = GetCoursePurchasesIn().FirstOrDefault(p => p.MerchantRefNo == txn);
            if (purchase == null)
                return NotFound("Transaction not found");


            var gross = purchase.GrossAmount; // 95000

            var cgst = Math.Round(gross * 0.09m, 2);
            var sgst = Math.Round(gross * 0.09m, 2);

            var totalGst = cgst + sgst; // 17100
            var net = gross - totalGst;

            var invoice = new
            {
                InvoiceNo = $"INV/{purchase.MerchantRefNo}",
                Date = purchase.CreatedDate.ToString("dd-MMM-yyyy"),
                createdDate = purchase.CreatedDate.ToString("dd-MMM-yyyy hh:mm tt"),
                TransactionId = purchase.MerchantRefNo,
                From = new
                {
                    Name = "Payman Fintech Solutions Pvt. Ltd.",
                    Address = "9-12 Hmt Nagar Stnumber 01, Nacharam, Hyderabad, Telangana, 500076",
                    GSTIN = "36AAOCP3061H1Z9"
                },
                To = new
                {
                    Name = purchase.Name,
                    Address = purchase.Address,
                    Email = purchase.CustomerEmail,
                    Mobile = purchase.MobileNumber,
                    GSTIN = ""
                },
                LineItems = new[]
                {
                    new
                    {
                        SNo = 1,
                        Service = purchase.Service,
                        HsnSac = purchase.HSN,
                        Qty = 1,
                        UnitPrice = Math.Round(net, 2),
                        Amount = gross
                    }
                },
                CGST = cgst,
                SGST = sgst,
                Total = gross
            };

            return View(invoice);
        }

    }

    public class CoursePurchase1
    {
        public int Id { get; set; }
        public string MerchantRefNo { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal TxnCharges { get; set; }
        public decimal GrossAmount { get; set; }   // total paid (incl. 18% GST)
        public string Service { get; set; }
        public string MobileNumber { get; set; }
        public string CustomerEmail { get; set; }
        public string HSN { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }         // customer address
        public DateTime CreatedDate { get; set; }
    }

}
