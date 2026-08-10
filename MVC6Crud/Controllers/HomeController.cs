using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PaymanWeb;
using Newtonsoft.Json;
using Razorpay.Api;
using SixLabors.Fonts;
using System.Data;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Xml.Serialization;
using XAct;
using XAct.Users;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static WhatsAppApi.Parser.FMessage;

namespace MVC6Crud.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IConfiguration configuration, DataUtils dataUtils)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
            _dataUtils = dataUtils;
        }

        public IActionResult Index()
        {
        MultiLayOut multiLayOut = new MultiLayOut();
            var request = HttpContext.Request;

            if (request.Host.Host.Contains("insurance", StringComparison.OrdinalIgnoreCase))
            {
                multiLayOut.IsfastTag = true;
                return RedirectToAction("Index", "Insurance");
            }

            if (request.Host.Host.Contains("fastag", StringComparison.OrdinalIgnoreCase))
            {
                multiLayOut.IsfastTag = true;
                return RedirectToAction("Index", "FasTag");
            }

            if (request.Host.Host.Contains("edu", StringComparison.OrdinalIgnoreCase))
            {
                multiLayOut.Isedu = true;
                return View("~/Views/Edu/Index.cshtml");
            }
            if (request.Host.Host.Contains("paymove", StringComparison.OrdinalIgnoreCase))
            {
                multiLayOut.Isedu = true;
                return View("~/Views/Paywoo/Index.cshtml");
            }
            else
            {
                return View(multiLayOut);
            }
        }


        public IActionResult ContactUs()
        {
            return View();
        }
        public IActionResult Services()
        {
            return View();
        }

        public IActionResult GasBill()
        {
            return View();
        }
        public IActionResult PostpaidBill()
        {
            return View();
        }
        public IActionResult Electricity()
        {
            return View();

        }
        public IActionResult Error()
        {
            return View();
        }
        public IActionResult Businesspayments()
        {
            return View();

        }
        public IActionResult FirAeps()
        {
            return View();

        }
        public IActionResult Creditcardbills()
        {
            return View();

        }
        public IActionResult Waterbills()
        {
            return View();
        }
        [HttpGet]
        public IActionResult signup()
        {
            return View();
        }
        [HttpPost]
        public IActionResult signup(Registartion registartion)
        {
            TempData["singup"] = "You have registered successfully. Our team will contact you shortly";
            return RedirectToAction("Index", "Home");
        }





        public IActionResult RefoundPolicy()
        {
            return View();
        }

        public IActionResult FastTag()
        {
            return View();
        }

        public IActionResult FasTag(bool isfastag= false)
        {
            MultiLayOut multiLayOut = new MultiLayOut();
            multiLayOut.IsfastTag = isfastag;
            return View(multiLayOut);
        }


        public IActionResult Insurance()
        {
            return View();
        }
        public IActionResult PrivacyPolicy()
        {
            return View();
        }
        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult TermsandCanditions()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}



        public async Task<IActionResult> Dashboard()
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");
            // Get user from session

            if (string.IsNullOrEmpty(appPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }
            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == appPhone);

            if (user.IsAgreement == false)
            {
                return RedirectToAction("Index", "DigiLocker", new { userPhone = appPhone });

            }
            var gateWayDetails = _context.PayManGateways.FirstOrDefault();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            if (gateWayDetails.Edu == true)
            {
                var payIns = _context.payManPayIns
                    .Where(t => t.Created >= today
                             && t.Created < tomorrow
                             && t.Gateway == "Vegaah"
                             && t.Status == true)
                    .Sum(rr => (decimal?)rr.Amount) ?? 0;

                if (payIns > gateWayDetails.EduEnableAmount)
                {
                    gateWayDetails.Edu = false;
                    _context.PayManGateways.Update(gateWayDetails);
                    _context.SaveChanges();
                }
            }

            if (gateWayDetails.PayOut == true)
            {
                var payouts = _context.payManPayOuts
                    .Where(t => t.DateTime >= today
                             && t.DateTime < tomorrow
                             && t.PayOutType == "Pine Labs"
                             && t.Status == true)
                    .Count();

                if (payouts > gateWayDetails.PayoutCount)
                {
                    gateWayDetails.PayOut = false;
                    _context.PayManGateways.Update(gateWayDetails);
                    _context.SaveChanges();
                }
            }



          
            if (user == null)
            {
                return RedirectToAction("WebLogout", "Login");
            }

            string pineLabsAmountStr ="";
            string instantPayAmountStr ="";
            string userWalletAmountPayAmountStr = "";

            decimal pineLabsAmount;
            decimal instantPayAmount;
            decimal userWalletAmount;

            int waitedMs = 0;
            int maxWaitMs = 3000;   // 3 seconds max wait --3000
            int intervalMs = 500;   // check every 0.5 second

            while (waitedMs < maxWaitMs)
            {
                pineLabsAmountStr = await _dataUtils.GetPinelabsAmount(appPhone);
                instantPayAmountStr = await GetInstantPayAmount();
                userWalletAmountPayAmountStr = await GetUserWalletAmount(appPhone);

                decimal.TryParse(pineLabsAmountStr, out pineLabsAmount);
                decimal.TryParse(instantPayAmountStr, out instantPayAmount);

                if (pineLabsAmount > 0) // adjust condition as needed
                {
                    break; // Data is ready
                }

                await Task.Delay(intervalMs);
                waitedMs += intervalMs;
            }


            // var instantPayAmount = await GetInstantPayAmount();
            //var pineLabsAmount = await GetPinelabsAmount();
            //var userWalletAmount = await GetUserWalletAmount(appPhone);

            var balanceCards = new List<BalanceCard>();
            if (user.CustomerType != "new")
            {
                balanceCards.Add(new BalanceCard
                {
                    Label = "Available Balance",
                    Amount = "₹" + userWalletAmountPayAmountStr,
                    IsHighlighted = false,
                    IconSvg = "" // optional
                });
            }

            if (user.IsAdmin == true || user.Phone == "9700008363")
            {
                if(user.IsAdmin == true)
                {
                    balanceCards.Add(new BalanceCard
                    {
                        Label = "PayOut Balance",
                        Amount = "₹" + pineLabsAmountStr,
                        IsHighlighted = false,
                        IconSvg = "" // optional
                    });
                }
                
                if(gateWayDetails.BillAvenue == true)
                {
                    balanceCards.Add(new BalanceCard
                    {
                        Label = "BillAvenue Balance",
                        Amount = "₹" + instantPayAmountStr,
                        IsHighlighted = true,
                        IconSvg = "" // optional
                    });
                }
                else
                {
                    balanceCards.Add(new BalanceCard
                    {
                        Label = "InstantPay Balance",
                        Amount = "₹" + instantPayAmountStr,
                        IsHighlighted = true,
                        IconSvg = "" // optional
                    });
                }
               
            }

            var model = new DashboardViewModelWeb
            {
                UserName = user.FirstName,
                UserPhone = user.Phone,
                Email = user.Email,
                IsAdmin = (bool)user.IsAdmin,
                ProfileImageUrl = "https://cdn.pixabay.com/photo/2017/07/18/23/23/user-2517433_1280.png",
                BottomBannerImageUrl = "https://paymanfintech.in/images/2decfe19-3f9f-4473-80d7-32b8c76ea11a0000.jpeg",
                PayOutEnable = gateWayDetails.PayOut,
                CCBill = gateWayDetails.CcBill,
                PayIn = gateWayDetails.PayIn,
                CustomerType = user.CustomerType,
                OpenMoney = (bool)user.OpenMoney,
                EduCashfree = (bool)user.EduCashfree,
                UserEasebuzz = (bool)user.Easebuzz,
                EduGateway = gateWayDetails.Edu,
                Easebuzz1= gateWayDetails.Easebuzz1,
                Easebuzz2 = gateWayDetails.Easebuzz2,
                RozorpayEdu = gateWayDetails.RazorPay,
                CarouselData = new List<CarouselItem>
                {
                    new CarouselItem {
                        Image = "https://media.istockphoto.com/id/613241502/photo/young-woman-shopping-on-line.jpg",
                        Title = "Seamless Payments",
                        Description = "Experience the ease of online transactions with top-notch security."
                    },
                    new CarouselItem {
                        Image = "https://img.freepik.com/free-photo/expressive-pretty-woman-posing.jpg",
                        Title = "Pay Bills Anytime",
                        Description = "Your bills, your time. Pay 24/7 with complete control."
                    },
                    new CarouselItem {
                        Image = "https://img.freepik.com/free-photo/standard-quality-control.jpg",
                        Title = "Smart & Secure",
                        Description = "Enjoy intelligent payment tracking and bank-grade encryption."
                    }
                },

                PaymentToggles = new List<PaymentToggle>
                {
                    new PaymentToggle { Label = "OTP LogIn", IsChecked = (bool)user.OtpLoginEnabled }
                },

                BalanceCards = balanceCards
            };

            return View(model);
        }


        public async Task<ActionResult> BeneficiaryList()
        {
            BeneficiaryListModel beneficiaryListModel = new BeneficiaryListModel();
            var appPhone = HttpContext.Session.GetString("AppPhone");
            // Get user from session

            if (string.IsNullOrEmpty(appPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }
            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == appPhone);
            var gateWayDetails = _context.PayManGateways.FirstOrDefault();
            if (user == null)
            {
                return RedirectToAction("WebLogout", "Login");
            }

            string pineLabsAmountStr = "";
            string userWalletAmountPayAmountStr = "";

            decimal pineLabsAmount;
            decimal userWalletAmount;

            int waitedMs = 0;
            int maxWaitMs = 3000;   // 3 seconds max wait
            int intervalMs = 500;   // check every 0.5 second

            while (waitedMs < maxWaitMs)
            {
                pineLabsAmountStr = await _dataUtils.GetPinelabsAmount(appPhone);
                userWalletAmountPayAmountStr = await GetUserWalletAmount(appPhone);

                decimal.TryParse(pineLabsAmountStr, out pineLabsAmount);

                if (pineLabsAmount > 0) // adjust condition as needed
                {
                    break; // Data is ready
                }

                await Task.Delay(intervalMs);
                waitedMs += intervalMs;
            }

            // var pineLabsAmount = await GetPinelabsAmount();
            //var userWalletAmount = await GetUserWalletAmount(appPhone);

            beneficiaryListModel.Pinlabamount = pineLabsAmountStr;
            beneficiaryListModel.UserAvlAmount = userWalletAmountPayAmountStr;
            beneficiaryListModel.Phone = appPhone;
            beneficiaryListModel.PayOutMinAmount = gateWayDetails.PayOutMinAmount;
            beneficiaryListModel.PayOutMaxAmount = gateWayDetails.PayOutMaxAmount;
            beneficiaryListModel.MinBalanceAvl = gateWayDetails.MinBalanceAvl;
            beneficiaryListModel.isCsbBankPayout = gateWayDetails.CsbbankPayout;


            return View(beneficiaryListModel);
        }

        public async Task<IActionResult> ElectricityPayBill(string billerName)
        {
            BeneficiaryListModel beneficiaryListModel = new BeneficiaryListModel();
            var appPhone = HttpContext.Session.GetString("AppPhone");
            // Get user from session

            if (string.IsNullOrEmpty(appPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }
            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == appPhone);
            var gateWayDetails = _context.PayManGateways.FirstOrDefault();
            if (user == null)
            {
                return RedirectToAction("WebLogout", "Login");
            }

            var instantPayAmount = await GetInstantPayAmount();
            var userWalletAmount = await GetUserWalletAmount(appPhone);

            beneficiaryListModel.InstatntPay = Convert.ToInt32(Convert.ToDecimal(instantPayAmount));
            beneficiaryListModel.UserAvlAmount = userWalletAmount;
            beneficiaryListModel.Phone = appPhone;
            beneficiaryListModel.PayOutMinAmount = gateWayDetails.PayOutMinAmount;
            beneficiaryListModel.PayOutMaxAmount = gateWayDetails.PayOutMaxAmount;
            beneficiaryListModel.MinBalanceAvl = gateWayDetails.MinBalanceAvl;
            beneficiaryListModel.CustomerType = user.CustomerType;
            beneficiaryListModel.billerName = billerName;

            return View(beneficiaryListModel);
        }

        public async Task<IActionResult> FasTagPayBill(string billerName)
        {
            BeneficiaryListModel beneficiaryListModel = new BeneficiaryListModel();
            var appPhone = HttpContext.Session.GetString("AppPhone");
            // Get user from session

            if (string.IsNullOrEmpty(appPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }
            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == appPhone);
            var gateWayDetails = _context.PayManGateways.FirstOrDefault();
            if (user == null)
            {
                return RedirectToAction("WebLogout", "Login");
            }

            var instantPayAmount = await GetInstantPayAmount();
            var userWalletAmount = await GetUserWalletAmount(appPhone);

            beneficiaryListModel.InstatntPay = Convert.ToInt32(Convert.ToDecimal(instantPayAmount));
            beneficiaryListModel.UserAvlAmount = userWalletAmount;
            beneficiaryListModel.Phone = appPhone;
            beneficiaryListModel.PayOutMinAmount = gateWayDetails.PayOutMinAmount;
            beneficiaryListModel.PayOutMaxAmount = gateWayDetails.PayOutMaxAmount;
            beneficiaryListModel.MinBalanceAvl = gateWayDetails.MinBalanceAvl;
            beneficiaryListModel.CustomerType = user.CustomerType;
            beneficiaryListModel.billerName = billerName;

            return View(beneficiaryListModel);
        }


        public async Task<ActionResult> CreditCardBillPay()
        {
            BeneficiaryListModel beneficiaryListModel = new BeneficiaryListModel();
            var appPhone = HttpContext.Session.GetString("AppPhone");
            // Get user from session

            if (string.IsNullOrEmpty(appPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }
            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == appPhone);
            var gateWayDetails = _context.PayManGateways.FirstOrDefault();
            if (user == null)
            {
                return RedirectToAction("WebLogout", "Login");
            }

            var instantPayAmount = await GetInstantPayAmount();
            var userWalletAmount = await GetUserWalletAmount(appPhone);

            beneficiaryListModel.InstatntPay = Convert.ToInt32(Convert.ToDecimal(instantPayAmount));
            beneficiaryListModel.UserAvlAmount = userWalletAmount;
            beneficiaryListModel.Phone = appPhone;
            beneficiaryListModel.PayOutMinAmount = gateWayDetails.PayOutMinAmount;
            beneficiaryListModel.PayOutMaxAmount = gateWayDetails.PayOutMaxAmount;
            beneficiaryListModel.MinBalanceAvl = gateWayDetails.MinBalanceAvl;
            beneficiaryListModel.CustomerType = user.CustomerType;

            return View(beneficiaryListModel);
        }

        public async Task<ActionResult> PayInHistory()
        {
            BeneficiaryListModel beneficiaryListModel = new BeneficiaryListModel();
            var appPhone = HttpContext.Session.GetString("AppPhone");
            // Get user from session

            if (string.IsNullOrEmpty(appPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }
            beneficiaryListModel.Phone = appPhone;

            return View(beneficiaryListModel);
        }

        public async Task<ActionResult> PayOutHistory()
        {
            BeneficiaryListModel beneficiaryListModel = new BeneficiaryListModel();
            var appPhone = HttpContext.Session.GetString("AppPhone");
            // Get user from session

            if (string.IsNullOrEmpty(appPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }
            beneficiaryListModel.Phone = appPhone;

            return View(beneficiaryListModel);
        }

        public async Task<ActionResult> Passbook()
        {
            BeneficiaryListModel beneficiaryListModel = new BeneficiaryListModel();
            var appPhone = HttpContext.Session.GetString("AppPhone");
            // Get user from session

            if (string.IsNullOrEmpty(appPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }
            beneficiaryListModel.Phone = appPhone;

            return View(beneficiaryListModel);
        }

        public async Task<ActionResult> AdminDashboard()
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");
            // Get user from session

            if (string.IsNullOrEmpty(appPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }

            return View();
        }


        public async Task<IActionResult> GetDashboardData()
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");
            // Get user from session

            if (string.IsNullOrEmpty(appPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }

            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == appPhone);
            if (user == null)
            {
                return RedirectToAction("WebLogout", "Login");
            }

            var instantPayAmount = await GetInstantPayAmount();
            var pineLabsAmount = await _dataUtils.GetPinelabsAmount(appPhone);
            var userWalletAmount = await GetUserWalletAmount(appPhone);

            var data = new
            {
                UserName = user.FirstName,
                UserPhone = user.Phone,
                Email = user.Email,
                IsAdmin = user.IsAdmin,
                ProfileImageUrl = "https://cdn.pixabay.com/photo/2017/07/18/23/23/user-2517433_1280.png",
                CarouselData = new[]
                {
                    new { Image = "https://media.istockphoto.com/id/613241502/photo/young-woman-shopping-on-line.jpg", Title = "Seamless Payments", Description = "Experience secure online transactions." },
                    new { Image = "https://img.freepik.com/free-photo/expressive-pretty-woman-posing.jpg", Title = "Pay Bills Anytime", Description = "Your bills, your time. Pay 24/7." },
                    new { Image = "https://img.freepik.com/free-photo/standard-quality-control.jpg", Title = "Smart & Secure", Description = "Bank-grade encryption & tracking." }
                },
                PaymentToggles = new[] {
                    new { Label = "PayIn", IsChecked = true },
                    new { Label = "PayOut", IsChecked = true },
                    new { Label = "CCBill", IsChecked = true }
                },
                BalanceCards = new[] {
                    new { Label = "Available Balance", Amount = "₹"+userWalletAmount, IsHighlighted = false },
                    new { Label = "PayOut Balance", Amount = "₹"+pineLabsAmount, IsHighlighted = false },
                    new { Label = "InstantPay Balance", Amount = "₹"+ instantPayAmount, IsHighlighted = true }
                },
                Services = new[] {
                    new { Title = "Balance TopUp", Description = "Recharge balance instantly", IconClass = "fas fa-money-check-alt" },
                    new { Title = "My PayIns", Description = "Track incoming transactions", IconClass = "fas fa-arrow-down" },
                    new { Title = "My Payouts", Description = "Monitor outgoing payments", IconClass = "fas fa-arrow-up" },
                    new { Title = "Credit Card Bill Pay", Description = "Pay CC bills on time", IconClass = "fas fa-credit-card" },
                    new { Title = "Expense Cards", Description = "Manage team spending", IconClass = "fas fa-wallet" },
                    new { Title = "Gift Cards", Description = "Send digital gift cards", IconClass = "fas fa-gift" },
                    new { Title = "Bill Payments", Description = "Pay utilities online", IconClass = "fas fa-file-invoice" }
                },
                BottomBannerImageUrl = "https://paymanfintech.in/images/2decfe19-3f9f-4473-80d7-32b8c76ea11a0000.jpeg"
            };

            return Ok(data);
        }


        [HttpPost]
        public IActionResult GetDashboardStats([FromBody] DateRangeFilter1 filter)
        {
            DateTime start = filter.StartDate ?? DateTime.Today;
            DateTime end = filter.EndDate ?? DateTime.Today;

            var result = new DashboardStats();

            using (var conn = new SqlConnection(_configuration.GetConnectionString("MVC6CrudConnetionString")))
            using (var cmd = new SqlCommand("GetAdminBalance", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StartDate", start);
                cmd.Parameters.AddWithValue("@EndDate", end);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result.TotalAvailableBalance = reader.GetDecimal(reader.GetOrdinal("TotalAvailableBalance"));
                        result.TodayPayOut = reader.IsDBNull(reader.GetOrdinal("TodayPayOut")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TodayPayOut"));
                        result.TodayPayIn = reader.IsDBNull(reader.GetOrdinal("TodayPayIn")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TodayPayIn"));
                    }
                }
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    totalUsers = result.TotalAvailableBalance,
                    totalPayIns = result.TodayPayIn,
                    totalPayOuts = result.TodayPayOut,
                    totalBalance = 0
                }
            });
        }



        public IActionResult GetFeatureAccess()
        {
            var gateWayDetails = _context.PayManGateways.FirstOrDefault();
            var access = new
            {
                payIn = gateWayDetails.PayIn,
                payOut = gateWayDetails.PayOut,
                ccBill = gateWayDetails.CcBill,
                instantPay = gateWayDetails.InstantPay,
                billAvenue= gateWayDetails.BillAvenue,
                easebyzz1= gateWayDetails.Easebuzz1,
                easebuzz2= gateWayDetails.Easebuzz2,
                eduCashfree= gateWayDetails.Edu,
                pineLab= gateWayDetails.PineLabPayout,
                csbBank = gateWayDetails.CsbbankPayout,
                edutrav = gateWayDetails.Edu,
                rozorpayedu = gateWayDetails.RazorPay,
            };
            return Json(new { success = true, data = access });
        }


        public async Task<IActionResult> UpdateFeatureAccess([FromBody] FeatureToggleRequest request)
        {
            var users = await _context.payManUsers
              .Where(t => t.IsAdmin == false)
              .ToListAsync();
            var gateWayDetails = _context.PayManGateways.FirstOrDefault();

            if (request.Feature == "PayOut")
            {
                gateWayDetails.PayOut = request.Enabled;
                foreach (var user in users)
                {
                    user.PayOut = request.Enabled;
                }
                _context.PayManGateways.Update(gateWayDetails);
                _context.payManUsers.UpdateRange(users);

                await _context.SaveChangesAsync();
            }

            if (request.Feature == "CCBill")
            {
                gateWayDetails.CcBill = request.Enabled;
                foreach (var user in users)
                {
                    user.PayOut = request.Enabled;
                }
                _context.PayManGateways.Update(gateWayDetails);
                _context.payManUsers.UpdateRange(users);
                await _context.SaveChangesAsync();
            }

            if (request.Feature == "PayIn")
            {
                gateWayDetails.PayIn = request.Enabled;
                foreach (var user in users)
                {
                    user.PayOut = request.Enabled;
                }
                _context.PayManGateways.Update(gateWayDetails);
                _context.payManUsers.UpdateRange(users);
                await _context.SaveChangesAsync();
            }

            if (request.Feature == "InstantPay")
            {
                gateWayDetails.InstantPay = request.Enabled;
                gateWayDetails.BillAvenue = !request.Enabled;
                _context.PayManGateways.Update(gateWayDetails);
                await _context.SaveChangesAsync();
            }

            if (request.Feature == "BillAvenue")
            {
                gateWayDetails.BillAvenue = request.Enabled;
                gateWayDetails.InstantPay = !request.Enabled;
                _context.PayManGateways.Update(gateWayDetails);
                await _context.SaveChangesAsync();
            }

            if (request.Feature == "Easebuzz1")
            {
                gateWayDetails.Easebuzz1 = request.Enabled;
                gateWayDetails.Easebuzz2 = !request.Enabled;
                _context.PayManGateways.Update(gateWayDetails);
                await _context.SaveChangesAsync();
            }

            if (request.Feature == "Easebuzz2")
            {
                gateWayDetails.Easebuzz2 = request.Enabled;
                gateWayDetails.Easebuzz1 = !request.Enabled;
                _context.PayManGateways.Update(gateWayDetails);
                await _context.SaveChangesAsync();
            }

            if (request.Feature == "EduCashfree")
            {
                gateWayDetails.Edu = request.Enabled;
                _context.PayManGateways.Update(gateWayDetails);
                await _context.SaveChangesAsync();
            }

            if (request.Feature == "PineLab")
            {
                gateWayDetails.PineLabPayout = request.Enabled;
                _context.PayManGateways.Update(gateWayDetails);
                await _context.SaveChangesAsync();
            }

            if (request.Feature == "CSbBank")
            {
                gateWayDetails.CsbbankPayout = request.Enabled;
                _context.PayManGateways.Update(gateWayDetails);
                await _context.SaveChangesAsync();
            }

            if (request.Feature == "EduRozerpay")
            {
                gateWayDetails.RazorPay = request.Enabled;
                _context.PayManGateways.Update(gateWayDetails);
                await _context.SaveChangesAsync();
            }

            if (request.Feature == "Travelvegaah")
            {
                gateWayDetails.Edu = request.Enabled;
                _context.PayManGateways.Update(gateWayDetails);
                await _context.SaveChangesAsync();
            }

            // In real app, update DB here
            Console.WriteLine($"Feature '{request.Feature}' set to {request.Enabled}");
            return Json(new { success = true });
        }


        public IActionResult GetAllUsers()
        {

            var results = new List<UserAvailableBalanceDto>();

            var connectionString = _configuration.GetConnectionString("MVC6CrudConnetionString"); // or hardcode if needed

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetUsersAvailableBalance", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new UserAvailableBalanceDto
                            {
                                UserPhone = reader["UserPhone"].ToString(),
                                Name = reader["name"].ToString(),
                                Email = reader["email"].ToString(),
                                UserPin = reader["UserPin"].ToString(),
                                WebPass = reader["WebPass"].ToString(),
                                PayOut = reader["PayOut"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PayOut"]),
                                PayIn = reader["PayIn"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PayIn"]),
                                PayIncomm = reader["PayIncomm"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PayIncomm"]),
                                WalletBalance = reader["WalletBalance"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["WalletBalance"]),
                            });
                        }
                    }
                }
            }

            return Json(new { success = true, data = results });
        }


        [HttpGet]
        public IActionResult GetAllPayIns(string startDate, string endDate)
        {
            DateTime? start = null;
            DateTime? end = null;

            if (!string.IsNullOrEmpty(startDate))
                start = DateTime.Parse(startDate);

            if (!string.IsNullOrEmpty(endDate))
                end = DateTime.Parse(endDate).AddDays(1).AddSeconds(-1); // Include full day till 23:59:59

            var payins = _context.payManPayIns
                .Where(p => (start == null || p.Created >= start)
                         && (end == null || p.Created <= end))
                .OrderByDescending(p => p.Created)
                .Take(100) // limit records for performance, optional
                .Select(p => new
                {
                    transactionId = p.EasePayId,
                    userPhone = p.UserPhone,
                    amount = p.Amount,
                    status = p.Status == true ? "Success" : "Pending",
                    date = p.Created.HasValue ? p.Created.Value.ToString("o") : null
                })
                .ToList();

            return Json(new { success = true, data = payins });
        }



        [HttpGet]
        public IActionResult GetAllPayOuts(string startDate, string endDate)
        {
            DateTime? start = null;
            DateTime? end = null;

            if (!string.IsNullOrEmpty(startDate))
                start = DateTime.Parse(startDate);

            if (!string.IsNullOrEmpty(endDate))
                end = DateTime.Parse(endDate).AddDays(1).AddSeconds(-1); // Include end date till 23:59:59

            var payouts = _context.payManPayOuts
                .Where(p => (start == null || p.DateTime >= start)
                         && (end == null || p.DateTime <= end))
                .OrderByDescending(p => p.DateTime)
                .Select(p => new
                {
                    id = p.Id,
                    refId = p.RefId,
                    userPhone = p.UserPhone,
                    amount = p.Amount,
                    status = p.Status == true ? "Success" : "Failed",
                    mode = p.PayOutType,
                    date = p.DateTime.HasValue ? p.DateTime.Value.ToString("o") : null,
                    result = p.Result
                })
                .ToList();

            return Json(new { success = true, data = payouts });
        }

        [HttpGet]
        public IActionResult GetAllPayOutsForGstInvoice(string startDate, string endDate,string userPhone)
        {
            DateTime? start = null;
            DateTime? end = null;

            if (!string.IsNullOrEmpty(startDate))
                start = DateTime.Parse(startDate);

            if (!string.IsNullOrEmpty(endDate))
                end = DateTime.Parse(endDate).AddDays(1).AddSeconds(-1); // Include end date till 23:59:59

            var payouts = _context.payManPayOuts
                .Where(p => (start == null || p.DateTime >= start)
                         && (end == null || p.DateTime <= end) && p.Status == true && p.UserPhone == userPhone)
                .OrderByDescending(p => p.DateTime)
                .Select(p => new
                {
                    id = p.Id,
                    refId = p.RefId,
                    userPhone = p.UserPhone,
                    amount = p.Amount,
                    status = p.Status == true ? "Success" : "Failed",
                    mode = p.PayOutType,
                    date = p.DateTime.HasValue ? p.DateTime.Value.ToString("o") : null,
                    result = p.Result
                })
                .ToList();

            return Json(new { success = true, data = payouts });
        }



        public async Task<IActionResult> RefundPayout([FromBody] RefundRequest req)
        {
            var payout = _context.payManPayOuts.FirstOrDefault(p => p.Id == Guid.Parse( req.Id));
            if (payout == null) return NotFound(new { success = false, message = "Not found" });

            payout.Status = false;
            payout.Result = "Refunded";
            await _context.SaveChangesAsync();

            // histori


            var userWalletAmount = await GetUserWalletAmount(payout.UserPhone);
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            var payInHistory = new PayManHistory
            {
                UserId = payout.UserId,
                UserPhone = payout.UserPhone,
                TxnId = payout.PayOutId,
                Amount = payout.Amount,
                CardNumber = payout.AccountNo,
                Mode = "Refund",
                Status = true,
                Created = istTime,
                AvlBalance = Convert.ToDecimal(userWalletAmount),
                PayInId = payout.Id
            };
            _context.payManHistories.Add(payInHistory);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpGet]
        public IActionResult GetCustomerDetails(string mobileNumber, string loginuserphone)
        {
            //var appPhone = HttpContext.Session.GetString("AppPhone");

            if (string.IsNullOrEmpty(loginuserphone))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            // Fetch customer cards including Gateway
            var customers = _context.payInProfiles
                .Where(c => c.Mobile == mobileNumber && c.UserPhone == loginuserphone)
                .Select(c => new
                {
                    cardHolderName = c.Name,
                    cardHolderEmail = c.Email,
                    creditCardNum = c.CardNumber,
                    gatewayId = 1 // Or fetch from DB if column exists
                })
                .ToList();

            if (!customers.Any())
            {
                return Json(new { success = false, data = new List<object>(), message = "No customer found." });
            }

            return Json(new { success = true, data = customers });
        }


        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = _context.payManUsers
                .Select(u => new
                {
                    name = u.FirstName,
                    phone = u.Phone
                })
                .ToList();

            return Ok(users);
        }

        [HttpGet]
        public IActionResult GetBanks()
        {
            var banks = _context.accountVerificationBanks
                .OrderBy(b => b.BankName)
                .Select(b => new { name = b.BankName }) // adjust property name as per your model
                .ToList();

            return Ok(banks);
        }

        [HttpGet]
        public IActionResult GetGateways()
        {
            var gateways = new[]
            {
                new { id = "Easebuzz1", name = "Easebuzz1" },
                new { id = "Easebuzz2", name = "Easebuzz2" },
                new { id = "Cashfree", name = "Easebuzz" }
            };

            return Ok(gateways);
        }

        [HttpGet]
        public IActionResult GetCards()
        {
            var cards = new[]
            {
                 new { id = 101, number = "Visa" },
                 new { id = 102, number = "Master" },
                 new { id = 103, number = "Rupay" }
            };

            return Ok(cards);
        }

        [HttpPost]
        public async Task<IActionResult> AddPayIn([FromBody] PayInModelDTO model)
        {
            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == model.UserPhone);
            if (user == null)
            {
                return RedirectToAction("WebLogout", "Login");
            }

            // Get gateway config
            var easebuzzGateway = await _context.PayManGateways.FirstOrDefaultAsync(t => t.GatewayName == "Easebuzz");

            // Check if this payment already exists (idempotency)
            var existing = await _context.payManPayIns
                .FirstOrDefaultAsync(t => t.EasePayId == model.OrderRefId && t.Status == true);

            if (existing == null)
            {
                var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                 TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                decimal margin = Convert.ToDecimal(user.Margin ?? "0");

                if (model.card.ToLower() == "mastercard")
                    margin = user.MasterMarigin ?? margin;

                //if (cardType.ToLower() == "true")
                //    margin = user.CarporateCardMarigin ?? margin;
                //else if (model.card.ToLower() == "mastercard")
                //    margin = user.MasterMarigin ?? margin;
                //else if (BankName.ToLower().Contains("hdfc"))
                //    margin = user.HdfcMargin ?? margin;

                var payInApp = new PayManPayIn
                {
                    UserId = user.Id,
                    UserPhone = model.UserPhone,
                    TxnId = model.OrderRefId,
                    EasePayId = model.OrderRefId,
                    Email = user.Email,
                    CardNumber = model.CardNumber,
                    EaseCardNum = model.CardNumber,
                    Amount = model.Amount,
                    Gateway = model.GatewayId,
                    BankName = "",
                    CardBrand = model.card,
                    IsCorporate = "",
                    PayInCommission = model.Amount * margin / 100,
                    PaymanCommission = model.Amount * (easebuzzGateway?.PaymanComm ?? 0) / 100,
                    Created = istTime,
                    Status = true,
                    Result = "success",
                    Device = "Web"
                };

                _context.payManPayIns.Add(payInApp);
                await _context.SaveChangesAsync();

                var avlAmount = await GetUserWalletAmount(model.UserPhone);

                var payInHistory = new PayManHistory
                {
                    UserId = user.Id,
                    UserPhone = model.UserPhone,
                    TxnId = model.OrderRefId,
                    Amount = model.Amount,
                    CardNumber = model.CardNumber,
                    Mode = "PayIn",
                    Status = true,
                    Created = istTime,
                    AvlBalance = Convert.ToDecimal(avlAmount),
                    PayInId = payInApp.Id
                };

                _context.payManHistories.Add(payInHistory);
                await _context.SaveChangesAsync();
            }
            // Save to DB...
            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddPayOut([FromBody] PayOutModelDTO model)
        {
            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == model.UserPhone);
            if (user == null)
            {
                return RedirectToAction("WebLogout", "Login");
            }


            // saving part
            DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            var transaction = new PayManPayOut
            {
                UserId = user.Id,
                UserPhone = user.Phone,
                PayOutId = model.OrderRefId,
                RefId = model.OrderRefId,
                AccountHolderName = model.AccountHolderName ?? "Unknown",
                AccountNo = model.AccountNumber,
                IfscCode = "",
                Amount = model.Amount,
                PayoutCommission = 30,
                BeneId = "",
                PayOutType = "manual",
                TxnType = "IMPS",
                Email = user.Email,
                Status = true,
                Result = "Sucess",
                DateTime = istDateTime,
                Device = "Mobile"
            };

            _context.payManPayOuts.Add(transaction);
            await _context.SaveChangesAsync();

            var userWalletAmount = await GetUserWalletAmount(user.Phone);

            var payInHistory = new PayManHistory
            {
                UserId = user.Id,
                UserPhone = user.Phone,
                TxnId = model.OrderRefId,
                Amount = model.Amount ,
                CardNumber = model.AccountNumber,
                Mode = "PayOut",
                Status = true,
                Created = istDateTime,
                AvlBalance = Convert.ToDecimal(userWalletAmount),
                PayInId = transaction.Id
            };
            _context.payManHistories.Add(payInHistory);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }


        public async Task<string> GetInstantPayAmount()
        {
            var billAvenue = await _context.PayManGateways.FirstOrDefaultAsync();

            if (billAvenue.BillAvenue == true)
            {
                var avlbal = await BillAvanueBalance();
                return avlbal ?? "0.00";
            }
            else
            {
                try
                {
                    BalanceResponseApp balanceResponse = new BalanceResponseApp();

                    using (HttpClient client = new HttpClient())
                    {
                        // Headers
                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                        client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                        client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
                        client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
                        client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "13.200.194.39");
                        client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", "490007");

                        var requestData = new
                        {
                            bankProfileId = "0",
                            accountNumber = "9100748033",
                            externalRef = "PROD1981",
                            latitude = "20.126",
                            longitude = "78.3228"
                        };

                        string jsonData = System.Text.Json.JsonSerializer.Serialize(requestData);
                        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.PostAsync("https://api.instantpay.in/accounts/balance", content);
                        string responseContent = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            // Log or throw custom exception here if needed
                            return "0.00";
                        }

                        balanceResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<BalanceResponseApp>(responseContent);
                        return balanceResponse?.Data?.Balance?.Available ?? "0.00";
                    }
                }
                catch (Exception ex)
                {
                    // Optionally log the exception
                    return "0.00";
                }
            }
           
            
        }

        public async Task<string> GetPinelabsAmount()
        {
            string balance = "0.00";
            try
            {
                JWT_Generator generator = new JWT_Generator();
                var bearerToken = generator.GenerateToken();

                string baseUrl = "https://api.pluralonline.com/payouts/v2/payments/";
                string endpoint = "funding-account";
                string currencyPrefix = "INR"; // Move to config if needed

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(baseUrl);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                    HttpResponseMessage response = await client.GetAsync(endpoint);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        BankAccountApp objStatus = JsonConvert.DeserializeObject<BankAccountApp>(responseContent);

                        if (objStatus?.balance?.value != null)
                        {
                            balance = AddLastTwoDigitsToBeginning(currencyPrefix, Convert.ToInt32(objStatus.balance.value));
                        }
                    }
                    else
                    {
                        // Optionally log the failure response
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception (preferably with ILogger)
            }

            return balance;
        }

        public async Task<string> GetUserWalletAmount(string phone)
        {
            var userDetails = await _context.payManUsers
                                            .FirstOrDefaultAsync(t => t.Phone == phone);
            if (userDetails == null)
                return "User not found";

            var payIns = await _context.payManPayIns.Where(t => t.UserPhone == phone && t.Status == true).ToListAsync();
            var accountVerifications = await _context.payManBeneficiaryAccounts.Where(t => t.UserPhone == phone && t.VerficationFlag == "Y").ToListAsync();
            var payOuts = await _context.payManPayOuts.Where(t => t.UserPhone == phone && t.Status == true).ToListAsync();

            decimal totalPayIn = payIns.Sum(t => t.Amount ?? 0);
            decimal payInCommission = payIns.Sum(t => t.PayInCommission ?? 0);
            decimal accountCommission = accountVerifications.Sum(t => t.VerificationComm ?? 0);
            decimal payOutCommission = payOuts.Sum(t => t.PayoutCommission ?? 0);
            decimal totalPayOut = payOuts.Sum(t => t.Amount ?? 0);

            decimal totalDeductions = payInCommission + accountCommission + payOutCommission + totalPayOut;
            decimal walletAmount = totalPayIn - totalDeductions;

            return walletAmount.ToString("0.00");
        }

        public string AddLastTwoDigitsToBeginning(string originalString, int number)
        {
            // Get the last two digits of the number
            int lastTwoDigits = number / 100;

            // Convert lastTwoDigits to string and prepend to originalString
            string result = lastTwoDigits.ToString();//lastTwoDigits.ToString() + originalString;

            return result;
        }


        public async Task<string> BillAvanueBalance()
        {
            var accessCode = _configuration["BillAvenueKeys:accessCode"];
            var workingKey = _configuration["BillAvenueKeys:workingKey"];
            var instituteId = _configuration["BillAvenueKeys:instituteId"];
            var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];

            string requestId = GenerateRequestId();
            string ver = "1.0";
            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<depositDetailsRequest> 
 <fromDate>2025-08-20</fromDate> 
 <toDate>2025-08-24</toDate> 
 <transType>CR</transType> 
 <agents> 
  <agentId>CC01RP91AGTBAA919897</agentId> 
  <agentId>CC01RP91AGTBAA919896</agentId> 
  <agentId>CC01RP91AGTBAA919898</agentId> 
 </agents> 
