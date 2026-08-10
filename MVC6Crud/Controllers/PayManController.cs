using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Identity.Client;
using ClosedXML.Excel;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.PineLab;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Common;
//using OfficeOpenXml;
using Razorpay.Api;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using User = MVC6Crud.Models.User;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Mail;
using WhatsAppApi;
using Microsoft.Data.SqlClient;
using System.Data;
using XAct.Users;
using System.Reflection.Metadata.Ecma335;
using System.Net.Http;
using System.Text.Json;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Reflection;
using AspNetCoreGeneratedDocument;
using MVC6Crud.Models.PaymanApp;
using System.Globalization;
using static WhatsAppApi.Parser.FMessage;
using System.Drawing.Printing;
using MVC6Crud.Models.PaymanWeb;

namespace MVC6Crud.Controllers
{
    public class PayManController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PayManController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult PayManDash()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SubmitPayIn(PayManHome model)
        {
            var founds = _context.payManGateWayMarigins.Select(t => t.PayManFounds).FirstOrDefault();

            var sumamount = model.SefexFinalBalance + Convert.ToDecimal(model.InstantPayBalance) + model.Srrbankamount + model.EasebuzzFeatueSeletment + model.RazorpayAmount;

            var calculateamount =  sumamount - model.PayOutUsersAvaliableBalance;
            var profit = calculateamount - founds;
            var monthName = DateTime.Now.ToString("MMMM");
            TempData["SuccessMsg"] = $"Payman Profit for {monthName}: {profit}";
            return RedirectToAction("PayManHome", "PayMan");
        }

        public IActionResult PayStatus(PaymentStatusViewModel paymentStatusViewModel)
        {
            return View(paymentStatusViewModel);
        }

        public async Task<string> GetInstantPayAmount()
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

        public void UpdatePayinStaus()
        {
            var today = DateTime.Today;

            var payinSum = _context.payManPayIns
                .Where(t => t.Created.HasValue &&
                            t.Created.Value.Date == today &&
                            t.Status == true)
                .Sum(t => t.Amount);

            var gateWayDetails = _context.PayManGateways.FirstOrDefault();

            if (gateWayDetails == null)
                return; // Or log error / throw exception

            if (payinSum > gateWayDetails.EasebuzzGatewayEnableAmount)
            {
                gateWayDetails.Easebuzz1 = false;
                gateWayDetails.Easebuzz2 = true;
            }
            else
            {
                gateWayDetails.Easebuzz1 = true;
                gateWayDetails.Easebuzz2 = false;
            }

            _context.PayManGateways.Update(gateWayDetails);
            _context.SaveChanges();
        }



        [HttpGet]
        public async Task<IActionResult> PayManHome()
        {
            
            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone || u.FirstName == appPhone);

            var PayOutEnable = GetPayOutEnable();
           // UpdatePayinStaus();

            if (user == null || user.App == false )
            {
                var userId = HttpContext.Session.GetString("UserId");
                var Adharverificationdetais = _context.adharVerifications.Where(t => t.UserId == userId).FirstOrDefault();
                if (Adharverificationdetais != null)
                {
                    if (!Adharverificationdetais.AdharVerified && Adharverificationdetais.UserId != "MUIEP2")
                    {
                        return RedirectToAction("AdharNumberVerify", "PayMan");
                    }
                }
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }
                //var a = 50000 * 1.3 /100;
                var payManHome = GetAvaliableAmount(userId);

                if (userId == "MUIEP1")
                {
                    var jfdhd = Convert.ToDecimal(payManHome.InstantPayBalance);

                    var totalAmount = payManHome.SefexFinalBalance + jfdhd;
                    var avalAmountParam = new SqlParameter("@AvalAmount", SqlDbType.Decimal) { Value = totalAmount };

                    // Define output parameter
                    var resultParam = new SqlParameter("@result", SqlDbType.Decimal) { Direction = ParameterDirection.Output };

                    // Define query for the stored procedure
                    var query = "EXEC PayManDashboard @AvalAmount, @result OUTPUT";

                    // Execute the query and get the JSON result
                    var jsonResult = _context.Database
                        .SqlQueryRaw<string>(query, avalAmountParam, resultParam)
                        .AsEnumerable()
                        .FirstOrDefault(); // Assuming the first row is the result

                    var jsonString = jsonResult?.ToString();

                    // Deserialize to DashboardResponse which contains the list of DashboardResult
                    var dashboardResponse = JsonConvert.DeserializeObject<DashboardResponse>(jsonString);

                    // Now you can access the list of DashboardResult
                    var dashboardResults = dashboardResponse?.DashboardResult;


                    //var rrr = PayManDashBoard(payManHome.SefexFinalBalance);
                    foreach (var item in dashboardResults)
                    {
                        payManHome.PayManProfilt = Convert.ToDecimal(string.Format("{0:0.00}", item.PayManProfit));
                        payManHome.PayInAmount = Convert.ToDecimal(string.Format("{0:0.00}", item.PayInAmount));
                        payManHome.PayOutUsersAvaliableBalance = Convert.ToDecimal(string.Format("{0:0.00}", item.PayOutUsersAvailableBalance));
                        payManHome.PayInAmountAfterBankCommission = Convert.ToDecimal(string.Format("{0:0.00}", item.PayInAmountAfterBankCommission));

                        payManHome.PayInAmountRazorPayCommissionSettelment = Convert.ToDecimal(string.Format("{0:0.00}", item.PayInAmountRazorPayCommissionSettlement));
                        payManHome.PayInAmountLyraPayCommissionSettelment = Convert.ToDecimal(string.Format("{0:0.00}", item.PayInAmountLyraPayCommissionSettlement));
                        payManHome.PayInAmountRazorPaySettelment = Convert.ToDecimal(string.Format("{0:0.00}", item.PayInAmountRazorPaySettlement));
                        payManHome.PayInAmountLyraPaySettelment = Convert.ToDecimal(string.Format("{0:0.00}", item.PayInAmountLyraPaySettlement));
                    }

                }


                payManHome.PayOutEnable = PayOutEnable;

                return View(payManHome);
            }
            else
            {
                PayManHome payManHome1 = new PayManHome();

                if(user.IsAdmin == true)
                {
                    decimal totalUsersAvlAmount = 0;
                    var avlamount = await UserAvlBalance();
                    payManHome1.PayOutUsersAvaliableBalance = avlamount;
                    payManHome1.SefexPay = (bool)user.PayIn;
                    payManHome1.PineLab = (bool)user.PayOut;
                    payManHome1.InstantPay = (bool)user.CCBill;

                }

                var Adharverificationdetais = _context.aadharDetails.Where(t => t.Phone == user.Phone).FirstOrDefault();
                if (Adharverificationdetais != null)
                {
                    if (Adharverificationdetais.Status == false)
                    {
                        return RedirectToAction("AdharNumberVerify", "PayMan");
                    }
                }
                if (string.IsNullOrEmpty(appPhone))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }

                var instantPayAmount = await GetInstantPayAmount();
                var pineLabsAmount = await GetPinelabsAmount();
                var userWalletAmount = await GetUserWalletAmount(appPhone);

                payManHome1.Amount = Convert.ToDecimal(userWalletAmount);
                payManHome1.InstantPayBalance = instantPayAmount;
                payManHome1.SefexFinalBalance = Convert.ToDecimal(pineLabsAmount);
                payManHome1.Name = user.FirstName;
                payManHome1.PhoneNumber = user.Phone;

                payManHome1.PayOutEnable = PayOutEnable;


                return View(payManHome1);
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

        public  PayManHome GetAvaliableAmount(string? userId, bool isDasboard = false)
        {
            PayManHome payManHome = new PayManHome();

            var paymentdetails = _context.payIns.Where(t => t.UserId == userId && (t.Sttaus == "PAID" || t.Sttaus == "captured" || t.Sttaus == "success")).ToList();
            var userdetails = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
            var payoutdetails = _context.payOutTransectionDetails.Where(t => t.userId == userId && (t.bankStatus == "PENDING" || t.bankStatus == "PROCESSED")).ToList();
            var bankVerification = _context.beneficiaryAccounts.Where(t => t.UserId == userId && t.VerficationFlag == "Y").ToList();

            var totalVeficationAmount = bankVerification.Sum(t => t.VerificationComm);

            var totalPayIn = paymentdetails.Sum(t => t.Amount);
            var totalPayIncommission = paymentdetails.Sum(t => t.PayInCommission);

            var totalPayOut = payoutdetails.Sum(t => Convert.ToDouble(t.txnAmount));
            var totalPayoutcommission = payoutdetails.Sum(t => t.PayoutCommission);

            var distibuterCommission = _context.payIns.Where(t => t.DistributerUserId == userId && (t.Sttaus == "PAID" || t.Sttaus == "captured" || t.Sttaus == "success")).Sum(t => t.DistibuterCommission);


            var totalPayIns = totalPayIn - totalPayIncommission;
            var ExludedAmount = totalPayOut + totalPayoutcommission;


            // var percentageCaleculation = (double)totalPayIn * Convert.ToDouble(userdetails.Margin) / 100;
            var result = (double)totalPayIns - ExludedAmount;
            var hjds = result - totalVeficationAmount;
            var finalAmount = hjds + (double)distibuterCommission;
            var dhf = String.Format("{0:0.00}", finalAmount);

            payManHome.Amount = Convert.ToDecimal(dhf);
            TempData["Amount"] = payManHome.Amount.ToString();
            payManHome.Name = userdetails.Name;
            payManHome.PhoneNumber = userdetails.Phone;
            payManHome.Role = userdetails.Role;

            //Payoutconfc
            var objPayOutcon = _context.payOutConfics.FirstOrDefault();
            if (objPayOutcon != null)
            {
                payManHome.SefexPay = objPayOutcon.SefexPay;
                payManHome.PineLab = objPayOutcon.PineLab;
                payManHome.InstantPay = objPayOutcon.InstantPay;
            }

            if (isDasboard == false)
            {
                var getSefaxFinalBalance = GetSefexBalance(payManHome.SefexPay).Result;
                var InstantPayBal = BlanceCheck().Result;
                payManHome.SefexFinalBalance = Convert.ToDecimal(getSefaxFinalBalance);
                payManHome.InstantPayBalance = InstantPayBal;
            }


            return payManHome;
        }


        [HttpGet]
        public IActionResult BalanceTopUp()
        {
            return View();
        }
        [HttpPost]
        public IActionResult BalanceTopUp(PayPayment reg)
        {
            return RedirectToAction("Pay", "PayMan");
        }

        [HttpGet]
        public IActionResult Registration(int? Id)
        {
            // Whatsapp();
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LogOut", "LogIn");
            }
            var userdetails = _context.users.Where(t => t.UserId == userId).FirstOrDefault();

            Registartion re = new Registartion();
            re.MariginAccess = userdetails.Name;
            re.MasterMarginEnable = userdetails.MasterMarginEnable;
            if (Id != 0 && Id != null)
            {
                var uus = _context.users.Where(t => t.Id == Id).FirstOrDefault();
                var userde = new Registartion()
                {
                    Email = uus.Email,
                    Password = uus.Password,
                    UserName = uus.Name,
                    Margin = uus.Margin.ToString(),
                    PhoneNumber = uus.Phone,
                    CustomerType = uus.Role,
                    DMTAccessable = uus.DMTAccessable,
                    IsActive = uus.IsActive,
                    Id = uus.Id,
                    BalanceTopUp = uus.BalanceTopUp,
                    MariginAccess = userdetails.Name,
                    SrrEnable = uus.SrrEnable,
                    MasterMarginEnable = uus.MasterMarginEnable,
                    MasterMargin =uus.MasterMargin.ToString()
                };
                return View(userde);
            }

            return View(re);
        }
        [HttpPost]
        public IActionResult Registration(Registartion reg)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var useroo = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LogOut", "LogIn");
            }
            if (reg.Id == 0)
            {
                var userdetailsd = _context.users.Where(t => t.Name == reg.UserName).FirstOrDefault();
                if (userdetailsd != null)
                {
                    TempData["useralrdeyexists"] = "User (" + userdetailsd.Name + ") alredy exists!.";
                    return RedirectToAction("PayManHome", "PayMan");
                }

                var lstestUserId = _context.users.Max(t => t.Id);
                var UId = lstestUserId + 1;
                var user = new User()
                {
                    Name = reg.UserName,
                    Phone = reg.PhoneNumber,
                    Email = reg.Email,
                    Password = GenerateRandomPassword(10),
                    Role = reg.CustomerType,
                    CreatedDate = DateTime.Now,
                    UserId = "MUIEP" + UId,
                    Margin = decimal.Parse(reg.Margin),
                    DMTAccessable = false,
                    IsActive = reg.IsActive,
                    DistributeruserId = userId,
                    DistributerMarigin = 0,//decimal.Parse(reg.Margin) - useroo.Margin,
                    BalanceTopUp = false,
                    SrrEnable = false,
                    ChangePassword = false,
                    MasterMargin = useroo.MasterMarginEnable ==true ? decimal.Parse(reg.MasterMargin) : decimal.Parse("1.62"),
                    MasterMarginEnable = false
                };
                if (user != null)
                {
                    _context.users.Add(user);
                    _context.SaveChanges();
                    var body = HTMLTemp(reg.UserName, user.Password);
                    SendEmail(reg.Email, "Welcome to Payman! Your Merchant Account is Ready", body);
                    TempData["SuccessMsg"] = "User (" + user.Name + ") added successfully.";
                    return RedirectToAction("PayManHome", "PayMan");
                }
            }
            else
            {
                 var hgv = _context.users.Where(t => t.Id == reg.Id).FirstOrDefault();

                if(hgv != null)
                {
                    hgv.Name = reg.UserName;
                    hgv.Phone = reg.PhoneNumber;
                    hgv.Email = reg.Email;
                    // Password = _context.users.Where(t => t.Id == reg.Id).Select(s => s.Password).FirstOrDefault(),
                    hgv.Role = reg.CustomerType;
                    // UserId = _context.users.Where(t => t.Id == reg.Id).Select(s => s.UserId).FirstOrDefault(),
                    hgv.CreatedDate = DateTime.Now;
                    hgv.Margin = decimal.Parse(reg.Margin);
                    hgv.DMTAccessable = reg.DMTAccessable;
                    hgv.IsActive = reg.IsActive;
                    //Id = reg.Id,
                    //DistributeruserId = _context.users.Where(t => t.Id == reg.Id).Select(s => s.DistributeruserId).FirstOrDefault(),
                    hgv.DistributerMarigin = decimal.Parse(reg.Margin) - _context.users.Where(t => t.UserId == (_context.users.Where(t => t.Id == reg.Id).Select(s => s.DistributeruserId).FirstOrDefault())).Select(s => s.Margin).FirstOrDefault();
                    hgv.BalanceTopUp = reg.BalanceTopUp;
                    //ChangePassword = _context.users.Where(t => t.Id == reg.Id).Select(s => s.ChangePassword).FirstOrDefault(),
                    hgv.SrrEnable = reg.SrrEnable;
                   /// hgv.MasterMargin = decimal.Parse(reg.MasterMargin);
                    //MasterMarginEnable = _context.users.Where(t => t.Id == reg.Id).Select(s => s.MasterMarginEnable).FirstOrDefault()
                }

                //var user = new User()
                //{
                //    Name = reg.UserName,
                //    Phone = reg.PhoneNumber,
                //    Email = reg.Email,
                //    Password = _context.users.Where(t => t.Id == reg.Id).Select(s => s.Password).FirstOrDefault(),
                //    Role = reg.CustomerType,
                //    UserId = _context.users.Where(t => t.Id == reg.Id).Select(s => s.UserId).FirstOrDefault(),
                //    CreatedDate = DateTime.Now,
                //    Margin = decimal.Parse(reg.Margin),
                //    DMTAccessable = reg.DMTAccessable,
                //    IsActive = reg.IsActive,
                //    Id = reg.Id,
                //    DistributeruserId = _context.users.Where(t => t.Id == reg.Id).Select(s => s.DistributeruserId).FirstOrDefault(),
                //    DistributerMarigin = decimal.Parse(reg.Margin) - _context.users.Where(t => t.UserId == (_context.users.Where(t => t.Id == reg.Id).Select(s => s.DistributeruserId).FirstOrDefault())).Select(s => s.Margin).FirstOrDefault(),
                //    BalanceTopUp = reg.BalanceTopUp,
                //    ChangePassword = _context.users.Where(t => t.Id == reg.Id).Select(s => s.ChangePassword).FirstOrDefault(),
                //    SrrEnable = reg.SrrEnable,
                //    MasterMargin = decimal.Parse(reg.MasterMargin),
                //    MasterMarginEnable = _context.users.Where(t => t.Id == reg.Id).Select(s => s.MasterMarginEnable).FirstOrDefault()
                //};
                if (hgv != null)
                {
                    _context.users.Update(hgv);
                    _context.SaveChanges();
                    TempData["SuccessMsg"] = "User (" + hgv.Name + ") Updated successfully.";
                    return RedirectToAction("PayManHome", "PayMan");
                }
            }

            return View();
        }
        public static string GenerateRandomPassword(int length)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
            StringBuilder result = new StringBuilder();
            Random random = new Random();

            while (0 < length--)
            {
                result.Append(validChars[random.Next(validChars.Length)]);
            }

            return result.ToString();
        }
        public string HTMLTemp(string username, string pass)
        {
            return "<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>Merchant Onboarding Confirmation</title>\r\n</head>\r\n<body style=\"font-family: Arial, sans-serif; color: #333; line-height: 1.6;\">\r\n\r\n    <div style=\"max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;\">\r\n        <h2 style=\"color: #0073e6;\">Dear Merchant,</h2>\r\n\r\n        <p>Congratulations!!</p>\r\n\r\n        <p>Your Merchant Onboarding Application has been successfully submitted in Payman.</p>\r\n\r\n        <p>Use the link and credentials below to login to your account:</p>\r\n\r\n        <table style=\"width: 100%; margin: 20px 0; border-collapse: collapse;\">\r\n            <tr>\r\n                <td style=\"padding: 10px; border: 1px solid #ddd;\"><strong>Login URL:</strong></td>\r\n                <td style=\"padding: 10px; border: 1px solid #ddd;\"><a href=\"https://paymanfintech.in/\" style=\"color: #0073e6;\">https://paymanfintech.in/</a></td>\r\n            </tr>\r\n            <tr>\r\n                <td style=\"padding: 10px; border: 1px solid #ddd;\"><strong>User Name:</strong></td>\r\n                <td style=\"padding: 10px; border: 1px solid #ddd;\">" + username + "</td>\r\n            </tr>\r\n            <tr>\r\n                <td style=\"padding: 10px; border: 1px solid #ddd;\"><strong>Password:</strong></td>\r\n                <td style=\"padding: 10px; border: 1px solid #ddd;\">" + pass + "</td>\r\n            </tr>\r\n        </table>\r\n\r\n        <p>If you did not make this request, or need further assistance, reach out to us:</p>\r\n        <p>Email: <a href=\"mailto:support@paymanfintech.in\" style=\"color: #0073e6;\">support@paymanfintech.in</a><br>\r\n        Call: +91 9100748033</p>\r\n\r\n        <p style=\"color: #999;\">Please do not reply to this email.</p>\r\n\r\n        <p>Thank You,<br>\r\n        Payman Fintech Team</p>\r\n    </div>\r\n\r\n</body>\r\n</html>";
        }
        public string HTMLTemp1(string username, string pass)
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Reset Notification</title>
</head>
<body style=""font-family: Arial, sans-serif; color: #333; line-height: 1.6;"">

    <div style=""max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;"">
        <h2 style=""color: #0073e6;"">Dear Merchant,</h2>

        <p>Your password has been successfully reset. Please find the details below:</p>

        <p>Use the following credentials to access your account:</p>

        <table style=""width: 100%; margin: 20px 0; border-collapse: collapse;"">
            <tr>
                <td style=""padding: 10px; border: 1px solid #ddd;""><strong>Login URL:</strong></td>
                <td style=""padding: 10px; border: 1px solid #ddd;""><a href=""https://paymanfintech.in/"" style=""color: #0073e6;"">https://paymanfintech.in/</a></td>
            </tr>

            <tr>
                <td style=""padding: 10px; border: 1px solid #ddd;""><strong>Password:</strong></td>
                <td style=""padding: 10px; border: 1px solid #ddd;"">" + pass + @"</td>
            </tr>
        </table>

        <p>If you did not request this password reset or believe this email was sent in error, please contact us immediately:</p>
        <p>Email: <a href=""mailto:support@paymanfintech.in"" style=""color: #0073e6;"">support@paymanfintech.in</a><br>
        Call: +91 9100748033</p>

        <p style=""color: #999;"">This is an automated email. Please do not reply to this message.</p>

        <p>Thank you,<br>
        Payman Fintech Team</p>
    </div>

