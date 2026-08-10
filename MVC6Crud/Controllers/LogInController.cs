using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.PaymanWeb;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using XAct.Users;
using System.Text;
using Microsoft.EntityFrameworkCore;
//using XAct.Users;

namespace MVC6Crud.Controllers
{
    //[Authorize]
    public class LogInController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LogInController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult LogIn()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
           var user = _context.payManUsers.FirstOrDefault(u => (u.Phone == model.Email || u.FirstName == model.Email) && u.WedPWD == model.Password);

            if (user == null || user.App == false)
            {
                var userdetais = _context.users.Where(t => t.Name == model.Email && t.Password == model.Password && t.IsActive == true).FirstOrDefault();

                if (userdetais != null)
                {
                    if (userdetais.ChangePassword)
                    {
                        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, model.Email) },
                            CookieAuthenticationDefaults.AuthenticationScheme);
                        var principl = new ClaimsPrincipal(identity);
                        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principl);
                        HttpContext.Session.SetString("UserName", model.Email);
                        HttpContext.Session.SetString("Email", userdetais.Email);
                        HttpContext.Session.SetString("Phone", userdetais.Phone);
                        HttpContext.Session.SetString("UserId", userdetais.UserId);
                        var adharVerificationStatus = _context.adharVerifications.Where(t => t.UserId == userdetais.UserId).FirstOrDefault();

                        if (adharVerificationStatus != null)
                        {
                            if (adharVerificationStatus.AdharVerified)
                            {
                                return RedirectToAction("PayManHome", "PayMan");
                            }
                            else
                            {
                                return RedirectToAction("AdharNumberVerify", "PayMan");
                            }
                        }
                        else
                        {
                            if (userdetais.UserId == "MUIEP2")
                            {
                                return RedirectToAction("PayManHome", "PayMan");
                            }
                            else
                            {
                                return RedirectToAction("AdharNumberVerify", "PayMan");
                            }

                        }
                    }
                    else
                    {
                        return RedirectToAction("ChangePassword", "PayMan", new { Id = userdetais.Id });
                    }
                }
                else
                {
                    TempData["LogIn"] = "Please enter valid user details!";
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                if(user != null)
                {
                    if(user.Web == true)//changepaassword
                    {
                        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.FirstName) },
                           CookieAuthenticationDefaults.AuthenticationScheme);
                        var principl = new ClaimsPrincipal(identity);
                        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principl);
                        HttpContext.Session.SetString("UserName", user.FirstName);
                        HttpContext.Session.SetString("Email", user.Email);
                        HttpContext.Session.SetString("AppPhone", user.Phone);

                        return RedirectToAction("PayManHome", "PayMan");
                    }
                    else
                    {
                        return RedirectToAction("AppChangePassword", "PayMan", new { Id = user.Id });
                    }
                    
                }
                else
                {
                    TempData["LogIn"] = "Please enter valid user details!";
                    return RedirectToAction("Index", "Home");
                }


            }
          
        }

        [HttpGet]
        public IActionResult LogOut()
        {

            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }



        [HttpPost]
        public async Task<IActionResult> WebLogIn([FromBody] WebLoginRequest request)
        {
            try
            {
                //_context.errorModels.Add(new ErrorModel
                //{
                //    payload = "WebLogIn User detais sucess1",
                //    agId = ",",
                //    reqTime = "",
                //    respTime =  "",
                //    uid = "",
                //    requestId = "",
                //    statuscode = true,
                //    jsonBody = ""
                //});

                //await _context.SaveChangesAsync();

                var user = _context.payManUsers
                    .FirstOrDefault(u => (u.FirstName == request.Email || u.Phone == request.Email)
                                      && u.WedPWD == request.Password
                                      && u.Web == true);

                //_context.errorModels.Add(new ErrorModel
                //{
                //    payload = "WebLogIn User detais sucess",
                //    agId = ",",
                //    reqTime = "",
                //    respTime = user.FirstName ?? "",
                //    uid = "",
                //    requestId = "",
                //    statuscode = true,
                //    jsonBody = ""
                //});

                //await _context.SaveChangesAsync();

                if (user != null)
                {
                    // Store session values
                    HttpContext.Session.SetString("UserName", user.FirstName);
                    HttpContext.Session.SetString("UserPhone", user.Phone);
                    HttpContext.Session.SetString("Email", user.Email);
                    HttpContext.Session.SetString("AppPhone", user.Phone);

                    if (user.OtpLoginEnabled == true)
                    {
                        user.ResetOtp = GenerateRandomPIN(6);
                        user.OtpGeneratedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync();

                        var body = HTMLTemp1(user.ResetOtp);

                        SendEmail(
                            user.Email,
                            "LogIn OTP Notification - Payman Fintech",
                            body
                        );

                        return Ok(new
                        {
                            success = true,
                            OtpLoginEnabled = true,
                            userphone = user.Phone
                        });
                    }
                    else
                    {
                        return Ok(new
                        {
                            success = true,
                            OtpLoginEnabled = false,
                            userphone = user.Phone
                        });
                    }
                }

                return Ok(new
                {
                    success = false,
                    OtpLoginEnabled = false,
                    userphone = (string?)null
                });
            }
            catch (Exception ex)
            {

                _context.errorModels.Add(new ErrorModel
                {
                    payload = "WebLogIn Exception",
                    agId = ",",
                    reqTime = ex.Message,
                    respTime = ex.StackTrace ?? "",
                    uid = "",
                    requestId = "",
                    statuscode = true,
                    jsonBody = ""
                });

                await _context.SaveChangesAsync();

                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        
        public IActionResult WebLogout()
        {
            HttpContext.Session.Clear(); // Clear session
            return RedirectToAction("Index", "Home"); // Redirect to homepage
        }


        [HttpPost]
        public async Task<IActionResult> SendResetPinMail([FromBody] ResetPinRequest11 request)
        {
            if (string.IsNullOrEmpty(request.Phone))
                return BadRequest(new { status = "fail", message = "Phone number is required." });

            try
            {
                var userDetails = await _context.payManUsers.FirstOrDefaultAsync(t => t.Email == request.Phone);

                if (userDetails == null)
                {
                    return NotFound(new { status = "fail", message = "User not found." });
                }

                // Optionally generate a new PIN (if you want to reset it)
                //if (request.Divice == "Web")
                //{
                //    userDetails.WedPWD = null;
                //}
                //else
                //{
                //    userDetails.UserPin = null;
                //}

                //userDetails.ResetOtp = GenerateRandomPIN(6);
                //userDetails.OtpGeneratedAt = DateTime.UtcNow;
                //await _context.SaveChangesAsync();

                var body = HTMLTemp2(userDetails.WedPWD);  // Corrected variable name
                SendEmail(userDetails.Email, "PAYMAN FasTag - Forgot Password Request", body);

                return Ok(new { status = "success", message = "Reset mail sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "fail", message = $"Error sending email: {ex.Message}" });
            }
        }



        public string HTMLTemp2(string password)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Recovery</title>
</head>
<body style=""font-family: Arial, sans-serif; color: #333; line-height: 1.6; background-color: #f4f4f4; padding: 20px;"">

    <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 30px; border: 1px solid #ddd; border-radius: 8px;"">

        <h2 style=""color: #0073e6; text-align:center;"">
            PAYMAN FASTAG
        </h2>

        <p>Dear,</p>

        <p>
            As per your request, here are your login credentials:
        </p>

        <div style=""background:#f8f9fa; border:1px solid #ddd; border-radius:6px; padding:20px; text-align:center; margin:20px 0;"">
            <p style=""margin:0; font-size:16px;""><strong>Your Password</strong></p>
            <p style=""font-size:28px; color:#0073e6; font-weight:bold; letter-spacing:2px; margin:10px 0;"">
                {password}
            </p>
        </div>

        <p>
            Please keep your password confidential and do not share it with anyone.
        </p>

        <p>
            If you did not request this email, please contact our support team immediately.
        </p>

        <hr style=""border:none; border-top:1px solid #eee; margin:25px 0;"">

        <p>
            <strong>Support</strong><br>
            📧 support@paymanfintech.in<br>
            📞 +91 9100748033
        </p>

        <p style=""color:#888; font-size:12px;"">
            This is an automated email. Please do not reply to this message.
        </p>

        <p>
            Regards,<br>
            <strong>Payman FasTag Team</strong>
        </p>

    </div>

</body>
</html>";
        }


        [HttpPost]
        public async Task<IActionResult> VerifyOtpWeb([FromBody] OtpRequest request)
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");

            var userDetails = await _context.payManUsers
                                                .FirstOrDefaultAsync(t => t.Phone == appPhone);
            if (userDetails == null)
            {
                return NotFound(new { status = "fail", message = "User not found." });
            }

            // Optional: Check OTP expiration (e.g., valid for 5 mins)
            if (userDetails.OtpGeneratedAt.HasValue && (DateTime.UtcNow - userDetails.OtpGeneratedAt.Value).TotalMinutes > 5)
            {
                return BadRequest(new { status = false, message = "OTP has expired." });
            }

            if (userDetails.ResetOtp == request.Otp)
            {
                // Invalidate OTP after verification
                userDetails.ResetOtp = null;
                userDetails.OtpGeneratedAt = null;
                _context.SaveChanges();

                return Ok(new { status = true, message = "OTP verified successfully." });
            }


            return Ok(new { success = false});
        }

        public static string GenerateRandomPIN(int length)
        {
            const string validChars = "1234567890";
            StringBuilder result = new StringBuilder();
            Random random = new Random();

            for (int i = 0; i < length; i++)
            {
                result.Append(validChars[random.Next(validChars.Length)]);
            }

            return result.ToString();
        }

        public string HTMLTemp1(string otp)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Reset OTP</title>
</head>
<body style=""font-family: Arial, sans-serif; color: #333; line-height: 1.6;"">

    <div style=""max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;"">
        <h2 style=""color: #0073e6;"">Dear Merchant,</h2>

        <p>You have requested to reset your PIN. Please use the following 6-digit OTP to proceed:</p>

        <div style=""text-align: center; margin: 20px 0;"">
            <span style=""display: inline-block; font-size: 24px; font-weight: bold; letter-spacing: 6px; color: #0073e6;"">
                {otp}
            </span>
        </div>

        <p>This OTP is valid for the next 5 minutes. Do not share it with anyone.</p>

        <p>If you did not request this action, please contact us immediately:</p>
        <p>
            Email: <a href=""mailto:support@paymanfintech.in"" style=""color: #0073e6;"">support@paymanfintech.in</a><br>
            Call: +91 9100748033
        </p>

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
                Credentials = new NetworkCredential("payman111223@gmail.com", "gutwwjudtrwkdjum"),
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



    }

    public class ResetPinRequest11
    {
        public string Phone { get; set; } = string.Empty;
        public string? Divice { get; set; } = string.Empty;
    }
}
