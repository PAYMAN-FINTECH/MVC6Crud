using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.App;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PaymanWeb;
using MVC6Crud.Models.PineLab;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using XAct.Library.Settings;
using XAct.Users;
using OtpResponse = MVC6Crud.Models.PaymanApp.OtpResponse;

namespace MVC6Crud.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;
        public AuthController(ApplicationDbContext context, IConfiguration configuration, DataUtils dataUtils)
        {
            _context = context;
            _configuration = configuration;
            _dataUtils = dataUtils;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Authorize]    // ← this is key
        public IActionResult ValidateToken()
        {
            Console.Write("testing");
            // now User.Claims will actually be populated
            var phoneNumber = User.Claims
                                  .FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?
                                  .Value;

            if (string.IsNullOrEmpty(phoneNumber))
                return Unauthorized(new { isValid = false, message = "Invalid token claims." });

            return Ok(new { isValid = true, message = "Token is valid", phone = phoneNumber });
        }


        [HttpPost]
        public IActionResult Login([FromBody] LoginRequest1 request)
        {
            if (string.IsNullOrEmpty(request.Phone))
            {
                return BadRequest(new { message = "Phone number is required." });
            }

            return Ok(new { exists = true, message = "OTP sent successfully." });
        }

        [HttpPost]
        public IActionResult VerifyOTP1([FromBody] OTPRequest request)
        {
            if (request.Otp != "1234")
            {
                return BadRequest(new { message = "Invalid OTP" });
            }

            string token = GenerateJwtToken(request.Phone);

            return Ok(new { token });
        }


        private string GenerateJwtToken(string phoneNumber)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsA32ByteLongSecretKeyForJWT!"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, phoneNumber),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
    };

            var token = new JwtSecurityToken(
                issuer: "https://paymanfintech.in/",
                audience: "https://paymanfintech.in/",
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost]
        public async Task<JsonResult> AdharNumberVerify1([FromBody] AdharNumberRequest request)
        {
            var userDetails = await _context.payManUsers
                                           .Where(t => t.Phone == request.Phone)
                                           .FirstOrDefaultAsync();
            if (userDetails == null)
            {
                return Json(new { success = false, message = "user details not valid." });
            }
            // Validate if Aadhar number is provided
            if (string.IsNullOrEmpty(request.AdharNumber))
            {
                return Json(new { success = false, message = "Aadhar number is required." });
            }

            // Retrieve the userId from session
            // var userId = HttpContext.Session.GetString("UserId");

            //if (string.IsNullOrEmpty(userId))
            //{
            //    return Json(new { success = false, message = "User not found in session." });
            //}

            // Aadhar verification API URL
            var url = "https://api.cashfree.com/verification/offline-aadhaar/otp";

            // Payload to send to the API
            var payload = new { aadhaar_number = request.AdharNumber };

            // CashFree API credentials
            var clientId = "CF678492CQOFTSG8OI4S73DVFVN0";
            var clientSecret = "cfsk_ma_prod_1a60ece5af3976c5258d7afd8aa0a8eb_063c29b6";

            try
            {
                // Using HttpClient to call the external service
                using (var client = new HttpClient())
                {
                    // Set request headers
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("x-client-id", clientId);
                    client.DefaultRequestHeaders.Add("x-client-secret", clientSecret);
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // Convert the payload object to JSON
                    var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    // Send the POST request
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    // Check if the response is successful
                    if (!response.IsSuccessStatusCode)
                    {
                        return Json(new { success = false, message = "Failed to send OTP. Please try again." });
                    }

                    // Read and deserialize the response
                    string responseText = await response.Content.ReadAsStringAsync();
                    var otpResponse = JsonConvert.DeserializeObject<OtpResponse>(responseText);

                    // Check the response from CashFree
                    if (otpResponse != null)
                    {
                        var adharDetails = await _context.aadharDetails
                                           .Where(t => t.Phone == request.Phone && t.Status == true)
                                           .FirstOrDefaultAsync();
                        if(adharDetails == null)
                        {
                            TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                            DateTime istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
                            var adharDeatils = new AadharDetails
                            {
                                UserId = userDetails.Id,
                                Phone = request.Phone,
                                AadharNo = request.AdharNumber,
                                AadharRefId = otpResponse?.ref_id,
                                Created = istTime
                            };
                            _context.aadharDetails.Add(adharDeatils);
                            _context.SaveChanges();
                        }
                        else
                        {
                            return Json(new { success = false, message = "User alredy verified" });
                        }
                        
                        // Return success with OTP response
                        return Json(new { success = true, result = otpResponse?.Message,refid= otpResponse?.ref_id  });
                    }
                    else
                    {
                        // Return failure with the error message from CashFree
                        return Json(new { success = false, message = otpResponse?.Message ?? "Unknown error occurred." });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception and return a failure message
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> VerifyAdharOTP([FromBody] VerifyAdharOtpRequest request)
        {
            var userDetails = await _context.payManUsers
                                          .Where(t => t.Phone == request.Phone)
                                          .FirstOrDefaultAsync();
            if (userDetails == null)
            {
                return Json(new { success = false, message = "user details not valid." });
            }

            if (string.IsNullOrEmpty(request.Otp))
                return Json(new { success = false, message = "OTP is required." });

            var adharDetails = await _context.aadharDetails
                                           .Where(t => t.Phone == request.Phone)
                                           .FirstOrDefaultAsync();

            if (adharDetails == null || string.IsNullOrEmpty(adharDetails.AadharRefId))
                return Json(new { success = false, message = "Reference ID not found for the user." });

            var clientId = "CF678492CQOFTSG8OI4S73DVFVN0";
            var clientSecret = "cfsk_ma_prod_1a60ece5af3976c5258d7afd8aa0a8eb_063c29b6";

            try
            {
                using var client = new HttpClient();
                client.BaseAddress = new Uri("https://api.cashfree.com");
                client.DefaultRequestHeaders.Add("x-client-id", clientId);
                client.DefaultRequestHeaders.Add("x-client-secret", clientSecret);

                var requestData = new
                {
                    otp = request.Otp,
                    ref_id = request.RefId
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/verification/offline-aadhaar/verify", content);

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Failed to verify Aadhaar OTP." });
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var otpResponse = JsonConvert.DeserializeObject<VerifyAdharOTPResponse1>(responseString);

                if (otpResponse?.Status == "VALID")
                {
                    adharDetails.Otp = request.Otp;//int.TryParse(request.Otp, out var parsedOtp) ? parsedOtp : 0;
                    adharDetails.Address = otpResponse.Address;
                    adharDetails.Name = otpResponse.Name;
                    adharDetails.Status = true;

                    _context.aadharDetails.Update(adharDetails);

                    userDetails.IsAadherVerified = true;
                    _context.payManUsers.Update(userDetails);

                    await _context.SaveChangesAsync();

                    if(userDetails.CustomerType == "new")
                    {
                        var document = new UserDocuments
                        {
                            UserId = userDetails.Id,
                            Phone = userDetails.Phone,
                            PanCardNumber = "BKWPK2832H",

                            AadharFront = null,

                            AadharBack =  null,

                            PanCard =  null, 

                            UploadedAt = DateTime.Now
                        };

                        _context.userDocuments.Add(document);
                        await _context.SaveChangesAsync();

                    }

                    return Json(new { success = true, message = "Aadhaar KYC completed successfully.", otpResponse= otpResponse });
                }
                else
                {
                    return Json(new { success = false, message = otpResponse?.Message ?? "Aadhaar verification failed. Please try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocuments([FromBody] AadharDocumentUploadModel model)
        {
            var userDetails = await _context.payManUsers
                                           .Where(t => t.Phone == model.Phone)
                                           .FirstOrDefaultAsync();
            if (userDetails == null)
            {
                return Json(new { success = false, message = "user details not valid." });
            }

            try
            {
                if (string.IsNullOrEmpty(model.PanCardNumber) ||
                    string.IsNullOrEmpty(model.AadharFront) ||
                    string.IsNullOrEmpty(model.AadharBack) ||
                    string.IsNullOrEmpty(model.PanCard))
                {
                    return BadRequest(new { success = false, message = "All fields are required." });
                }

                TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);

                var document = new UserDocuments
                {
                    UserId= userDetails.Id,
                    Phone = model.Phone,
                    PanCardNumber = model.PanCardNumber,
                    AadharFront = Convert.FromBase64String(model.AadharFront),
                    AadharBack = Convert.FromBase64String(model.AadharBack),
                    PanCard = Convert.FromBase64String(model.PanCard),
                    UploadedAt = istTime
                };

                _context.userDocuments.Add(document);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Documents uploaded successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
            }
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


        [HttpPost]
        public async Task<IActionResult> GetPaymanAccountAmount([FromBody] PaymanAccountsAmount paymanAccountsAmount)
        {
            bool aadharVerified = false;
            bool isAdmin = false;


            var instantPayAmount = await GetInstantPayAmount();
            var pineLabsAmount = await _dataUtils.GetPinelabsAmount(paymanAccountsAmount.Phone);
            var userWalletAmount = await GetUserWalletAmount(paymanAccountsAmount.Phone);

            var gateWayDetails = await _context.PayManGateways.FirstOrDefaultAsync();

            var aadharStatus = await _context.aadharDetails
                .FirstOrDefaultAsync(t => t.Phone == paymanAccountsAmount.Phone && t.Status == true);

            var userDetails = await _context.payManUsers
                                         .Where(t => t.Phone == paymanAccountsAmount.Phone)
                                         .FirstOrDefaultAsync();

            if (aadharStatus != null && aadharStatus.Status == true)
            {
                aadharVerified = true;
            }

            if (userDetails != null && userDetails.IsAdmin == true)
            {
                isAdmin = true;
            }


            return Ok(new
            {
                success = true,
                instantpayamount = instantPayAmount,
                pinelabsamount = pineLabsAmount,
                userwalletamount = userWalletAmount,
                aadharverified = aadharVerified,
                isadmin = isAdmin,
                customerType = userDetails.CustomerType,
                isPayOut = userDetails.PayOut,
                otpLoginEnabled = userDetails.OtpLoginEnabled,
                payOutEnable = GetPayOutEnable(),
                payOutMinAmount = gateWayDetails.PayOutMinAmount,
                payOutMaxAmount = gateWayDetails.PayOutMaxAmount,
                minBalanceAvl = gateWayDetails.MinBalanceAvl,
                easebuzz1 = gateWayDetails.Easebuzz1,
                easebuzz2 = gateWayDetails.Easebuzz2,
                edu = gateWayDetails.Edu,
            });
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


        [HttpPost]
        public IActionResult UpdateOtpLoginStatus([FromBody] OtpLoginUpdateRequest request)
        {
            // Validate and update database
            var user = _context.payManUsers.FirstOrDefault(u => u.Phone == request.Phone);
            if (user == null) return NotFound(new { success = false, message = "User not found" });

            user.OtpLoginEnabled = request.OtpLoginEnabled;
            _context.payManUsers.Update(user);
            _context.SaveChanges();

            return Ok(new { success = true });
        }


        [HttpPost]
        public async Task<IActionResult> UpdateToggle([FromBody] ToggleUpdateRequest request)
        {
            var appPhone = HttpContext.Session.GetString("AppPhone");

            if (request == null || string.IsNullOrWhiteSpace(request.Label))
            {
                return BadRequest("Invalid request data.");
            }

            var users = await _context.payManUsers
                .Where(u => u.Phone == appPhone)
                .FirstOrDefaultAsync();

            if(users != null)
            {
                users.OtpLoginEnabled = request.IsChecked;
                _context.payManUsers.Update(users);
                await _context.SaveChangesAsync();
            }
            

            return Ok(new { success = true, message = "OTP LogIn updated successfully." });
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
        [HttpGet]
        public async Task<IActionResult> GetGateways(string mobile = "")
        {
            var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
            var today = istNow.Date;

            if (string.IsNullOrEmpty(mobile))
            {
                var gatewayss = await _context.gateways
                    .Where(g => g.IsActive)
                    .Select(g => new
                    {
                        g.Id,
                        g.GatewayName,
                        g.App,
                        g.Web,
                        g.IsActive,
                        g.GatwayLimit,
                        g.StoreName
                    })
                    .ToListAsync();

                return Ok(gatewayss);
            }

            var user = await _context.payManUsers
                .FirstOrDefaultAsync(x => x.Phone == mobile);

            if (user == null)
                return NotFound("User not found");

            IQueryable<Gateways> gatewayQuery;

            if (user.IsAdmin ?? false)
            {
                gatewayQuery = _context.gateways;
            }
            else
            {
                gatewayQuery = _context.userLookUps
                    .Where(x => x.UserId == user.Id && x.IsEnabled)
                    .Select(x => x.Gateway);
            }

            var gateways = await gatewayQuery
                .Select(g => new
                {
                    g.Id,
                    g.GatewayName,
                    g.App,
                    g.Web,
                    g.IsActive,
                    g.GatwayLimit,
                    g.StoreName,

                    TodayAmount = _context.payManPayIns
                        .Where(p => p.Gateway == g.StoreName && p.Status == true && p.Created >= today)
                        .Sum(p => (decimal?)p.Amount) ?? 0
                })
                .ToListAsync();

            // Filter gateways whose limit crossed
            var availableGateways = gateways
                .Where(g => g.TodayAmount < g.GatwayLimit)
                .Select(g => new
                {
                    g.Id,
                    g.GatewayName,
                    g.App,
                    g.Web,
                    g.IsActive,
                    g.StoreName
                })
                .ToList();

            return Ok(availableGateways);
        }


        [HttpGet]
        public async Task<IActionResult> GetRecentPayments(string mobile)
        {
            var gateways = await Task.Run(() => _context.payManPayIns
                .Where(g => g.UserPhone == mobile).OrderByDescending(g => g.Created).Take(15)
                .Select(g => new
                {
                    g.Id,
                    g.Status,
                    g.Amount,
                    g.CreditCardHolderNum,
                    g.CreditCardHolderName,
                    g.CardholderMobileNo,
                    g.Created,
                    g.Email
                })
                .ToList());

            return Ok(gateways);
        }

        [HttpGet]
        public async Task<IActionResult> SearchPayments(string mobile, string name)
        {
            var query = _context.payManPayIns
                .Where(g => g.UserPhone == mobile);

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(g =>
                    EF.Functions.Like(g.CreditCardHolderNum, $"%{name}%") ||
                    EF.Functions.Like(g.CreditCardHolderName, $"%{name}%") ||
                    EF.Functions.Like(g.CardholderMobileNo, $"%{name}%")
                );
            }

            var gateways = await query
                .OrderByDescending(g => g.Created)
                .Take(50)
                .Select(g => new
                {
                    g.Id,
                    g.Status,
                    g.Amount,
                    g.CreditCardHolderNum,
                    g.CreditCardHolderName,
                    g.CardholderMobileNo,
                    g.Created,
                    g.Email
                })
                .ToListAsync();

            return Ok(gateways);
        }

        [HttpPost]
        public async Task<IActionResult> SendOtp([FromBody] AppLoginRequest req)
        {
            try
            {
                var istTime = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                var otp = _dataUtils.GenerateOtp();

                // Save OTP
                _context.OtpLogs.Add(new OtpLog
                {
                    Phone = req.Phone,
                    Otp = otp,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                    Created = istTime
                });

                await _context.SaveChangesAsync();

                string message = $"{otp} is your OTP from PAYMAN to authenticate. Valid for 5 minutes. Never share your OTP or account details with anyone. Regards, PAYMAN";

                // Send SMS
                var smsResponse = await _dataUtils.SendDLTSms(req.Phone, message);

                // Example response:
                // {"return":true,"request_id":"SAW4KB4zebYMeJY","message":["SMS sent successfully."]}

                if (smsResponse != null && smsResponse.@return == true)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "OTP sent successfully"
                        // otp = otp // Remove in production
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to send OTP"
                    });
                }

                //return Ok(new
                //{
                //    success = true,
                //    message = "OTP sent successfully"
                //    // otp = otp // Remove in production
                //});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
        {
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
            );

            var otp = await _context.OtpLogs
                .Where(o => o.Phone == req.Phone && o.Otp == req.Otp && !o.IsUsed)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            if (otp == null || otp.ExpiryTime < DateTime.UtcNow)
                return Unauthorized("Invalid OTP");

            otp.IsUsed = true;

            var user = await _context.AppUser.FirstOrDefaultAsync(x => x.Phone == req.Phone);
            var isNewUser = false;

            if (user == null)
            {
                user = new AppUser
                {
                    Phone = req.Phone,
                    CreatedAt = DateTime.UtcNow // ✅ store UTC
                };

                _context.AppUser.Add(user);
                isNewUser = true;
            }

            await _context.SaveChangesAsync();

            var token = GenerateToken(user.Id, user.Phone);
            var refresh = GenerateRefreshToken();

            _context.RefreshToken.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refresh,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                Created = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            var pmUser = await _context.pMUsers
                .FirstOrDefaultAsync(t => t.Phone == user.Phone);

            return Ok(new
            {
                token,
                refreshToken = refresh,
                isNewUser,
                kycCompleted = pmUser?.IsKycCompleted ?? false,
                usertype = pmUser?.UserType ?? "",
                isactive = pmUser?.IsActive ?? false,
            });
        }

        /// 🔄 REFRESH TOKEN
        [HttpPost]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                           TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            var storedToken = await _context.RefreshToken
                .FirstOrDefaultAsync(x =>
                    x.Token == request.RefreshToken &&
                    !x.IsRevoked &&
                    x.ExpiresAt > DateTime.UtcNow);

            if (storedToken == null)
                return Unauthorized(new { message = "Invalid refresh token" });

            var user = await _context.AppUser.FindAsync(storedToken.UserId);
            if (user == null)
                return Unauthorized();

            // 🔐 Revoke old token
            storedToken.IsRevoked = true;

            // 🔑 Generate new tokens
            var newAccessToken = GenerateToken(user.Id, user.Phone);
            var newRefreshToken = GenerateRefreshToken();

            _context.RefreshToken.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                Created = istTime
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            });
        }

        public string GenerateToken(Guid userId, string phone)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("paymanfintechsolutionltd_@1234567890")
            );

            var token = new JwtSecurityToken(
                issuer: "https://paymanfintech.in",
                audience: "https://paymanfintech.in",
                claims: new[]
                {
                    new Claim("uid", userId.ToString()),
                    new Claim("phone", phone)
                },
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        [HttpPost]
        public async Task<IActionResult> ScannerPayment(
    [FromBody] ScannerPaymentRequest request)
        {
            if (request == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid request."
                });
            }

            var user = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == request.AgentMobile);

            if (user == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid request."
                });
            }
            // Check if this payment already exists (idempotency)
            var existing = await _context.payManPayIns
                .FirstOrDefaultAsync(t => t.EasePayId == request.PaymentTxnId && t.Status == true);

            if (existing == null)
            {
                var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                 TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                decimal margin = Convert.ToDecimal("1.60" ?? "0");

                //if (model.card.ToLower() == "mastercard")
                //    margin = user.MasterMarigin ?? margin;

                //if (cardType.ToLower() == "true")
                //    margin = user.CarporateCardMarigin ?? margin;
                //else if (model.card.ToLower() == "mastercard")
                //    margin = user.MasterMarigin ?? margin;
                //else if (BankName.ToLower().Contains("hdfc"))
                //    margin = user.HdfcMargin ?? margin;

                var payInApp = new PayManPayIn
                {
                    UserId = user.Id,
                    UserPhone = user.Phone,
                    TxnId = request.PaymentTxnId,
                    EasePayId = request.PaymentTxnId,
                    Email = user.Email,
                    CardNumber = request.CustomerCardNumber,
                    EaseCardNum = request.CustomerCardNumber,
                    Amount = request.Amount,
                    Gateway = "Scanner",
                    BankName = request.BankName,
                    CardBrand = request.CardBrand,
                    IsCorporate = request.CardType,
                    PayInCommission = request.Amount * margin / 100,
                    PaymanCommission = 0,
                    Created = istTime,
                    Status = true,
                    Result = "success",
                    Device = "Web"
                };

                _context.payManPayIns.Add(payInApp);
                await _context.SaveChangesAsync();

                var avlAmount = await GetUserWalletAmount(user.Phone);

                var payInHistory = new PayManHistory
                {
                    UserId = user.Id,
                    UserPhone = user.Phone,
                    TxnId = request.PaymentTxnId,
                    Amount = request.Amount,
                    CardNumber = request.CustomerCardNumber,
                    Mode = "PayIn",
                    Status = true,
                    Created = istTime,
                    AvlBalance = Convert.ToDecimal(avlAmount),
                    PayInId = payInApp.Id
                };

                _context.payManHistories.Add(payInHistory);
                await _context.SaveChangesAsync();
            }

                return Json(new
            {
                success = true,
                message = "Payment details saved successfully."
            });
        }

    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }
    public class VerifyOtpRequest
    {
        public string Phone { get; set; }
        public string Otp { get; set; }
    }

    public class ScannerPaymentRequest
    {
        public decimal Amount { get; set; }

        public string CustomerMobile { get; set; }

        public string CustomerEmail { get; set; }

        public string CustomerCardNumber { get; set; }

        public string BankName { get; set; }

        public string CardBrand { get; set; }

        public string CardType { get; set; }

        public string AgentMobile { get; set; }

        public string PaymentTxnId { get; set; }
    }
}
