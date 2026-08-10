using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using MVC6Crud.Models.App;
using MVC6Crud.Models.DTOS;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PineLab;
using Newtonsoft.Json;
using System.Text;
using XAct.Library.Settings;
using XAct.Users;
using OtpResponse = MVC6Crud.Models.PaymanApp.OtpResponse;

namespace MVC6Crud.Controllers
{
    public class KycController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DigiLockerService _digilockerService;
        private readonly string clientId = "CF678492CQOFTSG8OI4S73DVFVN0";
        private readonly string clientSecret = "cfsk_ma_prod_1a60ece5af3976c5258d7afd8aa0a8eb_063c29b6";
        public KycController(ApplicationDbContext context, IConfiguration configuration, DigiLockerService digilockerService)
        {
            _context = context;
            _configuration = configuration;
            _digilockerService = digilockerService;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetKycStatus()
        {
            var userId = long.Parse(User.FindFirst("uid")!.Value);

            var user = await _context.pMUsers.FindAsync(userId);

            return Ok(new
            {
                kycCompleted = user.IsKycCompleted
            });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitPan([FromBody] PanRequest1 model)
        {
            if (string.IsNullOrWhiteSpace(model.PanNumber))
                return BadRequest("PAN number required");

            var user = await _context.pMUsers
                .FirstOrDefaultAsync(x => x.Phone == model.Phone);

            if (user == null)
                return NotFound("User not found");

            var pan = await _context.panDetails
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (pan == null)
            {
                pan = new PanDetails
                {
                    UserId = user.Id,
                    PanNumber = model.PanNumber,
                    CreatedAt = DateTime.UtcNow
                };
                _context.panDetails.Add(pan);
            }

            pan.PanNumber = model.PanNumber.ToUpper();
            pan.PanNumber = $"{model.PanNumber.Substring(0, 3)}XXXX{model.PanNumber.Substring(7)}";
            pan.IsVerified = false; // ✅ PAN verification not done yet

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "PAN submitted successfully"
            });
        }