</depositDetailsRequest>";


            string encryptedPayload = Encrypt(merchantData, workingKey);
            string apiUrl = $"https://api.billavenue.com/billpay/enquireDeposit/fetchDetails/xml" +
                            $"?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}&encRequest={encryptedPayload}";

            // If required by BillAvenue, add endpoint IP also
            //if (!string.IsNullOrEmpty(endpointIp))
            //    apiUrl += $"&endIP={endpointIp}";

   

            using var client = new HttpClient();

            try
            {
                var response = await client.PostAsync(apiUrl, null);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    string decryptedResponse = Decrypt(result, workingKey);

                    // Deserialize XML to object
                    var serializer = new XmlSerializer(typeof(DepositEnquiryResponse));
                    DepositEnquiryResponse billResponse;
                    using (var reader = new StringReader(decryptedResponse))
                    {
                        billResponse = (DepositEnquiryResponse)serializer.Deserialize(reader);
                    }
                    return billResponse.CurrentBalance.ToString();
                }

                return "0.00";
            }
            catch (Exception ex)
            {
                // Log ex
                return "0.00";
            }
        }


        public async Task<decimal> UserAvlBalance()
        {


            // var queryweb = "EXEC GetTotalWebAvailableBalance";

            //// Execute the query and get the JSON result
            //var jsonResultweb = _context.Database
            //    .SqlQueryRaw<decimal>(queryweb)
            //    .AsEnumerable()
            //    .FirstOrDefault(); // Assuming the first row is the result



            var query = "EXEC GetTotalAvailableBalance";

            // Execute the query and get the JSON result
            var jsonResult = _context.Database
                .SqlQueryRaw<decimal>(query)
                .AsEnumerable()
                .FirstOrDefault(); // Assuming the first row is the result

            var reult = jsonResult;
            return reult;

        }

        private string GenerateRequestId()
        {
            return $"{Guid.NewGuid():N}{DateTime.Now:fff}".Substring(0, 35);
        }

        private string Encrypt(string plainText, string key)
        {
            byte[] keyBytes = HexToBytes(MD5Hash(key));
            byte[] iv = new byte[16] {
                0x00, 0x01, 0x02, 0x03,
                0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0a, 0x0b,
                0x0c, 0x0d, 0x0e, 0x0f
            };

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                ICryptoTransform encryptor = aes.CreateEncryptor();

                byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return BitConverter.ToString(encrypted).Replace("-", "").ToLower();
            }
        }

        private string Decrypt(string encryptedHex, string key)
        {
            byte[] keyBytes = HexToBytes(MD5Hash(key));
            byte[] iv = new byte[16] {
                0x00, 0x01, 0x02, 0x03,
                0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0a, 0x0b,
                0x0c, 0x0d, 0x0e, 0x0f
            };

            byte[] encryptedBytes = HexToBytes(encryptedHex);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
        }

        private string MD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private byte[] HexToBytes(string hex)
        {
            int length = hex.Length;
            byte[] result = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return result;
        }


        [HttpPost]
        public async Task<IActionResult> AddProfile([FromBody] PayInProfiles model)
        {

           // var appPhone = HttpContext.Session.GetString("AppPhone");
            if (string.IsNullOrEmpty(model.UserPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }


            if (string.IsNullOrEmpty(model.Mobile) || string.IsNullOrEmpty(model.Name))
                return Ok(new { success = false, message = "Mobile and Name are required", profile = model });

            var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                 TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            // Check if profile already exists
            var existing = await _context.payInProfiles.FirstOrDefaultAsync(p => p.Mobile == model.Mobile && p.Name == model.Name && p.UserPhone == model.UserPhone);
            if (existing != null)
                return Ok(new { success = false, message = "Profile already exists for this mobile and name.", profile = model });

            model.CreatedDate = istTime;
            model.UserPhone = model.UserPhone;
            _context.payInProfiles.Add(model);
            await _context.SaveChangesAsync();

            return Ok(new { success = true ,message = "Profile saved successfully.", profile = model });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCardDetailsinProfile([FromBody] PayInProfiles model)
        {

            //var appPhone = HttpContext.Session.GetString("AppPhone");
            if (string.IsNullOrEmpty(model.UserPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }

            bool isValid = await _dataUtils.SavePayInProfile(model.Mobile, model.CardNumber, model.UserPhone);
            if (!isValid)
            {
                return Ok(new { success = false, message = "Profile saved failed." });
            }
            else
            {
                return Ok(new { success = true, message = "Profile saved successfully."});
            }
        }

        public IActionResult Agrement()
        {
            return View();
        }

    }
}