</body>
</html>";
        }
        public void SendEmail(string toAddress, string subject, string body)
        {
            MailAddress from = new MailAddress("payman111223@gmail.com", "PAYMAN");
            List<string> toAddresses1 = new List<string>
        {
            toAddress
        };
            List<string> ccAddresses1 = new List<string>
        {
            "jurrajanardhan@gmail.com",
            "ajaykusa43@gmail.com"
        };

            MailMessage message = new MailMessage
            {
                From = from,
                Subject = subject,
                Body = body,
                IsBodyHtml = true // Set to true if your email body contains HTML
            };

            // Add multiple recipients to the To field dynamically
            foreach (string address in toAddresses1)
            {
                message.To.Add(address);
            }
            foreach (string address in ccAddresses1)
            {
                message.Bcc.Add(address);
            }

            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("payman111223@gmail.com", "nolvsexhjwnoevco"),
                EnableSsl = true
            };

            try
            {
                smtpClient.Send(message);
                Console.WriteLine("Email sent successfully.");
            }
            catch (SmtpException ex)
            {
                Console.WriteLine("Error sending email: " + ex.Message);
            }
        }

        public static string Base64Encode(string text)
        {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
        }
        public void Whatsapp()
        {
            string from = "919849800697";
            string password = "869922060444572";
            string to = "919966529779";
            string message = "Hello, this is a test message from .NET!";

            // Initialize the WhatsApp API
            WhatsApp wa = new WhatsApp(from, password, "NickName", true, true);

            wa.OnConnectSuccess += () =>
            {

                wa.OnLoginSuccess += (phoneNumber, data) =>
                {
                    wa.SendMessage(to, message);
                };

                wa.OnLoginFailed += (data) =>
                {
                };

                wa.Login();
            };

            wa.OnConnectFailed += (ex) =>
            {
            };

            wa.Connect();

        }

        public IActionResult Pay()
        {
            PayPayment payment = new PayPayment();

            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);

            if (user == null || user.App == false)
            {
                payment.UserName = HttpContext.Session.GetString("UserName");
                payment.Email = HttpContext.Session.GetString("Email");
                payment.phone = HttpContext.Session.GetString("Phone");

                var userId = HttpContext.Session.GetString("UserId");
                var todaydate = DateTime.Now.ToString("yyyy-MM-dd");

                var userdetails = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
                var PayIntotal = _context.payIns.Where(t => t.GateWay == 8 && (t.Sttaus == "success" || t.Sttaus == "captured")).ToList();
                var ff = PayIntotal.Where(t => t.CreatedDate.ToString("yyyy-MM-dd") == todaydate && t.GateWay == 8).Sum(t => t.Amount);
                if (!userdetails.BalanceTopUp)
                {
                    TempData["useralrdeyexists"] = "Service not available please contact Admin";
                    return RedirectToAction("PayManHome", "PayMan");
                }
                if (userdetails.SrrEnable == true)
                {
                    payment.IsSrrEnable = true;
                }
                if (ff > 9000000)
                {
                    payment.EaseBuzzEnable = true;
                }
            }
            else
            {
                payment.UserName = user.FirstName;
                payment.Email = user.Email;
                payment.phone = user.Phone;

                var todaydate = DateTime.Now.ToString("yyyy-MM-dd");

                var userdetails = _context.payManUsers.Where(t => t.Phone == user.Phone).FirstOrDefault();
                var PayIntotal = _context.payManPayIns.Where(t => t.Status == true).ToList();
                var ff = PayIntotal.Where(t => t.Created.Value.ToString("yyyy-MM-dd") == todaydate).Sum(t => t.Amount);
                if (userdetails.PayIn == false)
                {
                    TempData["useralrdeyexists"] = "Service not available please contact Admin";
                    return RedirectToAction("PayManHome", "PayMan");
                }
                if (ff > 9000000)
                {
                    payment.EaseBuzzEnable = true;
                }
            }

               

            return View(payment);
        }
        public IActionResult GetOperator(int employeeId)
        {
            return Json("Data received for Employee ID: " + employeeId);
        }

        public async Task<IActionResult> Electricity()
        {
            PayPayment payment = new PayPayment();
            payment.UserName = HttpContext.Session.GetString("UserName");
            payment.Email = HttpContext.Session.GetString("Email");
            var employees = new List<GetCircle>
    {
        new GetCircle { Id = 1, Name = "AP" },
        new GetCircle { Id = 2, Name = "TS" },
        new GetCircle { Id = 3, Name = "DL" }
    };
            var getOperator = new List<GetOperator>
    {
        new GetOperator { Id = 1, Name = "TS Ele" },
        new GetOperator { Id = 2, Name = "AP Ele" }
    };

            var getBalance = new List<GetBalance>
    {
        new GetBalance { Id = 1, Name = "90.00" },
        new GetBalance { Id = 2, Name = "190.00" },
        new GetBalance { Id = 3, Name = "230.00" }
    };
            var model = new Reacharge
            {
                GetCircle = employees,
                SelectedGetCircleId = 0, // You can set the default selected employee ID here
                GetOperator = getOperator,
                SelectedGetOperatorId = 0,
                GetBalance = getBalance,
                SelectedGetBalanceId = 0,
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Electricity(PayPayment payment = null)
        {
            TempData["SuccessMsg"] = "Electricity bill Processed successfully.";
            return RedirectToAction("PayManHome", "PayMan");
        }
        public async Task<IActionResult> Internet()
        {
            PayPayment payment = new PayPayment();
            payment.UserName = HttpContext.Session.GetString("UserName");
            payment.Email = HttpContext.Session.GetString("Email");
            var employees = new List<GetCircle>
    {
        new GetCircle { Id = 1, Name = "AP" },
        new GetCircle { Id = 2, Name = "TS" },
        new GetCircle { Id = 3, Name = "DL" }
    };
            var getOperator = new List<GetOperator>
    {
        new GetOperator { Id = 1, Name = "Jio" },
        new GetOperator { Id = 2, Name = "Airtel" },
        new GetOperator { Id = 3, Name = "ACT" }
    };

            var getBalance = new List<GetBalance>
    {
        new GetBalance { Id = 1, Name = "900.00" },
        new GetBalance { Id = 2, Name = "1900.00" },
        new GetBalance { Id = 3, Name = "230.00" }
    };
            var model = new Reacharge
            {
                GetCircle = employees,
                SelectedGetCircleId = 0, // You can set the default selected employee ID here
                GetOperator = getOperator,
                SelectedGetOperatorId = 0,
                GetBalance = getBalance,
                SelectedGetBalanceId = 0,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Internet(PayPayment payment = null)
        {
            TempData["SuccessMsg"] = "Internet bill Processed successfully.";
            return RedirectToAction("PayManHome", "PayMan");
        }
        public async Task<IActionResult> DTH()
        {
            PayPayment payment = new PayPayment();
            payment.UserName = HttpContext.Session.GetString("UserName");
            payment.Email = HttpContext.Session.GetString("Email");
            var employees = new List<GetCircle>
    {
        new GetCircle { Id = 1, Name = "AP" },
        new GetCircle { Id = 2, Name = "TS" },
        new GetCircle { Id = 3, Name = "DL" }
    };
            var getOperator = new List<GetOperator>
    {
        new GetOperator { Id = 1, Name = "Jio" },
        new GetOperator { Id = 2, Name = "Airtel" },
        new GetOperator { Id = 3, Name = "Sun" }
    };

            var getBalance = new List<GetBalance>
    {
        new GetBalance { Id = 1, Name = "900.00" },
        new GetBalance { Id = 2, Name = "1900.00" },
        new GetBalance { Id = 3, Name = "2300.00" }
    };
            var model = new Reacharge
            {
                GetCircle = employees,
                SelectedGetCircleId = 0, // You can set the default selected employee ID here
                GetOperator = getOperator,
                SelectedGetOperatorId = 0,
                GetBalance = getBalance,
                SelectedGetBalanceId = 0,
            };

            //TempData["SuccessMsg"] = "DTH bill Processed successfully.";
            //return RedirectToAction("PayManHome", "PayMan");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DTH(PayPayment payment = null)
        {
            TempData["SuccessMsg"] = "DTH bill Processed successfully.";
            return RedirectToAction("PayManHome", "PayMan");
        }

        public async Task<IActionResult> WaterBill()
        {
            PayPayment payment = new PayPayment();
            payment.UserName = HttpContext.Session.GetString("UserName");
            payment.Email = HttpContext.Session.GetString("Email");
            var employees = new List<GetCircle>
    {
        new GetCircle { Id = 1, Name = "AP" },
        new GetCircle { Id = 2, Name = "TS" },
        new GetCircle { Id = 3, Name = "DL" }
    };
            var getOperator = new List<GetOperator>
    {
        new GetOperator { Id = 1, Name = "Jio" },
        new GetOperator { Id = 2, Name = "Airtel" },
        new GetOperator { Id = 3, Name = "Sun" }
    };

            var getBalance = new List<GetBalance>
    {
        new GetBalance { Id = 1, Name = "900.00" },
        new GetBalance { Id = 2, Name = "1900.00" },
        new GetBalance { Id = 3, Name = "2300.00" }
    };
            var model = new Reacharge
            {
                GetCircle = employees,
                SelectedGetCircleId = 0, // You can set the default selected employee ID here
                GetOperator = getOperator,
                SelectedGetOperatorId = 0,
                GetBalance = getBalance,
                SelectedGetBalanceId = 0,
            };

            //TempData["SuccessMsg"] = "DTH bill Processed successfully.";
            //return RedirectToAction("PayManHome", "PayMan");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> WaterBill(PayPayment payment = null)
        {
            TempData["SuccessMsg"] = "Water Bill Processed successfully.";
            return RedirectToAction("PayManHome", "PayMan");
        }




        [HttpGet]
        public async Task<IActionResult> Reacharge()
        {

            PayPayment payment = new PayPayment();
            var cir = new List<Circle>();
            var ope = new List<Operator>();
            payment.UserName = HttpContext.Session.GetString("UserName");
            payment.Email = HttpContext.Session.GetString("Email");

            //Get Circule

            // Define the base URL and the parameters
            string baseUrl = "http://Cyrusrecharge.in/api/GetOperator.aspx";
            string memberId = "AP595800";
            string pin = "~|MdopNV9~x!DdD";
            string method = "getcircle";

            // Construct the full URL with query parameters
            string requestUrl = $"{baseUrl}?memberid={memberId}&pin={pin}&Method={method}";

            // Create an instance of HttpClient
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Send a GET request to the URL
                    HttpResponseMessage response = await client.GetAsync(requestUrl);

                    // Ensure the response status code is successful
                    response.EnsureSuccessStatusCode();

                    // Read the response content as a string
                    string responseBody = await response.Content.ReadAsStringAsync();
                    List<ResponseData> response1 = JsonConvert.DeserializeObject<List<ResponseData>>(responseBody);
                    cir = response1[0].Data;

                    // Output the response
                    Console.WriteLine(responseBody);
                }
                catch (HttpRequestException e)
                {
                    // Handle any exceptions that occur during the request
                    Console.WriteLine($"Request error: {e.Message}");
                }

            }



            //Get Operator
            string method1 = "getoperator";

            // Construct the full URL with query parameters
            string requestUrl1 = $"{baseUrl}?memberid={memberId}&pin={pin}&Method={method1}";

            // Create an instance of HttpClient
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Send a GET request to the URL
                    HttpResponseMessage response = await client.GetAsync(requestUrl1);

                    // Ensure the response status code is successful
                    response.EnsureSuccessStatusCode();

                    // Read the response content as a string
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<List<ApiResponse>>(responseBody);

                    // Get the operators list from the deserialized data
                    ope = apiResponse[0].Data[0].Data;

                    // Output the response
                    Console.WriteLine(responseBody);
                }
                catch (HttpRequestException e)
                {
                    // Handle any exceptions that occur during the request
                    Console.WriteLine($"Request error: {e.Message}");
                }

            }


            var model = new Reacharge
            {
                Data = cir,
                operators = ope
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Reacharge(Reacharge payment)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var UserName = HttpContext.Session.GetString("UserName");
            var Email = HttpContext.Session.GetString("Email");
            var phone = HttpContext.Session.GetString("Phone");
            var userdetais = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
            var paymanComm = _context.payManGateWayMarigins.FirstOrDefault();
            LyraPaymentDetails lyraPaymentDetails = new LyraPaymentDetails();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Set the base address and headers
                    client.BaseAddress = new Uri("https://api.in.lyra.com");
                    client.DefaultRequestHeaders.Clear();
                    //client.DefaultRequestHeaders.Add("accept", "application/json");
                    //client.DefaultRequestHeaders.Add("Authorization", "Basic MTgwNTU5MzI6dGVzdHBhc3N3b3JkX0VVSElqdXZMeWplbTVGQnhpZVh4dHYzTUE3SXA1dmFkQXF1blVEMzJSOHdKaw==");

                    string username = "33738761";
                    string password = "prodpassword_swN2AIAi7sMIrXNXxjzq3pnZ8AmfjhEOgs54etLUNeWwQ";
                    // Combine username and password into a single string
                    string credentials = $"{username}:{password}";
                    // Base64 encode the credentials
                    string base64Credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
                    // Set the Authorization header
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

                    // Set other headers
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var trnsOrderId = "payman" + DateTime.Now.ToString("yyyymmddhhmmss");
                    var convertamount = Convert.ToInt32(payment.featchAmountDropdown);
                    // JSON data to be sent in the request body
                    var jsonData = new
                    {
                        orderId = trnsOrderId,
                        orderInfo = "PayMan payment",
                        currency = "INR",
                        amount = convertamount + "00",
                        customer = new
                        {
                            name = UserName,
                            emailId = Email,
                            phone = phone,
                        },
                        @return = new
                        {
                            method = "POST",
                            url = "https://paymanfintech.in/PayMan/ReachargeLayraStatus?trnsOrderId=" + trnsOrderId + "&payment=" + payment,
                            ///url = "https://localhost:44384/PayMan/ReachargeLayraStatus?trnsOrderId="+ trnsOrderId + "&payment=" + payment,
                            timeout = 600
                        }
                    };

                    // Convert JSON data to StringContent
                    var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(jsonData), Encoding.UTF8, "application/json");

                    // Send POST request
                    var response = await client.PostAsync("/pg/rest/v1/charge", content);
                    if (response.IsSuccessStatusCode)
                    {
                        // Read response
                        string responseString = await response.Content.ReadAsStringAsync();
                        lyraPaymentDetails = JsonConvert.DeserializeObject<LyraPaymentDetails>(responseString);
                        // Output the response
                        if (lyraPaymentDetails != null)
                        {
                            //TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                            //DateTime? dt = DateTime.Now;
                            //DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(dt.Value, istTimeZone);
                            var commission = (lyraPaymentDetails.Amount / 100) * userdetais.Margin / 100;
                            var distibutercommission = (lyraPaymentDetails.Amount / 100) * userdetais.DistributerMarigin / 100;
                            var paymancommission = (lyraPaymentDetails.Amount / 100) * paymanComm.Lyra / 100;
                            PayIn payIn = new PayIn();
                            payIn.OrderId = lyraPaymentDetails.OrderId;
                            payIn.OrderRefNumber = lyraPaymentDetails.Uuid;
                            payIn.Currency = lyraPaymentDetails.Currency;
                            payIn.UserId = userId;
                            payIn.MobileNumber = payment.MobileNumber;
                            payIn.Amount = lyraPaymentDetails.Amount / 100;
                            payIn.Sttaus = lyraPaymentDetails.Status;
                            payIn.Refound = lyraPaymentDetails.Refunded;
                            payIn.GateWay = 3;
                            payIn.CreatedDate = DateTime.Now;
                            payIn.PayInCommission = commission;
                            payIn.DistributerUserId = userdetais.DistributeruserId;
                            payIn.DistibuterCommission = distibutercommission;
                            payIn.PaymanCommission = paymancommission;
                            payIn.IssueBank = "";
                            Console.WriteLine(responseString);
                            _context.payIns.Add(payIn);
                            _context.SaveChanges();
                        }
                    }
                    else
                    {
                        return Json(new { Success = false, Message = "Verification failed", phonepeResponse = lyraPaymentDetails });

                    }
                }
            }
            catch (Exception e)
            {

            }
            // TempData["SuccessMsg"] = "Reacharge Processed successfully.";
            return Redirect(lyraPaymentDetails.PaymentLink);
        }

        [HttpPost]
        public async Task<IActionResult> ReachargeLayraStatus(string trnsOrderId = null, Reacharge payment = null)
        {
            try
            {
                var payinstatus = _context.payIns.Where(t => t.OrderId == trnsOrderId).FirstOrDefault();
                if (payinstatus != null)
                {
                    using (HttpClient client = new HttpClient())
                    {
                        // Set the base address and headers
                        client.BaseAddress = new Uri("https://api.in.lyra.com");
                        client.DefaultRequestHeaders.Clear();
                        //client.DefaultRequestHeaders.Add("accept", "application/json");
                        //client.DefaultRequestHeaders.Add("Authorization", "Basic MTgwNTU5MzI6dGVzdHBhc3N3b3JkX0VVSElqdXZMeWplbTVGQnhpZVh4dHYzTUE3SXA1dmFkQXF1blVEMzJSOHdKaw==");

                        string username = "33738761";
                        string password = "prodpassword_swN2AIAi7sMIrXNXxjzq3pnZ8AmfjhEOgs54etLUNeWwQ";
                        // Combine username and password into a single string
                        string credentials = $"{username}:{password}";
                        // Base64 encode the credentials
                        string base64Credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
                        // Set the Authorization header
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

                        // Set other headers
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                        // Send POST request
                        var response = await client.GetAsync("/pg/rest/v1/charge/" + payinstatus.OrderRefNumber);
                        if (response.IsSuccessStatusCode)
                        {
                            string responseString = await response.Content.ReadAsStringAsync();
                            var lyraPaymentDetails = JsonConvert.DeserializeObject<LyraPaymentDetails>(responseString);

                            if (lyraPaymentDetails.Status == "PAID")
                            {
                                var rs = GetReharge(payment);
                                TempData["PaymentSucess"] = "You have " + lyraPaymentDetails.Amount / 100 + " recharged Successfully";
                            }
                            else
                            {
                                TempData["PaymentSucess"] = "recharge Failed !";
                            }


                            if (lyraPaymentDetails != null)
                            {
                                payinstatus.Sttaus = lyraPaymentDetails.Status;
                                _context.payIns.Update(payinstatus);
                                _context.SaveChanges();
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {

            }

            return RedirectToAction("PayManHome", "PayMan");
        }


        public async Task<JsonResult> GetOperatorPlans(string OperatorCode, string CircleCode, string mobileNumber)
        {
            var jjhsjh = new List<Circle>();

            string baseUrl = "http://Cyrusrecharge.in/api/GetOperator.aspx";
            string memberId = "AP595800";
            string pin = "~|MdopNV9~x!DdD";
            string method = "getcircle";

            // Construct the full URL with query parameters
            string requestUrl = $"{baseUrl}?memberid={memberId}&pin={pin}&Method={method}";

            // Create an instance of HttpClient
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Send a GET request to the URL
                    HttpResponseMessage response = await client.GetAsync(requestUrl);

                    // Ensure the response status code is successful
                    response.EnsureSuccessStatusCode();

                    // Read the response content as a string
                    string responseBody = await response.Content.ReadAsStringAsync();
                    List<ResponseData> response1 = JsonConvert.DeserializeObject<List<ResponseData>>(responseBody);
                    jjhsjh = response1[0].Data;

                    // Output the response
                    Console.WriteLine(responseBody);
                }
                catch (HttpRequestException e)
                {
                    // Handle any exceptions that occur during the request
                    Console.WriteLine($"Request error: {e.Message}");
                }

            }









            //string baseUrl = "https://cyrusrecharge.in/API/CyrusPlanFatchAPI.aspx";
            //string APIID = "AP595800";
            //string PASSWORD = "~|MdopNV9~x!DdD";
            //string Operator_Code = OperatorCode;
            //string Circle_Code = CircleCode;
            //string MobileNumber = mobileNumber;

            //// Construct the full URL with query parameters
            //string requestUrl = $"{baseUrl}?APIID={APIID}&PASSWORD={PASSWORD}&Operator_Code={Operator_Code}&Circle_Code={Circle_Code}&MobileNumber={MobileNumber}&data=ALL";

            // Create an instance of HttpClient
            //using (HttpClient client = new HttpClient())
            //{
            //    try
            //    {
            //        // Send a GET request to the URL
            //        HttpResponseMessage response = await client.GetAsync(requestUrl);

            //        // Ensure the response status code is successful
            //        response.EnsureSuccessStatusCode();

            //        // Read the response content as a string
            //        string responseBody = await response.Content.ReadAsStringAsync();
            //        List<ResponseData> response1 = JsonConvert.DeserializeObject<List<ResponseData>>(responseBody);
            //       // cir = response1[0].Data;

            //        // Output the response
            //        Console.WriteLine(responseBody);
            //    }
            //    catch (HttpRequestException e)
            //    {
            //        // Handle any exceptions that occur during the request
            //        Console.WriteLine($"Request error: {e.Message}");
            //    }

            //}

            return Json(new { sucess = "sucess", result = jjhsjh });
        }
        public async Task<string> GetReharge(Reacharge payment)
        {
            string baseUrl = "https://cyrusrecharge.in/services_cyapi/recharge_cyapi.aspx";
            string memberId = "AP595800";
            string pin = "~|MdopNV9~x!DdD";
            string number = "9849800697";
            string operatorCode = payment.operatorDropdown;
            string circle = payment.circleDropdown;
            string amount = "10";
            string usertx = "payman" + DateTime.Now.ToString("yyyymmddhhmmss");
            string format = "json";

            // Construct the full URL with query parameters
            string requestUrl = $"{baseUrl}?memberid={memberId}&pin={pin}&number={number}&operator={operatorCode}&circle={circle}&amount={amount}&usertx={usertx}&format={format}";

            // Create an instance of HttpClient
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Send a GET request to the URL
                    HttpResponseMessage response = await client.GetAsync(requestUrl);

                    // Ensure the response status code is successful
                    response.EnsureSuccessStatusCode();

                    // Read the response content as a string
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Output the response
                    Console.WriteLine(responseBody);
                }
                catch (HttpRequestException e)
                {
                    // Handle any exceptions that occur during the request
                    Console.WriteLine($"Request error: {e.Message}");
                }
            }
            return "sucess";
        }

        [HttpPost]
        public async Task<JsonResult> BankdetailsVerify(Guid Id)
        {

            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);

            if (user == null || user.App == false)
            {
                var staus = CashfreeBankAccountVerifaction(Id);
                if (!await staus)
                {
                    var objResult = _context.beneficiaryAccounts.Where(t => t.Id == Id).FirstOrDefault();
                    string cjcnc = "";
                    if (objResult != null)
                    {
                        Vendor exampleVendor = Vendor.GetExample1(objResult);
                        var jsonString1 = Newtonsoft.Json.JsonConvert.SerializeObject(exampleVendor);
                        string text = jsonString1;
                        string key = "snIQCqiMiBa8KkkafkQ1K+U3IGqlqwQKQZMEnnit8FQ="; //prod
                        ///string key = "I9GEKefOec+jaZuUUV+/ym5yViDfeK4JAn0HDlqPiUc=";
                        byte[] iv = Encoding.ASCII.GetBytes("0123456789abcdef");
                        int size = 16;
                        int pad = size - (text.Length % size);
                        string padText = text + new string(Convert.ToChar(pad), pad);

                        using (Aes aesAlg = Aes.Create())
                        {
                            aesAlg.Key = Convert.FromBase64String(key);
                            aesAlg.IV = iv;
                            aesAlg.Mode = CipherMode.CBC;
                            aesAlg.Padding = PaddingMode.Zeros;

                            System.Security.Cryptography.ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                            byte[] encrypted;

                            using (var msEncrypt = new System.IO.MemoryStream())
                            {
                                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                                {
                                    using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                                    {
                                        swEncrypt.Write(padText);
                                    }
                                    encrypted = msEncrypt.ToArray();
                                }
                            }

                            cjcnc = Convert.ToBase64String(encrypted);
                        }

                        //Api call

                        ServicePointManager.Expect100Continue = false;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                        var PhonePeGatewayURL = "https://remittance.safexpay.com"; //prod   
                                                                                   //var PhonePeGatewayURL = "https://neodev2.safexpay.com";

                        var httpClient = new HttpClient();
                        var uri = new Uri($"{PhonePeGatewayURL}/agWalletAPI/Contact/createContactAPI");

                        string jjddf = "AGEN3250012853"; // Prod
                                                         //string jjddf = "AGEN8080091116";

                        var jsonBody = $"{{\"payload\":\"{cjcnc}\",\"uId\":\"{jjddf}\"}}";
                        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                        RootObject objRoot = new RootObject();
                        var response = await httpClient.PostAsync(uri, content);
                        if (response.IsSuccessStatusCode)
                        {
                            response.EnsureSuccessStatusCode();

                            // Read and deserialize the response content
                            var responseContent = await response.Content.ReadAsStringAsync();
                            ErrorModel nnj = new ErrorModel();
                            nnj.agId = "Benficary verification";
                            nnj.payload = "";
                            nnj.reqTime = "";
                            nnj.requestId = jsonString1;
                            nnj.respTime = responseContent;
                            nnj.uid = response.ReasonPhrase;
                            nnj.jsonBody = jsonBody;
                            nnj.statuscode = response.IsSuccessStatusCode;
                            _context.errorModels.Add(nnj);
                            _context.SaveChanges();
                            var PymentStatusDetails = JsonConvert.DeserializeObject<BeneficiaryAddResponse>(responseContent);

                            if (PymentStatusDetails.responseData != null)
                            {
                                string crypt = PymentStatusDetails.responseData;
                                string result = crypt.Trim('"').Replace("\\u003d", "=");//Regex.Replace(crypt, pattern, "");

                                byte[] cryptBytes = Convert.FromBase64String(result);
                                string text1 = "";
                                using (Aes aesAlg = Aes.Create())
                                {
                                    aesAlg.Key = Convert.FromBase64String(key);
                                    aesAlg.IV = iv;
                                    aesAlg.Mode = CipherMode.CBC;
                                    aesAlg.Padding = PaddingMode.Zeros;

                                    System.Security.Cryptography.ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                                    byte[] decrypted;

                                    using (var msDecrypt = new System.IO.MemoryStream(cryptBytes))
                                    {
                                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                                        {
                                            using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                                            {
                                                string padText1 = srDecrypt.ReadToEnd();

                                                int pad1 = Convert.ToInt32(padText1[padText1.Length - 1]);

                                                // if (pad > padText.Length)
                                                ///return "Error";

                                                text1 = padText1.Substring(0, padText1.Length - pad1);
                                                //return text;
                                                objRoot = JsonConvert.DeserializeObject<RootObject>(text1);
                                                //   Staus = objRoot.payOutBean.bankStatus;
                                                // TempData["PayOutSucess"] = "PayOut transection Sucess!";
                                            }
                                        }
                                    }
                                }
                                if (objRoot != null)
                                {
                                    objResult.ContactId = objRoot.contact_id;
                                    objResult.ContactName = objRoot.beneficiaryAPIResponseBean.Select(t => t.account_name).FirstOrDefault(); ;
                                    objResult.BeneId = objRoot.beneficiaryAPIResponseBean.Select(t => t.bene_id).FirstOrDefault();
                                    objResult.VerficationFlag = objRoot.beneficiaryAPIResponseBean.Select(t => t.verficationFlag).FirstOrDefault();
                                    objResult.AccountNo = objRoot.beneficiaryAPIResponseBean.Select(t => t.account_no).FirstOrDefault();
                                    objResult.IfscCode = objRoot.beneficiaryAPIResponseBean.Select(t => t.ifsc_code).FirstOrDefault();
                                    objResult.Date = text1;
                                    if (objRoot.beneficiaryAPIResponseBean.Select(t => t.verficationFlag).FirstOrDefault() == "Y")
                                    {
                                        objResult.VerificationComm = 5;
                                    }
                                    _context.beneficiaryAccounts.Update(objResult);
                                    _context.SaveChanges();
                                }
                                return Json(new { Success = true });
                            }
                            else
                            {
                                return Json(new { Success = false });
                            }
                        }
                    }
                    else
                    {
                        return Json(new { Success = false });
                    }
                }

                return Json(new { Success = true });
            }
            else
            {
                var status = await CashfreeBankAccountVerifactionAPP(Id);

                if (status)
                {
                    return Json(new { Success = true });
                }
                else
                {
                    return Json(new { Success = false });
                }
            }
            return Json(new { Success = true });
        }

        public async Task<bool> CashfreeBankAccountVerifaction(Guid Id)
        {
            var objResult = _context.beneficiaryAccounts.Where(t => t.Id == Id).FirstOrDefault();
            var clientId = "CF678492CQOFTSG8OI4S73DVFVN0";
            var clientSecret = "cfsk_ma_prod_1a60ece5af3976c5258d7afd8aa0a8eb_063c29b6";

            bool status = false;

            using (HttpClient client = new HttpClient())
            {
                var requestUrl = "https://api.cashfree.com/verification/bank-account/sync";
                var requestData = new
                {
                    bank_account = objResult.AccountNo,
                    ifsc = objResult.IfscCode,
                    name = objResult.ContactName,
                    phone = objResult.MobileNumber
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-client-id", clientId);
                client.DefaultRequestHeaders.Add("x-client-secret", clientSecret);

                HttpResponseMessage response = await client.PostAsync(requestUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    var bankResponse = JsonConvert.DeserializeObject<CashfreeBankAccountVerificationResponse>(responseData);
                    if (bankResponse != null)
                    {
                        objResult.ContactId = bankResponse.reference_id.ToString();
                        objResult.ContactName = bankResponse.name_at_bank;
                        objResult.BeneId = bankResponse.reference_id.ToString();
                        objResult.VerficationFlag = bankResponse.account_status == "VALID" ? "Y" : "N";
                        objResult.Date = "Cashfree Account verification";
                        if (bankResponse.account_status == "VALID")
                        {
                            objResult.VerificationComm = 5;
                        }
                        _context.beneficiaryAccounts.Update(objResult);
                        _context.SaveChanges();
                        if (bankResponse.account_status == "VALID")
                        {
                            status = true;
                        }
                    }
                    Console.WriteLine("Response: " + responseData);
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                }
            }

            return status;
        }

        public async Task<bool> CashfreeBankAccountVerifactionAPP(Guid Id)
        {
            var objResult = await _context.payManBeneficiaryAccounts.FirstOrDefaultAsync(t => t.Id == Id);
            if (objResult == null)
                return false;

            var clientId = "CF678492CQOFTSG8OI4S73DVFVN0";
            var clientSecret = "cfsk_ma_prod_1a60ece5af3976c5258d7afd8aa0a8eb_063c29b6";

            bool status = false;

            using (HttpClient client = new HttpClient())
            {
                var requestUrl = "https://api.cashfree.com/verification/bank-account/sync";
                var requestData = new
                {
                    bank_account = objResult.AccountNo,
                    ifsc = objResult.IfscCode,
                    name = objResult.ContactName,
                    phone = objResult.MobileNumber
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-client-id", clientId);
                client.DefaultRequestHeaders.Add("x-client-secret", clientSecret);

                try
                {
                    var response = await client.PostAsync(requestUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();
                        var bankResponse = JsonConvert.DeserializeObject<CashfreeBankAccountVerificationResponse>(responseData);

                        if (bankResponse != null)
                        {
                            objResult.ContactId = bankResponse.reference_id.ToString();
                            objResult.ContactName = bankResponse.name_at_bank;
                            objResult.BeneId = bankResponse.reference_id.ToString();
                            objResult.VerficationFlag = bankResponse.account_status == "VALID" ? "Y" : "N";
                            objResult.VerificationComm = bankResponse.account_status == "VALID" ? 5 : 0;

                            _context.payManBeneficiaryAccounts.Update(objResult);
                            await _context.SaveChangesAsync();

                            status = bankResponse.account_status == "VALID";
                        }
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        // optionally log the error
                    }
                }
                catch (Exception ex)
                {
                    // optionally log the exception
                }
            }

            return status;
        }

        [HttpPost]
        public async Task<JsonResult> Paywithben(Guid Id, string amount)
        {

            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);
            if (user == null || user.App == false)
            {
                var userId = HttpContext.Session.GetString("UserId");
                var userdetais = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { Success = false, objRoot = new { staus = "", OrderrefNo = "" } });
                }
                var objResult = _context.beneficiaryAccounts.Where(t => t.Id == Id).FirstOrDefault();
                var objPayOutConfic = _context.payOutConfics.FirstOrDefault();

                string staus = string.Empty;
                string OrderrefNo = string.Empty;
                PayWithBeanResponseRootObject objRoot = new PayWithBeanResponseRootObject();
                PInelabsPayoutResponse pInelabsPayoutResponse = new PInelabsPayoutResponse();

                if (objResult != null)
                {

                    // Get first letter of the name
                    string firstLetter = userdetais.Name.Substring(0, 1);

                    // Get last 4 digits of the phone number
                    string lastFourDigits = userdetais.Phone.Substring(userdetais.Phone.Length - 4);

                    // Combine the results
                    string result = firstLetter + lastFourDigits;



                    JWT_Generator generator = new JWT_Generator();
                    var test = generator.GenerateToken();
                    var apiUrl = "https://api.pluralonline.com/payouts/v2/payments/banks"; // Replace with your API URL
                    var bearerToken = test; // Replace with your actual bearer token
                    ///var jsonContent = "{\"key1\":\"value1\", \"key2\":\"value2\"}"; // Replace with your actual JSON content

                    PInelabsPayoutAmount amount1 = new PInelabsPayoutAmount
                    {
                        currency = "INR",
                        value = Convert.ToInt32(amount + "00")
                    };

                    // Instantiate the TransactionRequest class and assign values
                    PInelabsPayoutTransactionRequest request = new PInelabsPayoutTransactionRequest
                    {
                        clientReferenceId = Guid.NewGuid().ToString(), // Generating a new GUID
                        payeeName = objResult.ContactName.Replace(".", ""),
                        accountNumber = objResult.AccountNo,
                        branchCode = objResult.IfscCode,
                        email = objResult.EmailId,
                        phone = objResult.MobileNumber,
                        amount = amount1,
                        mode = "IMPS",
                        remarks = result
                    };
                    var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(request);

                    using (var client = new HttpClient())
                    {
                        // Set the base address for the client
                        client.BaseAddress = new Uri(apiUrl);

                        // Add the bearer token to the Authorization header
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                        // Create the StringContent object with the JSON content
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        try
                        {
                            // Send a POST request to the specified endpoint with the JSON content
                            HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                            // Check if the response was successful
                            if (response.IsSuccessStatusCode)
                            {
                                // Read and process the response content
                                string responseContent = await response.Content.ReadAsStringAsync();
                                pInelabsPayoutResponse = JsonConvert.DeserializeObject<PInelabsPayoutResponse>(responseContent);

                                var dbdh = PinelabsStatus(bearerToken, "https://api.pluralonline.com/payouts/v2/payments", pInelabsPayoutResponse.paymentReferenceId).Result;
                                //Console.WriteLine("Response Content: " + responseContent);
                                PayOutTransectionDetails obj = new PayOutTransectionDetails();
                                var aamount = pInelabsPayoutResponse.amount.value / 100;
                                var bnsd = DateTime.Now;
                                obj.payoutId = pInelabsPayoutResponse.clientReferenceId;
                                obj.mobileNo = objResult.MobileNumber;
                                obj.txnAmount = aamount.ToString();
                                obj.accountNo = pInelabsPayoutResponse.accountNumber.ToString();
                                obj.ifscCode = objResult.IfscCode;
                                obj.customerId = "PineLab";
                                obj.userId = userId;
                                obj.beneId = objResult.BeneId;
                                obj.accountHolderName = pInelabsPayoutResponse.payeeName;
                                obj.aggregatorId = "";
                                obj.txnStatus = dbdh.ToString();
                                obj.bankStatus = dbdh.ToString();
                                obj.spkRefNo = "";
                                obj.statusCode = "";
                                obj.statusDesc = "";
                                obj.orderRefNo = pInelabsPayoutResponse.paymentReferenceId;
                                obj.customerName = pInelabsPayoutResponse.payeeName;
                                obj.aggregtorName = "";
                                obj.emailId = pInelabsPayoutResponse.email;
                                obj.txnType = "IMPS";
                                obj.PayoutCommission = 15;
                                obj.CreatedDate = bnsd;

                                _context.payOutTransectionDetails.Add(obj);
                                _context.SaveChanges();
                                TempData["PayOutSucess"] = "PayOut transection Sucess!";
                                if (dbdh.ToString() == "PROCESSED" || dbdh.ToString() == "PENDING")
                                {
                                    staus = "Sucess";
                                    OrderrefNo = pInelabsPayoutResponse.paymentReferenceId;
                                }
                                else
                                {
                                    staus = dbdh.ToString();
                                    OrderrefNo = "";
                                }
                            }
                            else
                            {
                                ErrorModel nn = new ErrorModel();
                                nn.agId = "PineLab pay out";
                                nn.payload = "";
                                nn.reqTime = "";
                                nn.requestId = jsonContent;
                                nn.respTime = "";
                                nn.uid = response.ReasonPhrase;
                                nn.jsonBody = "";
                                nn.statuscode = response.IsSuccessStatusCode;
                                _context.errorModels.Add(nn);
                                _context.SaveChanges();
                                TempData["PayOutSucess"] = "PayOut transection failed!";
                                return Json(new { Success = false, objRoot = new { staus = staus, OrderrefNo = OrderrefNo } });
                            }
                        }
                        catch (Exception ex)
                        {
                            TempData["PayOutSucess"] = "PayOut transection failed!";
                            return Json(new { Success = false, objRoot = new { staus = staus, OrderrefNo = OrderrefNo } });
                        }
                    }


                }

                return Json(new { Success = true, objRoot = new { staus = staus, OrderrefNo = OrderrefNo } });
            }
            else
            {
               

                if (Convert.ToInt32(amount) <= 0)
                {
                    return Json(new { Success = false, objRoot = new { message = "InvalidAmount", OrderRefNo = "" } });
                }

                var amount1 = Convert.ToInt32(Convert.ToInt32(amount) * 100);

                var userDetails = await _context.payManUsers.FirstOrDefaultAsync(t => t.Phone == appPhone);
                if (userDetails == null)
                {
                    return Json(new { Success = false, objRoot = new { message = "UserNotFound", OrderRefNo = "" } });
                }

                var beneficiary = await _context.payManBeneficiaryAccounts.FirstOrDefaultAsync(t => t.Id == Id);
                if (beneficiary == null)
                {
                    return Json(new { Success = false, objRoot = new { message = "BeneficiaryNotFound", OrderRefNo = "" } });
                }

                var payoutConfig = await _context.payOutConfics.FirstOrDefaultAsync();

                string status = "";
                string orderRefNo = "";

                try
                {
                    // Get first letter of the name
                    string firstLetter = userDetails.FirstName.Substring(0, 1);

                    // Get last 4 digits of the phone number
                    string lastFourDigits = userDetails.Phone.Substring(userDetails.Phone.Length - 4);

                    // Combine the results
                    string result = firstLetter + lastFourDigits;


                    var token = new JWT_Generator().GenerateToken();
                    var apiUrl = "https://api.pluralonline.com/payouts/v2/payments/banks";

                    var payoutRequest = new PInelabsPayoutTransactionRequest
                    {
                        clientReferenceId = Guid.NewGuid().ToString(),
                        payeeName = beneficiary.ContactName?.Replace(".", "") ?? "Unknown",
                        accountNumber = beneficiary.AccountNo,
                        branchCode = beneficiary.IfscCode,
                        email = beneficiary.EmailId,
                        phone = beneficiary.MobileNumber,
                        amount = new PInelabsPayoutAmount
                        {
                            currency = "INR",
                            value = Convert.ToInt32(amount + "00")
                        },
                        mode = "IMPS",
                        remarks = result
                    };

                    var jsonContent = JsonConvert.SerializeObject(payoutRequest);

                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var response = await client.PostAsync(apiUrl, new StringContent(jsonContent, Encoding.UTF8, "application/json"));

                    if (!response.IsSuccessStatusCode)
                    {
                        return Json(new { Success = false, objRoot = new { status = "Failed", OrderRefNo = "" } });
                    }

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var payoutResponse = JsonConvert.DeserializeObject<PInelabsPayoutResponse>(responseBody);



                    DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                    var transaction = new PayManPayOut
                    {
                        UserId = userDetails.Id,
                        UserPhone = appPhone,
                        PayOutId = payoutResponse.clientReferenceId,
                        RefId = payoutResponse.paymentReferenceId,
                        AccountHolderName = payoutResponse.payeeName ?? "Unknown",
                        AccountNo = payoutResponse.accountNumber,
                        IfscCode = beneficiary.IfscCode,
                        Amount = payoutResponse.amount.value / 100.0m,
                        PayoutCommission = 15,
                        BeneId = beneficiary.BeneId,
                        PayOutType = "Pine Labs",
                        TxnType = "IMPS",
                        Email = userDetails.Email,
                        Status = true,
                        Result = "Sucess",
                        DateTime = istDateTime,
                        Device = "Web"
                    };

                    _context.payManPayOuts.Add(transaction);
                    await _context.SaveChangesAsync();

                    var userWalletAmount = await GetUserWalletAmount(appPhone);

                    var payInHistory = new PayManHistory
                    {
                        UserId = userDetails.Id,
                        UserPhone = appPhone,
                        TxnId = payoutResponse.paymentReferenceId,
                        Amount = payoutResponse.amount.value / 100.0m,
                        CardNumber = payoutResponse.accountNumber,
                        Mode = "PayOut",
                        Status = status == "Success",
                        Created = istDateTime,
                        AvlBalance = Convert.ToDecimal(userWalletAmount),
                        PayInId = transaction.Id
                    };
                    _context.payManHistories.Add(payInHistory);
                    await _context.SaveChangesAsync();


                    // Retry logic: check status up to 5 times
                    string payoutStatus = "";
                    int retryCount = 0;
                    int maxRetries = 3;

                    while (retryCount < maxRetries)
                    {
                        payoutStatus = await PinelabsStatus(token, "https://api.pluralonline.com/payouts/v2/payments", payoutResponse.paymentReferenceId);

                        if (payoutStatus == "PROCESSED")
                        {
                            status = "Success";
                            orderRefNo = payoutResponse.paymentReferenceId;
                            break;
                        }
                        else if (payoutStatus == "FAILED" || payoutStatus == "REJECTED")
                        {
                            status = payoutStatus;
                            break;
                        }

                        await Task.Delay(3000); // 3-second wait
                        retryCount++;
                    }

                    if (status != "Success")
                    {
                        status = payoutStatus;
                    }

                    var getPayOut = await _context.payManPayOuts
                   .FirstOrDefaultAsync(t => t.RefId == payoutResponse.paymentReferenceId);

                    var getPayOuthistorie = await _context.payManHistories
                        .FirstOrDefaultAsync(t => t.TxnId == payoutResponse.paymentReferenceId);

                    if (getPayOut != null)
                    {
                        getPayOut.Result = payoutStatus;
                        //getPayOut.Status = payoutStatus == "PROCESSED";

                        _context.payManPayOuts.Update(getPayOut);
                    }

                    //if (getPayOuthistorie != null)
                    //{
                    //    getPayOuthistorie.Status = payoutStatus == "PROCESSED";
                    //    _context.payManHistories.Update(getPayOuthistorie);
                    //}

                    // Save only if any changes are made
                    if (getPayOut != null || getPayOuthistorie != null)
                    {
                        await _context.SaveChangesAsync();
                    }


                    return Json(new { Success = true, objRoot = new { staus = status, OrderrefNo = payoutResponse.paymentReferenceId } });

                }
                catch (Exception ex)
                {
                    // Log error if needed
                    return Json(new { Success = false, objRoot = new { message = "Error", OrderRefNo = "" } });
                }
            }              
        }

        [HttpGet]
        public async Task<IActionResult> PennyDrop()
        {

            /// var bnnn = "\"\\\"CFX8iaECQj5blvy66MVVfBcZN0Ehq7L8DBOW57aRBvyU8rByUYZ65ypQztII7xw5Joo38L+VStQ8I7EpY4YiBRCSTCFDRHpDnoaDdylA3y5RzoYAkToHh6jyDqFrg16pI/7Hw1BT2HdjC5EShsf3ObfTpH17UXMXLAmLrayIDgctOkunPdmKa98HYP/rJWI+RKo1NY78KIj/oetQxocyvJQwWDkaR7unrxGTyUyIwTQgj/+PXNKduA9RyAzqu2NJcHNq4k+9tpG39SPGE5354wTrFGBuh9w3jUYzBblAUG0IMbAOrczTV9ua2ILZC+1hX696tjWtHRkB0qpw18PXHO2MtKAyk+K498eqizEqfRzVVM4QYJ9vi3POE1v3j5MCzocj4qKmTvYbE8+sHRzOngOyXwETX1mbVinInaG/hTcO37zusQyTHCedDZdvav9xQOhW1UkshSJMuCZ3g4t9wX97zzAuwM28nGSugTtU5j578f4fN5nP4bZN17b+q1De\\\"\"";
            string pattern = @"^[^\w\s]+|[^\w\s]+$";

            // Remove the special characters using Regex.Replace

            string cjcnc = "";
            Vendor exampleVendor = Vendor.GetExample();
            var jsonString1 = Newtonsoft.Json.JsonConvert.SerializeObject(exampleVendor);
            string text = jsonString1; //"{\r\n  \"contact_type\": \"Vendor\",\r\n  \"name\": \"Prashant\",\r\n  \"org_name\": \"Safexpay\",\r\n  \"email_id\": \"prashantt@safexpay.com\",\r\n  \"mobile_no\": \"7859939009\",\r\n  \"me_id\": \"AGEN8080091116\",\r\n  \"banks\": [\r\n    {\r\n      \"account_no\": \"35907708703\",\r\n      \"ifsc_code\": \"SBIN0016389\",\r\n      \"category_type\": \"BANK\",\r\n      \"account_holder_name\": \"Prashant\",\r\n      \"code\": \"\"\r\n    },\r\n    {\r\n      \"account_no\": \"99999999\",\r\n      \"ifsc_code\": \"BARB0KONGAO\",\r\n      \"category_type\": \"BANK\",\r\n      \"account_holder_name\": \"Shrav\",\r\n      \"code\": \"\"\r\n    }\r\n  ],\r\n  \"pan_no\": \"ATCPT2378M\",\r\n  \"registration_type\": \"Consumer\",\r\n  \"gst_no\": \"\",\r\n  \"notes\": \"testing\"\r\n}";
            // string key = "WqV81Myi0evx587ixP37dOAlV8H26T6bh3HCgKiG328=";


            string key = "snIQCqiMiBa8KkkafkQ1K+U3IGqlqwQKQZMEnnit8FQ="; //prod
            //string key = "I9GEKefOec+jaZuUUV+/ym5yViDfeK4JAn0HDlqPiUc=";

            byte[] iv = Encoding.ASCII.GetBytes("0123456789abcdef");
            int size = 16;
            int pad = size - (text.Length % size);
            string padText = text + new string(Convert.ToChar(pad), pad);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Convert.FromBase64String(key);
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.Zeros;

                System.Security.Cryptography.ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                byte[] encrypted;

                using (var msEncrypt = new System.IO.MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(padText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }

                cjcnc = Convert.ToBase64String(encrypted);
            }

            //Api call

            ServicePointManager.Expect100Continue = false;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //var PhonePeGatewayURL = "https://api-preprod.phonepe.com/apis/pg-sandbox";
            var PhonePeGatewayURL = "https://remittance.safexpay.com"; //prod   
                                                                       //var PhonePeGatewayURL = "https://neodev2.safexpay.com";

            var httpClient = new HttpClient();
            var uri = new Uri($"{PhonePeGatewayURL}/agWalletAPI/Contact/createContactAPI");

            // Add headers
            //httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            //httpClient.DefaultRequestHeaders.Add("X-VERIFY", phonePePayment.X_VERIFY);

            string jjddf = "AGEN3250012853"; // Prod
            //string jjddf = "AGEN8080091116";
            // Create JSON request body
            var jsonBody = $"{{\"payload\":\"{cjcnc}\",\"uId\":\"{jjddf}\"}}";
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            RootObject objRoot = new RootObject();
            var response = await httpClient.PostAsync(uri, content);
            if (response.IsSuccessStatusCode)
            {
                response.EnsureSuccessStatusCode();

                // Read and deserialize the response content
                var responseContent = await response.Content.ReadAsStringAsync();
                var PymentStatusDetails = JsonConvert.DeserializeObject<BeneficiaryAddResponse>(responseContent);

                string crypt = PymentStatusDetails.responseData;
                //string key = "WqV81Myi0evx587ixP37dOAlV8H26T6bh3HCgKiG328=";
                //string inputStr = Encoding.UTF8.GetString(Convert.FromBase64String(crypt));
                // crypt = crypt.Replace('-', '+').Replace('_', '/').PadRight(4 * ((crypt.Length + 3) / 4), '=');
                //byte[] iv = Encoding.ASCII.GetBytes("0123456789abcdef");
                string result = crypt.Trim('"').Replace("\\u003d", "=");//Regex.Replace(crypt, pattern, "");

                byte[] cryptBytes = Convert.FromBase64String(result);
                string text1 = "";
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Convert.FromBase64String(key);
                    aesAlg.IV = iv;
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.Zeros;

                    System.Security.Cryptography.ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    byte[] decrypted;

                    using (var msDecrypt = new System.IO.MemoryStream(cryptBytes))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                            {
                                string padText1 = srDecrypt.ReadToEnd();

                                int pad1 = Convert.ToInt32(padText1[padText1.Length - 1]);

                                // if (pad > padText.Length)
                                ///return "Error";

                                text1 = padText1.Substring(0, padText1.Length - pad1);
                                //return text;
                                objRoot = JsonConvert.DeserializeObject<RootObject>(text1);
                                //   Staus = objRoot.payOutBean.bankStatus;
                                TempData["PayOutSucess"] = "PayOut transection Sucess!";
                            }
                        }
                    }
                }
                List<BeneficiaryAccounts> beneficiaryAccounts = new List<BeneficiaryAccounts>();
                if (objRoot.beneficiaryAPIResponseBean.Count > 0)
                {
                    foreach (var item in objRoot.beneficiaryAPIResponseBean)
                    {
                        BeneficiaryAccounts obj = new BeneficiaryAccounts();
                        obj.ContactId = objRoot.contact_id;
                        obj.ContactName = objRoot.contact_name;
                        obj.Date = text1;
                        obj.BeneId = item.bene_id;
                        obj.AccountNo = item.account_no;
                        obj.IfscCode = item.ifsc_code;
                        obj.VerficationFlag = item.verficationFlag;
                        obj.Page = item.page;
                        obj.Size = item.size;
                        beneficiaryAccounts.Add(obj);
                    }
                    _context.beneficiaryAccounts.AddRange(beneficiaryAccounts);
                    _context.SaveChanges();
                }
            }
            return RedirectToAction("PayManHome", "PayMan");
        }

        [HttpGet]
        public async Task<IActionResult> PayOut()
        {
            PayOutBeanRequest model = new PayOutBeanRequest();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PayOut(PayOutBeanRequest rootRequest)
        {
            string Staus = string.Empty;

            RootRequest data = new RootRequest
            {
                header = new HeaderRequest
                {
                    operatingSystem = "WEB",
                    sessionId = "AGEN3250012853", //prod
                                                  // sessionId = "AGEN5500134316",
                    version = "1.0.0"
                },
                userInfo = new MVC6Crud.Models.UserInfo { },
                transaction = new TransactionRequest
                {
                    requestType = "WTW",
                    requestSubType = "PWTB",
                    tranCode = 0,
                    txnAmt = 0.0,
                    id = "AGEN3250012853", //prod
                                           // id= "AGEN5500134316",
                    surChargeAmount = 0.0,
                    txnCode = 0,
                    userType = 0
                },
                payOutBean = new PayOutBeanRequest
                {
                    mobileNo = rootRequest.mobileNo,
                    txnAmount = rootRequest.txnAmount,
                    accountNo = rootRequest.accountNo,
                    ifscCode = rootRequest.ifscCode,
                    bankName = rootRequest.bankName,
                    accountHolderName = rootRequest.accountHolderName,
                    txnType = rootRequest.txnType,
                    accountType = rootRequest.accountType,
                    emailId = rootRequest.emailId,
                    orderRefNo = "payman" + DateTime.Now.ToString("yyyymmddhhmmss"),
                    count = 0
                }
            };
            var username = HttpContext.Session.GetString("UserName");

            Root objRoot = new Root();
            //Encript
            string cjcnc = "";

            var jsonString1 = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            string text = jsonString1;
            //string text = "{\r\n\t\"contact_type\": \"Vendor\",\r\n\t\"name\": \"Prashant\",\r\n\t\"org_name\": \"Safexpay\",\r\n\t\"email_id\": \"prashantt@safexpay.com\",\r\n\t\"mobile_no\": \"8879944117\",\r\n\t\"me_id\": \"AGEN2010106153\",\r\n\t\"banks\": [\r\n\t\t{\r\n\t\t\t\"account_no\": \"35927808744\",\r\n\t\t\t\"ifsc_code\": \"SBIN0016389\",\r\n\t\t\t\"category_type\": \"BANK\",\r\n\t\t\t\"account_holder_name\": \"Prashant\",\r\n\t\t\t\"code\": \"\"\r\n\t\t},\r\n\t\t{\r\n\t\t\t\"account_no\": \"99999999\",\r\n\t\t\t\"ifsc_code\": \"BARB0KONGAO\",\r\n\t\t\t\"category_type\": \"BANK\",\r\n\t\t\t\"account_holder_name\": \"Shrav\",\r\n\t\t\t\"code\": \"\"\r\n\t\t}\r\n\t],\r\n\t\"pan_no\": \"ATCPT2378M\",\r\n\t\"registration_type\": \"Consumer\",\r\n\t\"gst_no\": \"\",\r\n\t\"notes\": \"testing\"\r\n}";
            // string key = "WqV81Myi0evx587ixP37dOAlV8H26T6bh3HCgKiG328=";
            string key = "snIQCqiMiBa8KkkafkQ1K+U3IGqlqwQKQZMEnnit8FQ="; //prod

            byte[] iv = Encoding.ASCII.GetBytes("0123456789abcdef");
            int size = 16;
            int pad = size - (text.Length % size);
            string padText = text + new string(Convert.ToChar(pad), pad);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Convert.FromBase64String(key);
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.Zeros;

                System.Security.Cryptography.ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                byte[] encrypted;

                using (var msEncrypt = new System.IO.MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(padText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }

                cjcnc = Convert.ToBase64String(encrypted);
            }

            //Api call

            ServicePointManager.Expect100Continue = false;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //var PhonePeGatewayURL = "https://api-preprod.phonepe.com/apis/pg-sandbox";
            var PhonePeGatewayURL = "https://remittance.safexpay.com"; //prod 
                                                                       // var PhonePeGatewayURL = "https://neodev2.safexpay.com";

            var httpClient = new HttpClient();
            var uri = new Uri($"{PhonePeGatewayURL}/agWalletAPI/v2/agg");

            // Add headers
            //httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            //httpClient.DefaultRequestHeaders.Add("X-VERIFY", phonePePayment.X_VERIFY);

            string jjddf = "AGEN3250012853"; // Prod
            //string jjddf = "AGEN5500134316";
            // Create JSON request body
            var jsonBody = $"{{\"payload\":\"{cjcnc}\",\"uId\":\"{jjddf}\"}}";
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Send POST request
            var response = await httpClient.PostAsync(uri, content);
            if (response.IsSuccessStatusCode)
            {
                response.EnsureSuccessStatusCode();

                // Read and deserialize the response content
                var responseContent = await response.Content.ReadAsStringAsync();


                //Decript

                var PymentStatusDetails = JsonConvert.DeserializeObject<EncriptedObject>(responseContent);
                string crypt = PymentStatusDetails.payload;
                //string key = "WqV81Myi0evx587ixP37dOAlV8H26T6bh3HCgKiG328=";

                //byte[] iv = Encoding.ASCII.GetBytes("0123456789abcdef");
                byte[] cryptBytes = Convert.FromBase64String(crypt);
                string text1 = "";
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Convert.FromBase64String(key);
                    aesAlg.IV = iv;
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.Zeros;

                    System.Security.Cryptography.ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    byte[] decrypted;

                    using (var msDecrypt = new System.IO.MemoryStream(cryptBytes))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                            {
                                string padText1 = srDecrypt.ReadToEnd();

                                int pad1 = Convert.ToInt32(padText1[padText1.Length - 1]);

                                // if (pad > padText.Length)
                                ///return "Error";

                                text1 = padText1.Substring(0, padText1.Length - pad1);
                                //return text;
                                objRoot = JsonConvert.DeserializeObject<Root>(text1);
                                Staus = objRoot.payOutBean.bankStatus;
                                TempData["PayOutSucess"] = "PayOut transection Sucess!";
                            }
                        }
                    }
                }
                SafexPayOut obj = new SafexPayOut();
                obj.MobileNo = data.payOutBean.mobileNo;
                obj.TxnAmount = data.payOutBean.txnAmount;
                obj.AccountNo = data.payOutBean.accountNo;
                obj.IfscCode = data.payOutBean.ifscCode;
                obj.BankName = data.payOutBean.bankName;
                obj.AccountHolderName = data.payOutBean.accountHolderName;
                obj.TxnType = data.payOutBean.txnType;
                obj.AccountType = data.payOutBean.accountType;
                obj.EmailId = data.payOutBean.emailId;
                obj.OrderRefNo = data.payOutBean.orderRefNo;
                obj.JSONtext = text;
                obj.Key = key;
                obj.JsonEncrypted = cjcnc;
                obj.SefexAPI = "";
                obj.SefexRequest = jsonBody;
                obj.AgId = PymentStatusDetails.agId;
                obj.SefexResponsePayload = PymentStatusDetails.payload;
                obj.ReqTime = PymentStatusDetails.reqTime;
                obj.RespTime = PymentStatusDetails.respTime;
                obj.RequestId = PymentStatusDetails.requestId;
                obj.Rid = PymentStatusDetails.uid;
                obj.SefexDecryptor = text1;
                obj.BankStatus = objRoot.payOutBean.bankStatus;

                _context.safexPayOuts.Add(obj);
                _context.SaveChanges();
            }
            else
            {
                TempData["PayOutSucess"] = "PayOut transection failed!";

            }
            return RedirectToAction("Status", "PayMan", new { status = Staus });
        }

        public IActionResult Status(string status = null)
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Lyra()
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = System.Net.Http.HttpMethod.Post,
                RequestUri = new Uri("https://api.in.lyra.com/pg/rest/v1/charge"),
                Headers =
    {
        { "Accept", "application/json, application/xml" },
    },
                Content = new StringContent("{ \"orderId\" : \"3347y0ml\", \"currency\" : \"INR\", \"amount\" : \"1234\", \"orderInfo\":\"Payment\", \"maxAgeInHours\":\"24\", \"customer\" : { \"name\" : \"Payzen Customer\", \"emailId\" : \"pc@kvanto.com\", \"phone\" : \"+4520848002\", \"Address\" : \"Peblinge Dossering 32\"\r\n}, \"webhook\":{ \"url\":\"https://lyra.com/\"\r\n}\r\n}")
                {
                    Headers =
        {
            ContentType = new MediaTypeHeaderValue("application/json")
        }
                }
            };
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine(body);
            }

            return View();
        }

        // POST: /Home/GeneratePaymentLink
        [HttpPost]
        public async Task<JsonResult> GeneratePaymentLink([FromBody] VerifyRequestModel phonePePayment)
        {
            try
            {
                var username = HttpContext.Session.GetString("UserName");
                var email = HttpContext.Session.GetString("Email");
                var phone = HttpContext.Session.GetString("Phone");
                var userId = HttpContext.Session.GetString("UserId");

                // ON LIVE URL YOU MAY GET CORS ISSUE, ADD Below LINE TO RESOLVE
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                //var PhonePeGatewayURL = "https://api-preprod.phonepe.com/apis/pg-sandbox";
                var PhonePeGatewayURL = "https://api.phonepe.com/apis/hermes";

                var httpClient = new HttpClient();
                var uri = new Uri($"{PhonePeGatewayURL}/pg/v1/pay");

                // Add headers
                httpClient.DefaultRequestHeaders.Add("accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("X-VERIFY", phonePePayment.X_VERIFY);

                // Create JSON request body
                var jsonBody = $"{{\"request\":\"{phonePePayment.base64}\"}}";
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Send POST request
                var response = await httpClient.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();

                // Read and deserialize the response content
                var responseContent = await response.Content.ReadAsStringAsync();

                if (phonePePayment.TransactionId != null)
                {
                    var ppp = new PaymentTransectionId
                    {
                        UserName = username,
                        Phone = phonePePayment.CustomerNo,
                        Email = email,
                        TrasId = phonePePayment.TransactionId,
                        CreatedDate = DateTime.Now,
                        UserId = userId
                    };
                    _context.paymentTransectionIds.Add(ppp);
                    _context.SaveChanges();
                }

                // Return a response
                return Json(new { Success = true, Message = "Verification successful", phonepeResponse = responseContent });
            }
            catch (Exception ex)
            {
                // Handle errors and return an error response
                return Json(new { Success = false, Message = "Verification failed", Error = ex.Message });
            }
        }

        // POST: /Home/CheckPaymentStatus
        [HttpPost]
        public async Task<JsonResult> CheckPaymentStatus([FromBody] VerifyRequestModel phonePePayment)
        {
            try
            {
                // ON LIVE URL YOU MAY GET CORS ISSUE, ADD Below LINE TO RESOLVE
                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var PhonePeGatewayURL = "https://api.phonepe.com/apis/hermes";

                var httpClient = new HttpClient();
                var uri = new Uri($"{PhonePeGatewayURL}/pg/v1/status/{phonePePayment.MERCHANTID}/{phonePePayment.TransactionId}");

                // Add headers
                httpClient.DefaultRequestHeaders.Add("accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("X-VERIFY", phonePePayment.X_VERIFY);
                httpClient.DefaultRequestHeaders.Add("X-MERCHANT-ID", phonePePayment.MERCHANTID);

                // Create JSON request body

                // Send POST request
                var response = await httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                // Read and deserialize the response content
                var responseContent = await response.Content.ReadAsStringAsync();

                // Return a response
                return Json(new { Success = true, Message = "Verification successful", phonepeResponse = responseContent });
            }
            catch (Exception ex)
            {
                // Handle errors and return an error response
                return Json(new { Success = false, Message = "Verification failed", Error = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Pay1(string transid = null)
        {



            VerifyRequestModel phonePePayment = new VerifyRequestModel();
            phonePePayment.TransactionId = transid;
            //phonePePayment.MERCHANTID = "PGTESTPAYUAT";
            phonePePayment.MERCHANTID = "M22MZ1VGJ41EU";
            // var PhonePeGatewayURL = "https://api-preprod.phonepe.com/apis/pg-sandbox";
            var PhonePeGatewayURL = "https://api.phonepe.com/apis/hermes";
            //byte[] bytes = Encoding.UTF8.GetBytes($"/pg/v1/status/{phonePePayment.MERCHANTID}/{phonePePayment.TransactionId}099eb0cd-02cf-4e2a-8aca-3e6c6aff0399");
            byte[] bytes = Encoding.UTF8.GetBytes($"/pg/v1/status/{phonePePayment.MERCHANTID}/{phonePePayment.TransactionId}2f49de74-4932-46e3-9460-819b9009cb57");
            XSystem.Security.Cryptography.SHA256Managed hashstring = new XSystem.Security.Cryptography.SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            phonePePayment.X_VERIFY = hashString + "###1";

            var httpClient = new HttpClient();
            var uri = new Uri($"{PhonePeGatewayURL}/pg/v1/status/{phonePePayment.MERCHANTID}/{phonePePayment.TransactionId}");

            // Add headers
            httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("X-VERIFY", phonePePayment.X_VERIFY);
            httpClient.DefaultRequestHeaders.Add("X-MERCHANT-ID", phonePePayment.MERCHANTID);

            // Create JSON request body

            // Send POST request  
            var response = await httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            // Read and deserialize the response content
            var responseContent = await response.Content.ReadAsStringAsync();

            var PymentStatusDetails = JsonConvert.DeserializeObject<PaymentStatus>(responseContent);
            if (PymentStatusDetails.code == "PAYMENT_SUCCESS")
            {
                var Trns = _context.paymentTransectionIds.Where(t => t.TrasId == transid).FirstOrDefault();
                var Epaymentdetails = _context.paymentDetails.Where(t => t.MerchantTransactionId == transid).FirstOrDefault();
                if (PymentStatusDetails != null && Trns != null && Epaymentdetails == null)
                {
                    var paymentdetails = new PaymentDetails()
                    {
                        Email = Trns.Email,
                        Phone = Trns.Phone,
                        UserId = Trns.UserId,
                        Payment_Status = PymentStatusDetails.code,
                        MerchantId = PymentStatusDetails.data.merchantId,
                        MerchantTransactionId = PymentStatusDetails.data.merchantTransactionId,
                        Amount = PymentStatusDetails.data.amount / 100,
                        State = PymentStatusDetails.data.state,
                        CreatedDate = DateTime.Now,
                        CreatedBy = Trns.UserName

                    };
                    _context.paymentDetails.Add(paymentdetails);
                    _context.SaveChanges();
                }


                TempData["PaymentSucess"] = "You have " + PymentStatusDetails.data.amount / 100 + " Transferred Successfully";
            }
            return RedirectToAction("PayManHome", "PayMan");
        }
        //public T ConvertJsonToClass<T>(this string json)
        //{
        //    var hh = Newtonsoft.Json.JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.None,
        //        new JsonSerializerSettings()
        //        {
        //            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        //        });
        //    var obj=JsonConvert.DeserializeObject<T>(hh);
        //    return obj;

        //}

        [HttpPost]
        public async Task<JsonResult> LayraIntiatePayment(int amount, string enterCustomerNumber)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var UserName = HttpContext.Session.GetString("UserName");
            var Email = HttpContext.Session.GetString("Email");
            var phone = HttpContext.Session.GetString("Phone");
            var userdetais = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
            var paymanComm = _context.payManGateWayMarigins.FirstOrDefault();
            LyraPaymentDetails lyraPaymentDetails = new LyraPaymentDetails();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Set the base address and headers
                    client.BaseAddress = new Uri("https://api.in.lyra.com");
                    client.DefaultRequestHeaders.Clear();
                    //client.DefaultRequestHeaders.Add("accept", "application/json");
                    //client.DefaultRequestHeaders.Add("Authorization", "Basic MTgwNTU5MzI6dGVzdHBhc3N3b3JkX0VVSElqdXZMeWplbTVGQnhpZVh4dHYzTUE3SXA1dmFkQXF1blVEMzJSOHdKaw==");

                    string username = "33738761";
                    string password = "prodpassword_swN2AIAi7sMIrXNXxjzq3pnZ8AmfjhEOgs54etLUNeWwQ";
                    // Combine username and password into a single string
                    string credentials = $"{username}:{password}";
                    // Base64 encode the credentials
                    string base64Credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
                    // Set the Authorization header
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

                    // Set other headers
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var trnsOrderId = "payman" + DateTime.Now.ToString("yyyymmddhhmmss");
                    // JSON data to be sent in the request body
                    var jsonData = new
                    {
                        orderId = trnsOrderId,
                        orderInfo = "PayMan payment",
                        currency = "INR",
                        amount = amount + "00",
                        customer = new
                        {
                            name = UserName,
                            emailId = Email,
                            phone = phone,
                        },
                        @return = new
                        {
                            method = "POST",
                           url = "https://paymanfintech.in/PayMan/LayraStatus?trnsOrderId=" + trnsOrderId,
                            //url = "https://localhost:44384/PayMan/LayraStatus?trnsOrderId="+ trnsOrderId,
                            timeout = 600
                        }
                    };

                    // Convert JSON data to StringContent
                    var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(jsonData), Encoding.UTF8, "application/json");

                    // Send POST request
                    var response = await client.PostAsync("/pg/rest/v1/charge", content);
                    if (response.IsSuccessStatusCode)
                    {
                        // Read response
                        string responseString = await response.Content.ReadAsStringAsync();
                        lyraPaymentDetails = JsonConvert.DeserializeObject<LyraPaymentDetails>(responseString);
                        // Output the response
                        if (lyraPaymentDetails != null)
                        {
                            //var BankCardName = await LookupBinAsync(enterCustomerNumber.Substring(0, 8));
                            //if (BankCardName.Brand == null || BankCardName.Brand == "")
                            //{
                            //    return Json(new { Success = false, Message = "failed", phonepeResponse = lyraPaymentDetails });
                            //}
                            //if (BankCardName.Bank.Name == "Hdfc Bank Limited")
                            //{
                            //    userdetais.Margin = userdetais.Margin + (decimal)0.10;
                            //}


                            TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                            DateTime utcDateTime = DateTime.UtcNow; // Get the current UTC time
                            DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, istTimeZone);

                            var gdfsjdjhf = userdetais.Margin + (decimal)0.20;
                            var commission = (lyraPaymentDetails.Amount / 100) * gdfsjdjhf / 100;
                            var distibutercommission = (lyraPaymentDetails.Amount / 100) * userdetais.DistributerMarigin / 100;
                            var paymancommission = (lyraPaymentDetails.Amount / 100) * paymanComm.Lyra / 100;

                            PayIn payIn = new PayIn();
                            payIn.OrderId = lyraPaymentDetails.OrderId;
                            payIn.OrderRefNumber = lyraPaymentDetails.Uuid;
                            payIn.Currency = lyraPaymentDetails.Currency;
                            payIn.UserId = userId;
                            payIn.MobileNumber = enterCustomerNumber;
                            payIn.Amount = lyraPaymentDetails.Amount / 100;
                            payIn.Sttaus = lyraPaymentDetails.Status;
                            payIn.Refound = lyraPaymentDetails.Refunded;
                            payIn.GateWay = 3;
                            payIn.CreatedDate = istDateTime;
                            payIn.PayInCommission = commission;

                            payIn.DistributerUserId = userdetais.DistributeruserId;
                            payIn.DistibuterCommission = distibutercommission;
                            payIn.PaymanCommission = paymancommission;
                            payIn.IssueBank = "";
                            Console.WriteLine(responseString);
                            _context.payIns.Add(payIn);
                            _context.SaveChanges();
                        }
                    }
                    else
                    {
                        return Json(new { Success = false, Message = "Verification failed", phonepeResponse = lyraPaymentDetails });

                    }
                }
            }
            catch (Exception e)
            {

            }

            return Json(new { Success = true, Message = "Verification successful", phonepeResponse = lyraPaymentDetails });
        }

        [HttpPost]
        public async Task<IActionResult> LayraStatus(string trnsOrderId = null)
        {
            PayPayment payPayment = new PayPayment();
            try
            {

                var userId = HttpContext.Session.GetString("UserId");
                var payinstatus = _context.payIns.Where(t => t.OrderId == trnsOrderId).FirstOrDefault();
                var userdetais = _context.users.Where(t => t.UserId == payinstatus.UserId).FirstOrDefault();
                if (payinstatus != null)
                {
                    using (HttpClient client = new HttpClient())
                    {
                        // Set the base address and headers
                        client.BaseAddress = new Uri("https://api.in.lyra.com");
                        client.BaseAddress = new Uri("https://api.in.lyra.com");
                        client.DefaultRequestHeaders.Clear();
                        //client.DefaultRequestHeaders.Add("accept", "application/json");
                        //client.DefaultRequestHeaders.Add("Authorization", "Basic MTgwNTU5MzI6dGVzdHBhc3N3b3JkX0VVSElqdXZMeWplbTVGQnhpZVh4dHYzTUE3SXA1dmFkQXF1blVEMzJSOHdKaw==");

                        string username = "33738761";
                        string password = "prodpassword_swN2AIAi7sMIrXNXxjzq3pnZ8AmfjhEOgs54etLUNeWwQ";
                        // Combine username and password into a single string
                        string credentials = $"{username}:{password}";
                        // Base64 encode the credentials
                        string base64Credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
                        // Set the Authorization header
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

                        // Set other headers
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                        // Send POST request
                        var response = await client.GetAsync("/pg/rest/v1/charge/" + payinstatus.OrderRefNumber);
                        if (response.IsSuccessStatusCode)
                        {
                            string responseString = await response.Content.ReadAsStringAsync();
                            var lyraPaymentDetails = JsonConvert.DeserializeObject<LyraPaymentDetails>(responseString);
                            var andna = lyraPaymentDetails.Amount / 100;
                            if (lyraPaymentDetails.Status == "PAID")
                            {
                                payPayment.Amount = lyraPaymentDetails.Amount.ToString();
                                payPayment.EnterCustomerNumber = payinstatus.MobileNumber;
                                payPayment.Sucess = true;
                                int success = 1; // Assuming your logic sets this to true

                                TempData["LyraPayInSucess"] = success;
                                TempData["LyraPayInAmount"] = andna.ToString();
                                TempData["LyraPayInCardNumber"] = payinstatus.MobileNumber;
                                TempData["LyraPayInPaymentId"] = payinstatus.OrderId;
                                //TempData["PaymentSucess"] = "You have " + lyraPaymentDetails.Amount / 100 + " Transferred Successfully";
                            }
                            else
                            {
                                int success = 2; // Assuming your logic sets this to true

                                TempData["LyraPayInSucess"] = success;
                                TempData["LyraPayInAmount"] = andna.ToString();
                                TempData["LyraPayInCardNumber"] = payinstatus.MobileNumber;
                                TempData["LyraPayInPaymentId"] = payinstatus.OrderId;
                                //TempData["PaymentSucess"] = "Transecction Failed !";
                            }


                            if (lyraPaymentDetails != null)
                            {
                                var issubank = lyraPaymentDetails.Transactions[0].IssuingBank;
                                var issuecard = lyraPaymentDetails.Transactions[0].Scheme;
                                if (issuecard == "MASTERCARD" || issuecard == "mastercard" )
                                {
                                    var commiid = userdetais.Margin + (decimal)1.30;
                                    var commission = (lyraPaymentDetails.Amount / 100) * commiid / 100;
                                    payinstatus.PayInCommission = commission;
                                    payinstatus.IssueBank = issubank +"_"+ issuecard;
                                }
                                else if (issubank.Contains("HDFC") || issubank.Contains("hdfc"))
                                {
                                    var commiid = userdetais.Margin + (decimal)0.30;
                                    var commission = (lyraPaymentDetails.Amount / 100) * commiid / 100;
                                    payinstatus.PayInCommission = commission;
                                    payinstatus.IssueBank = issubank + "_" + issuecard;
                                }
                                else
                                {
                                    payinstatus.IssueBank = issubank + "_" + issuecard;
                                }
                                payinstatus.Sttaus = lyraPaymentDetails.Status;
                                _context.payIns.Update(payinstatus);
                                _context.SaveChanges();
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {

            }

            return RedirectToAction("Pay", "PayMan", payPayment);
        }


        [HttpPost]
        public async Task<JsonResult> IntiatePayment(int amount, string cardnumber)
        {
            //var BankCardName = await LookupBinAsync(cardnumber.Substring(0, 8));
            //if (BankCardName.Brand != null && BankCardName.Brand != "")
            //{
            //    if (BankCardName.Brand.Contains("Business"))
            //    {
            //        return Json(new { Success = false, OrderId = ViewBag.orderId });
            //    }
            //}else if(BankCardName.Brand == null || BankCardName.Brand == "")
            //{
            //    return Json(new { Success = false, OrderId = ViewBag.orderId , Message = "filed"});
            //}

            //var key = "rzp_live_2EIqs9bxOALurg";
            //var secrate = "KcADpSWx41UCr6tPjGDT10EU";

            var key = "rzp_test_lGsdswJA3Wc7jY";
            var secrate = "a8NHWgCI8mvxEyOa1Y9vq7p8";
            RazorpayClient razorpayClient = new RazorpayClient(key, secrate);
            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("amount", Convert.ToDecimal(amount) * 100);
            data.Add("currency", "INR");
            Order order = razorpayClient.Order.Create(data);
            ViewBag.orderId = order["id"].ToString();
            return Json(new { Success = true, OrderId = ViewBag.orderId });
        }
        [HttpGet]
        public async Task<JsonResult> ConfirmPayment(string razorpay_payment_id, string razorpay_order_id, string razorpay_signature, string amount, string enterCustomerNumber)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var phone = HttpContext.Session.GetString("Phone");

            var userdetails = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
            var paymanComm = _context.payManGateWayMarigins.FirstOrDefault();
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            attributes.Add("razorpay_payment_id", razorpay_payment_id);
            attributes.Add("razorpay_order_id", razorpay_order_id);
            attributes.Add("razorpay_signature", razorpay_signature);
            //return Ok();
            try
            {
                //var key = "rzp_live_2EIqs9bxOALurg";
                //var secrate = "KcADpSWx41UCr6tPjGDT10EU";

                var key = "rzp_test_lGsdswJA3Wc7jY";
                var secrate = "a8NHWgCI8mvxEyOa1Y9vq7p8";
                RazorpayClient _razorpayClient = new RazorpayClient(key, secrate);

                //if (Utils.verifyPaymentSignature(attributes))
                //{
                //    var order = _razorpayClient.Order.Fetch(razorpay_order_id);
                //    var payment = _razorpayClient.Payment.Fetch(razorpay_payment_id);
                //    if (payment["status"] == "captured")
                //    {
                //        return Ok("Payment Successful");
                //    }
                //}
                Utils.verifyPaymentSignature(attributes);
                var order = _razorpayClient.Order.Fetch(razorpay_order_id);
                var payment = _razorpayClient.Payment.Fetch(razorpay_payment_id);
                var razorPayCard = _razorpayClient.Card.FetchCardDetails(razorpay_payment_id);
                //if (payment["status"] == "captured")
                //{
                //    TempData["PaymentSucess"] = "You have " + amount + " Transferred Successfully";
                //}
                //else
                //{
                //    TempData["PaymentSucess"] = "You have " + amount + " Transferred failed";
                //}


                //var BankCardName = await LookupBinAsync(enterCustomerNumber.Substring(0, 8));
                //if (BankCardName.Brand == null || BankCardName.Brand == "")
                //{
                //    TempData["PaymentSucess"] = "The card has been attempted multiple times. Please try again later or change your location.";
                //    return RedirectToAction("PayManHome", "PayMan");
                //}
                //if (BankCardName.Bank.Name == "Hdfc Bank Limited")
                //{
                //    userdetails.Margin = userdetails.Margin + (decimal)0.10;
                //}


                PayIn payIn = new PayIn();
                TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime utcDateTime = DateTime.UtcNow; // Get the current UTC time
                DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, istTimeZone);
                var commissssion = userdetails.Margin;
                var issu = razorPayCard["issuer"];
                var issuecard = razorPayCard["network"];
                if (razorPayCard["network"] == "MasterCard")
                {
                    commissssion = userdetails.Margin + (decimal)1.28;
                }
                else if (razorPayCard["sub_type"] == "business")
                {
                    commissssion = userdetails.Margin + (decimal)2.10;
                }
                else if (razorPayCard["issuer"] == "HDFC" || razorPayCard["issuer"] == "hdfc")
                {
                    commissssion = userdetails.Margin + (decimal)0.10;
                }
                var commm = Convert.ToInt32(amount) * commissssion / 100;
                var distibutercommission = Convert.ToInt32(amount) * userdetails.DistributerMarigin / 100;
                var paymancommission = Convert.ToInt32(amount) * paymanComm.RazorPay / 100;

                payIn.OrderId = razorpay_order_id;
                payIn.OrderRefNumber = razorpay_payment_id;
                payIn.Currency = "INR";
                payIn.UserId = userId;
                payIn.MobileNumber = enterCustomerNumber;
                payIn.Amount = Convert.ToInt32(amount);
                payIn.Refound = 0;
                payIn.Sttaus = payment["status"];
                payIn.GateWay = 2;
                payIn.CreatedDate = istDateTime;
                payIn.PayInCommission = commm;

                payIn.DistributerUserId = userdetails.DistributeruserId;
                payIn.DistibuterCommission = distibutercommission;
                payIn.PaymanCommission = paymancommission;
                payIn.IssueBank = razorPayCard["issuer"] == null ? "" : razorPayCard["issuer"] + "_"+ issuecard;
                _context.payIns.Add(payIn);
                _context.SaveChanges();


                return Json(new { Success = true, Paymentid = razorpay_payment_id });
            }
            catch (Exception ex)
            {
                //  TempData["PaymentSucess"] = "You have " + amount + " failed transection.";
                return Json(new { Success = false, Paymentid = razorpay_payment_id });
            }


        }



        [HttpGet]
        public async Task<IActionResult> AddBeneficiaryAccounts(string? txtSearch, int pg = 1)
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);
            if (user == null || user.App == false)
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }
                var phone = HttpContext.Session.GetString("Phone");

                var userdetails = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
                if (!userdetails.DMTAccessable)
                {
                    TempData["useralrdeyexists"] = "Service not available please contact Admin";
                    return RedirectToAction("PayManHome", "PayMan");
                }
                else
                {
                    var payManHome = GetAvaliableAmount(userId);
                    var amount = payManHome.Amount;

                    AddBeneficiaryAccountsDTO beneficiaryAccounts = new AddBeneficiaryAccountsDTO();

                    beneficiaryAccounts.Options = _context.accountVerificationBanks.Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.BankName
                    }).OrderBy(t => t.Text).ToList();

                    beneficiaryAccounts.Options.Insert(0, new SelectListItem
                    {
                        Value = "",
                        Text = "Select a bank",
                        Selected = true
                    });


                    //  beneficiaryAccounts.MobileNumber = phone;

                    beneficiaryAccounts.UserId = userId;
                    beneficiaryAccounts.SefexFinalBalance = payManHome.SefexFinalBalance.ToString();
                    if (amount != null)
                    {
                        beneficiaryAccounts.AvaliableAmount = amount.ToString();
                    }
                    else
                    {
                        beneficiaryAccounts.AvaliableAmount = "0.00";
                    }


                    var result = await _context.beneficiaryAccounts
                        .Where(t => (t.UserId == userId || t.MobileNumber == phone) && t.IsActive == true)
                        .OrderByDescending(t => t.CreatedDate)
                        .ToListAsync();
                    if (!String.IsNullOrEmpty(txtSearch))
                    {
                        beneficiaryAccounts.BeneficiaryAccounts = result.Where(b => (b.ContactName.Contains(txtSearch) || b.MobileNumber.Contains(txtSearch)) && b.UserId == userId).ToList();
                    }
                    else
                    {
                        beneficiaryAccounts.BeneficiaryAccounts = result;
                    }

                    //pager functianality
                    const int pageSize = 10;
                    if (pg < 1)
                        pg = 1;

                    int resCount = beneficiaryAccounts.BeneficiaryAccounts.Count();
                    var pager = new Pager(resCount, pg, pageSize);
                    int recSkip = (pg - 1) * pageSize;
                    var data = beneficiaryAccounts.BeneficiaryAccounts.Skip(recSkip).Take(pager.PageSize).ToList();

                    this.ViewBag.Pager = pager;
                    beneficiaryAccounts.BeneficiaryAccounts = data;
                    return View(beneficiaryAccounts);
                }
            }
            else
            {
                var gateWayDetails = await _context.PayManGateways.FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(appPhone))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }

                if (user.PayOut == false)
                {
                    TempData["useralrdeyexists"] = "Service not available please contact Admin";
                    return RedirectToAction("PayManHome", "PayMan");
                }
                else
                {
                    var avlamount = await GetUserWalletAmount(appPhone);
                    var pineLabsAmount = await GetPinelabsAmount();
                    var amount = avlamount;

                    AddBeneficiaryAccountsDTO beneficiaryAccounts = new AddBeneficiaryAccountsDTO();

                    beneficiaryAccounts.Options = _context.accountVerificationBanks.Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.BankName
                    }).OrderBy(t => t.Text).ToList();

                    beneficiaryAccounts.Options.Insert(0, new SelectListItem
                    {
                        Value = "",
                        Text = "Select a bank",
                        Selected = true
                    });


                    //  beneficiaryAccounts.MobileNumber = phone;
                    beneficiaryAccounts.SefexFinalBalance = pineLabsAmount.ToString();
                    if (amount != null)
                    {
                        beneficiaryAccounts.AvaliableAmount = amount.ToString();
                    }
                    else
                    {
                        beneficiaryAccounts.AvaliableAmount = "0.00";
                    }


                    var result = await _context.payManBeneficiaryAccounts
                        .Where(t => (t.UserPhone == appPhone && t.IsActive == true))
                        .OrderByDescending(t => t.CreatedDate)
                        .ToListAsync();
                    if (!String.IsNullOrEmpty(txtSearch))
                    {
                        beneficiaryAccounts.PayManBeneficiaryAccounts = result.Where(b => (b.ContactName.Contains(txtSearch) || b.MobileNumber.Contains(txtSearch)) && b.UserPhone == appPhone).ToList();
                    }
                    else
                    {
                        beneficiaryAccounts.PayManBeneficiaryAccounts = result;
                    }

                    //pager functianality
                    const int pageSize = 10;
                    if (pg < 1)
                        pg = 1;

                    int resCount = beneficiaryAccounts.PayManBeneficiaryAccounts.Count();
                    var pager = new Pager(resCount, pg, pageSize);
                    int recSkip = (pg - 1) * pageSize;
                    var data = beneficiaryAccounts.PayManBeneficiaryAccounts.Skip(recSkip).Take(pager.PageSize).ToList();

                    this.ViewBag.Pager = pager;
                    beneficiaryAccounts.PayManBeneficiaryAccounts = data;
                    beneficiaryAccounts.PayOutMinAmount = gateWayDetails.PayOutMinAmount;
                    beneficiaryAccounts.PayOutMaxAmount = gateWayDetails.PayOutMaxAmount;
                    beneficiaryAccounts.MinBalanceAvl = gateWayDetails.MinBalanceAvl;
                    return View(beneficiaryAccounts);
                }
            }

              


        }

        [HttpPost]
        public async Task<IActionResult> AddBeneficiaryAccounts(BeneficiaryAccounts beneficiaryAccountsViewModel)
        {

            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);

            if (user == null  || user.App == false )
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }
                var Email = HttpContext.Session.GetString("Email");
                var objResult1 = _context.beneficiaryAccounts.Where(t => t.MobileNumber == beneficiaryAccountsViewModel.MobileNumber).ToList();
                if (objResult1.Count >= 5)
                {
                    TempData["BeneficiaryAdded"] = "Please enter a different mobile number. Each mobile number can only be used to verify up to 5 accounts.";
                    return RedirectToAction("AddBeneficiaryAccounts", "PayMan");
                }

                var objResult = _context.beneficiaryAccounts.Where(t => t.AccountNo == beneficiaryAccountsViewModel.AccountNo && t.MobileNumber == beneficiaryAccountsViewModel.MobileNumber).FirstOrDefault();

                if (objResult == null)
                {
                    var beneficiaryAccounts = new BeneficiaryAccounts
                    {
                        UserId = userId,
                        ContactName = beneficiaryAccountsViewModel.ContactName,
                        MobileNumber = beneficiaryAccountsViewModel.MobileNumber,
                        IfscCode = beneficiaryAccountsViewModel.IfscCode.ToUpper(),
                        EmailId = Email,//beneficiaryAccountsViewModel.EmailId,
                        AccountNo = beneficiaryAccountsViewModel.AccountNo,
                        TxnType = beneficiaryAccountsViewModel.TxnType,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        MeId = "",
                        ContactId = "",
                        BeneId = "",
                        VerficationFlag = "",
                        Page = 0,
                        Size = 0,
                        Date = "",
                        Responce = ""

                    };
                    _context.beneficiaryAccounts.Add(beneficiaryAccounts);
                    _context.SaveChanges();
                    TempData["BeneficiaryAdded"] = "Beneficiary Account Added Successfully";
                    return RedirectToAction("AddBeneficiaryAccounts", "PayMan");
                }
                else
                {
                    if (beneficiaryAccountsViewModel.VerficationFlag != "Y")
                    {
                        objResult.IfscCode = beneficiaryAccountsViewModel.IfscCode;
                        objResult.TxnType = beneficiaryAccountsViewModel.TxnType;
                        objResult.ContactName = beneficiaryAccountsViewModel.ContactName;
                        objResult.MobileNumber = beneficiaryAccountsViewModel.MobileNumber;
                        _context.beneficiaryAccounts.Update(objResult);
                        _context.SaveChanges();
                        TempData["BeneficiaryAdded"] = "Beneficiary Account updated Successfully!";
                        return RedirectToAction("AddBeneficiaryAccounts", "PayMan");
                    }
                    else
                    {
                        TempData["BeneficiaryAdded"] = "Beneficiary Account alredy exists!";
                        return RedirectToAction("AddBeneficiaryAccounts", "PayMan");
                    }

                }

            }
            else
            {
                DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                int existingCount = await _context.payManBeneficiaryAccounts
                    .CountAsync(b => b.MobileNumber == beneficiaryAccountsViewModel.MobileNumber);

                if (existingCount >= 5)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "This mobile number is already linked to 5 accounts. Please use a different number."
                    });
                }

                var existingAccount = await _context.payManBeneficiaryAccounts
                    .FirstOrDefaultAsync(b =>
                        b.AccountNo == beneficiaryAccountsViewModel.AccountNo &&
                        b.MobileNumber == beneficiaryAccountsViewModel.MobileNumber);

                if (existingAccount == null)
                {
                    var newAccount = new PayManBeneficiaryAccounts
                    {
                        UserId = user.Id,
                        UserPhone = appPhone,
                        ContactName = beneficiaryAccountsViewModel.ContactName,
                        MobileNumber = beneficiaryAccountsViewModel.MobileNumber,
                        IfscCode = beneficiaryAccountsViewModel.IfscCode.ToUpper(),
                        EmailId = user.Email,
                        AccountNo = beneficiaryAccountsViewModel.AccountNo,
                        TxnType = beneficiaryAccountsViewModel.TxnType,
                        IsActive = true,
                        CreatedDate = istDateTime,
                        ContactId = "",
                        BeneId = "",
                        VerficationFlag = ""
                    };

                    await _context.payManBeneficiaryAccounts.AddAsync(newAccount);
                    await _context.SaveChangesAsync();

                    TempData["BeneficiaryAdded"] = "Beneficiary Account Added Successfully";
                    return RedirectToAction("AddBeneficiaryAccounts", "PayMan");
                }
                else
                {
                    if (existingAccount.VerficationFlag != "Y")
                    {
                        existingAccount.ContactName = beneficiaryAccountsViewModel.ContactName;
                        existingAccount.MobileNumber = beneficiaryAccountsViewModel.MobileNumber;
                        existingAccount.IfscCode = beneficiaryAccountsViewModel.IfscCode.ToUpper();
                        existingAccount.TxnType = beneficiaryAccountsViewModel.TxnType;

                        _context.payManBeneficiaryAccounts.Update(existingAccount);
                        await _context.SaveChangesAsync();

                        TempData["BeneficiaryAdded"] = "Beneficiary Account updated Successfully!";
                        return RedirectToAction("AddBeneficiaryAccounts", "PayMan");
                    }

                    TempData["BeneficiaryAdded"] = "Beneficiary Account alredy exists!";
                    return RedirectToAction("AddBeneficiaryAccounts", "PayMan");
                }

            }

        }

        [HttpPost]
        public async Task<JsonResult> PaySttaus(string payoutId, string orderRefNo)
        {
            var rootObject = new StatusRootObject
            {
                Header = new StatusHeader
                {
                    OperatingSystem = "WEB",
                    SessionId = "AGEN3250012853",
                    Version = "1.0.0"
                },
                UserInfo = new StatusUserInfo
                {
                    // Initialize properties as needed
                },
                Transaction = new StatusTransaction
                {
                    RequestType = "TMH",
                    RequestSubType = "STCHK",
                    TranCode = 0,
                    TxnAmt = 0.0
                },
                PayOutBean = new StatusPayOutBean
                {
                    PayoutId = payoutId,
                    OrderRefNo = orderRefNo
                }
            };
            var jsonString1 = Newtonsoft.Json.JsonConvert.SerializeObject(rootObject);
            string cjcnc = "";
            string text = jsonString1;
            //string text = "{\"header\":\r\n{ \"operatingSystem\": \"WEB\", \"sessionId\": \"AGEN3250012853 \", \"version\": \"1.0.0\"\r\n},\"transaction\":\r\n{ \"requestType\": \"WTW\", \"requestSubType\": \"BENEL\", \"id\": \"AGEN3250012853 \"\r\n}\r\n}";
            string key = "snIQCqiMiBa8KkkafkQ1K+U3IGqlqwQKQZMEnnit8FQ="; //prod

            byte[] iv = Encoding.ASCII.GetBytes("0123456789abcdef");
            int size = 16;
            int pad = size - (text.Length % size);
            string padText = text + new string(Convert.ToChar(pad), pad);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Convert.FromBase64String(key);
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.Zeros;

                System.Security.Cryptography.ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                byte[] encrypted;

                using (var msEncrypt = new System.IO.MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(padText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }

                cjcnc = Convert.ToBase64String(encrypted);
            }

            //Api call

            ServicePointManager.Expect100Continue = false;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //var PhonePeGatewayURL = "https://api-preprod.phonepe.com/apis/pg-sandbox";
            var PhonePeGatewayURL = "https://remittance.safexpay.com"; //prod 
                                                                       // var PhonePeGatewayURL = "https://neodev2.safexpay.com";

            var httpClient = new HttpClient();
            var uri = new Uri($"{PhonePeGatewayURL}/agWalletAPI/v2/agg");

            // Add headers
            //httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            //httpClient.DefaultRequestHeaders.Add("X-VERIFY", phonePePayment.X_VERIFY);

            string jjddf = "AGEN3250012853"; // Prod
                                             //string jjddf = "AGEN5500134316";
                                             // Create JSON request body
            var jsonBody = $"{{\"payload\":\"{cjcnc}\",\"uId\":\"{jjddf}\"}}";
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Send POST request
            var response = await httpClient.PostAsync(uri, content);


            if (response.IsSuccessStatusCode)
            {
                response.EnsureSuccessStatusCode();

                // Read and deserialize the response content
                var responseContent = await response.Content.ReadAsStringAsync();
                //Decript
                ErrorModel nn = new ErrorModel();
                nn.agId = "st1";
                nn.payload = "";
                nn.reqTime = "";
                nn.requestId = "";
                nn.respTime = responseContent;
                nn.uid = response.ReasonPhrase;
                nn.jsonBody = jsonBody;
                nn.statuscode = response.IsSuccessStatusCode;
                _context.errorModels.Add(nn);
                _context.SaveChanges();

                var PymentStatusDetails = JsonConvert.DeserializeObject<EncriptedObject>(responseContent);
                string crypt = PymentStatusDetails.payload;
                //  string crypt = "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";
                byte[] cryptBytes = Convert.FromBase64String(crypt);
                string text1 = "";
                string key1 = "snIQCqiMiBa8KkkafkQ1K+U3IGqlqwQKQZMEnnit8FQ="; //prod

                byte[] iv1 = Encoding.ASCII.GetBytes("0123456789abcdef");
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Convert.FromBase64String(key1);
                    aesAlg.IV = iv1;
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.Zeros;

                    System.Security.Cryptography.ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    byte[] decrypted;

                    using (var msDecrypt = new System.IO.MemoryStream(cryptBytes))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                            {
                                string padText1 = srDecrypt.ReadToEnd();

                                int pad1 = Convert.ToInt32(padText1[padText1.Length - 1]);

                                // if (pad > padText.Length)
                                ///return "Error";

                                text1 = padText1.Substring(0, padText1.Length - pad1);
                                ErrorModel nn1 = new ErrorModel();
                                nn1.agId = "st";
                                nn1.payload = text1;
                                nn1.reqTime = "";
                                nn1.requestId = "";
                                nn1.respTime = "responseContent";
                                nn1.uid = text1;
                                nn1.jsonBody = "jsonBody";
                                nn1.statuscode = false;
                                _context.errorModels.Add(nn);
                                _context.SaveChanges();
                                //return text;
                                // objRoot = JsonConvert.DeserializeObject<PayWithBeanResponseRootObject>(text1);
                                // Staus = objRoot.payOutBean.bankStatus;
                                TempData["PayOutSucess"] = "PayOut transection Sucess!";
                            }
                        }
                    }
                }
            }

            return Json(new { Success = true });
        }

        [HttpGet]
        public async Task<string> GetSefexBalance(bool sefexpay = false)
        {

            var balance = string.Empty;
            SefexBalRoot objRoot = new SefexBalRoot();
            if (sefexpay == true)
            {
                string cjcnc = "";
                // string text = jsonString1;
                string text = "{\r\n\t\"header\": {\r\n\t\t\"operatingSystem\": \"WEB\",\r\n\t\t\"sessionId\": \"AGEN3250012853 \",\r\n\t\t\"version\": \"1.0.0\"\r\n\t},\r\n\t\"userInfo\": {},\r\n\t\"transaction\": {\r\n\t\t\"requestType\": \"WTW\",\r\n\t\t\"requestSubType\": \"GPWB\",\r\n\t\t\"id\": \"AGEN3250012853 \"\r\n\t},\r\n\t\"preLoadingWallet\": {\r\n\t\t\"walletType\": \"PAYOUT\"\r\n\t}\r\n}";
                string key = "snIQCqiMiBa8KkkafkQ1K+U3IGqlqwQKQZMEnnit8FQ="; //prod

                byte[] iv = Encoding.ASCII.GetBytes("0123456789abcdef");
                int size = 16;
                int pad = size - (text.Length % size);
                string padText = text + new string(Convert.ToChar(pad), pad);

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Convert.FromBase64String(key);
                    aesAlg.IV = iv;
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.Zeros;

                    System.Security.Cryptography.ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    byte[] encrypted;

                    using (var msEncrypt = new System.IO.MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(padText);
                            }
                            encrypted = msEncrypt.ToArray();
                        }
                    }

                    cjcnc = Convert.ToBase64String(encrypted);
                }

                //Api call

                ServicePointManager.Expect100Continue = false;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                //var PhonePeGatewayURL = "https://api-preprod.phonepe.com/apis/pg-sandbox";
                var PhonePeGatewayURL = "https://remittance.safexpay.com"; //prod 
                                                                           // var PhonePeGatewayURL = "https://neodev2.safexpay.com";

                var httpClient = new HttpClient();
                var uri = new Uri($"{PhonePeGatewayURL}/agWalletAPI/v2/agg");

                // Add headers
                //httpClient.DefaultRequestHeaders.Add("accept", "application/json");
                //httpClient.DefaultRequestHeaders.Add("X-VERIFY", phonePePayment.X_VERIFY);

                string jjddf = "AGEN3250012853"; // Prod
                                                 //string jjddf = "AGEN5500134316";
                                                 // Create JSON request body
                var jsonBody = $"{{\"payload\":\"{cjcnc}\",\"uId\":\"{jjddf}\"}}";
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Send POST request
                var response = await httpClient.PostAsync(uri, content);


                if (response.IsSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();

                    // Read and deserialize the response content
                    var responseContent = await response.Content.ReadAsStringAsync();
                    //Decript
                    ErrorModel nn = new ErrorModel();
                    nn.agId = "sefexbal1";
                    nn.payload = "";
                    nn.reqTime = "";
                    nn.requestId = "";
                    nn.respTime = responseContent;
                    nn.uid = response.ReasonPhrase;
                    nn.jsonBody = jsonBody;
                    nn.statuscode = response.IsSuccessStatusCode;
                    _context.errorModels.Add(nn);
                    _context.SaveChanges();

                    var PymentStatusDetails = JsonConvert.DeserializeObject<EncriptedObject>(responseContent);
                    string crypt = PymentStatusDetails.payload;
                    //string crypt = "Jq0iflnx3x4fET2w3udpwnQU6Uj+ghl7+HlS7QYGm8PF/KAhyZu8Kgk0gz5JEi8J5c4KFYgK0kAgPC2JyDReQ4cENE7HqfyhxDuHrsxLwpf2Fd2p8No+S/7GtzRct8NcRFSC+XzXyVY16qVtynpCVAl1TySFYMZz8fqL2bWoghJGVthhmYyYfryZt9oS6aeIsRz/NhMJiSmuAmkgErzx3oGbnlEtKndhI50fUC85suB4K28DT2MyEe3zLVE3h/mkXwIepQBcoIRjYcj83sa4fJdcjSLuA/wYDDcR8uP+hyMG3+ARuYW81j5/MiRo6gxFe69BkaYTH67zFNtWJpETVq2SSHwbX2szcWeYdzEXmExM6dqXvr5Oj7eF55Ms4byQk/2BTCWYsRwcd+Q1hjI99hRqxHbxkES0U+BAgeZKmq5wmZs3xIJvsM6wlpHz8qylMCpjnZf2hu1GOrgmlfO2THLeDtApG9l7p+qw/o46CiUnO2xsA68ctfi72oS6wJvbS2i+65NaZ2AW/DMPDtxYTa5VTEk/jfqa0LBwu64G79zbn5hXb0h1ednPq+TNND3XHmATBYMhdhvFzLW3P1kgKse4CrqT7/Wrvs722wkV6Xc=";
                    byte[] cryptBytes = Convert.FromBase64String(crypt);
                    string text1 = "";
                    string key1 = "snIQCqiMiBa8KkkafkQ1K+U3IGqlqwQKQZMEnnit8FQ="; //prod

                    byte[] iv1 = Encoding.ASCII.GetBytes("0123456789abcdef");
                    using (Aes aesAlg = Aes.Create())
                    {
                        aesAlg.Key = Convert.FromBase64String(key1);
                        aesAlg.IV = iv1;
                        aesAlg.Mode = CipherMode.CBC;
                        aesAlg.Padding = PaddingMode.Zeros;

                        System.Security.Cryptography.ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                        byte[] decrypted;

                        using (var msDecrypt = new System.IO.MemoryStream(cryptBytes))
                        {
                            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                            {
                                using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                                {
                                    string padText1 = srDecrypt.ReadToEnd();

                                    int pad1 = Convert.ToInt32(padText1[padText1.Length - 1]);

                                    text1 = padText1.Substring(0, padText1.Length - pad1);

                                    //return text;
                                    objRoot = JsonConvert.DeserializeObject<SefexBalRoot>(text1);
                                }
                            }
                        }
                    }
                }
                balance = objRoot.Transaction == null ? "0.00" : objRoot.Transaction.FinalBalance.ToString();
            }
            else
            {
                JWT_Generator generator = new JWT_Generator();
                var test = generator.GenerateToken();
                var url = "https://api.pluralonline.com/payouts/v2/payments/"; // Replace with your API URL
                var bearerToken = test; // Replace with your actual bearer token
                string endpoint = "funding-account";
                var objStatus = new BankAccount();
                using (var client = new HttpClient())
                {
                    // Set the base address for the client
                    client.BaseAddress = new Uri(url);

                    // Add the bearer token to the Authorization header
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                    // Create the StringContent object with the JSON content
                    //var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    try
                    {
                        // Send a POST request to the specified endpoint with the JSON content
                        HttpResponseMessage response = await client.GetAsync(endpoint);

                        // Check if the response was successful
                        if (response.IsSuccessStatusCode)
                        {
                            // Read and process the response content
                            string responseContent = await response.Content.ReadAsStringAsync();
                            objStatus = JsonConvert.DeserializeObject<BankAccount>(responseContent);
                            Console.WriteLine("Response Content: " + responseContent);
                        }
                        else
                        {
                            Console.WriteLine("Error: " + response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception: " + ex.Message);
                    }
                }
                string originalString = "INR"; // Example string

                string modifiedString = objStatus.balance == null ? "0.00" : AddLastTwoDigitsToBeginning(originalString, Convert.ToInt32(objStatus.balance.value));

                balance = modifiedString;
            }

            return balance;
        }
        public string AddLastTwoDigitsToBeginning(string originalString, int number)
        {
            // Get the last two digits of the number
            int lastTwoDigits = number / 100;

            // Convert lastTwoDigits to string and prepend to originalString
            string result = lastTwoDigits.ToString();//lastTwoDigits.ToString() + originalString;

            return result;
        }

        [HttpGet]
        public async Task<IActionResult> TransectionHistory(string? EnterDate)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LogOut", "LogIn");
            }
            if (EnterDate == null)
            {
                EnterDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            TransectionHistoryDTO transactions = new TransectionHistoryDTO();
            List<TransectionHistory> transectionHistories = new List<TransectionHistory>();
            var userslist = _context.users.Where(t=>t.IsActive == true).ToList();
            if (userslist != null)
            {

                transactions.EnterDate = EnterDate;
                foreach (var item in userslist)
                {
                    var payManHome = GetAvaliableAmount(item.UserId);
                    TransectionHistory transectionHistory = new TransectionHistory();
                    List<PayIn> payIns = new List<PayIn>();
                    List<PayOutTransectionDetails> payOutTransectionDetails = new List<PayOutTransectionDetails>();
                    var PayIntotal = _context.payIns.Where(t => t.UserId == item.UserId && (t.Sttaus == "PAID" || t.Sttaus == "captured" || t.Sttaus == "success")).ToList();
                    if (PayIntotal != null)
                    {
                        payIns = PayIntotal.Where(t => t.CreatedDate.ToString("yyyy-MM-dd") == EnterDate).ToList();
                    }
                    var PayOuttotal = _context.payOutTransectionDetails.Where(t => t.userId == item.UserId && t.txnStatus == "PENDING").ToList();
                    if (PayOuttotal != null)
                    {
                        payOutTransectionDetails = PayOuttotal.Where(t => t.CreatedDate?.ToString("yyyy-MM-dd") == EnterDate).ToList();
                    }

                    var totalPayoutcommission = payOutTransectionDetails.Count == 0 ? 0 : payOutTransectionDetails.Sum(t => t.PayoutCommission);

                    transectionHistory.DateTime = DateTime.Now;
                    transectionHistory.UserName = item.Name;
                    transectionHistory.UserId = item.UserId;
                    transectionHistory.PayInAmount = payIns.Count == 0 ? 0 : payIns.Sum(t => Convert.ToDecimal(t.Amount));
                    transectionHistory.PayOutAmount = payOutTransectionDetails.Count == 0 ? 0 : payOutTransectionDetails.Sum(t => Convert.ToDecimal(t.txnAmount));

                    var ExludedAmount = transectionHistory.PayOutAmount + totalPayoutcommission;
                    var percentageCaleculation = (double)transectionHistory.PayInAmount * Convert.ToDouble(item.Margin) / 100;
                    var result = (double)transectionHistory.PayInAmount - percentageCaleculation;
                    // var dhf = String.Format("{0:0.00}", result - (double)ExludedAmount);
                    transectionHistory.AvaliableBalance = payManHome.Amount.ToString();
                    transectionHistory.PayInCommision = Convert.ToDecimal(string.Format("{0:0.00}", percentageCaleculation));
                    transectionHistory.PayOutCommision = Convert.ToDecimal(string.Format("{0:0.00}", totalPayoutcommission));

                    transectionHistories.Add(transectionHistory);
                }
            }
            transactions.transectionHistories = transectionHistories.ToList();
            return View(transactions);
        }

        [HttpGet]
        public async Task<IActionResult> PayInHistory(string? EnterDate)
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);
            if (user == null || user.App == false)
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }
                PayInHistoryDTO transactions = new PayInHistoryDTO();
                List<PayInHistory> payInHistory = new List<PayInHistory>();
                if (EnterDate == null)
                {
                    EnterDate = DateTime.Now.ToString("yyyy-MM-dd");
                }
                var userdetails = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
                List<PayIn> payIns = new List<PayIn>();
                List<PayIn> payInss = new List<PayIn>();
                var PayIntotal = _context.payIns.Where(t => t.UserId == userId).ToList();
                var PayIntotalComm = _context.payIns.Where(t => t.DistributerUserId == userId && t.DistibuterCommission != Convert.ToDecimal("0.00") && (t.Sttaus == "PAID" || t.Sttaus == "captured")).ToList();

                if (PayIntotal != null)
                {
                    payIns = PayIntotal.Where(t => t.CreatedDate.ToString("yyyy-MM-dd") == EnterDate).OrderByDescending(t => t.CreatedDate).ToList();
                }

                if (PayIntotalComm != null)
                {
                    payInss = PayIntotalComm.Where(t => t.CreatedDate.ToString("yyyy-MM-dd") == EnterDate).OrderByDescending(t => t.CreatedDate).ToList();
                }

                if (payIns != null || PayIntotalComm != null)
                {
                    transactions.EnterDate = EnterDate;
                    int Sno = 1;
                    if (payIns.Count > 0)
                    {
                        foreach (var item in payIns)
                        {

                            // Get the last 4 digits
                            string lastFourDigits = item.MobileNumber.Length >= 4 ? item.MobileNumber.Substring(item.MobileNumber.Length - 4) : item.MobileNumber;

                            TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                            DateTime? dt = item.CreatedDate;
                            DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(dt.Value, istTimeZone);

                            var percentageCaleculation = (double)item.Amount * Convert.ToDouble(userdetails.Margin) / 100;
                            PayInHistory pay = new PayInHistory();
                            pay.Sno = Sno;
                            pay.TxnDate = item.CreatedDate.ToString("M/dd/yyyy HH:mm:ss");// item.CreatedDate.ToString("dd/MM/yyyy HH:mm:ss");
                            pay.PaymentDetails = item.OrderId.ToUpper();
                            pay.TxnAmount = item.Amount;
                            pay.TxnCharges = Convert.ToDecimal(string.Format("{0:0.00}", item.PayInCommission));
                            pay.creditAmount = Convert.ToDecimal(string.Format("{0:0.00}", item.Amount - item.PayInCommission));
                            pay.DistributerComm = Convert.ToDecimal(string.Format("{0:0.00}", item.DistibuterCommission));
                            pay.TxnStatus = item.Sttaus;
                            pay.CardNo = lastFourDigits;
                            var bf = item.DistibuterCommission.ToString();
                            pay.UserName = _context.users.Where(t => t.UserId == item.UserId).Select(tt => tt.Name).FirstOrDefault();
                            Sno++;
                            payInHistory.Add(pay);
                        }
                    }

                    if (payInss.Count > 0)
                    {
                        foreach (var item in payInss)
                        {
                            string lastFourDigits = item.MobileNumber.Length >= 4 ? item.MobileNumber.Substring(item.MobileNumber.Length - 4) : item.MobileNumber;

                            TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                            DateTime? dt = item.CreatedDate;
                            DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(dt.Value, istTimeZone);

                            var percentageCaleculation = (double)item.Amount * Convert.ToDouble(userdetails.Margin) / 100;
                            PayInHistory pay = new PayInHistory();
                            pay.Sno = Sno;
                            pay.TxnDate = item.CreatedDate.ToString("M/dd/yyyy HH:mm:ss");// item.CreatedDate.ToString("dd/MM/yyyy HH:mm:ss");
                            pay.PaymentDetails = item.OrderId.ToUpper();
                            pay.TxnAmount = item.Amount;
                            pay.TxnCharges = Convert.ToDecimal(string.Format("{0:0.00}", item.PayInCommission));
                            pay.creditAmount = Convert.ToDecimal(string.Format("{0:0.00}", item.Amount - item.PayInCommission));
                            pay.DistributerComm = Convert.ToDecimal(string.Format("{0:0.00}", item.DistibuterCommission));
                            pay.TxnStatus = item.Sttaus;
                            pay.CardNo = lastFourDigits;
                            var bf = item.DistibuterCommission.ToString();
                            pay.UserName = _context.users.Where(t => t.UserId == item.UserId).Select(tt => tt.Name).FirstOrDefault();
                            Sno++;
                            payInHistory.Add(pay);
                        }
                    }

                }
                transactions.transectionHistories = payInHistory.OrderByDescending(t => t.TxnDate).ToList();

                return View(transactions);
            }
            else
            {
                if (string.IsNullOrEmpty(appPhone))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }
                PayInHistoryDTO transactions = new PayInHistoryDTO();

                List<PayInHistory> payInHistory = new List<PayInHistory>();

                if (EnterDate == null)
                {
                    EnterDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                List<PayManPayIn> payIns = new List<PayManPayIn>();

                var payInTotal = _context.payManPayIns
                    .Where(t => t.UserPhone == appPhone)
                    .ToList();

                if (payInTotal != null)
                {
                    DateTime enteredDate = DateTime.ParseExact(EnterDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                    payIns = payInTotal
                        .Where(t => t.Created.HasValue && t.Created.Value.Date == enteredDate.Date)
                        .OrderByDescending(t => t.Created)
                        .ToList();
                }



                if (payIns != null)
                {
                    transactions.EnterDate = EnterDate;
                    int Sno = 1;

                    if (payIns.Count > 0)
                    {
                        foreach (var item in payIns)
                        {
                            // Get the last 4 digits of card number
                            string lastFourDigits = item.EaseCardNum?.Length >= 4
                                ? item.EaseCardNum.Substring(item.EaseCardNum.Length - 4)
                                : item.EaseCardNum;

                            var dhf = item.Amount - item.PayInCommission;

                            PayInHistory pay = new PayInHistory
                            {
                                
                                
                                Sno = Sno,
                                TxnDate = item.Created.HasValue
                                    ? item.Created.Value.ToString("M/dd/yyyy HH:mm:ss")
                                    : string.Empty,
                                PaymentDetails = item.EasePayId?.ToUpper(),
                                TxnAmount = item.Amount ?? 0,
                                TxnCharges = item.PayInCommission ?? 0,
                                creditAmount = dhf ?? 0,
                                TxnStatus = item.Status == true ? "Success" : "Failed",
                                CardNo = lastFourDigits
                            };

                            Sno++;
                            payInHistory.Add(pay);
                        }
                    }
                }

                transactions.transectionHistories = payInHistory.OrderByDescending(t => t.TxnDate).ToList();

                return View(transactions);
            }
               
        }


        public bool GetPayOutEnable()
        {
            var today = DateTime.Today;

            var gateWayDetails = _context.PayManGateways.FirstOrDefault();

            int payOutCount = _context.payManPayOuts
                .Count(t => t.DateTime.HasValue &&
                            t.DateTime.Value.Date == today &&
                            t.PayOutType == "Pine Labs");

            int result =  payOutCount;

            return result <= gateWayDetails.PayoutCount;
        }



        [HttpGet]
        public async Task<IActionResult> PayOutHistory(string? EnterDate)
        {

            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);
            if (user == null || user.App == false)
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }
                PayInHistoryDTO transactions = new PayInHistoryDTO();
                List<PayInHistory> payInHistory = new List<PayInHistory>();
                if (EnterDate == null)
                {
                    EnterDate = DateTime.Now.ToString("yyyy-MM-dd");
                }
                ViewBag.PayOutSearch = EnterDate;
                var userdetails = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
                List<PayOutTransectionDetails> payOutTransectionDetails = new List<PayOutTransectionDetails>();
                var PayOuttotal = _context.payOutTransectionDetails.Where(t => t.userId == userId).ToList();
                if (PayOuttotal != null)
                {
                    payOutTransectionDetails = PayOuttotal.Where(t => t.CreatedDate?.ToString("yyyy-MM-dd") == EnterDate).OrderByDescending(t => t.CreatedDate).ToList();
                }
                if (payOutTransectionDetails != null)
                {
                    transactions.EnterDate = EnterDate;
                    int Sno = 1;
                    foreach (var item in payOutTransectionDetails)
                    {
                        TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                        DateTime? dt = item.CreatedDate;
                        PayInHistory pay = new PayInHistory();
                        DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(dt.Value, istTimeZone);

                        pay.Sno = Sno;
                        pay.TxnDate = item.CreatedDate?.ToString("M/dd/yyyy HH:mm:ss");//DateTime.ParseExact(item.CreatedDate, "M/dd/yyyy h: mm:ss tt",CultureInfo.InvariantCulture);
                        pay.PaymentDetails = "Transaction ID: " + item.payoutId.ToUpper();
                        var fsdfh = Convert.ToInt32(Convert.ToDecimal(item.txnAmount));
                        pay.TxnAmount = Convert.ToDecimal(string.Format("{0:0.00}", fsdfh));
                        pay.TxnCharges = Convert.ToDecimal(string.Format("{0:0.00}", item.PayoutCommission));
                        var fbdsf = Convert.ToDouble(fsdfh) - item.PayoutCommission;
                        pay.creditAmount = Convert.ToDecimal(string.Format("{0:0.00}", fsdfh));
                        pay.TxnStatus = (item.txnStatus == "PENDING" || item.txnStatus == "PROCESSED") ? "Paid" : item.txnStatus;
                        pay.AccountNo = item.accountNo;
                        pay.Name = item.accountHolderName;
                        Sno++;
                        payInHistory.Add(pay);
                    }
                }
                transactions.transectionHistories = payInHistory.ToList();

                return View(transactions);
            }
            else
            {
                if (string.IsNullOrEmpty(appPhone))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }
                PayInHistoryDTO transactions = new PayInHistoryDTO();
                List<PayInHistory> payInHistory = new List<PayInHistory>();

                if (EnterDate == null)
                {
                    EnterDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                List<PayManPayOut> payOutTransectionDetails = new List<PayManPayOut>();

                var PayOuttotal = _context.payManPayOuts
                    .Where(t => t.UserPhone == appPhone)
                    .ToList();

                if (PayOuttotal != null)
                {
                    DateTime enteredDate = DateTime.ParseExact(EnterDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                    payOutTransectionDetails = PayOuttotal
                        .Where(t => t.DateTime.HasValue && t.DateTime.Value.Date == enteredDate.Date)
                        .OrderByDescending(t => t.DateTime)
                        .ToList();
                }
                if (payOutTransectionDetails != null)
                {
                    transactions.EnterDate = EnterDate;
                    int Sno = 1;
                    foreach (var item in payOutTransectionDetails)
                    {
                        PayInHistory pay = new PayInHistory();

                        pay.Sno = Sno;
                        pay.TxnDate = item.DateTime.HasValue
                                    ? item.DateTime.Value.ToString("M/dd/yyyy HH:mm:ss")
                                    : string.Empty;
                        pay.PaymentDetails = "Transaction ID: " + item.RefId.ToUpper();
                        pay.TxnAmount = item.Amount ?? 0;
                        pay.TxnCharges = item.PayoutCommission ?? 0;
                        var fbdsf = item.Amount - item.PayoutCommission;
                        pay.creditAmount = fbdsf ?? 0;
                        pay.TxnStatus = item.Result;
                        pay.AccountNo = item.AccountNo;
                        pay.Name = item.AccountHolderName;
                        Sno++;
                        payInHistory.Add(pay);
                    }
                }
                transactions.transectionHistories = payInHistory.ToList();

                return View(transactions);
            }
               
        }

        [HttpGet]
        public async Task<IActionResult> UsersList()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LogOut", "LogIn");
            }

            RisterDTO risterDTO = new RisterDTO();
            // Load users along with their PayIn details
            risterDTO.registartions = await _context.users
                .Select(user => new UserDTO
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    UserId = user.UserId,
                    Password = user.Password,
                    Role = user.Role,
                    CreatedDate = user.CreatedDate,
                    Margin = user.Margin,
                    DMTAccessable = user.DMTAccessable,
                    IsActive = user.IsActive,
                    DistributeruserId = user.DistributeruserId,
                    DistributerMarigin = user.DistributerMarigin,
                    BalanceTopUp = user.BalanceTopUp,
                    ChangePassword = user.ChangePassword,
                    SrrEnable = user.SrrEnable,
                    MasterMarginEnable = user.MasterMarginEnable,
                    MasterMargin = user.MasterMargin,
                    payIns = _context.payIns.Where(t => t.UserId == user.UserId) // Assuming `PayIns` is a navigation property
                        .Select(payIn => new PayIn
                        {
                            Id = payIn.Id,
                            Amount = payIn.Amount,
                            CreatedDate = payIn.CreatedDate,
                            Sttaus = payIn.Sttaus,
                            UserId = payIn.UserId,
                            OrderId = payIn.OrderId,
                            OrderRefNumber = payIn.OrderRefNumber.ToUpper(),
                            PayInCommission =payIn.PayInCommission,
                            MobileNumber = payIn.MobileNumber,
                        }).OrderByDescending(t=>t.CreatedDate).Take(15).ToList(),
                    payOutTransectionDetails = _context.payOutTransectionDetails.Where(t => t.userId == user.UserId) // Assuming `PayIns` is a navigation property
                        .Select(payIn => new PayOutTransectionDetails
                        {
                            Id = payIn.Id,
                            txnAmount = payIn.txnAmount,
                            CreatedDate = payIn.CreatedDate,
                            bankStatus = payIn.bankStatus == "PENDING" ? "Sucess": payIn.bankStatus,
                            userId = payIn.userId,
                            payoutId = payIn.payoutId,
                            orderRefNo = payIn.orderRefNo.ToUpper(),
                            accountHolderName = payIn.accountHolderName,
                            accountNo = payIn.accountNo,
                        }).OrderByDescending(t => t.CreatedDate).Take(15).ToList(),
                })
                .ToListAsync();

            return View(risterDTO);
        }

        [HttpPost]
        public void Return(FormCollection form)
        {
            try
            {

                string[] merc_hash_vars_seq;
                string merc_hash_string = string.Empty;
                string merc_hash = string.Empty;
                string order_id = string.Empty;
                string hash_seq = "key|txnid|amount|productinfo|firstname|email|udf1|udf2|udf3|udf4|udf5|udf6|udf7|udf8|udf9|udf10";

                //if (form["status"].ToString() == "success")
                //{

                //    merc_hash_vars_seq = hash_seq.Split('|');
                //    Array.Reverse(merc_hash_vars_seq);
                //    merc_hash_string = ConfigurationManager.AppSettings["SALT"] + "|" + form["status"].ToString();


                //    foreach (string merc_hash_var in merc_hash_vars_seq)
                //    {
                //        merc_hash_string += "|";
                //        merc_hash_string = merc_hash_string + (form[merc_hash_var] != null ? form[merc_hash_var] : "");

                //    }
                //    Response.Write(merc_hash_string);
                //    merc_hash = Generatehash512(merc_hash_string).ToLower();



                //    if (merc_hash != form["hash"])
                //    {
                //        Response.Write("Hash value did not matched");

                //    }
                //    else
                //    {
                //        order_id = Request.Form["txnid"];

                //        ViewData["Message"] = "Status is successful. Hash value is matched";
                //        Response.Write("<br/>Hash value matched");

                //        //Hash value did not matched
                //    }

                //}

                //else
                //{

                //    Response.Write("Hash value did not matched");
                //    // osc_redirect(osc_href_link(FILENAME_CHECKOUT, 'payment' , 'SSL', null, null,true));

                //}
            }

            catch (Exception ex)
            {
                // Response.Write("<span style='color:red'>" + ex.Message + "</span>");

            }


        }



        [HttpGet]
        public IActionResult PinelabsPayOut()
        {

            // string crypt = PymentStatusDetails.payload;
            //string crypt = "Jq0iflnx3x4fET2w3udpwnQU6Uj+ghl7+HlS7QYGm8PF/KAhyZu8Kgk0gz5JEi8JN8R/iOLfAQgxbMKkrnDRCyDLhWtOjq3FHwWrJazVGRJQrzfLpGNJ9uX4Z0+eQxSOIq+W60hkLtF6lYzSHW2K6+Wnm5zwWNeC4MPt9rRsyTjR3qNOwE+XKqwBs2PV3vvVWpZIrwC7tFhaQsfpWyodi1wTH8zG/6SUH+47Avd+sa07T1QX4eYSkUp3fVEjybA7ThiuZQLRy2Z6uVsJDD+lS5wRmhvTt0zoExaQWxYnnj8eKYDVs3+5tXLsIGD6AM5k3s2uqym3qo6zlgDeyELlme7Rl3BmcuQzStMO6EXiNzPJ+ymzJFM1knGtPuBHJXGwRE3FL/UiKnrqMa8ZhvAa7ozX6PT+YyfLlqVvHqH9WufFsS3FMQ/mmWU/mVkb4eQWoiVtx3n0FtioMi8MdrO4MltyQzqB1AY9ZnFgKyYWWv291Pc3J/hb6gijbZmZrmTI8glxa65Fg/HYeDvjK+gaDmyXjp7d0s0nkiP+56lS80NcIZW50AE5QowZ0Zd8OjL9Q46Y/LUapD+2hD7CTh3+X7Cwm3nRxPByxSBwi0QgMAzp27b+v80LHEb6O7QJZBuL5YtKWY8HTOaGZK3gDIdIV0OUMG4pGFCu9qG0JIc7So0tubBhpxLv/IVWfwuTfjcgZjbOOYNpy44EU7nDp8ZpF7KvqWaxNe9jSwIoZDIpt10fGwGDVUdpaxsKudxXADrvCaIDvj8MlBgIllyYVlkq9K0Dh2yvFDOVnZdSElVyF/6OJ0KMJb7wkwgpyX+pd2Ru42tMYm5WnVf2VaMPX6dTmrq+n5X87MWELmOCWw+LNv1Aa5Z+8eTJnfGq02fC1/INwQrWaOaGGJl7BrRFOnUeHMA/1yHVIBVl+YhlLtX+s9FRfi/cOvc5fi6RPzBi/lwjTJDj3rMPJdbbN5xOT1iKiSjIjf4CBYMQ3u2fpgz1aky7VCdvIhFRjWDThyshtAfW7uYLMlxoNYacVuzxWR9M5nf/jAyq6ECDgylbpmcLBGs6eJgkF75mFgcW3BmY9EN9kpaj4SyPBfyDe3wc5VrRqemz1HOl95mpPFGUonlZCEdK9rIt8u+KOftFPAgGwDw7hQ/4+MKbqgKQeT4Q8LNwCzJH2ik1rNT/x5QoK1XmX1atcCeURsPZ2aylra0j9V+MBd1y+e6EU66xtbs1pEKYhhYwhaAfCcWvFDnY8czRYH3GyHkEyFnF64vpS+dxKVwzDR4bHNc5+yP3qmIqvrEne+BuL4gMALSI6SbXc4dZ26+Rp3ArbGLuZsuR/6He5wyO++q31Ans5QCGT/D2MSnd0OTKssSg55JZHTdVxguri/o7OBuz58LbMxs5FuZt+ZxT";
            //byte[] cryptBytes = Convert.FromBase64String(crypt);
            //string text1 = "";
            //string key1 = "snIQCqiMiBa8KkkafkQ1K+U3IGqlqwQKQZMEnnit8FQ="; //prod

            //byte[] iv1 = Encoding.ASCII.GetBytes("0123456789abcdef");
            //using (Aes aesAlg = Aes.Create())
            //{
            //    aesAlg.Key = Convert.FromBase64String(key1);
            //    aesAlg.IV = iv1;
            //    aesAlg.Mode = CipherMode.CBC;
            //    aesAlg.Padding = PaddingMode.Zeros;

            //    System.Security.Cryptography.ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            //    byte[] decrypted;

            //    using (var msDecrypt = new System.IO.MemoryStream(cryptBytes))
            //    {
            //        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            //        {
            //            using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
            //            {
            //                string padText1 = srDecrypt.ReadToEnd();

            //                int pad1 = Convert.ToInt32(padText1[padText1.Length - 1]);

            //                // if (pad > padText.Length)
            //                ///return "Error";

            //                text1 = padText1.Substring(0, padText1.Length - pad1);
            //                //return text;
            //                //objRoot = JsonConvert.DeserializeObject<PayWithBeanResponseRootObject>(text1);
            //                // Staus = objRoot.payOutBean.bankStatus;
            //                TempData["PayOutSucess"] = "PayOut transection Sucess!";
            //            }
            //        }
            //    }
            //}


            JWT_Generator generator = new JWT_Generator();
            var test = generator.GenerateToken();

            //try
            //{
            //    using (RSA rsa = RSA.Create())
            //    {
            //        rsa.KeySize = 2048;
            //        RSAParameters rsaKeyInfo = rsa.ExportParameters(includePrivateParameters: true);

            //        string publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
            //        string privateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());

            //        Console.WriteLine("Private Key is " + privateKey + "\n");
            //        Console.WriteLine("Public Key is " + publicKey + "\n");
            //    }
            //}
            //catch (CryptographicException e)
            //{
            //    Console.WriteLine(e.Message);
            //}

            var kjfhjdsf = test;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PayOutConfc([FromBody] PayOutRequest request)
        {

            bool payin = request.PaymentMethods.Contains("SefexPay", StringComparer.OrdinalIgnoreCase);
            bool payout = request.PaymentMethods.Contains("PineLab", StringComparer.OrdinalIgnoreCase);
            bool ccbill = request.PaymentMethods.Contains("InstantPay", StringComparer.OrdinalIgnoreCase);

            // Get all non-admin users
            var users = await _context.payManUsers
                .Where(t => t.IsAdmin == false)
                .ToListAsync();
            var webusers = _context.users.Where(t => t.Id != 1 && t.Id != 5).ToList();

            foreach (var user in webusers)
            {
                user.BalanceTopUp = payin;
                user.DMTAccessable = payout;
            }

            _context.users.UpdateRange(webusers);
            await _context.SaveChangesAsync();

            foreach (var user in users)
            {
                user.PayIn = payin;
                user.PayOut = payout;
                user.CCBill = ccbill;
            }

            _context.payManUsers.UpdateRange(users);
            await _context.SaveChangesAsync();

            return RedirectToAction("PayManHome", "PayMan");
        }


        public async Task<string> PinelabsStatus(string token, string url, string txtid)
        {
            string endpoint = "?paymentReferenceId=" + txtid;
            var objStatus = new PineLabsPaoutStatusRootObject();
            using (var client = new HttpClient())
            {
                // Set the base address for the client
                client.BaseAddress = new Uri(url);

                // Add the bearer token to the Authorization header
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Create the StringContent object with the JSON content
                //var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                try
                {
                    // Send a POST request to the specified endpoint with the JSON content
                    HttpResponseMessage response = await client.GetAsync(endpoint);

                    // Check if the response was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read and process the response content
                        string responseContent = await response.Content.ReadAsStringAsync();
                        objStatus = JsonConvert.DeserializeObject<PineLabsPaoutStatusRootObject>(responseContent);
                        Console.WriteLine("Response Content: " + responseContent);
                    }
                    else
                    {
                        Console.WriteLine("Error: " + response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                }
            }
            var nbs = objStatus.payments[0].status;
            return nbs;
        }


        [HttpPost]
        public async Task<JsonResult> Create(int amount)
        {
            var ammaont = amount + "00";
            try
            {
                payment_mode payment_Mode = new payment_mode();
                txn_data txn_Data = new txn_data();
                customer_data customerData = new customer_data();
                udf_data udfData = new udf_data();
                ProductDetails[] productDetails = new ProductDetails[2];
                var methods = GetPaymentMethods(payment_Mode);
                var trnsOrderId = "payman" + DateTime.Now.ToString("yyyymmddhhmmss");
                var body = new
                {
                    merchant_data = new
                    {
                        merchant_id = "107606",
                        merchant_access_code = "a487ae43-5b0a-4e81-84f9-e86cdd9a5cb4",
                        unique_merchant_txn_id = trnsOrderId,
                        merchant_return_url = "https://paymanfintech.in/PayMan/PineLabpgSatatus?trnsOrderId=" + trnsOrderId,
                        //merchant_return_url = "http://192.168.101.93:7050/ChargingResp.aspx",
                    },
                    payment_data = new
                    {
                        amount_in_paisa = ammaont,
                    },
                    txn_data = new
                    {
                        navigation_mode = 2,
                        payment_mode = "4,1,3,10,14,11",//string.Join(",", methods),
                        transaction_type = 1

                    }
                };

                var endpoint = "https://uat.pinepg.in/api/";
                var url = endpoint + "v2/accept/payment";

                using (var httpClient = new HttpClient())
                {
                    var jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);
                    var base64Body = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonBody));
                    var hash = PineLabHash.GenerateCreateOrderHash(base64Body, "C9955B6F98054C5B9D5E7C1088818870");
                    var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
                    request.Headers.Add("X-VERIFY", hash);
                    request.Content = new StringContent("{\"request\":\"" + base64Body + "\"}", Encoding.UTF8,
                        "application/json");
                    var response = await httpClient.SendAsync(request);
                    request.Dispose();
                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var data = JsonConvert.DeserializeObject<CreateOrderResponse>(responseContent);
                        var returnData = new CreateOrderReturnType
                        {
                            status = true,
                            url = data?.redirect_url ?? "",
                            token = data?.token ?? ""
                        };
                        return Json(new { Success = true, Message = "Verification successful", phonepeResponse = returnData });
                    }

                    throw new Exception(responseContent);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex?.Message ?? "Something went wrong");
            }
        }

        public async Task<IActionResult> PineLabpgSatatus(string? trnsOrderId, string? userId, string? phone)
        {
            ErrorModel nnj = new ErrorModel();
            nnj.agId = "Pinelab PG1";
            nnj.payload = "";
            nnj.reqTime = "";
            nnj.requestId = trnsOrderId == null ? "Test" : trnsOrderId;
            nnj.respTime = userId;
            nnj.uid = "";
            nnj.jsonBody = phone;
            nnj.statuscode = false;
            _context.errorModels.Add(nnj);
            _context.SaveChanges();
            //trnsOrderId = "payman20245508035507";
            var userdetails = _context.users.Where(t => t.Name == userId).FirstOrDefault();
            var paymanComm = _context.payManGateWayMarigins.FirstOrDefault();
            try
            {
                var body = new
                {
                    ppc_MerchantID = "107606",
                    ppc_MerchantAccessCode = "a487ae43-5b0a-4e81-84f9-e86cdd9a5cb4",
                    ppc_TransactionType = "3",
                    ppc_UniqueMerchantTxnID = trnsOrderId
                };

                var endpoint = "https://uat.pinepg.in/api/";
                var url = endpoint + "PG/V2";

                using (var httpClient = new HttpClient())
                {
                    var hash = PineLabHash.GenerateFetchOrderHash(body, "C9955B6F98054C5B9D5E7C1088818870");
                    var dataString = CreateDataString(new
                    {
                        ppc_MerchantID = "107606",
                        ppc_MerchantAccessCode = "a487ae43-5b0a-4e81-84f9-e86cdd9a5cb4",
                        ppc_TransactionType = "3",
                        ppc_UniqueMerchantTxnID = trnsOrderId,
                        ppc_DIA_SECRET = hash,
                        ppc_DIA_SECRET_TYPE = "sha256"
                    });

                    var content = new StringContent(dataString, Encoding.UTF8, "application/x-www-form-urlencoded");
                    var response = await httpClient.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    ErrorModel nn2j = new ErrorModel();
                    nn2j.agId = "Pinelab PG2";
                    nn2j.payload = "";
                    nn2j.reqTime = "";
                    nn2j.requestId = trnsOrderId == null ? "Test" : trnsOrderId;
                    nn2j.respTime = responseContent;
                    nn2j.uid = response.ReasonPhrase;
                    nn2j.jsonBody = "responseContent";
                    nn2j.statuscode = response.IsSuccessStatusCode;
                    _context.errorModels.Add(nn2j);
                    _context.SaveChanges();
                    //if (response.IsSuccessStatusCode)
                    //{
                    var data = JsonConvert.DeserializeObject<PaymentResponse?>(responseContent);

                    ErrorModel nn2jn = new ErrorModel();
                    nn2jn.agId = "Pinelab PG3";
                    nn2jn.payload = "";
                    nn2jn.reqTime = "";
                    nn2jn.requestId = trnsOrderId == null ? "Test" : trnsOrderId;
                    nn2jn.respTime = responseContent;
                    nn2jn.uid = data.ppc_PinePGTransactionID;
                    nn2jn.jsonBody = userdetails.Name;
                    nn2jn.statuscode = response.IsSuccessStatusCode;
                    _context.errorModels.Add(nn2jn);
                    _context.SaveChanges();

                    if (data != null)
                    {
                        //TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                        //DateTime? dt = DateTime.Now;
                        //DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(dt.Value, istTimeZone);
                        PayIn payIn1 = new PayIn();
                        var ddd = DateTime.Now;
                        var gg = Convert.ToInt32(data.ppc_Amount) / 100;
                        var commm = gg * userdetails.Margin / 100;
                        var distibutercommission = (gg / 100) * userdetails.DistributerMarigin / 100;
                        var paymancommission = (gg / 100) * paymanComm.Lyra / 100;

                        payIn1.OrderId = data.ppc_UniqueMerchantTxnID;
                        payIn1.OrderRefNumber = data.ppc_UniqueMerchantTxnID;
                        payIn1.Currency = "INR";
                        payIn1.UserId = userdetails.UserId == null ? "" : userdetails.UserId;
                        payIn1.MobileNumber = phone == null ? "" : phone;
                        payIn1.Amount = gg;
                        payIn1.Sttaus = data.ppc_TxnResponseMessage;
                        payIn1.Refound = 0;
                        payIn1.GateWay = 5;
                        payIn1.CreatedDate = DateTime.Now;
                        payIn1.PayInCommission = commm;
                        payIn1.DistributerUserId = userdetails.DistributeruserId;
                        payIn1.DistibuterCommission = distibutercommission;
                        payIn1.PaymanCommission = paymancommission;
                        payIn1.IssueBank = "";
                        _context.payIns.Add(payIn1);
                        _context.SaveChanges();
                    }

                    if (data.ppc_TxnResponseMessage == "SUCCESS")
                    {
                        TempData["PaymentSucess"] = "You have " + Convert.ToInt32(data.ppc_Amount) / 100 + " Transferred Successfully";
                    }
                    else
                    {
                        TempData["PaymentSucess"] = "You have " + Convert.ToInt32(data.ppc_Amount) / 100 + " Transferred failed";
                    }
                    //}
                    //else
                    //{
                    //    TempData["PaymentSucess"] = "You have  Transferred failed";
                    //    // Handle error response
                    //    Console.WriteLine($"Error: {response.StatusCode}");
                    //    Console.WriteLine(responseContent);
                    //}

                    ErrorModel nnju = new ErrorModel();
                    nnju.agId = "Pinelab PG";
                    nnju.payload = "";
                    nnju.reqTime = "";
                    nnju.requestId = trnsOrderId == null ? "Test" : trnsOrderId;
                    nnju.respTime = responseContent;
                    nnju.uid = response.ReasonPhrase;
                    nnju.jsonBody = "responseContent";
                    nnju.statuscode = response.IsSuccessStatusCode;
                    _context.errorModels.Add(nnju);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.WriteLine($"Exception: {ex.Message}");
            }
            return RedirectToAction("PayManHome", "PayMan");
        }

        private string CreateDataString(object data)
        {
            var sortedKeys = data.GetType().GetProperties()
                .Select(property => new { Key = property.Name, Value = property.GetValue(data) })
                .OrderBy(item => item.Key)
                .Select(item => $"{item.Key}={item.Value}")
                .ToArray();

            return string.Join("&", sortedKeys);
        }
        private string GetPaymentMethods(payment_mode paymentMode)
        {
            var methods = new List<string>();
            var conditions =
                new[]
                {
                new { condition = paymentMode.cards, value = "1" },
                new { condition = paymentMode.netbanking, value = "3" },
                new { condition = paymentMode.emi, value = "4" },
                new { condition = paymentMode.cardless_emi, value = "19" },
                new { condition = paymentMode.upi, value = "10" },
                new { condition = paymentMode.wallet, value = "11" },
                new { condition = paymentMode.debit_emi, value = "14" },
                new { condition = paymentMode.prebooking, value = "16" },
                new { condition = paymentMode.bnpl, value = "17" },
                new { condition = paymentMode.paybypoints, value = "20" }
                };

            foreach (var condition in conditions)
                if (condition.condition)
                    methods.Add(condition.value);

            return string.Join(',', methods);
        }


        [HttpGet]
        public IActionResult CreditCardPayment(string? Amount)
        {
            var res = _context.payOutConfics.FirstOrDefault();
            var model = new BankViewModel
            {
                Options = _context.bankDetails.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.BankName
                }).OrderBy(t => t.Text).ToList()

            };
            model.Options.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Select a bank",
                Selected = true
            });
            model.AvaliableAmount = Amount;
            model.PayOutAmount = GetSefexBalance(res.SefexPay).Result;
            return View(model);
        }
        [HttpPost]
        public async Task<JsonResult> CreditCardPayment(BankViewModel bankViewModel)
        {

            PInelabsPayoutResponse pInelabsPayoutResponse = new PInelabsPayoutResponse();
            string staus = string.Empty;
            string OrderrefNo = string.Empty;
            var userId = HttpContext.Session.GetString("UserId");
            var userdetails = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
            JWT_Generator generator = new JWT_Generator();
            var test = generator.GenerateToken();
            var apiUrl = "https://api.pluralonline.com/payouts/v2/payments/banks"; // Replace with your API URL
            var bearerToken = test; // Replace with your actual bearer token
            ///var jsonContent = "{\"key1\":\"value1\", \"key2\":\"value2\"}"; // Replace with your actual JSON content

            PInelabsPayoutAmount amount1 = new PInelabsPayoutAmount
            {
                currency = "INR",
                value = Convert.ToInt32(bankViewModel.BillAmount + "00")
            };

            Guid guid = Guid.Parse(bankViewModel.SelectedOption);
            var ifsccode = _context.bankDetails.Where(t => t.Id == guid).Select(t => t.IfscCode).FirstOrDefault();

            // Instantiate the TransactionRequest class and assign values
            PInelabsPayoutTransactionRequest request = new PInelabsPayoutTransactionRequest
            {
                clientReferenceId = Guid.NewGuid().ToString(), // Generating a new GUID
                payeeName = bankViewModel.AccountHolderName,
                accountNumber = bankViewModel.CardNumber,
                branchCode = ifsccode,
                email = userdetails.Email,
                phone = bankViewModel.PhoneNumber,
                amount = amount1,
                mode = "NEFT",
                remarks = "NA"
            };
            var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(request);

            using (var client = new HttpClient())
            {
                // Set the base address for the client
                client.BaseAddress = new Uri(apiUrl);

                // Add the bearer token to the Authorization header
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                // Create the StringContent object with the JSON content
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                try
                {
                    // Send a POST request to the specified endpoint with the JSON content
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                    // Check if the response was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read and process the response content
                        string responseContent = await response.Content.ReadAsStringAsync();
                        pInelabsPayoutResponse = JsonConvert.DeserializeObject<PInelabsPayoutResponse>(responseContent);

                        var dbdh = PinelabsStatus(bearerToken, "https://api.pluralonline.com/payouts/v2/payments", pInelabsPayoutResponse.paymentReferenceId).Result;
                        //Console.WriteLine("Response Content: " + responseContent);
                        PayOutTransectionDetails obj = new PayOutTransectionDetails();
                        var aamount = pInelabsPayoutResponse.amount.value / 100;
                        var bnsd = DateTime.Now;
                        obj.payoutId = pInelabsPayoutResponse.clientReferenceId;
                        obj.mobileNo = bankViewModel.PhoneNumber;
                        obj.txnAmount = aamount.ToString();
                        obj.accountNo = pInelabsPayoutResponse.accountNumber.ToString();
                        obj.ifscCode = ifsccode;
                        obj.customerId = "Credit Card pay";
                        obj.userId = userId;
                        obj.beneId = "";
                        obj.accountHolderName = pInelabsPayoutResponse.payeeName;
                        obj.aggregatorId = "";
                        obj.txnStatus = dbdh.ToString();
                        obj.bankStatus = dbdh.ToString();
                        obj.spkRefNo = "";
                        obj.statusCode = "";
                        obj.statusDesc = "NEFT";
                        obj.orderRefNo = pInelabsPayoutResponse.paymentReferenceId;
                        obj.customerName = pInelabsPayoutResponse.payeeName;
                        obj.aggregtorName = "";
                        obj.emailId = pInelabsPayoutResponse.email;
                        obj.txnType = "NEFT";
                        obj.PayoutCommission = 15;
                        obj.CreatedDate = bnsd;

                        _context.payOutTransectionDetails.Add(obj);
                        _context.SaveChanges();
                        TempData["PaymentSucess"] = "Credit card pay transection Sucess!";
                        if (dbdh.ToString() == "PROCESSED" || dbdh.ToString() == "PENDING")
                        {
                            staus = "Sucess";
                            OrderrefNo = pInelabsPayoutResponse.paymentReferenceId;
                        }
                        else
                        {
                            staus = dbdh.ToString();
                            OrderrefNo = "";
                        }
                    }
                    else
                    {
                        ErrorModel nn = new ErrorModel();
                        nn.agId = "Credit card pay";
                        nn.payload = "";
                        nn.reqTime = "";
                        nn.requestId = jsonContent;
                        nn.respTime = "";
                        nn.uid = response.ReasonPhrase;
                        nn.jsonBody = "";
                        nn.statuscode = response.IsSuccessStatusCode;
                        _context.errorModels.Add(nn);
                        _context.SaveChanges();
                        TempData["PayOutSucess"] = "credit card pay transection failed!";
                        return Json(new { Success = false, objRoot = new { staus = staus, OrderrefNo = OrderrefNo } });
                    }
                }
                catch (Exception ex)
                {
                    TempData["PayOutSucess"] = "credit card pay transection failed!";
                    return Json(new { Success = false, objRoot = new { staus = staus, OrderrefNo = OrderrefNo } });
                }
            }
            return Json(new { Success = true, objRoot = new { staus = staus, OrderrefNo = OrderrefNo } });
        }
        [HttpGet]
        public JsonResult GetBankDescription(Guid id)
        {
            var bank = _context.accountVerificationBanks.FirstOrDefault(p => p.Id == id);
            if (bank != null)
            {
                return Json(new { description = bank.IFSCCode });
            }
            return Json(new { description = string.Empty });
        }


        [HttpGet]
        public IActionResult PayOutExportToExcel(string? PayOutSearch)
        {
            PayInHistoryDTO transactions = new PayInHistoryDTO();
            List<PayInHistory> payInHistory = new List<PayInHistory>();
            if (PayOutSearch == null)
            {
                PayOutSearch = DateTime.Now.ToString("yyyy-MM-dd");
            }
            var userId = HttpContext.Session.GetString("UserId");
            var userdetails = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
            List<PayOutTransectionDetails> payOutTransectionDetails = new List<PayOutTransectionDetails>();
            var PayOuttotal = _context.payOutTransectionDetails.Where(t => t.userId == userId && t.txnStatus == "PENDING").ToList();
            if (PayOuttotal != null)
            {
                payOutTransectionDetails = PayOuttotal.Where(t => t.CreatedDate?.ToString("yyyy-MM-dd") == PayOutSearch).ToList();
            }
            //var data = GetData(); // Fetch your data here


            var data = GetData();

            // Create a new workbook
            using (var workbook = new XLWorkbook())
            {
                // Add a worksheet
                var worksheet = workbook.Worksheets.Add("People");

                // Add headers
                worksheet.Cell(1, 1).Value = "Name";
                worksheet.Cell(1, 2).Value = "Age";

                // Add data
                for (int i = 0; i < data.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = data[i].Name;
                    worksheet.Cell(i + 2, 2).Value = data[i].Age;
                }

                // Save the workbook to a MemoryStream
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var fileBytes = stream.ToArray();

                    // Return the file as a downloadable file
                    return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "People.xlsx");
                }

            }
        }

        public class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }
        private List<Person> GetData()
        {
            // For demonstration, return a list of people
            return new List<Person>
        {
            new Person { Name = "John Doe", Age = 30 },
            new Person { Name = "Jane Smith", Age = 25 }
        };
        }

        private async Task<BinLookupResponse> GetCardTypeDeatils(string bin)
        {
            // string bin = "55358305";

            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"https://lookup.binlist.net/{bin}");

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content as a string
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    // Parse the JSON response into a JObject
                    var otpResponse = JsonConvert.DeserializeObject<BinLookupResponse>(jsonResponse);

                    return otpResponse;
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                    return null;
                }
            }
        }


        [HttpGet]
        public IActionResult GetPassbook()
        {

            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);
            if (user == null || user.App == false)
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }
                PassBooksList passBooks = new PassBooksList();
                List<PassBook> objPayInPassbook = new List<PassBook>();
                var objPayIns = _context.payIns.Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedDate).Take(40).ToList();
                var objPayOuts = _context.payOutTransectionDetails.Where(t => t.userId == userId).OrderByDescending(t => t.CreatedDate).Take(40).ToList();
                if (objPayIns.Count > 0)
                {
                    foreach (var item in objPayIns)
                    {
                        TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                        DateTime? dt = item.CreatedDate;
                        DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(dt.Value, istTimeZone);

                        PassBook passBook = new PassBook();
                        passBook.TransectionId = item.OrderId.ToUpper();
                        passBook.UserId = userId;
                        passBook.Amount = item.Amount.ToString();
                        passBook.AccountNo = "";
                        passBook.AccountHolderName = "";
                        passBook.Status = (item.Sttaus == "PAID" || item.Sttaus == "captured" || item.Sttaus == "success") ? "Success" : item.Sttaus;
                        passBook.txtType = "IMPS";
                        passBook.CreatedDate = item.CreatedDate.ToString("M/dd/yyyy HH:mm:ss");
                        passBook.commission = item.PayInCommission.ToString();
                        passBook.Currency = "";
                        passBook.GateWayType = "PAY IN";
                        objPayInPassbook.Add(passBook);

                    }
                }
                if (objPayOuts.Count > 0)
                {
                    foreach (var item in objPayOuts)
                    {
                        TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                        DateTime? dt = item.CreatedDate;
                        DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(dt.Value, istTimeZone);

                        PassBook passBook = new PassBook();
                        passBook.TransectionId = item.orderRefNo.ToUpper();
                        passBook.UserId = userId;
                        passBook.Amount = item.txnAmount.ToString();
                        passBook.AccountNo = item.accountNo;
                        passBook.AccountHolderName = item.accountHolderName;
                        passBook.Status = (item.txnStatus == "PENDING" || item.txnStatus == "PROCESSED") ? "Success" : item.txnStatus;
                        passBook.txtType = "IMPS";
                        passBook.CreatedDate = item.CreatedDate?.ToString("M/dd/yyyy HH:mm:ss");
                        passBook.commission = item.PayoutCommission.ToString();
                        passBook.Currency = "INR";
                        passBook.GateWayType = "PAY OUT";
                        passBook.IfscCode = item.ifscCode;
                        objPayInPassbook.Add(passBook);

                    }
                }
                var result = objPayInPassbook.OrderByDescending(t => t.CreatedDate).ToList();
                passBooks.passBooks = result;
                return View(passBooks);
            }
            else
            {
                if (string.IsNullOrEmpty(appPhone))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }

                PassBooksList passBooks = new PassBooksList();
                List<PassBook> objPayInPassbook = new List<PassBook>();

                var objPayIns = _context.payManHistories
                    .Where(t => t.UserPhone == appPhone)
                    .OrderByDescending(t => t.Created)
                    .Take(70)
                    .ToList();

                if (objPayIns.Any())
                {
                    foreach (var item in objPayIns)
                    {
                        var passBook = new PassBook
                        {
                            TransectionId = item.TxnId?.ToUpper(),
                            UserId = appPhone,
                            Amount = item.Amount.ToString(),
                            AccountNo = item.CardNumber,
                            Status = item.Status == true ? "Success" : "Failed",
                            txtType = "IMPS",
                            CreatedDate = item.Created.HasValue
                                ? item.Created.Value.ToString("M/dd/yyyy HH:mm:ss")
                                : string.Empty,
                            Currency = string.Empty,
                            GateWayType = item.Mode == "PayIn" ? "PAY IN" : item.Mode
                        };

                        if (item.Mode == "PayOut" || item.Mode == "CC Bill")
                        {
                            var payOut = _context.payManPayOuts
                                .FirstOrDefault(t => t.RefId == item.TxnId);

                            passBook.AccountHolderName = payOut?.AccountHolderName ?? "N/A";
                            passBook.commission = payOut?.PayoutCommission.ToString() ?? "0";
                        }
                        else
                        {
                            var payIn = _context.payManPayIns
                                .FirstOrDefault(t => t.EasePayId == item.TxnId);

                            passBook.AccountHolderName = "PAYMAN";
                            passBook.commission = payIn?.PayInCommission.ToString() ?? "0";
                        }

                        objPayInPassbook.Add(passBook);
                    }
                }

                var result = objPayInPassbook
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();

                passBooks.passBooks = result;

                return View(passBooks);

            }


        }
        [HttpGet]
        public IActionResult InActivateAccount(Guid? Id)
        {

            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);
            if (user == null || user.App == false)
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }
                // Guid guid = Guid.Parse(Id);
                var ojResult = _context.beneficiaryAccounts.Where(t => t.Id == Id).FirstOrDefault();
                if (ojResult != null)
                {
                    ojResult.IsActive = false;
                    _context.beneficiaryAccounts.Update(ojResult);
                    _context.SaveChanges();
                }
                TempData["VerificationStatus"] = "Account deleted Succesfully !";
                return RedirectToAction("AddBeneficiaryAccounts", "PayMan");
            }
            else
            {
                if (string.IsNullOrEmpty(appPhone))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }
                // Guid guid = Guid.Parse(Id);
                var ojResult = _context.payManBeneficiaryAccounts.Where(t => t.Id == Id).FirstOrDefault();
                if (ojResult != null)
                {
                    ojResult.IsActive = false;
                    _context.payManBeneficiaryAccounts.Update(ojResult);
                    _context.SaveChanges();
                }
                TempData["VerificationStatus"] = "Account deleted Succesfully !";
                return RedirectToAction("AddBeneficiaryAccounts", "PayMan");
            }
        }


             

        [HttpGet]
        public async Task<IActionResult> AdharNumberVerify(string? AdharNumber, string? refid, string? otp)
        {
            AdharVerification adharVerification = new AdharVerification();
            if (AdharNumber != null && (refid != null || otp != null))
            {
                adharVerification.AdharNumber = AdharNumber;
                if (otp != null)
                {
                    adharVerification.AdharOtp = Convert.ToInt32(otp);
                }

                if (refid != "null" && refid != null)
                {
                    adharVerification.AdharRefId = Convert.ToInt32(refid);
                }

            }
            return View(adharVerification);
        }


        [HttpPost]
        public async Task<JsonResult> VerifyAdharOTP(string? adharnumber1, string? otp)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var hfdh = _context.adharVerifications.Where(t => t.UserId == userId).FirstOrDefault();


            // Client ID and Secret
            var clientId = "CF678492CQOFTSG8OI4S73DVFVN0";
            var clientSecret = "cfsk_ma_prod_1a60ece5af3976c5258d7afd8aa0a8eb_063c29b6";

            var client = new HttpClient();

            // Set the base address and headers
            client.BaseAddress = new Uri("https://api.cashfree.com");
            client.DefaultRequestHeaders.Add("x-client-id", clientId);
            client.DefaultRequestHeaders.Add("x-client-secret", clientSecret);

            // Create the request data
            var requestData = new
            {
                otp = otp,
                ref_id = hfdh.AdharRefId.ToString()
            };

            // Serialize the data to JSON
            var json = System.Text.Json.JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the request
            var response = await client.PostAsync("/verification/offline-aadhaar/verify", content);

            // Read and output the response
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
            var otpResponse = JsonConvert.DeserializeObject<VerifyAdharOTPResponse>(responseString);
            if (otpResponse.Status == "VALID")
            {
                hfdh.AdharOtp = Convert.ToInt32(otp);
                hfdh.AdharAdress = otpResponse.Address;
                hfdh.AdharName = otpResponse.Name;
                _context.adharVerifications.Update(hfdh);
                _context.SaveChanges();
                //TempData["PaymentSucess"] = "Adhar KYC Completed!";
                return Json(new { sucess = true });
            }
            else
            {
                TempData["PaymentSucess"] = "Adhar KYC Failed Can you please try once again.";
                return Json(new { sucess = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAdharDetails(AdharVerification adharVerification)
        {
            const long maxFileSize = 300 * 1024; // 250 KB in bytes
            if (adharVerification.FrontImageFile.Length > maxFileSize)
            {
                AdharVerification adharVerification1 = new AdharVerification();
                TempData["FileSize"] = "Front image size cannot exceed 300 KB.";
                return View(adharVerification1);
            }
            if (adharVerification.BackImageFile.Length > maxFileSize)
            {
                AdharVerification adharVerification2 = new AdharVerification();
                TempData["FileSize"] = "Back image size cannot exceed 300 KB.";
                return View(adharVerification2);
            }
            if (adharVerification.PANImageFile.Length > maxFileSize)
            {
                AdharVerification adharVerification3 = new AdharVerification();
                TempData["FileSize"] = "Back image size cannot exceed 300 KB.";
                return View(adharVerification3);
            }

            var adharDetails = _context.adharVerifications.Where(t => t.AdharNumber == adharVerification.AdharNumber).FirstOrDefault();
            if (adharDetails.AdharNumber != null)
            {
                ImageModel imageModel = new ImageModel();
                if (adharVerification.FrontImageFile != null && adharVerification.FrontImageFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await adharVerification.FrontImageFile.CopyToAsync(memoryStream);
                        imageModel.FrontImage = memoryStream.ToArray();
                    }
                }

                if (adharVerification.BackImageFile != null && adharVerification.BackImageFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await adharVerification.BackImageFile.CopyToAsync(memoryStream);
                        imageModel.BackImage = memoryStream.ToArray();
                    }
                }
                if (adharVerification.PANImageFile != null && adharVerification.PANImageFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await adharVerification.PANImageFile.CopyToAsync(memoryStream);
                        imageModel.PanImage = memoryStream.ToArray();
                    }
                }
                adharDetails.PanNumber = adharVerification.PanNumber;
                adharDetails.BackImage = imageModel.BackImage;
                adharDetails.FrontImage = imageModel.FrontImage;
                adharDetails.PanImage = imageModel.PanImage;
                adharDetails.AdharVerified = true;
                _context.adharVerifications.Update(adharDetails);
                _context.SaveChanges();
                TempData["PaymentSucess"] = "Adhar KYC Completed!";
                return RedirectToAction("PayManHome", "PayMan");
            }
            else
            {

                return View();
            }
        }

        [HttpGet]
        public IActionResult ChangePassword(int? Id)
        {
            ChangePassword changePassword = new ChangePassword();
            changePassword.Id = Id ?? 0;
            return View(changePassword);
        }
        [HttpPost]
        public IActionResult ChangePassword(ChangePassword changePassword)
        {
            var userdetails = _context.users.Where(t => t.Id == changePassword.Id).FirstOrDefault();
            bool status = false;
            if (userdetails != null)
            {
                if (userdetails.Password == changePassword.OldPassword)
                {
                    if (changePassword.NewPassword == changePassword.ConfirmPassword)
                    {
                        userdetails.Password = changePassword.ConfirmPassword;
                        userdetails.ChangePassword = true;
                        _context.Update(userdetails);
                        _context.SaveChanges();
                        status = true;
                    }
                    else
                    {
                        TempData["validPass"] = "New and Confirm Passwords not mmatching ";
                        return View(changePassword);
                    }
                }
                else
                {
                    TempData["validPass"] = "Please enter valid old password.";
                    return View(changePassword);
                }

            }
            if (status == true)
            {
                TempData["singup"] = "Your password has been changed successfully. Please log in using your new password.";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View(changePassword);
            }

        }




        [HttpGet]
        public IActionResult AppChangePassword(Guid Id)
        {
            return View();
        }
        [HttpPost]
        public IActionResult AppChangePassword(AppChangePassword changePassword)
        {
            var userdetails = _context.payManUsers.Where(t => t.Id == changePassword.Id).FirstOrDefault();

            var userdetails1 = _context.users.Where(t => t.Email == userdetails.Email).FirstOrDefault();

            bool status = false;
            if (userdetails != null)
            {
                if (userdetails.WedPWD == changePassword.OldPassword)
                {
                    if (changePassword.NewPassword == changePassword.ConfirmPassword)
                    {
                        userdetails.WedPWD = changePassword.ConfirmPassword;
                        userdetails.Web = true;
                        _context.Update(userdetails);
                        _context.SaveChanges();

                        if(userdetails1 != null)
                        {
                            userdetails1.Password = changePassword.ConfirmPassword;
                            userdetails1.ChangePassword = true;
                            _context.Update(userdetails1);
                            _context.SaveChanges();
                        }
                        status = true;
                    }
                    else
                    {
                        TempData["validPass"] = "New and Confirm Passwords not mmatching ";
                        return View(changePassword);
                    }
                }
                else
                {
                    TempData["validPass"] = "Please enter valid old password.";
                    return View(changePassword);
                }

            }
            if (status == true)
            {
                TempData["singup"] = "Your password has been changed successfully. Please log in using your new password.";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View(changePassword);
            }

        }

        private static readonly HttpClient client = new HttpClient();

        public async Task<BinLookupResponse> LookupBinAsync(string bin)
        {
            // Perform the GET request to the binlist.net API
            HttpResponseMessage response = await client.GetAsync($"https://lookup.binlist.net/{bin}");

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Read the response content as a string
                string jsonResponse = await response.Content.ReadAsStringAsync();

                // Parse the JSON response into a BinLookupResponse object
                var binResponse = JsonConvert.DeserializeObject<BinLookupResponse>(jsonResponse);

                return binResponse;
            }
            else
            {
                // Log or handle the error accordingly
                Console.WriteLine("Error: " + response.StatusCode);
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAdharDetais()
        {
            // string enterCustomerNumber = "553583059856885587785";
            //var hfj = await LookupBinAsync(enterCustomerNumber.Substring(0, 8));
            // Retrieve all products from the database
            var products = _context.adharVerifications.Where(t => t.AdharVerified == true).ToList();

            // Pass the products to the view
            return View(products);
        }
        public IActionResult GetImage(Guid id)
        {
            var product = _context.adharVerifications.Find(id);

            if (product?.FrontImage != null)
            {
                return File(product.FrontImage, "image/jpeg");
            }

            return NotFound(); // Or return a default image
        }

        public IActionResult BackGetImage(Guid id)
        {
            var product = _context.adharVerifications.Find(id);

            if (product?.BackImage != null)
            {
                return File(product.BackImage, "image/jpeg");
            }

            return NotFound(); // Or return a default image
        }

        public IActionResult PanGetImage(Guid id)
        {
            var product = _context.adharVerifications.Find(id);

            if (product?.PanImage != null)
            {
                return File(product.PanImage, "image/jpeg");
            }

            return NotFound(); // Or return a default image
        }


        public DashboardViewModel PayManDashBoard(decimal avalAmount)
        {
            DashboardViewModel dashboardViewModel = new DashboardViewModel();
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var PaymanGatewayMarigins = _context.payManGateWayMarigins.FirstOrDefault();
            var PayIntotal = _context.payIns.ToList();
            var totalPayInsRazorPay = PayIntotal.Where(t => t.CreatedDate.ToString("yyyy-MM-dd") == today && t.Sttaus == "captured" && !t.IssueBank.Contains("MasterCard")).ToList();
            var totalPayInsRazorPayMaster = PayIntotal.Where(t => t.CreatedDate.ToString("yyyy-MM-dd") == today && t.Sttaus == "captured" && t.IssueBank.Contains("MasterCard")).ToList();
            var totalPayInsLyra = PayIntotal.Where(t => t.CreatedDate.ToString("yyyy-MM-dd") == today && t.Sttaus == "PAID").ToList();
            var totalRazorPaySum = totalPayInsRazorPay.Sum(t => t.Amount) + totalPayInsRazorPayMaster.Sum(t => t.Amount);
            var sumOfPayIns = totalRazorPaySum + totalPayInsLyra.Sum(t => t.Amount);

            var totalRazarpayafterbankCommissionVisa = (double)totalPayInsRazorPay.Sum(t => t.Amount) * (double)1.003 / 100;
            var totalRazarpayafterbankCommissionmaster = (double)totalPayInsRazorPayMaster.Sum(t => t.Amount) * (double)2.301 / 100;
            var totalLyraafterbankCommission = (double)totalPayInsLyra.Sum(t => t.Amount) * (double)PaymanGatewayMarigins.Lyra / 100;
            var razorpaycomm = totalRazarpayafterbankCommissionVisa + totalRazarpayafterbankCommissionmaster;

            var totalAmountafterBankCommission = razorpaycomm + totalLyraafterbankCommission;

            var userslist = _context.users.Where(t => t.IsActive == true).ToList();
            decimal UsersAvaliblelis = 0;
            foreach (var item in userslist)
            {
                var payManHome = GetAvaliableAmount(item.UserId, true);
                UsersAvaliblelis = UsersAvaliblelis + payManHome.Amount;
            }
            var hjvv = sumOfPayIns - totalAmountafterBankCommission;
            var afterFounds = hjvv - (double)PaymanGatewayMarigins.PayManFounds;

            var paymanProfit = afterFounds - (double)UsersAvaliblelis;

            var today1 = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            var totalPayInsRazorPaySettelment = PayIntotal.Where(t => t.CreatedDate.ToString("yyyy-MM-dd") == today1 && t.Sttaus == "captured").ToList();
            var totalPayInsLyraSettelment = PayIntotal.Where(t => t.CreatedDate.ToString("yyyy-MM-dd") == today1 && t.Sttaus == "PAID").ToList();
            var totalRazarpayafterbankCommissionSettelment = (double)totalPayInsRazorPaySettelment.Sum(t => t.Amount) * (double)PaymanGatewayMarigins.RazorPay / 100;
            var totalLyraafterbankCommissionSettelment = (double)totalPayInsLyraSettelment.Sum(t => t.Amount) * (double)PaymanGatewayMarigins.Lyra / 100;
            var lyraresult = totalPayInsLyraSettelment.Sum(t => t.Amount) - totalLyraafterbankCommissionSettelment;
            var razorpayresult = totalPayInsRazorPaySettelment.Sum(t => t.Amount) - totalRazarpayafterbankCommissionSettelment;

            dashboardViewModel.PayInAmount = sumOfPayIns;
            dashboardViewModel.PayInAmountAfterBankCommission = (decimal)totalAmountafterBankCommission;
            dashboardViewModel.PayManProfilt = (decimal)paymanProfit + avalAmount;
            dashboardViewModel.PayOutUsersAvaliableBalance = UsersAvaliblelis;
            dashboardViewModel.PayManFounds = PaymanGatewayMarigins.PayManFounds;


            dashboardViewModel.PayInAmountLyraPayCommissionSettelment = (decimal)lyraresult;
            dashboardViewModel.PayInAmountRazorPayCommissionSettelment = (decimal)razorpayresult;
            dashboardViewModel.PayInAmountLyraPaySettelment = (decimal)totalPayInsLyraSettelment.Sum(t => t.Amount);
            dashboardViewModel.PayInAmountRazorPaySettelment = (decimal)totalPayInsRazorPaySettelment.Sum(t => t.Amount);



            return dashboardViewModel;
        }

        [HttpGet]
        public async  Task<IActionResult> SearchBeneficiaryAccounts(string search, int pg = 1)
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);
            if (user == null || user.App == false)
            {
                var userId = HttpContext.Session.GetString("UserId");
                var payManHome = GetAvaliableAmount(userId);
                var amount = payManHome.Amount;
                // Filter the beneficiary accounts based on the search term (search by name or mobile)
                var filteredAccounts = search == null ? _context.beneficiaryAccounts
                    .Where(b => b.UserId == userId && b.IsActive == true)
                    .ToList() :
                    _context.beneficiaryAccounts
                    .Where(b => (b.ContactName.Contains(search) || b.MobileNumber.Contains(search) || b.AccountNo.Contains(search)) && b.UserId == userId && b.IsActive == true)
                    .ToList();

                var model = new AddBeneficiaryAccountsDTO
                {
                    UserId = userId,
                    SefexFinalBalance = payManHome.SefexFinalBalance.ToString(),
                    AvaliableAmount = amount == null ? "0.00" : amount.ToString(),
                    BeneficiaryAccounts = filteredAccounts
                    // Add any other properties of AddBeneficiaryAccountsDTO that need to be set
                };

                const int pageSize = 10;
                if (pg < 1)
                    pg = 1;

                int resCount = model.BeneficiaryAccounts.Count();
                var pager = new Pager(resCount, pg, pageSize);
                int recSkip = (pg - 1) * pageSize;
                var data = model.BeneficiaryAccounts.Skip(recSkip).Take(pager.PageSize).ToList();

                this.ViewBag.Pager = pager;

                // Return only the partial view with the filtered results
                return PartialView("_BeneficiaryGrid", model);

            }
            else
            {
                var avlamount = await GetUserWalletAmount(appPhone);
                var pineLabsAmount = await GetPinelabsAmount();
                var amount = avlamount;
                // Filter the beneficiary accounts based on the search term (search by name or mobile)
                var filteredAccounts = search == null ? _context.payManBeneficiaryAccounts
                    .Where(b => b.UserPhone == appPhone && b.IsActive == true)
                    .ToList() :
                    _context.payManBeneficiaryAccounts
                    .Where(b => (b.ContactName.Contains(search) || b.MobileNumber.Contains(search) || b.AccountNo.Contains(search)) && b.UserPhone == appPhone && b.IsActive == true)
                    .ToList();

                var model = new AddBeneficiaryAccountsDTO
                {
                    SefexFinalBalance = pineLabsAmount.ToString(),
                    AvaliableAmount = amount == null ? "0.00" : amount.ToString(),
                    PayManBeneficiaryAccounts = filteredAccounts
                    // Add any other properties of AddBeneficiaryAccountsDTO that need to be set
                };

                const int pageSize = 10;
                if (pg < 1)
                    pg = 1;

                int resCount = model.PayManBeneficiaryAccounts.Count();
                var pager = new Pager(resCount, pg, pageSize);
                int recSkip = (pg - 1) * pageSize;
                var data = model.PayManBeneficiaryAccounts.Skip(recSkip).Take(pager.PageSize).ToList();

                this.ViewBag.Pager = pager;

                // Return only the partial view with the filtered results
                return PartialView("_BeneficiaryGrid", model);
            }



        }

        [HttpGet]
        public async Task<IActionResult> AddFailedTransections()
        {
            //var token = "EAAWy60a5AkgBOxPYMFHUDaR7BZB43RsrmGyZAQhK20nE8yUhan5XeefUZBKsOgyh0EsaJQB14ELmZB630iJeBFsHLecOih2LPFwG1guqpzGZBUQG5YN9ALDELNPngYv0GN5M6y7jLjWMkIbZCcCVSpOt6BJHIN4HR2DMmFsKBJg2OjKevWtXWOr4VTP4yKZBKVzzAZDZD"; // Replace with your token
            //var phoneNumberId ="+1555-170-2952"; // Replace with your phone number ID
            //var toPhoneNumber = "9849800697"; // Receiver's phone number

            //var messagePayload = new
            //{
            //    messaging_product = "whatsapp",
            //    to = toPhoneNumber,
            //    type = "text",
            //    text = new { body = "Hello, this is a test message from WhatsApp Cloud API!" }
            //};

            //var url = $"https://graph.facebook.com/v16.0/{phoneNumberId}/messages";

            //using var httpClient = new HttpClient();
            //httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            //var content = new StringContent(JsonConvert.SerializeObject(messagePayload), Encoding.UTF8, "application/json");

            //var response = await httpClient.PostAsync(url, content);

            //if (response.IsSuccessStatusCode)
            //{
            //    Console.WriteLine("Message sent successfully.");
            //}
            //else
            //{
            //    var error = await response.Content.ReadAsStringAsync();
            //    Console.WriteLine($"Error: {error}");
            //}



            AddFailedrecords addFailedrecords = new AddFailedrecords();

            addFailedrecords.Options = _context.users.Select(p => new SelectListItem
            {
                Value = p.UserId.ToString(),
                Text = p.Name
            }).OrderBy(t => t.Text).ToList();

            addFailedrecords.Options.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Select a User",
                Selected = true
            });

            return View(addFailedrecords);
        }

        [HttpPost]
        public async Task<IActionResult> AddFailedTransections(AddFailedrecords addFailedRecords)
        {
            var user = _context.users.FirstOrDefault(u => u.UserId == addFailedRecords.SelectedOption);


            var appUser = _context.payManUsers.FirstOrDefault(t => t.Phone == user.Phone);
            if (appUser == null || appUser.App == false)
            {
                var gatewayMargin = _context.payManGateWayMarigins.FirstOrDefault();

                TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime utcDateTime = DateTime.UtcNow; // Get the current UTC time
                DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, istTimeZone);

                if (user == null || gatewayMargin == null)
                {
                    TempData["ErrorMsg"] = "Invalid user or gateway margin details.";
                    return RedirectToAction("PayManHome", "PayMan");
                }

                var amount = Convert.ToInt32(addFailedRecords.Amount);
                var selectedGateway = Convert.ToInt32(addFailedRecords.selectedGateway);

                int gateway = selectedGateway switch
                {
                    2 or 3 => 2,
                    4 => 3,
                    5 or 6 or 7 => 6,
                    8 or 9 => 8,
                    10 or 11 => 10,
                    _ => 0
                };

                decimal commission = selectedGateway switch
                {
                    3 => amount * 2.301m / 100,
                    6 => amount * 1.75m / 100,
                    4 => amount * 1.50m / 100,
                    8 => amount * 1.42m / 100,
                    9 => amount * 1.62m / 100,
                    10 => amount * 1.42m / 100,
                    11 => amount * 1.62m / 100,
                    2 or 5 or 7 => amount * user.Margin / 100,
                    _ => 0m
                };

                var distributorCommission = amount * user.DistributerMarigin / 100;
                var paymanCommission = amount * gatewayMargin.Lyra / 100;

                var payIn = new PayIn
                {
                    OrderId = addFailedRecords.OrderId,
                    OrderRefNumber = addFailedRecords.OrderRefIf,
                    Currency = "INR",
                    UserId = user.UserId ?? string.Empty,
                    MobileNumber = addFailedRecords.Cardno,
                    Amount = amount,
                    Sttaus = selectedGateway == 2 ? "captured" : "success",
                    Refound = 0,
                    GateWay = gateway,
                    CreatedDate = istDateTime,
                    PayInCommission = commission,
                    DistributerUserId = user.DistributeruserId,
                    DistibuterCommission = distributorCommission,
                    PaymanCommission = paymanCommission,
                    IssueBank = "IssueCard_" + addFailedRecords.Cardtype
                };

                _context.payIns.Add(payIn);
                _context.SaveChanges();

                TempData["SuccessMsg"] = $"Amount added to {user.Name}";
                return RedirectToAction("PayManHome", "PayMan");

            }
            else
            {
                // Convert UTC to IST
                var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                // Null checks for required inputs
                if (addFailedRecords == null || string.IsNullOrWhiteSpace(addFailedRecords.OrderId) || string.IsNullOrWhiteSpace(addFailedRecords.Cardno))
                {
                    TempData["ErrorMsg"] = "Invalid transaction data.";
                    return RedirectToAction("PayManHome", "PayMan");
                }

                // Get user from phone (or however you identify user)
                if (appUser == null)
                {
                    TempData["ErrorMsg"] = "User not found.";
                    return RedirectToAction("PayManHome", "PayMan");
                }

                // Get gateway
                var easebuzzGateway = await _context.PayManGateways.FirstOrDefaultAsync(t => t.GatewayName == "Easebuzz");
                if (easebuzzGateway == null)
                {
                    TempData["ErrorMsg"] = "Easebuzz gateway config missing.";
                    return RedirectToAction("PayManHome", "PayMan");
                }

                // Check for duplicate TxnId
                bool txnExists = await _context.payManPayIns.AnyAsync(x => x.TxnId == addFailedRecords.OrderRefIf);
                if (txnExists)
                {
                    TempData["ErrorMsg"] = "Duplicate transaction.";
                    return RedirectToAction("PayManHome", "PayMan");
                }

                // Convert amount
                decimal amount = Convert.ToDecimal(addFailedRecords.Amount);

                // Calculate commissions
                decimal userCommission = (amount * Convert.ToDecimal(appUser.Margin)) / 100;
                decimal paymanCommission = (decimal)((easebuzzGateway.PaymanComm > 0) ? (amount * easebuzzGateway.PaymanComm) / 100 : 0);

                
                try
                {
                    // Add PayIn record
                    var payInApp = new PayManPayIn
                    {
                        UserId = appUser.Id,
                        UserPhone = appUser.Phone,
                        TxnId = addFailedRecords.OrderId,
                        EasePayId = addFailedRecords.OrderRefIf,
                        Email = appUser.Email,
                        CardNumber = addFailedRecords.Cardno,
                        EaseCardNum = addFailedRecords.Cardno,
                        Amount = amount,
                        Gateway = "Easebuzz",
                        BankName = "",
                        CardBrand = addFailedRecords.Cardtype,
                        IsCorporate = "",
                        PayInCommission = userCommission,
                        PaymanCommission = paymanCommission,
                        Created = istTime,
                        Status = true,
                        Result = "Success",
                        Device = "Web"
                    };

                    _context.payManPayIns.Add(payInApp);
                    await _context.SaveChangesAsync();

                    // Get current wallet balance
                    var avlamount = await GetUserWalletAmount(appUser.Phone);

                    // Add PayIn history record
                    var payInHistory = new PayManHistory
                    {
                        UserId = appUser.Id,
                        UserPhone = appUser.Phone,
                        TxnId = addFailedRecords.OrderRefIf,
                        Amount = amount,
                        CardNumber = addFailedRecords.Cardno,
                        Mode = "PayIn",
                        Status = true,
                        Created = istTime,
                        AvlBalance = Convert.ToDecimal(avlamount),
                        PayInId = payInApp.Id
                    };

                    _context.payManHistories.Add(payInHistory);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMsg"] = $"Amount ₹{amount} added to {appUser.FirstName}";
                    return RedirectToAction("PayManHome", "PayMan");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMsg"] = "Transaction failed: " + ex.Message;
                    return RedirectToAction("PayManHome", "PayMan");
                }
            }
        }




        [HttpGet]
        public async Task<JsonResult> SrrConfirmPayment(string paymentid, string orderdd, string paymentstatus, string issuerbank, string issuecard, string selectedgat, string amount, string enterCustomerNumber, string cardtype)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var phone = HttpContext.Session.GetString("Phone");

            var userdetails = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
            var paymanComm = _context.payManGateWayMarigins.FirstOrDefault();

            try
            {

                PayIn payIn = new PayIn();
                TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime utcDateTime = DateTime.UtcNow; // Get the current UTC time
                DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, istTimeZone);
                var commissssion = userdetails.Margin;

                if (cardtype == "business" || cardtype == "BUSINESS")
                {
                    commissssion = userdetails.Margin + (decimal)2.10;
                }
                else if (issuecard == "MasterCard")
                {
                    
                    if(userId == "MUIEP41" || userId == "MUIEP10")
                    {
                        commissssion = userdetails.Margin + (decimal)0.70;
                    }
                    else
                    {
                        commissssion = (decimal)userdetails.MasterMargin;  
                    }
                }
                else if (issuerbank == "HDFC" || issuerbank == "hdfc")
                {
                    commissssion = userdetails.Margin + (decimal)0.10;
                }
                
                var commm = Convert.ToInt32(amount) * commissssion / 100;


                decimal kjfds;
                if((userId == "MUIEP41" || userId == "MUIEP10") && issuecard == "MasterCard")
                {
                    kjfds = (decimal)0.25;
                }
                else
                {
                    if (issuecard == "MasterCard")
                    {
                        kjfds = (decimal)userdetails.MasterMargin - (decimal)1.75; 
                    }
                    else
                    {
                        kjfds = userdetails.DistributerMarigin;
                    }
                        
                }

                var distibutercommission = Convert.ToInt32(amount) * kjfds / 100;
                var paymancommission = Convert.ToInt32(amount) * paymanComm.RazorPay / 100;

                payIn.OrderId = orderdd;
                payIn.OrderRefNumber = paymentid;
                payIn.Currency = "INR";
                payIn.UserId = userId;
                payIn.MobileNumber = enterCustomerNumber;
                payIn.Amount = Convert.ToInt32(amount);
                payIn.Refound = 0;
                payIn.Sttaus = paymentstatus;
                payIn.GateWay = Convert.ToInt32(selectedgat);
                payIn.CreatedDate = istDateTime;
                payIn.PayInCommission = commm;

                payIn.DistributerUserId = userdetails.DistributeruserId;
                payIn.DistibuterCommission = distibutercommission;
                payIn.PaymanCommission = paymancommission;
                payIn.IssueBank =  issuerbank+ "_"+ cardtype + "_" + issuecard;
                _context.payIns.Add(payIn);
                _context.SaveChanges();


                return Json(new { Success = true, Paymentid = paymentid, orderid= orderdd, enteredamount = amount ,cardnumber= enterCustomerNumber });
            }
            catch (Exception ex)
            {
                //  TempData["PaymentSucess"] = "You have " + amount + " failed transection.";
                return Json(new { Success = false, Paymentid = paymentid });
            }


        }



        [HttpGet]
        public IActionResult GenerateInvoicePdf()
        {
            GenerateInvoicePdf generateInvoicePdf = new GenerateInvoicePdf();
            generateInvoicePdf.Options = _context.users.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name
            }).OrderBy(t => t.Text).ToList();

            generateInvoicePdf.Options.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Select a User",
                Selected = true
            });
            return View(generateInvoicePdf);
        }

        public IActionResult GetAmountsByUser(int userId)
        {
            var userdetails = _context.users.Where(t => t.Id == userId).FirstOrDefault();
            var amounts = _context.payOutTransectionDetails // Assuming you have transactions or a similar entity to retrieve amounts
                .Where(t => t.userId == userdetails.UserId).OrderByDescending(t=>t.CreatedDate)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.txnAmount.ToString() // Format amount, e.g., $40,000
                }).Take(20)
                .ToList();

            return Json(amounts);
        }
        public IActionResult GetUtrByAmount(string amount)
        {
            Guid guid;
            if (!Guid.TryParse(amount, out guid))
            {
                return BadRequest("Invalid amount format.");
            }

            var result = _context.payOutTransectionDetails
                .Where(t => t.Id == guid)
                .Select(t => new
                {
                    AccountNo = t.accountNo,
                    IFSC = t.ifscCode,
                    UTRNumber = t.orderRefNo.ToUpper() // Replace with the actual field name for UTR ID if different
                })
                .FirstOrDefault();

            if (result == null)
            {
                return NotFound("Transaction not found.");
            }

            return Json(result);
        }

        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string email)
        {
            var isValidUser = _context.users.FirstOrDefault(t => t.Email == email);
            var isValidAppUser = _context.payManUsers.FirstOrDefault(t => t.Email == email);

            if (isValidAppUser == null)
            {
                TempData["validPass"] = "Please enter a valid email associated with your Payman account.";
                return View();
            }

            if (isValidUser == null)
            {
                TempData["validPass"] = "Please enter a valid email associated with your Payman account.";
                return View();
            }
            else
            {
                var pass = GenerateRandomPassword(10);
                var body = HTMLTemp1(isValidUser.Name, pass);
                SendEmail(email, "Password Reset Notification - Payman Fintech", body);

                isValidUser.ChangePassword = false;
                isValidUser.Password = pass;
                _context.Update(isValidUser);
                _context.SaveChanges();

                isValidAppUser.Web = false;
                isValidAppUser.WedPWD = pass;
                _context.Update(isValidAppUser);
                _context.SaveChanges();
                TempData["LogIn"] = "A new password has been sent to your registered email address. Please use the new password to log in.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public IActionResult InitiatePayment(int amount, string cardnumber)
        {
            var paymentModel = new AirPayPaymentModel
            {
                MerchantId = "328263",
                Username = "sgk5kmAbHU",
                Password = "A34trWXP",
                Amount = amount.ToString("F2"),
                Currency = "INR",
                OrderId = Guid.NewGuid().ToString()
            };

            // Simulate saving order details in a session/database (optional)
            HttpContext.Session.SetString("OrderDetails", JsonConvert.SerializeObject(paymentModel));

            return Json(new { orderId = paymentModel.OrderId }); // Return JSON response for AJAX
        }


        [HttpGet]
        public IActionResult SubmitToAirPay()
        {
            var phone = HttpContext.Session.GetString("OrderDetails");
            var billerResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<AirPayPaymentModel>(phone);

            decimal amount1 = Decimal.Parse(billerResponse.Amount);
            string orderId = "PAYMAN" + DateTime.Now.ToString("yyyymmddhhmmss");
            // Collecting form data
            // Manually assigning values to variables
            var buyerEmail = "buyer@example.com";
            var buyerPhone = "9999999999";
            var buyerFirstName = "John";
            var buyerLastName = "Doe";
            var buyerAddress = "123 Street Name";
            var buyerCity = "CityName";

            // Additional variables with manual assignments
            var buyerState = "StateName";
            var buyerCountry = "CountryName";
            var buyerPinCode = "123456";
           // var orderId = Guid.NewGuid().ToString(); // Generate a unique order ID
            var amount = amount1.ToString("F2");
            var currency = "356";
            var isoCurrency = "INR";
            var customVar = "custom123";
            var txnSubtype = "01";


            // Replace these values with actual credentials
            string secret = "qmGrG8SY6U9j2CkN";
            string username = "sgk5kmAbHU";
            string password = "A34trWXP";
            string mercid = "328263";

            // Building the data string for checksum
            var allData = $"{buyerEmail}{buyerFirstName}{buyerLastName}{buyerAddress}{buyerCity}{buyerState}{buyerCountry}{amount}{orderId}";
            var date = DateTime.Now.ToString("yyyy-MM-dd");

            // Generate private key, key, and checksum
            var privateKey = GenerateSha256Hash($"{secret}@{username}:|:{password}");
            var key = GenerateSha256Hash($"{username}~:~{password}");
            var checksum = GenerateSha256Hash($"{key}@{allData}{date}");

            // Populate the model
            var model = new SendtoAirpayModel
            {
                BuyerEmail = buyerEmail,
                BuyerPhone = buyerPhone,
                BuyerFirstName = buyerFirstName,
                BuyerLastName = buyerLastName,
                BuyerAddress = buyerAddress,
                BuyerCity = buyerCity,
                BuyerState = buyerState,
                BuyerCountry = buyerCountry,
                BuyerPinCode = buyerPinCode,
                MerchantId = mercid,
                OrderId = orderId,
                Currency = currency,
                IsoCurrency = isoCurrency,
                Amount = amount,
                TxnSubtype = txnSubtype,
                CustomVar = customVar,
                PrivateKey = privateKey,
                Checksum = checksum
            };

            // Pass the model to the view
            return View("Sendtoairpay", model);
        }
        private string GenerateSha256Hash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        public ActionResult SucessResponsefromairpay()
        {
            return ViewBag();
        }
        public ActionResult CancelResponsefromairpay()
        {
            return ViewBag();
        }

        //public ActionResult responsefromairpay()
        //{
        //    // Capture the callback data sent by AirPay
        //    var transactionid = Request.Form["TRANSACTIONID"];
        //    var aptransactionid = Request.Form["APTRANSACTIONID"];
        //    var amount = Request.Form["AMOUNT"];
        //    var transactionstatus = Request.Form["TRANSACTIONSTATUS"];
        //    var message = Request.Form["MESSAGE"];
        //    var ap_SecureHash = Request.Form["ap_SecureHash"];
        //    var customvar = Request.Form["CUSTOMVAR"];
        //    var chmod = Request.Form["CHMOD"];

        //    // Create the data string for CRC validation
        //    var crcdata = transactionid + ":" + aptransactionid + ":" + amount + ":" + transactionstatus + ":" + message + ":" + mercid + ":" + username;

        //    if (chmod == "upi")
        //    {
        //        var customervpa = Request.Form["CUSTOMERVPA"];
        //        crcdata = crcdata + ":" + customervpa;
        //    }

        //    // If any required fields are empty, return failure
        //    if (string.IsNullOrEmpty(transactionid) || string.IsNullOrEmpty(aptransactionid) ||
        //        string.IsNullOrEmpty(amount) || string.IsNullOrEmpty(transactionstatus) ||
        //        string.IsNullOrEmpty(message) || string.IsNullOrEmpty(ap_SecureHash))
        //    {
        //        ViewBag.EStatus = "Empty Data";
        //        return View("Failureairpay");
        //    }

        //    // Perform CRC validation
        //    string strCRC = CRCCode(crcdata, ap_SecureHash);

        //    // Log CRC data for debugging (optional)
        //    System.Console.WriteLine(crcdata);

        //    // Set the ViewBag for the response
        //    ViewBag.Transactionid = transactionid;
        //    ViewBag.Aptransactionid = aptransactionid;
        //    ViewBag.Amount = amount;
        //    ViewBag.Transactionstatus = transactionstatus;
        //    ViewBag.Message = message;
        //    ViewBag.Customvar = customvar;

        //    // Check if the CRC is valid
        //    if (strCRC == "true")
        //    {
        //        // Check the transaction status
        //        if (transactionstatus == "200")
        //        {
        //            // Transaction was successful
        //            ViewBag.SuccessStatus = "SUCCESS TRANSACTION";
        //            return View("Successairpay");
        //        }
        //        else
        //        {
        //            // Transaction failed
        //            ViewBag.FailedStatus = "FAILED TRANSACTION";
        //            return View("Failureairpay");
        //        }
        //    }
        //    else
        //    {
        //        // Secure hash mismatch
        //        ViewBag.HashStatus = "SECURE HASH MISMATCH";
        //        return View("Failureairpay");
        //    }
        //}

        //// Helper method to calculate the CRC (checksum)
        //private string CRCCode(string crcdata, string secureHash)
        //{
        //    var generatedHash = GenerateCRC(crcdata); // Generate CRC hash from the data
        //    return generatedHash == secureHash ? "true" : "false"; // Compare and return true/false
        //}

        //// Method to generate CRC (secure hash)
        //private string GenerateCRC(string data)
        //{
        //    // Add the logic to generate the CRC (hash) based on your secret, username, password, and data
        //    var key = $"{username}~:~{password}";
        //    var hashData = key + "@" + data;
        //    return sha256_hash(hashData); // Assuming you have a method to hash it using SHA256
        //}

        //// SHA256 hash method (you can adjust it according to your needs)
        //private string sha256_hash(string rawData)
        //{
        //    using (var sha256 = System.Security.Cryptography.SHA256.Create())
        //    {
        //        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
        //        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        //    }
        //}



        [HttpGet]
        public async Task<IActionResult> FastTag(string searchTerm = "")
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);

            if (user == null || user.App == false)
            {
                var userId = HttpContext.Session.GetString("UserId");
                var jncnz = GetAvaliableAmount(userId, true);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }

                var balanceStr = await BlanceCheck();

                if (TempData["Amount"] != null)
                {
                    var amount = TempData["Amount"].ToString(); // Read and convert to string
                    ViewBag.Amount = amount; // You can pass it to the view if needed
                }

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                    client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
                    client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "13.200.194.39");
                    client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", "490007");

                    string url = "https://api.instantpay.in/marketplace/utilityPayments/billers";
                    var requestData = new
                    {
                        pagination = new { pageNumber = 1, recordsPerPage = 100 },
                        filters = new { categoryKey = "C15", updatedAfterDate = "" }
                    };

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        return View("Error");
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var billerResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<BillerResponse>(responseContent);


                    var filteredBillers = string.IsNullOrWhiteSpace(searchTerm)
                ? billerResponse.Data.Records
                : billerResponse.Data.Records
                    .Where(b => b.BillerName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                    var viewModel = new BillerViewModel
                    {
                        Billers = filteredBillers.Where(t => t.BillerStatus == "ACTIVE" && t.IsAvailable == true).ToList(),
                        SearchTerm = searchTerm,
                        AvlAmount = jncnz.Amount,
                        InstanPaYAvlBal = balanceStr
                    };

                    return View(viewModel);
                }
            }
            else
            {
                if (user.CCBill == false)
                {
                    TempData["useralrdeyexists"] = "Service not available please contact Admin";
                    return RedirectToAction("PayManHome", "PayMan");
                }
                var avlamount = await GetUserWalletAmount(appPhone);


                var balanceStr = await BlanceCheck();

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                    client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
                    client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "13.200.194.39");
                    client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", "490007");

                    string url = "https://api.instantpay.in/marketplace/utilityPayments/billers";
                    var requestData = new
                    {
                        pagination = new { pageNumber = 1, recordsPerPage = 100 },
                        filters = new { categoryKey = "C15", updatedAfterDate = "" }
                    };

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        return View("Error");
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var billerResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<BillerResponse>(responseContent);


                    var filteredBillers = string.IsNullOrWhiteSpace(searchTerm)
                ? billerResponse.Data.Records
                : billerResponse.Data.Records
                    .Where(b => b.BillerName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                    var viewModel = new BillerViewModel
                    {
                        Billers = filteredBillers.Where(t => t.BillerStatus == "ACTIVE" && t.IsAvailable == true).ToList(),
                        SearchTerm = searchTerm,
                        AvlAmount = Convert.ToDecimal(avlamount),
                        InstanPaYAvlBal = balanceStr
                    };

                    return View(viewModel);
                }
            }


        }


        //[HttpGet("credit-card-bill-pay")]
        [HttpGet]
        public async Task<IActionResult> CreditCardBillPay(string searchTerm = "")
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);

            if (user == null || user.App == false)
            {
                var userId = HttpContext.Session.GetString("UserId");
                var jncnz = GetAvaliableAmount(userId, true);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }

                var balanceStr = await BlanceCheck();

                if (TempData["Amount"] != null)
                {
                    var amount = TempData["Amount"].ToString(); // Read and convert to string
                    ViewBag.Amount = amount; // You can pass it to the view if needed
                }

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                    client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
                    client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "13.200.194.39");
                    client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", "490007");

                    string url = "https://api.instantpay.in/marketplace/utilityPayments/billers";
                    var requestData = new
                    {
                        pagination = new { pageNumber = 1, recordsPerPage = 100 },
                        filters = new { categoryKey = "C15", updatedAfterDate = "" }
                    };

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        return View("Error");
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var billerResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<BillerResponse>(responseContent);


                    var filteredBillers = string.IsNullOrWhiteSpace(searchTerm)
                ? billerResponse.Data.Records
                : billerResponse.Data.Records
                    .Where(b => b.BillerName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                    var viewModel = new BillerViewModel
                    {
                        Billers = filteredBillers.Where(t => t.BillerStatus == "ACTIVE" && t.IsAvailable == true).ToList(),
                        SearchTerm = searchTerm,
                        AvlAmount = jncnz.Amount,
                        InstanPaYAvlBal = balanceStr
                    };

                    return View(viewModel);
                }
            }
            else
            {
                if(user.CCBill == false)
                {
                    TempData["useralrdeyexists"] = "Service not available please contact Admin";
                    return RedirectToAction("PayManHome", "PayMan");
                }
                var avlamount = await GetUserWalletAmount(appPhone);


                var balanceStr = await BlanceCheck();

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                    client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
                    client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "13.200.194.39");
                    client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", "490007");

                    string url = "https://api.instantpay.in/marketplace/utilityPayments/billers";
                    var requestData = new
                    {
                        pagination = new { pageNumber = 1, recordsPerPage = 100 },
                        filters = new { categoryKey = "C15", updatedAfterDate = "" }
                    };

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        return View("Error");
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var billerResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<BillerResponse>(responseContent);


                    var filteredBillers = string.IsNullOrWhiteSpace(searchTerm)
                ? billerResponse.Data.Records
                : billerResponse.Data.Records
                    .Where(b => b.BillerName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                    var viewModel = new BillerViewModel
                    {
                        Billers = filteredBillers.Where(t => t.BillerStatus == "ACTIVE" && t.IsAvailable == true).ToList(),
                        SearchTerm = searchTerm,
                        AvlAmount = Convert.ToDecimal(avlamount),
                        InstanPaYAvlBal = balanceStr,
                        UserName = user.FirstName
                    };

                    return View(viewModel);
                }
            }

             
        }


        [HttpPost]
        public async Task<IActionResult> FetchBill([FromBody] BillRequest request)
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);

            if (user == null || user.App == false)
            {

                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }

                var mobile = request.Mobile;
                string dh = string.Empty;
                var lastfour = request.LastFourDigits;

                if (string.IsNullOrWhiteSpace(request.LastFourDigits) ||
                    string.IsNullOrWhiteSpace(request.Mobile) ||
                    string.IsNullOrWhiteSpace(request.CustomerMobile))
                {
                    return BadRequest(new { message = "Invalid request. Please provide all required fields." });
                }
                BillResponse1 customerParamDetailResponse = new BillResponse1();
                using (HttpClient client = new HttpClient())
                {
                    // Set up the headers
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
                    client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "13.200.194.39");
                    client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", "490007");



                    string url = "https://api.instantpay.in/marketplace/utilityPayments/billerDetails";

                    // Construct JSON dynamically with the billerId
                    string jsonBody = $"{{ \"billerId\": \"{request.BillerId}\" }}";
                    var content1 = new StringContent(jsonBody, Encoding.UTF8, "application/json");


                    var response1 = await client.PostAsync(url, content1);
                    if (!response1.IsSuccessStatusCode)
                    {
                        var billDetails1 = new BillResponse
                        {
                            Success = false,
                            DueAmount = "",
                            DueDate = "",
                            CustomerName = "",
                            EnquiryReferenceId = ""
                            //CardType = "SBI Card"
                        };

                        return Ok(billDetails1);
                    }

                    var responseContent1 = await response1.Content.ReadAsStringAsync();
                    var customerParamDetailResponse1 = Newtonsoft.Json.JsonConvert.DeserializeObject<BillerResponse11>(responseContent1);


                    if (customerParamDetailResponse1.Data != null)
                    {
                        var hgds = customerParamDetailResponse1.Data.PaymentModes.Where(t => t.Name == "Wallet").FirstOrDefault();
                        dh = hgds == null ? "UPI" : hgds.Name.ToString();
                        foreach (var item in customerParamDetailResponse1.Data.Parameters)
                        {
                            if (item.Name == "param1")
                            {
                                if (item.MaxLength == 4)
                                {
                                    mobile = request.LastFourDigits;
                                }
                                else
                                {
                                    mobile = request.Mobile;
                                }

                            }
                            else
                            {
                                if (item.MaxLength == 10)
                                {
                                    lastfour = request.Mobile;
                                }
                                else
                                {
                                    lastfour = request.LastFourDigits;
                                }
                            }
                        }
                    }

                    // Prepare the JSON data using billerId from the request model
                    var jsonData = $@"
        {{
            ""billerId"": ""{request.BillerId}"",
            ""initChannel"": ""AGT"",
            ""externalRef"": ""123TESTiiiii"",
            ""inputParameters"": {{
                ""param1"": ""{mobile}"",
                ""param2"": ""{lastfour}""
            }},
            ""deviceInfo"": {{
                ""mac"": ""BC-BE-33-65-E6-AC"",
                ""ip"": ""103.254.205.164""
            }},
            ""remarks"": {{
                ""param1"": ""{request.Mobile}"",
                ""param2"": ""{request.LastFourDigits}""
            }},
            ""transactionAmount"": 10
        }}";

                    // Set up the content
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                    // Make the POST request
                    var response = await client.PostAsync("https://api.instantpay.in/marketplace/utilityPayments/prePaymentEnquiry", content);

                    // Check the response
                    if (!response.IsSuccessStatusCode)
                    {
                        var billDetails1 = new BillResponse
                        {
                            Success = false,
                            DueAmount = "",
                            DueDate = "",
                            CustomerName = "",
                            EnquiryReferenceId = "",
                            Param1 = "",
                            Param2 = "",
                            Wallet = ""
                            //CardType = "SBI Card"
                        };

                        return Ok(billDetails1);
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    customerParamDetailResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<BillResponse1>(responseContent);
                }


                BillResponse billResponse = new BillResponse();
                // Simulate fetching bill details (Replace with actual DB/API call)
                if (customerParamDetailResponse != null && customerParamDetailResponse.Data != null && customerParamDetailResponse.Data.CustomerName != null)
                {
                    billResponse = new BillResponse
                    {
                        Success = true,
                        DueAmount = customerParamDetailResponse.Data.BillAmount,
                        DueDate = customerParamDetailResponse.Data.BillDueDate,
                        CustomerName = customerParamDetailResponse.Data.CustomerName,
                        EnquiryReferenceId = customerParamDetailResponse.Data.EnquiryReferenceId,
                        Status = customerParamDetailResponse.Status,
                        Param1 = mobile,
                        Param2 = lastfour,
                        Wallet = dh
                        //CardType = "SBI Card"
                    };

                }
                else
                {
                    billResponse = new BillResponse
                    {
                        Success = false,
                        DueAmount = "",
                        DueDate = "",
                        CustomerName = "",
                        EnquiryReferenceId = "",
                        Status = customerParamDetailResponse.Status,
                        Param1 = "",
                        Param2 = "",
                        Wallet = ""
                        //CardType = "SBI Card"
                    };
                }

                return Ok(billResponse);
            }
            else
            {
                if (string.IsNullOrEmpty(appPhone))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }
                var mobile = request.Mobile;
                string dh = string.Empty;
                var lastfour = request.LastFourDigits;

                if (string.IsNullOrWhiteSpace(request.LastFourDigits) ||
                    string.IsNullOrWhiteSpace(request.Mobile) ||
                    string.IsNullOrWhiteSpace(request.CustomerMobile))
                {
                    return BadRequest(new { message = "Invalid request. Please provide all required fields." });
                }
                BillResponse1 customerParamDetailResponse = new BillResponse1();
                using (HttpClient client = new HttpClient())
                {
                    // Set up the headers
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
                    client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "13.200.194.39");
                    client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", "490007");



                    string url = "https://api.instantpay.in/marketplace/utilityPayments/billerDetails";

                    // Construct JSON dynamically with the billerId
                    string jsonBody = $"{{ \"billerId\": \"{request.BillerId}\" }}";
                    var content1 = new StringContent(jsonBody, Encoding.UTF8, "application/json");


                    var response1 = await client.PostAsync(url, content1);
                    if (!response1.IsSuccessStatusCode)
                    {
                        var billDetails1 = new BillResponse
                        {
                            Success = false,
                            DueAmount = "",
                            DueDate = "",
                            CustomerName = "",
                            EnquiryReferenceId = ""
                            //CardType = "SBI Card"
                        };

                        return Ok(billDetails1);
                    }

                    var responseContent1 = await response1.Content.ReadAsStringAsync();
                    var customerParamDetailResponse1 = Newtonsoft.Json.JsonConvert.DeserializeObject<BillerResponse11>(responseContent1);


                    if (customerParamDetailResponse1.Data != null)
                    {
                        var hgds = customerParamDetailResponse1.Data.PaymentModes.Where(t => t.Name == "Wallet").FirstOrDefault();
                        dh = hgds == null ? "UPI" : hgds.Name.ToString();
                        foreach (var item in customerParamDetailResponse1.Data.Parameters)
                        {
                            if (item.Name == "param1")
                            {
                                if (item.MaxLength == 4)
                                {
                                    mobile = request.LastFourDigits;
                                }
                                else
                                {
                                    mobile = request.Mobile;
                                }

                            }
                            else
                            {
                                if (item.MaxLength == 10)
                                {
                                    lastfour = request.Mobile;
                                }
                                else
                                {
                                    lastfour = request.LastFourDigits;
                                }
                            }
                        }
                    }

                    // Prepare the JSON data using billerId from the request model
                    var jsonData = $@"
        {{
            ""billerId"": ""{request.BillerId}"",
            ""initChannel"": ""AGT"",
            ""externalRef"": ""123TESTiiiii"",
            ""inputParameters"": {{
                ""param1"": ""{mobile}"",
                ""param2"": ""{lastfour}""
            }},
            ""deviceInfo"": {{
                ""mac"": ""BC-BE-33-65-E6-AC"",
                ""ip"": ""103.254.205.164""
            }},
            ""remarks"": {{
                ""param1"": ""{request.Mobile}"",
                ""param2"": ""{request.LastFourDigits}""
            }},
            ""transactionAmount"": 10
        }}";

                    // Set up the content
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                    // Make the POST request
                    var response = await client.PostAsync("https://api.instantpay.in/marketplace/utilityPayments/prePaymentEnquiry", content);

                    // Check the response
                    if (!response.IsSuccessStatusCode)
                    {
                        var billDetails1 = new BillResponse
                        {
                            Success = false,
                            DueAmount = "",
                            DueDate = "",
                            CustomerName = "",
                            EnquiryReferenceId = "",
                            Param1 = "",
                            Param2 = "",
                            Wallet = ""
                            //CardType = "SBI Card"
                        };

                        return Ok(billDetails1);
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    customerParamDetailResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<BillResponse1>(responseContent);
                }


                BillResponse billResponse = new BillResponse();
                // Simulate fetching bill details (Replace with actual DB/API call)
                if (customerParamDetailResponse != null && customerParamDetailResponse.Data != null && customerParamDetailResponse.Data.CustomerName != null)
                {
                    billResponse = new BillResponse
                    {
                        Success = true,
                        DueAmount = customerParamDetailResponse.Data.BillAmount,
                        DueDate = customerParamDetailResponse.Data.BillDueDate,
                        CustomerName = customerParamDetailResponse.Data.CustomerName,
                        EnquiryReferenceId = customerParamDetailResponse.Data.EnquiryReferenceId,
                        Status = customerParamDetailResponse.Status,
                        Param1 = mobile,
                        Param2 = lastfour,
                        Wallet = dh
                        //CardType = "SBI Card"
                    };

                }
                else
                {
                    billResponse = new BillResponse
                    {
                        Success = false,
                        DueAmount = "",
                        DueDate = "",
                        CustomerName = "",
                        EnquiryReferenceId = "",
                        Status = customerParamDetailResponse.Status,
                        Param1 = "",
                        Param2 = "",
                        Wallet = ""
                        //CardType = "SBI Card"
                    };
                }

                return Ok(billResponse);
            }

        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromBody] BillRequest request)
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);

            if (user == null || user.App == false )
            {
                var userId = HttpContext.Session.GetString("UserId");
                var adharde = _context.adharVerifications.Where(t => t.UserId == userId).FirstOrDefault();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }

                if (request == null)
                {
                    PaymentResponseProcess paymentResponseProcess = new PaymentResponseProcess
                    {
                        Success = false,
                        Amount = "",
                        OrderId = "",
                        ReferenceId = "",
                        Category = "Credit Card",
                        BillerName = "ICICI Credit Card"
                    };

                    return Ok(paymentResponseProcess);
                }

                // Validate required fields
                if (string.IsNullOrEmpty(request.LastFourDigits) ||
                    string.IsNullOrEmpty(request.Mobile) ||
                    string.IsNullOrEmpty(request.CustomerMobile) ||
                    string.IsNullOrEmpty(request.BillerId) ||
                    request.Amount <= 0)
                {
                    PaymentResponseProcess paymentResponseProcess = new PaymentResponseProcess
                    {
                        Success = false,
                        Amount = "",
                        OrderId = "",
                        ReferenceId = "",
                        Category = "Credit Card",
                        BillerName = "ICICI Credit Card"
                    };

                    return Ok(paymentResponseProcess);
                }

                // Check balance
                //var balanceStr = await BlanceCheck();
                //if (!decimal.TryParse(balanceStr, out decimal balance))
                //{
                //    PaymentResponseProcess paymentResponseProcess = new PaymentResponseProcess
                //    {
                //        Success = false,
                //        Amount = "",
                //        OrderId = "",
                //        ReferenceId = "",
                //        Category = "Credit Card",
                //        BillerName = "ICICI Credit Card"
                //    };

                //    return Ok(paymentResponseProcess);
                //}

                var userdetails = _context.users.FirstOrDefault(t => t.UserId == userId);

                //if (balance >= request.Amount)
                //{
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
                    client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "13.200.194.39");
                    client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", "490007");

                    var paymentMode = request.Wallet;

                    object paymentInfo;

                    if (paymentMode == "UPI")
                    {
                        paymentInfo = new
                        {
                            Remarks = "VPA",
                            VPA = "9652724937@kotak"
                        };
                    }
                    else
                    {
                        paymentInfo = new
                        {
                            WalletName = "Forpay",
                            MobileNo = "7286887024"
                        };
                    }

                    var requestData = new
                    {
                        billerId = request.BillerId,
                        externalRef = "PAYMAN" + DateTime.Now.ToString("yyyymmddhhmmss"),
                        enquiryReferenceId = request.EnquiryReferenceId,
                        telecomCircle = "",
                        inputParameters = new { param1 = request.Param1, param2 = request.Param2 },
                        initChannel = "AGT",
                        deviceInfo = new
                        {
                            terminalId = "1998",
                            mobile = "7286887024",
                            postalCode = "505402",
                            geoCode = "28.6326,77.2175"
                        },
                        paymentMode = paymentMode,
                        paymentInfo = paymentInfo,

                        //paymentMode = "Wallet",
                        //paymentInfo = new { WalletName = "Forpay", MobileNo = "7286887024" },
                        remarks = new { param1 = request.Mobile, param2 = request.LastFourDigits },
                        transactionAmount = request.Amount,
                        customerPan = adharde.PanNumber
                    };

                    string json = System.Text.Json.JsonSerializer.Serialize(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response1 = await client.PostAsync("https://api.instantpay.in/marketplace/utilityPayments/payment", content);
                    string responseContent = await response1.Content.ReadAsStringAsync();

                    var transactionResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<TransactionResponse>(responseContent);

                    if (transactionResponse == null || transactionResponse.Data == null || transactionResponse.Data.Pool == null)
                    {
                        PaymentResponseProcess paymentResponseProcesss = new PaymentResponseProcess
                        {
                            Success = false,
                            Amount = "",
                            OrderId = "",
                            ReferenceId = "",
                            Category = "Credit Card",
                            BillerName = "ICICI Credit Card",
                            Status = transactionResponse.Status
                        };

                        return Ok(paymentResponseProcesss);
                    }

                    PaymentResponseProcess paymentResponseProcess = new PaymentResponseProcess
                    {
                        Success = true,
                        Amount = transactionResponse.Data.BillDetails.BillAmount,
                        OrderId = transactionResponse.Data.TxnReferenceId.ToUpper(),
                        ReferenceId = transactionResponse.Data.ExternalRef.ToUpper(),
                        Category = "Credit Card",
                        BillerName = "ICICI Credit Card",
                        Status = transactionResponse.Status
                    };

                    // Set the Indian Standard Time
                    TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                    DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istTimeZone);

                    PayOutTransectionDetails obj = new PayOutTransectionDetails
                    {
                        payoutId = transactionResponse.Data.TxnReferenceId,
                        mobileNo = transactionResponse.Data.BillerDetails.Account,
                        txnAmount = transactionResponse.Data.BillDetails.BillAmount,
                        accountNo = transactionResponse.Data.BillerDetails.Account,
                        ifscCode = "",
                        customerId = "CC Bill Pay",
                        userId = userdetails?.UserId ?? "Unknown",
                        beneId = "",
                        accountHolderName = transactionResponse.Data.BillDetails?.CustomerName ?? "Unknown",
                        aggregatorId = "",
                        txnStatus = "PROCESSED",
                        bankStatus = "PROCESSED",
                        spkRefNo = "",
                        statusCode = "",
                        statusDesc = "IMPS",
                        orderRefNo = transactionResponse.Data.PoolReferenceId,
                        customerName = transactionResponse.Data.BillDetails?.CustomerName ?? "Unknown",
                        aggregtorName = "",
                        emailId = userdetails?.Email ?? "Unknown",
                        txnType = "IMPS",
                        PayoutCommission = 15,
                        CreatedDate = istDateTime
                    };

                    _context.payOutTransectionDetails.Add(obj);
                    await _context.SaveChangesAsync();

                    return Ok(paymentResponseProcess);
                }
                //}
                //else
                //{
                //    PaymentResponseProcess paymentResponseProcess = new PaymentResponseProcess
                //    {
                //        Success = false,
                //        Amount = "",
                //        OrderId = "",
                //        ReferenceId = "",
                //        Category = "Credit Card",
                //        BillerName = "ICICI Credit Card"
                //    };

                //    return Ok(paymentResponseProcess);
                //}
            }
            else
            {

                if (string.IsNullOrEmpty(appPhone))
                {
                    return RedirectToAction("LogOut", "LogIn");
                }
                var documents = _context.userDocuments.Where(t => t.Phone == appPhone).FirstOrDefault();

                if (request == null)
                {
                    PaymentResponseProcess paymentResponseProcess = new PaymentResponseProcess
                    {
                        Success = false,
                        Amount = "",
                        OrderId = "",
                        ReferenceId = "",
                        Category = "Credit Card",
                        BillerName = "ICICI Credit Card"
                    };

                    return Ok(paymentResponseProcess);
                }

                // Validate required fields
                if (string.IsNullOrEmpty(request.LastFourDigits) ||
                    string.IsNullOrEmpty(request.Mobile) ||
                    string.IsNullOrEmpty(request.CustomerMobile) ||
                    string.IsNullOrEmpty(request.BillerId) ||
                    request.Amount <= 0)
                {
                    PaymentResponseProcess paymentResponseProcess = new PaymentResponseProcess
                    {
                        Success = false,
                        Amount = "",
                        OrderId = "",
                        ReferenceId = "",
                        Category = "Credit Card",
                        BillerName = "ICICI Credit Card"
                    };

                    return Ok(paymentResponseProcess);
                }

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
                    client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
                    client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "13.200.194.39");
                    client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", "490007");

                    var paymentMode = request.Wallet;

                    object paymentInfo;

                    if (paymentMode == "UPI")
                    {
                        paymentInfo = new
                        {
                            Remarks = "VPA",
                            VPA = "9652724937@kotak"
                        };
                    }
                    else
                    {
                        paymentInfo = new
                        {
                            WalletName = "Forpay",
                            MobileNo = "7286887024"
                        };
                    }

                    var requestData = new
                    {
                        billerId = request.BillerId,
                        externalRef = "PAYMAN" + DateTime.Now.ToString("yyyymmddhhmmss"),
                        enquiryReferenceId = request.EnquiryReferenceId,
                        telecomCircle = "",
                        inputParameters = new { param1 = request.Param1, param2 = request.Param2 },
                        initChannel = "AGT",
                        deviceInfo = new
                        {
                            terminalId = "1998",
                            mobile = "7286887024",
                            postalCode = "505402",
                            geoCode = "28.6326,77.2175"
                        },
                        paymentMode = paymentMode,
                        paymentInfo = paymentInfo,

                        //paymentMode = "Wallet",
                        //paymentInfo = new { WalletName = "Forpay", MobileNo = "7286887024" },
                        remarks = new { param1 = request.Mobile, param2 = request.LastFourDigits },
                        transactionAmount = request.Amount,
                        customerPan = documents.PanCardNumber
                    };

                    string json = System.Text.Json.JsonSerializer.Serialize(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response1 = await client.PostAsync("https://api.instantpay.in/marketplace/utilityPayments/payment", content);
                    string responseContent = await response1.Content.ReadAsStringAsync();

                    var transactionResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<TransactionResponse>(responseContent);

                    if (transactionResponse == null || transactionResponse.Data == null || transactionResponse.Data.Pool == null)
                    {
                        PaymentResponseProcess paymentResponseProcesss = new PaymentResponseProcess
                        {
                            Success = false,
                            Amount = "",
                            OrderId = "",
                            ReferenceId = "",
                            Category = "Credit Card",
                            BillerName = "ICICI Credit Card",
                            Status = transactionResponse.Status
                        };

                        return Ok(paymentResponseProcesss);
                    }

                    PaymentResponseProcess paymentResponseProcess = new PaymentResponseProcess
                    {
                        Success = true,
                        Amount = transactionResponse.Data.BillDetails.BillAmount,
                        OrderId = transactionResponse.Data.TxnReferenceId.ToUpper(),
                        ReferenceId = transactionResponse.Data.ExternalRef.ToUpper(),
                        Category = "Credit Card",
                        BillerName = "ICICI Credit Card",
                        Status = transactionResponse.Status
                    };

                    DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));


                    bool stus = false;
                    if (transactionResponse?.Status == "Transaction Under Process" || transactionResponse?.Status == "Transaction Successful")
                    {
                        stus = true;
                    }
                    var trandetails = new PayManPayOut
                    {
                        UserId = user.Id,
                        UserPhone = appPhone,
                        PayOutId = transactionResponse.Data.TxnReferenceId,
                        RefId = transactionResponse.Data.PoolReferenceId,
                        AccountHolderName = transactionResponse.Data.BillDetails?.CustomerName ?? "Unknown",
                        AccountNo = request.CustomerMobile,
                        IfscCode = "",
                        Amount = Convert.ToDecimal(request.Amount),
                        PayoutCommission = 15,
                        BeneId = transactionResponse?.Status,
                        PayOutType = "CC BILL",
                        TxnType = "IMPS",
                        Email = user.Email,
                        Status = stus,
                        DateTime = istDateTime,
                        Result = transactionResponse?.Status,
                        Device = "web"
                    };

                    _context.payManPayOuts.Add(trandetails);
                    await _context.SaveChangesAsync();

                    var userWalletAmount = await GetUserWalletAmount(appPhone);

                    var payInHistory = new PayManHistory
                    {
                        UserId = user.Id,
                        UserPhone = appPhone,
                        TxnId = transactionResponse.Data.TxnReferenceId,
                        Amount = Convert.ToDecimal(request.Amount),
                        CardNumber = request.CustomerMobile,
                        Mode = "CC Bill",
                        Status = stus,
                        Created = istDateTime,
                        AvlBalance = Convert.ToDecimal(userWalletAmount)
                    };
                    _context.payManHistories.Add(payInHistory);
                    await _context.SaveChangesAsync();

                    return Ok(paymentResponseProcess);
                }
                //}
                //else
                //{
                //    PaymentResponseProcess paymentResponseProcess = new PaymentResponseProcess
                //    {
                //        Success = false,
                //        Amount = "",
                //        OrderId = "",
                //        ReferenceId = "",
                //        Category = "Credit Card",
                //        BillerName = "ICICI Credit Card"
                //    };

                //    return Ok(paymentResponseProcess);
                //}
            }



        }


        public async Task<string> BlanceCheck()
        {
            BalanceResponse balanceResponse = new BalanceResponse();
            using (HttpClient client = new HttpClient())
            {
                // Set up headers
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
                client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
                client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "13.200.194.39"); //13.235.218.182
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
                    return "";
                }

                balanceResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<BalanceResponse>(responseContent);
                
            }
            return balanceResponse.Data == null ? "0.00" : balanceResponse.Data.Balance.Available;
        }

        public static string EncryptAadhaar(string aadhaarNumber, string encryptionKey)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(encryptionKey);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    byte[] aadhaarBytes = Encoding.UTF8.GetBytes(aadhaarNumber);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(aadhaarBytes, 0, aadhaarBytes.Length);

                    byte[] result = new byte[aes.IV.Length + encryptedBytes.Length];
                    Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
                    Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

                    return Convert.ToBase64String(result);
                }
            }
        }

        // [HttpPost("credit-card-bill-pay")]
        //public async Task<IActionResult> CreditCardBillPay(CreditCardBillPayViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    var request = new InstantPayRequest
        //    {
        //        Payer = new Payer
        //        {
        //            BankId = "0",
        //            BankProfileId = "0",
        //            AccountNumber = model.PayerAccountNumber,
        //            Name = model.PayerName,
        //            PaymentMode = "NETBANKING",
        //            CardNumber = "",
        //            CardSecurityCode = "",
        //            CardExpiry = new CardExpiry
        //            {
        //                Month = "",
        //                Year = ""
        //            },
        //            ReferenceNumber = ""
        //        },
        //        Payee = new Payee
        //        {
        //            AccountNumber = model.PayeeAccountNumber,
        //            Name = model.PayeeName
        //        },
        //        TransferMode = "CREDITCARD",
        //        TransferAmount = model.TransferAmount,
        //        ExternalRef = "BILLPAY1",
        //        Latitude = model.Latitude,
        //        Longitude = model.Longitude,
        //        Remarks = model.Remarks,
        //        AlertEmail = ""
        //    };

        //    var jsonRequest = System.Text.Json.JsonSerializer.Serialize(request);

        //    using (HttpClient client = new HttpClient())
        //    {
        //        // Add headers
        //        client.DefaultRequestHeaders.Add("Accept", "application/json");
        //        client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
        //        client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", "YWY3OTAzYzNlM2ExZTJlOQICjYngLiUetS9alAw+Pno=");
        //        client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", "7cbfae67dc982f208746416f850ce8875cdf98043a13cee1c4cf2b14c23d31fd");
        //        client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", "136.185.84.114");

        //        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        //        // Make the HTTP POST request
        //        HttpResponseMessage response = await client.PostAsync("https://api.instantpay.in/payments/payout", content);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            ViewBag.Message = "Payment processed successfully!";
        //            return View(new CreditCardBillPayViewModel());
        //        }
        //        else
        //        {
        //            var errorContent = await response.Content.ReadAsStringAsync();
        //            ViewBag.Error = $"Payment failed. Reason: {errorContent}";
        //            return View(model);
        //        }
        //    }
        //}



    public async Task<ActionResult> EbuzzStatus(
     string status = null,
     int amount = 0,
     string cardnumber = "",
     string TransectionId = null,
     string email = null,
     string cardType = null,
     string EasepayId = null,
     string BankName = "",
     string CardBrand = "",
     string CreditCardNum = "",
     string CreditCardHolderName = "",
     string CardholderMobileNo = ""
     )
        {
            // Normalize optional fields
            cardType ??= "NA";
            BankName = string.IsNullOrWhiteSpace(BankName) ? "NA" : BankName;
            CardBrand = string.IsNullOrWhiteSpace(CardBrand) ? "NA" : CardBrand;

            // Get user from session
            var appPhone = HttpContext.Session.GetString("AppPhone");
            if (string.IsNullOrEmpty(appPhone))
            {
                return RedirectToAction("WebLogout", "Login"); // or show error view
            }

            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == appPhone);
            if (user == null)
            {
                return RedirectToAction("WebLogout", "Login");
            }

            // Get gateway config
            var easebuzzGateway = await _context.PayManGateways.FirstOrDefaultAsync(t => t.GatewayName == "Easebuzz");

            // Check if this payment already exists (idempotency)
            var existing = await _context.payManPayIns
                .FirstOrDefaultAsync(t => t.EasePayId == EasepayId);

            if (existing == null)
            {
                var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                 TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                decimal margin = Convert.ToDecimal(user.Margin ?? "0");

                if (cardType.ToLower() == "true")
                    margin = user.CarporateCardMarigin ?? margin;
                else if (CardBrand.ToLower() == "mastercard")
                    margin = user.MasterMarigin ?? margin;
                else if (BankName.ToLower().Contains("hdfc"))
                    margin = user.HdfcMargin ?? margin;

                var payInApp = new PayManPayIn
                {
                    UserId = user.Id,
                    UserPhone = appPhone,
                    TxnId = TransectionId,
                    EasePayId = EasepayId,
                    Email = email,
                    CardNumber = cardnumber,
                    EaseCardNum = user.Email,
                    Amount = amount,
                    Gateway = easebuzzGateway.Easebuzz1 == true ? "Easebuzz1" : "Easebuzz2",
                    BankName = BankName,
                    CardBrand = CardBrand,
                    IsCorporate = cardType,
                    PayInCommission = amount * margin / 100,
                    PaymanCommission = amount * (easebuzzGateway?.PaymanComm ?? 0) / 100,
                    Created = istTime,
                    Status = status == "success",
                    Result = status,
                    Device = "Web",
                    CreditCardHolderNum = CreditCardNum,
                    CreditCardHolderName = CreditCardHolderName,
                    CardholderMobileNo = CardholderMobileNo
                };

                _context.payManPayIns.Add(payInApp);
                await _context.SaveChangesAsync();

                var avlAmount = await GetUserWalletAmount(appPhone);

                var payInHistory = new PayManHistory
                {
                    UserId = user.Id,
                    UserPhone = appPhone,
                    TxnId = EasepayId,
                    Amount = amount,
                    CardNumber = cardnumber,
                    Mode = "PayIn",
                    Status = status == "success",
                    Created = istTime,
                    AvlBalance = Convert.ToDecimal(avlAmount),
                    PayInId = payInApp.Id
                };

                _context.payManHistories.Add(payInHistory);
                await _context.SaveChangesAsync();
            }

            // View model for payment status screen
            var model = new PaymentStatusViewModel
            {
                IsSuccess = status == "success",
                Amount = amount,
                TransactionId = TransectionId,
                CardNumber = cardnumber
            };

            return View("PayStatus", model); // Shows the visual confirmation screen
        }

        public async Task<ActionResult> EbuzzStatus1(string status = null, int amount = 0, string cardnumber = "", string TransectionId = null, string email = null, string cardType = null, string EasepayId = null, string BankName = "", string CardBrand = null)
        {
            PayPayment payPayment = new PayPayment();
            payPayment.Sucess = true;
            payPayment.Amount = amount.ToString();
            payPayment.EnterCustomerNumber = cardnumber;
            // string firstWord = !string.IsNullOrWhiteSpace(BankName) ? BankName.Split(' ')[0] : "Unknown";

            if (cardType == null)
            {
                cardType = "NA";
            }
            if (BankName == null)
            {
                BankName = "NA";
            }
            if (CardBrand == null)
            {
                CardBrand = "NA";
            }

            if (status == "success")
            {
                TempData["LyraPayInSucess"] = 1;
                TempData["LyraPayInAmount"] = amount;
                TempData["LyraPayInCardNumber"] = cardnumber;
                TempData["LyraPayInPaymentId"] = TransectionId;
            }
            else
            {
                TempData["LyraPayInSucess"] = 2;
                TempData["LyraPayInAmount"] = amount;
                TempData["LyraPayInCardNumber"] = cardnumber;
                TempData["LyraPayInPaymentId"] = TransectionId;
            }


            var appPhone = HttpContext.Session.GetString("AppPhone");
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == appPhone);
            if (user == null || user.App == false )
            {
                var userId = HttpContext.Session.GetString("UserId");
                var phone = HttpContext.Session.GetString("Phone");

                var userdetails = _context.users.Where(t => t.UserId == userId).FirstOrDefault();
                var paymanComm = _context.payManGateWayMarigins.FirstOrDefault();

                try
                {

                    PayIn payIn = new PayIn();
                    TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                    DateTime utcDateTime = DateTime.UtcNow; // Get the current UTC time
                    DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, istTimeZone);
                    var commissssion = userdetails.Margin;

                    if (cardType == "true" || cardType == "TRUE")
                    {
                        commissssion = userdetails.Margin + (decimal)0.55;
                    }
                    else if (CardBrand == "MasterCard" || CardBrand == "MASTERCARD")
                    {
                        commissssion = (decimal)userdetails.MasterMargin;
                    }
                    else if (BankName.Contains("HDFC") || BankName.Contains("hdfc"))
                    {
                        commissssion = userdetails.Margin + (decimal)0.10;
                    }

                    var commm = Convert.ToInt32(amount) * commissssion / 100;


                    decimal kjfds;

                    kjfds = userdetails.DistributerMarigin;

                    var distibutercommission = Convert.ToInt32(amount) * kjfds / 100;
                    var paymancommission = Convert.ToInt32(amount) * paymanComm.RazorPay / 100;

                    var paynidetails = _context.payIns.Where(t => t.OrderRefNumber == EasepayId).FirstOrDefault();
                    if (paynidetails == null)
                    {
                        payIn.OrderId = TransectionId;
                        payIn.OrderRefNumber = EasepayId;
                        payIn.Currency = "INR";
                        payIn.UserId = userdetails.UserId;
                        payIn.MobileNumber = cardnumber;
                        payIn.Amount = Convert.ToInt32(amount);
                        payIn.Refound = 0;
                        payIn.Sttaus = status;
                        payIn.GateWay = 9;
                        payIn.CreatedDate = istDateTime;
                        payIn.PayInCommission = commm;

                        payIn.DistributerUserId = userdetails.DistributeruserId;
                        payIn.DistibuterCommission = distibutercommission;
                        payIn.PaymanCommission = paymancommission;
                        payIn.IssueBank = CardBrand;
                        _context.payIns.Add(payIn);
                        _context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    //  TempData["PaymentSucess"] = "You have " + amount + " failed transection.";

                }
            }
            else
            {
                var easebuzzGateway = await _context.PayManGateways.FirstOrDefaultAsync(t => t.GatewayName == "Easebuzz");

                var existing = await _context.payManPayIns.FirstOrDefaultAsync(t => t.EasePayId == EasepayId && t.UserPhone == appPhone);
                if (existing == null)
                {
                    var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                    decimal amoun1t = Convert.ToDecimal(amount);
                    decimal margin = Convert.ToDecimal(user.Margin ?? "0");
                    //decimal margin = Convert.ToDecimal(easebuzzGateway.PaymanComm ?? "0");


                    if (cardType == "true" || cardType == "TRUE")
                    {
                        margin = (decimal)user.CarporateCardMarigin;
                    }
                    else if (CardBrand == "MasterCard" || CardBrand == "MASTERCARD")
                    {
                        margin = (decimal)user.MasterMarigin;
                    }
                    else if (BankName.Contains("HDFC") || BankName.Contains("hdfc"))
                    {
                        margin = (decimal)user.HdfcMargin;
                    }

                    var payInApp = new PayManPayIn
                    {
                        UserId = user.Id,
                        UserPhone = appPhone,
                        TxnId = TransectionId,
                        EasePayId = EasepayId,
                        Email = user.Email,
                        CardNumber = cardnumber,
                        EaseCardNum = cardnumber,
                        Amount = Convert.ToDecimal(amoun1t),
                        Gateway = easebuzzGateway.Easebuzz1 == true ? "Easebuzz1" : "Easebuzz2",
                        BankName = BankName,
                        CardBrand = CardBrand,
                        IsCorporate = cardType,
                        PayInCommission = amount * margin / 100,
                        PaymanCommission = amount * easebuzzGateway.PaymanComm / 100,
                        Created = istTime,
                        Status = status == "success" ? true : false,
                        Result = status,
                        Device = "Web"
                    };

                    _context.payManPayIns.Add(payInApp);
                    await _context.SaveChangesAsync();


                    var avlamount = await GetUserWalletAmount(appPhone);




                    var payInHistory = new PayManHistory
                    {
                        UserId = user.Id,
                        UserPhone = user.Phone,
                        TxnId = EasepayId,
                        Amount = Convert.ToDecimal(amoun1t),
                        CardNumber = cardnumber,
                        Mode = "PayIn",
                        Status = status == "success" ? true : false,
                        Created = istTime,
                        AvlBalance = Convert.ToDecimal(avlamount),
                        PayInId = payInApp.Id
                    };
                    _context.payManHistories.Add(payInHistory);
                    await _context.SaveChangesAsync();

                }
            }
            return RedirectToAction("Pay", "PayMan", payPayment);
        }
    }


}