        [HttpPost]
        public async Task<IActionResult> VerifyAadhaarOtp([FromBody] VerifyAdharOtpRequest1 request)
        {
            if (string.IsNullOrWhiteSpace(request.Otp))
                return BadRequest("OTP required");

            var aadhaar = await _context.aadharKycDetails
                .FirstOrDefaultAsync(x => x.UserPhone == request.Phone);

            if (aadhaar == null)
                return BadRequest("Session not found");

            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://api.cashfree.com");

            client.DefaultRequestHeaders.Add("x-client-id", clientId);
            client.DefaultRequestHeaders.Add("x-client-secret", clientSecret);
            client.DefaultRequestHeaders.Add("x-api-version", "2023-01-01");

            // 🔹 VERIFY OTP
            var verifyPayload = new
            {
                otp = request.Otp,
                ref_id = aadhaar.ReferenceId
            };

            var verifyResponse = await client.PostAsync(
                "/verification/offline-aadhaar/verify",
                new StringContent(JsonConvert.SerializeObject(verifyPayload), Encoding.UTF8, "application/json")
            );

            if (!verifyResponse.IsSuccessStatusCode)
                return BadRequest(await verifyResponse.Content.ReadAsStringAsync());

            var verifyResult = JsonConvert.DeserializeObject<dynamic>(
                await verifyResponse.Content.ReadAsStringAsync());

            if (verifyResult.status != "VALID")
                return BadRequest("Invalid Aadhaar OTP");

            // 🔹 DOWNLOAD AADHAAR XML (AUTO)
            var docResponse = await client.GetAsync(
                $"/verification/offline-aadhaar/document?ref_id={aadhaar.ReferenceId}");

            if (!docResponse.IsSuccessStatusCode)
                return BadRequest(await docResponse.Content.ReadAsStringAsync());

            var docResult = JsonConvert.DeserializeObject<dynamic>(
                await docResponse.Content.ReadAsStringAsync());

            string aadhaarXml = docResult.aadhaar_xml;
            string shareCode = docResult.share_code;

            // 🔐 ENCRYPT XML BEFORE DB SAVE
            var encryptedXml = EncryptAadhaar(aadhaarXml);

            // 🔹 UPDATE KYC TABLE
            aadhaar.Name = verifyResult.name;
            aadhaar.Address = verifyResult.address;
            aadhaar.IsVerified = true;
            aadhaar.CreatedAt = DateTime.UtcNow;

            // 🔹 SAVE DOCUMENT
            //_context.aadharDocuments.Add(new AadharDocument
            //{
            //    UserId = aadhaar.UserId,
            //    AadhaarFile = encryptedXml,
            //    ShareCode = shareCode,
            //    IsEncrypted = true,
            //    UploadedAt = DateTime.UtcNow
            //});

            // 🔹 MARK USER KYC COMPLETE
            var user = await _context.pMUsers.FindAsync(aadhaar.UserId);
            user.IsKycCompleted = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                kycCompleted = true,
                aadhaarMasked = verifyResult.aadhaar_masked
            });
        }


        // =====================================================
        // 🔐 AES ENCRYPTION (SAMPLE)
        // =====================================================
        private string EncryptAadhaar(string plainText)
        {
            // Implement AES-256 or your existing encryption here
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }


        [HttpPost]
        public async Task<IActionResult> GenerateAadhaarOtp([FromBody] AdharNumberRequest1 request)
        {
            if (string.IsNullOrWhiteSpace(request.AdharNumber) || request.AdharNumber.Length != 12)
                return BadRequest("Valid Aadhaar number required");

            var user = await _context.pMUsers
                .FirstOrDefaultAsync(x => x.Phone == request.Phone);

            if (user == null)
                return NotFound("User not found");

            // Aadhar verification API URL
            var url = "https://api.cashfree.com/verification/offline-aadhaar/otp";

            // Payload to send to the API
            var payload = new
            {
                aadhaar_number = request.AdharNumber,
                generate_xml = true // 🔴 MUST
            };

            // CashFree API credentials
            var clientId = "CF678492CQOFTSG8OI4S73DVFVN0";
            var clientSecret = "cfsk_ma_prod_1a60ece5af3976c5258d7afd8aa0a8eb_063c29b6";

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

                var otpResponse = JsonConvert.DeserializeObject<OtpResponse>(
            await response.Content.ReadAsStringAsync());

                if (otpResponse == null || string.IsNullOrEmpty(otpResponse.ref_id))
                    return BadRequest("Invalid response from Aadhaar service");

                // 🔎 Check existing Aadhaar KYC attempt
                var aadhaarKyc = await _context.aadharKycDetails
                    .FirstOrDefaultAsync(x => x.UserId == user.Id);

                var maskedAadhaar = $"XXXX-XXXX-{request.AdharNumber.Substring(8, 4)}";

                if (aadhaarKyc == null)
                {
                    // 🆕 Insert
                    aadhaarKyc = new AadharKycDetails
                    {
                        UserId = user.Id,
                        UserPhone = user.Phone,
                        ReferenceId = Convert.ToInt32( otpResponse.ref_id),
                        AadhaarMasked = maskedAadhaar,
                        IsVerified = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.aadharKycDetails.Add(aadhaarKyc);
                }
                else
                {
                    // 🔁 Update existing attempt
                    aadhaarKyc.ReferenceId = Convert.ToInt32( otpResponse.ref_id);
                    aadhaarKyc.AadhaarMasked = maskedAadhaar;
                    aadhaarKyc.IsVerified = false;
                    aadhaarKyc.CreatedAt = DateTime.UtcNow;

                    _context.aadharKycDetails.Update(aadhaarKyc);
                }

                await _context.SaveChangesAsync();


                return Ok(new
                {
                    success = true,
                    refId = otpResponse.ref_id,
                    maskedAadhaar
                });
            }
        }

        //[HttpGet]
        //public async Task<IActionResult> callback( [FromQuery] string verification_id)
        //{
        //    //var session = await _context.DigiLockerSessions
        //    //    .FirstOrDefaultAsync(x => x.VerificationId == verification_id);

        //    //if (session == null)
        //    //    return BadRequest("Invalid verification id");

        //    //session.Status = status;
        //    //session.UpdatedAt = DateTime.UtcNow;

        //    await _context.SaveChangesAsync();

        //    // 🔁 Redirect to Flutter deep link
        //    return Redirect($"payman://kyc?vid={verification_id}");
        //}


        [HttpPost]
        public async Task<IActionResult> StartDigilocker([FromBody] StartDigiLockerRequestKyc req)
        {
            var vefId = "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var result = await _digilockerService.CreateDigilockerUrl(vefId);

            return Ok(new
            {
                result.verification_id,
                url = result.url
            });
        }


        [HttpPost]
        public async Task<IActionResult> GetDigiLockerStatus([FromBody] DigiLockerStatusRequestKyc1 request)
        {
            var user = await _context.pMUsers
                .FirstOrDefaultAsync(x => x.Phone == request.Phone);

            if (user == null)
                return NotFound("User not found");

            var result = await _digilockerService.GetStatusAsync(request.VerificationId, user.Id, user.Phone);

            //var record = await _context.aadharKycDetails
            //    .FirstOrDefaultAsync(x => x.AadharRefId == request.VerificationId);

            //if (record != null && result.Status == "VERIFIED")
            //{
            //    record.IsVerified = true;
            //    record.AadharNoMasked = "";
            //    record.VerifiedAt = DateTime.UtcNow;

            //    await _context.SaveChangesAsync();
            //}

            return Ok(result);
        }

        [HttpPost]
        public async Task<JsonResult> AdharNumberVerify1([FromBody] AdharNumberRequest request)
        {
            var userDetails = await _context.pMUsers
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
                                           .Where(t => t.Phone == request.Phone)
                                           .FirstOrDefaultAsync();
                        TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                        DateTime istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
                        if (adharDetails == null)
                        {
                            
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
                            if(adharDetails.Status == true)
                            {
                                return Json(new { success = false, message = "User alredy verified" });
                            }
                            else
                            {
                                adharDetails.UserId = userDetails.Id;
                                adharDetails.Phone = request.Phone;
                                adharDetails.AadharNo = request.AdharNumber;
                                adharDetails.AadharRefId = otpResponse?.ref_id;
                                adharDetails.Created = istTime;
                                _context.aadharDetails.Update(adharDetails);
                                _context.SaveChanges();
                            }
                            
                        }

                        // Return success with OTP response
                        return Json(new { success = true, result = otpResponse?.Message, refid = otpResponse?.ref_id });
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
            var userDetails = await _context.pMUsers
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

                    userDetails.IsKycCompleted = true;
                    _context.pMUsers.Update(userDetails);

                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Aadhaar KYC completed successfully.", otpResponse = otpResponse });
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

    }
}
