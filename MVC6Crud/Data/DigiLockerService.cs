
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Controllers;
using MVC6Crud.Models;
using MVC6Crud.Models.App;
using MVC6Crud.Models.DTOS;
using Newtonsoft.Json;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace MVC6Crud.Data
{
    public class DigiLockerService
    {
        private readonly string _clientId = "CF678492CQOFTSG8OI4S73DVFVN0";
        private readonly string _clientSecret = "cfsk_ma_prod_1a60ece5af3976c5258d7afd8aa0a8eb_063c29b6";
        private readonly string _createUrlEndpoint = "https://api.cashfree.com/verification/digilocker";
        private readonly string _statusEndpoint = "https://api.cashfree.com/verification/digilocker/status";
        private readonly string _documentEndpoint = "https://api.cashfree.com/verification/digilocker/document";
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public DigiLockerService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<CreateUrlResponse> CreateDigiLockerUrlAsync(string verificationId, string redirectUrl, string[] documents)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-client-id", _clientId);
                client.DefaultRequestHeaders.Add("x-client-secret", _clientSecret);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var requestObj = new
                {
                    verification_id = verificationId,
                    document_requested = documents,
                    redirect_url = redirectUrl,
                    user_flow = "signup"
                };
                string jsonBody = JsonConvert.SerializeObject(requestObj);
                HttpContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(_createUrlEndpoint, content);
               // response.EnsureSuccessStatusCode();

                string respJson = await response.Content.ReadAsStringAsync();
                var respObj = JsonConvert.DeserializeObject<CreateUrlResponse>(respJson);
                return respObj;
            }
        }

        public async Task<VerificationStatusResponse> GetVerificationStatusAsync(string verificationId)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-client-id", _clientId);
                client.DefaultRequestHeaders.Add("x-client-secret", _clientSecret);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string url = $"{_statusEndpoint}/{verificationId}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string respJson = await response.Content.ReadAsStringAsync();
                var respObj = JsonConvert.DeserializeObject<VerificationStatusResponse>(respJson);
                return respObj;
            }
        }

        public async Task<DocumentResponse> GetDocumentAsync(string verificationId, string documentType)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-client-id", _clientId);
                client.DefaultRequestHeaders.Add("x-client-secret", _clientSecret);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string url = $"{_documentEndpoint}/{documentType}?verification_id={verificationId}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string respJson = await response.Content.ReadAsStringAsync();
                var respObj = JsonConvert.DeserializeObject<DocumentResponse>(respJson);
                return respObj;
            }
        }


        //new

        public async Task<DigiLockerCreateResponseKyc> CreateDigilockerUrl(string verificationId)
        {
            using var client = new HttpClient
            {
                BaseAddress = new Uri("https://api.cashfree.com")
            };

            client.DefaultRequestHeaders.Add("x-client-id", _configuration["DigiLocker:ClientId"]);
            client.DefaultRequestHeaders.Add("x-client-secret", _configuration["DigiLocker:ClientSecret"]);
            client.DefaultRequestHeaders.Add("x-api-version", "2023-09-01");

            var payload = new
            {
                verification_id = verificationId,
                document_requested = new[] { "AADHAAR", "PAN" },
             //   redirect_url = _configuration["DigiLocker:RedirectUrl"],
                user_flow = "signin"
            };

            var response = await client.PostAsync(
                "/verification/digilocker",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            );

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception(json);

            return JsonConvert.DeserializeObject<DigiLockerCreateResponseKyc>(json)!;
        }


        //public async Task<DigiLockerStatusResponseKyc> GetStatusAsync(string verificationId)
        //{
        //    using var client = new HttpClient();
        //    client.BaseAddress = new Uri("https://api.cashfree.com");

        //    client.DefaultRequestHeaders.Clear();
        //    client.DefaultRequestHeaders.Add("x-client-id", _configuration["DigiLocker:ClientId"]);
        //    client.DefaultRequestHeaders.Add("x-client-secret", _configuration["DigiLocker:ClientSecret"]);
        //    client.DefaultRequestHeaders.Add("x-api-version", "2023-07-01");

        //    var response = await client.GetAsync(
        //        $"/verification/digilocker/status/{verificationId}"
        //    );

        //    if (!response.IsSuccessStatusCode)
        //        throw new Exception("Failed to fetch DigiLocker status");

        //    var json = await response.Content.ReadAsStringAsync();
        //    return JsonConvert.DeserializeObject<DigiLockerStatusResponseKyc>(json)!;
        //}

        public async Task<DigiLockerStatusResponse> GetStatusAsync(
    string verificationId,
    Guid userId,
    string userPhone)
        {
            try
            {
                using var client = new HttpClient();

                client.DefaultRequestHeaders.Add(
                    "x-client-id",
                    _configuration["DigiLocker:ClientId"]);

                client.DefaultRequestHeaders.Add(
                    "x-client-secret",
                    _configuration["DigiLocker:ClientSecret"]);

                var url =
                    $"https://api.cashfree.com/verification/digilocker?verification_id={Uri.EscapeDataString(verificationId)}";

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                // Store API log
                _context.errorModels.Add(new ErrorModel
                {
                    payload = "KYC Status Check",
                    agId = content,                 // Response body
                    reqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    respTime = "",
                    uid = "",
                    requestId = verificationId,
                    jsonBody = url,                 // Request URL
                    statuscode = response.IsSuccessStatusCode
                });

                await _context.SaveChangesAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Fail(
                        "FAILED",
                        $"Cashfree error: {response.StatusCode}");
                }

                var statusResult =
                    JsonConvert.DeserializeObject<DigiLockerCallbackModel1>(content);

                if (statusResult == null)
                {
                    return Fail("FAILED", "Invalid DigiLocker response");
                }

                if (!string.Equals(
                        statusResult.Status,
                        "AUTHENTICATED",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Fail(
                        statusResult.Status ?? "FAILED",
                        "DigiLocker authentication incomplete");
                }

                if (statusResult.UserDetails == null)
                {
                    return Fail("FAILED", "User details not found");
                }

                if (statusResult.UserDetails.Mobile != userPhone)
                {
                    return Fail("FAILED", "Mobile number mismatch");
                }

                if (statusResult.DocumentConsent == null ||
                    !statusResult.DocumentConsent.Any())
                {
                    return Fail("FAILED", "No consent documents found");
                }

                // Existing verification checks
                var panAlreadyVerified =
                    await _context.panDetails.AnyAsync(x =>
                        x.UserPhone == userPhone &&
                        x.IsVerified);

                var aadhaarAlreadyVerified =
                    await _context.aadharKycDetails.AnyAsync(x =>
                        x.UserPhone == userPhone &&
                        x.IsVerified);

                foreach (var consent in statusResult.DocumentConsent)
                {
                    var document = await FetchDocuments(
                        statusResult.VerificationId,
                        statusResult.ReferenceId,
                        consent);

                    if (document == null)
                    {
                        return Fail("FAILED", $"Document fetch failed for {consent}");
                    }

                    // ================= PAN =================
                    if (string.Equals(consent, "PAN", StringComparison.OrdinalIgnoreCase) &&
                        document?.PanData != null &&
                        string.Equals(document.PanData.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                    {
                        var panExists =
                            await _context.panDetails.AnyAsync(x =>
                                x.VerificationId == verificationId);

                        if (!panExists && !panAlreadyVerified)
                        {
                            DateTime? panDob = null;

                            if (!string.IsNullOrWhiteSpace(document.PanData.Dob))
                            {
                                if (DateTime.TryParseExact(
                                        document.PanData.Dob,
                                        "dd-MM-yyyy",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None,
                                        out var parsedDob))
                                {
                                    panDob = parsedDob;
                                }
                            }

                            _context.panDetails.Add(new PanDetails
                            {
                                UserId = userId,
                                UserPhone = userPhone,
                                ReferenceId = document.PanData.ReferenceId,
                                VerificationId = document.PanData.VerificationId,
                                Status = document.PanData.Status,
                                PanNumber = document.PanData.Pan,
                                PanType = document.PanData.Type,
                                NameOnPan = document.PanData.NameOnPan,
                                Dob = panDob ?? DateTime.MinValue,
                                Gender = document.PanData.Gender,
                                XmlFileUrl = document.PanData.XmlFile,
                                IsVerified = true,
                                CreatedAt = DateTime.UtcNow
                            });

                            panAlreadyVerified = true;
                        }
                    }

                    // ================= AADHAAR =================
                    if (string.Equals(consent, "AADHAAR", StringComparison.OrdinalIgnoreCase) &&
                        document?.AadhaarData != null &&
                        string.Equals(document.AadhaarData.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                    {
                        var aadhaarExists =
                            await _context.aadharKycDetails.AnyAsync(x =>
                                x.VerificationId == verificationId);

                        if (!aadhaarExists && !aadhaarAlreadyVerified)
                        {
                            _context.aadharKycDetails.Add(new AadharKycDetails
                            {
                                UserId = userId,
                                UserPhone = userPhone,
                                ReferenceId = document.AadhaarData.ReferenceId,
                                VerificationId = document.AadhaarData.VerificationId,
                                Status = document.AadhaarData.Status,
                                Name = document.AadhaarData.Name,
                                AadhaarMasked = document.AadhaarData.Uid,
                                Dob = ParseDate(document.AadhaarData.DateOfBirth),
                                Gender = document.AadhaarData.Gender,
                                CareOf = document.AadhaarData.CareOf,
                                Address = document.AadhaarData.Address,
                                Country = document.AadhaarData.SplitAddress?.Country,
                                State = document.AadhaarData.SplitAddress?.State,
                                District = document.AadhaarData.SplitAddress?.District,
                                Pincode = document.AadhaarData.SplitAddress?.Pincode,
                                PhotoBase64 = document.AadhaarData.PhotoLink,
                                XmlFileUrl = document.AadhaarData.XmlFile,
                                IsVerified = true,
                                CreatedAt = DateTime.UtcNow
                            });

                            aadhaarAlreadyVerified = true;
                        }
                    }
                }

                // Save all changes once
                await _context.SaveChangesAsync();

                // ================= UPDATE USER KYC =================
                if (panAlreadyVerified && aadhaarAlreadyVerified)
                {
                    var user = await _context.pMUsers
                        .FirstOrDefaultAsync(x => x.Id == userId);

                    if (user != null)
                    {
                        user.IsKycCompleted = true;

                        //_context.pMUsers.Update(user); // Not required if tracked

                        await _context.SaveChangesAsync();
                    }
                }

                return new DigiLockerStatusResponse
                {
                    IsSuccess = true,
                    Status = "VERIFIED",
                    Message = "Aadhaar & PAN verified successfully"
                };
            }
            catch (Exception ex)
            {
                return Fail("ERROR", ex.Message);
            }
        }
        private static DigiLockerStatusResponse Fail(string status, string message)
        {
            return new DigiLockerStatusResponse
            {
                IsSuccess = false,
                Status = status,
                Message = message
            };
        }

        private static DateTime? ParseDate(string date)
        {
            if (DateTime.TryParseExact(
                date,
                "dd-MM-yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
            {
                return parsed;
            }

            return null;
        }


        public async Task<DigiLockerFetchResult> FetchDocuments(
    string verificationId,
    long? referenceId = null,
    string documentConsent = null)
        {
            if (string.IsNullOrWhiteSpace(verificationId))
            {
                return new DigiLockerFetchResult
                {
                    IsSuccess = false,
                    Message = "VerificationId is required"
                };
            }

            if (string.IsNullOrWhiteSpace(documentConsent))
            {
                return new DigiLockerFetchResult
                {
                    IsSuccess = false,
                    Message = "DocumentConsent is required (AADHAAR / PAN)"
                };
            }

            try
            {
                const string baseUrl = "https://api.cashfree.com/verification/digilocker/document";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-client-id", _configuration["DigiLocker:ClientId"]);
                client.DefaultRequestHeaders.Add("x-client-secret", _configuration["DigiLocker:ClientSecret"]);

                var url = $"{baseUrl}/{documentConsent}" +
                          $"?verification_id={WebUtility.UrlEncode(verificationId)}";

                if (referenceId.HasValue)
                    url += $"&reference_id={referenceId.Value}";

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new DigiLockerFetchResult
                    {
                        IsSuccess = false,
                        Message = $"Cashfree error: {response.StatusCode}",
                        DocumentType = documentConsent
                    };
                }

                // PAN
                if (documentConsent.Equals("PAN", StringComparison.OrdinalIgnoreCase))
                {
                    var pan = JsonConvert.DeserializeObject<DigiLockerPanResponse>(content);

                    return new DigiLockerFetchResult
                    {
                        IsSuccess = pan?.Status == "SUCCESS",
                        Message = pan?.Status == "SUCCESS"
                            ? "PAN document fetched successfully"
                            : "PAN document fetch failed",
                        DocumentType = "PAN",
                        PanData = pan
                    };
                }

                // AADHAAR
                var aadhaar = JsonConvert.DeserializeObject<DigiLockerDocumentResponse>(content);

                return new DigiLockerFetchResult
                {
                    IsSuccess = aadhaar?.Status == "SUCCESS",
                    Message = aadhaar?.Status == "SUCCESS"
                        ? "Aadhaar document fetched successfully"
                        : "Aadhaar document fetch failed",
                    DocumentType = "AADHAAR",
                    AadhaarData = aadhaar
                };
            }
            catch (Exception ex)
            {
                return new DigiLockerFetchResult
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    DocumentType = documentConsent
                };
            }
        }



        public async Task<byte[]> DownloadDocument(string verificationId)
        {
            using var client = new HttpClient();
            var res = await client.GetAsync(
                $"/verification/digilocker/document/{verificationId}");

            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsByteArrayAsync();
        }


    }


    public class CreateUrlResponse
    {
        public string verification_id { get; set; }
        public long reference_id { get; set; }
        public string url { get; set; }
        public string status { get; set; }
        public string[] document_requested { get; set; }
        public string redirect_url { get; set; }
        public string user_flow { get; set; }
    }

    public class VerificationStatusResponse
    {
        public string verification_id { get; set; }
        public string status { get; set; }
        public string message { get; set; }
    }

    public class DocumentResponse
    {
        public string verification_id { get; set; }
        public string document_type { get; set; }
        public string status { get; set; }
        public string message { get; set; }
        public string photo_link { get; set; }
    }
}