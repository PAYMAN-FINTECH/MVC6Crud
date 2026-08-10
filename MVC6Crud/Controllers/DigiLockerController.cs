using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models.PaymanApp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using XAct.Library.Settings;
using System.Drawing.Printing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Razor;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace MVC6Crud.Controllers
{
    public class DigiLockerController : Controller
    {
        private readonly DigiLockerService _digilockerService;
        private readonly string clientId = "CF678492CQOFTSG8OI4S73DVFVN0";
        private readonly string clientSecret = "cfsk_ma_prod_1a60ece5af3976c5258d7afd8aa0a8eb_063c29b6";
        private readonly string apiUrl = "https://api.cashfree.com/verification/esignature/document";
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;

        public DigiLockerController(DigiLockerService digilockerService, IWebHostEnvironment env, ApplicationDbContext context,
             IRazorViewEngine viewEngine, ITempDataProvider tempDataProvider)
        {
            _digilockerService = digilockerService;
            _env = env;
            _context = context;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            QuestPDF.Settings.License = LicenseType.Community;
        }


        public async Task<string> SavePdfToDiskAsync(byte[] pdfBytes, string fileName)
        {
            // Correct: use WebRootPath
            var folder = Path.Combine(_env.WebRootPath, "signed-agreements");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, fileName);

            await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

            // return relative URL so browser can access it
            return "/signed-agreements/" + fileName;
        }




        public async Task<string> RenderViewToStringAsync(string viewName, object model)
        {
            var actionContext = new ActionContext(HttpContext, RouteData, ControllerContext.ActionDescriptor);


            using var sw = new StringWriter();


            var viewResult = _viewEngine.FindView(actionContext, viewName, false);
            if (viewResult.View == null)
                throw new Exception($"View {viewName} not found");


            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };


            var tempData = new TempDataDictionary(HttpContext, _tempDataProvider);


            var viewContext = new ViewContext(actionContext, viewResult.View, viewDictionary, tempData, sw, new HtmlHelperOptions());


            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }
        private AgreementViewModel LoadAgreementModel(string id)
        {

            return new AgreementViewModel
            {
                Id = "wytrewy",
                Name = "gfgsdjfds",
                Age = 43,
                PAN = "dgfds",
                Aadhaar = "11127737",
                Address = "fdsgfygsyf",
                SignerName = "gfhdsgjfg",
                AadhaarLast4 = "fdgsayfusgf",
                EsignUid = "hfgdhgf",
                SignatureImageBase64 = "SignatureImageBase64",
                TransactionId = "TransactionId",
                SignedAt = DateTime.Now,
                PdfFilePath = "PdfFilePath"
            };
        }

        //public byte[] BuildAgreementPdf()
        //{
        //    try
        //    {
        //        var document = new AgreementPdf();
        //        return document.GeneratePdf();
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return Array.Empty<byte>();
        //}

        private async Task<int?> UploadPdfBytesToDigiLocker(byte[] pdfBytes)
        {
            using var client = new HttpClient();


            client.DefaultRequestHeaders.Add("x-client-id", clientId);
            client.DefaultRequestHeaders.Add("x-client-secret", clientSecret);


            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(pdfBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");


            form.Add(fileContent, "document", "Agreement.pdf");


            var response = await client.PostAsync(apiUrl, form);
            if (!response.IsSuccessStatusCode) return null;


            var json = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(json);
            return result["document_id"]?.Value<int>();
        }


        [HttpGet]
        public IActionResult Index(string userPhone)
        {
            if (string.IsNullOrEmpty(userPhone))
                return BadRequest("User phone missing");

            var user = _context.payManUsers.FirstOrDefault(x => x.Phone == userPhone);

            if (user == null)
                return NotFound("User not found");

            var userdetails = _context.aadharDetails
        .FirstOrDefault(t => t.Phone == userPhone);

            if (userdetails == null)
                return BadRequest("User Aadhaar details not found");

            // 🔹 Fetch PAN from userDocuments table (corrected)
            var panNumber = _context.userDocuments
    .Where(t => t.Phone == userPhone)
    .Select(t => t.PanCardNumber)
    .FirstOrDefault();

            if (panNumber == null)
                return BadRequest("User document details not found");

            AgreementPage model = new AgreementPage
            {
                UserName = userdetails.Name,
                PanNumber = panNumber,
                AadhaarNumber = userdetails.AadharNo,
                Address = userdetails.Address,
                UserPhone = userPhone
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> StartDigiLocker([FromBody] KYCRequest request)
        {
            var response = await _digilockerService.CreateDigiLockerUrlAsync(
                "PAYMAN" + DateTime.Now.ToString("yyyymmddhhmmss"),
                request.RedirectUrl,
                request.Documents
            );

            return Ok(new { url = response.url });
        }


        [HttpGet]
        public async Task<IActionResult> Callback(string verification_id)
        {
            if (string.IsNullOrEmpty(verification_id))
            {
                return BadRequest("Verification ID is missing.");
            }

            try
            {
                Console.WriteLine($"DigiLocker Callback received: {verification_id}");

                // 1️⃣ Get the verification status
                var result = await GetVerificationStatus(verification_id);

                // 2️⃣ If success → fetch documents
                if (result.Status == "AUTHENTICATED")
                {
                    var documents = await FetchDocuments(result.VerificationId, result.ReferenceId, result.DocumentConsent.FirstOrDefault());

                    // Extract user data if present
                    //var userDetails = new
                    //{
                    //    Name = (string?)documents?.user_details?.name,
                    //    Dob = (string?)documents?.user_details?.dob,
                    //    Gender = (string?)documents?.user_details?.gender,
                    //    Eaadhar = (string?)documents?.user_details?.eaadhar,
                    //    Mobile = (string?)documents?.user_details?.mobile,
                    //    PhotoBase64 = (string?)documents?.photo // if available
                    //};

                    //// 3️⃣ Save into database
                    //var record = new VerificationRecord
                    //{
                    //    VerificationId = verification_id,
                    //    Status = status,
                    //    Name = userDetails.Name,
                    //    Dob = userDetails.Dob,
                    //    Gender = userDetails.Gender,
                    //    Eaadhar = userDetails.Eaadhar,
                    //    Mobile = userDetails.Mobile,
                    //    PhotoBase64 = userDetails.PhotoBase64,
                    //    CreatedOn = DateTime.Now
                    //};

                    //_context.VerificationRecords.Add(record);
                    //await _context.SaveChangesAsync();

                    // 4️⃣ Optionally redirect or show success
                    return Ok(new { message = "Verification successful" });
                }
                else
                {
                    // Save failure
                    //var failed = new VerificationRecord
                    //{
                    //    VerificationId = verification_id,
                    //    Status = status,
                    //    CreatedOn = DateTime.Now
                    //};

                    //_context.VerificationRecords.Add(failed);
                    //await _context.SaveChangesAsync();

                    return Ok(new { message = "Verification failed", result.Status });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Callback: " + ex.Message);
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        private async Task<DigiLockerCallbackModel1> GetVerificationStatus(string verificationId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // ✅ Set required headers
                    client.DefaultRequestHeaders.Add("x-client-id", clientId);
                    client.DefaultRequestHeaders.Add("x-client-secret", clientSecret);
                    //client.DefaultRequestHeaders.Add("x-api-version", "2023-08-01");

                    // ✅ Correct endpoint (NO trailing slash after /status)
                    var url = $"https://api.cashfree.com/verification/digilocker?verification_id={Uri.EscapeDataString(verificationId)}";
                    var response = await client.GetAsync(url);

                    var content = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<DigiLockerCallbackModel1>(content);
                        return result;
                    }
                    else
                    {
                        Console.WriteLine($"Cashfree Error: {response.StatusCode} - {content}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return null;
            }
        }

        private async Task<List<DigiLockerDocumentResponse>> FetchDocuments(string verificationId, long? referenceId = null, string DocumentConsent = null)
        {
            if (string.IsNullOrWhiteSpace(verificationId))
                throw new ArgumentException("verificationId must be provided", nameof(verificationId));

            try
            {
                const string baseUrl = "https://api.cashfree.com/verification/digilocker/document";
                const string documentType = "AADHAAR"; // change as needed

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("x-client-id", clientId);
                    client.DefaultRequestHeaders.Add("x-client-secret", clientSecret);

                    var urlBuilder = new StringBuilder($"{baseUrl}/{documentType}");
                    urlBuilder.Append($"?verification_id={WebUtility.UrlEncode(verificationId)}");
                    if (referenceId.HasValue)
                    {
                        urlBuilder.Append($"&reference_id={referenceId.Value}");
                    }

                    var response = await client.GetAsync(urlBuilder.ToString());
                    var content = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        // Optionally log the response content for debugging
                        throw new HttpRequestException($"Cashfree API error: {response.StatusCode} – {content}");
                    }

                    // The API returns a single object, not a list — adjust based on what you expect
                    var result = JsonConvert.DeserializeObject<DigiLockerDocumentResponse>(content);
                    return new List<DigiLockerDocumentResponse> { result };
                }
            }
            catch (Exception ex)
            {
                // Log ex.Message
                return null;
            }
        }




        [HttpPost]
        public async Task<IActionResult> UploadPdfBytes([FromBody] PdfUploadRequest request)
        {
            byte[] pdfBytes = request.PdfBytes.ToArray();

            var watermarkPath = Path.Combine(_env.WebRootPath, "images", "watermark.png");
            var watermarkBytes = await System.IO.File.ReadAllBytesAsync(watermarkPath);

            var userdetails = _context.aadharDetails
        .FirstOrDefault(t => t.Phone == request.userPhone);

            if (userdetails == null)
                return BadRequest("User Aadhaar details not found");

            // 🔹 Fetch PAN from userDocuments table (corrected)
            var panNumber = _context.userDocuments
    .Where(t => t.Phone == request.userPhone)
    .Select(t => t.PanCardNumber)
    .FirstOrDefault();

            if (panNumber == null)
                return BadRequest("User document details not found");

            // 3. Fill Agreement Data (dynamic)
            var agreementData = new AgreementData
            {
                FullName = userdetails.Name,
                Age = 10,
                Pan = panNumber,
                Aadhaar = userdetails.AadharNo,
                Address = userdetails.Address
            };

            // 4. Generate Final Agreement PDF
            var pdf = new AgreementPdf(watermarkBytes, agreementData);
            var finalPdfBytes = pdf.GeneratePdf();

            var documentId = await UploadPdfBytesToDigiLocker(finalPdfBytes);
            if (documentId != null)
            {
                var signingLink = await CreateESignRequestAsync(documentId.Value, request.userPhone, request.latitude, request.longitude);
                if (signingLink != null)
                {
                    return Ok(new { signingLink });
                }
            }

            return BadRequest("Unable to generate signing link");
        }

        
        private async Task<string> CreateESignRequestAsync(int documentId, string userPhone, string latitude, string longitude)
        {
            var vefId = "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss");

            var signedDocument = new SignedDocumentAgreement
            {
                UserId = userPhone,
                Verification_Id = vefId,
                isVerifed = false
            };

            await _context.SignedDocumentAgreements.AddAsync(signedDocument);
            await _context.SaveChangesAsync();

            var userdetails = _context.payManUsers.FirstOrDefault(t => t.Phone == userPhone);
            var useraadherdetails = _context.aadharDetails.FirstOrDefault(t => t.Phone == userPhone);

            if (userdetails == null || useraadherdetails == null)
                return null;

            // 🔹 Aadhaar last 4 digits (dynamic)
            string aadhaarLast4 = useraadherdetails.AadharNo?.Length >= 4
                ? useraadherdetails.AadharNo.Substring(useraadherdetails.AadharNo.Length - 4)
                : "";

            var requestUrl = "https://api.cashfree.com/verification/esignature";
            var requestData = new
            {
                verification_id = vefId,
                document_id = documentId,
                notification_modes = new[] { "email" },
                auth_type = "AADHAAR",
                expiry_in_days = "15",
                capture_location = true,  // IMPORTANT: Must be true when sending lat/long
                redirect_url = "https://paymanfintech.in/DigiLocker/RedirectStatus",

                signers = new[]
                {
            new
            {
                name = useraadherdetails.Name,
                email = userdetails.Email,
                phone = userdetails.Phone,
                sequence = 1,
                aadhaar_last_four_digit = aadhaarLast4,

                // Add latitude & longitude inside meta_data
                meta_data = new
                {
                    latitude = latitude,
                    longitude = longitude
                },

                sign_positions = new[]
                {
                    new {
        page = 1,
        top_left_x_coordinate = 340,
        top_left_y_coordinate = 120,
        bottom_right_x_coordinate = 500,
        bottom_right_y_coordinate = 500
    }
                }
            }
        }
            };

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-client-id", clientId);
                client.DefaultRequestHeaders.Add("x-client-secret", clientSecret);

                var jsonContent = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync(requestUrl, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<JObject>(jsonResponse);
                    return result["signing_link"]?.Value<string>();
                }
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> RedirectStatus(string verification_id)
        {
            var digiLockerResponse = await GetESignStatusAsync(verification_id);

            if (digiLockerResponse.Status == "SUCCESS" &&
                !string.IsNullOrEmpty(digiLockerResponse.Signed_Doc_Url))
            {
                // 1. Download signed PDF
                var filebytes = await StoreSignedDocumentAsync(
                    digiLockerResponse.Signed_Doc_Url
                );

                // 2. Fetch existing record
                var usersigndocument = _context.SignedDocumentAgreements
                    .FirstOrDefault(t => t.Verification_Id == digiLockerResponse.Verification_Id);

                if (usersigndocument != null)
                {
                    // 3. Update main ESIGN record
                    usersigndocument.DocumentData = filebytes;
                    usersigndocument.Document_Id = digiLockerResponse.Document_Id;
                    usersigndocument.Status = digiLockerResponse.Status;
                    usersigndocument.Reference_Id = digiLockerResponse.Reference_Id;
                    usersigndocument.Signed_Doc_Url = digiLockerResponse.Signed_Doc_Url;

                    var signer = digiLockerResponse.Signers?.FirstOrDefault();
                    if (signer != null)
                    {
                        usersigndocument.Is_Notified = signer.Is_Notified;
                        usersigndocument.Name = signer.Name;

                        var meta = signer.Meta_Data;
                        if (meta != null)
                        {
                            usersigndocument.Gender = meta.Gender;
                            usersigndocument.Year_Of_Birth = meta.Year_Of_Birth;
                            usersigndocument.Postal_Code = meta.Postal_Code;
                            usersigndocument.State = meta.State;
                            usersigndocument.Country = meta.Country;
                            usersigndocument.Serial_Number = meta.Serial_Number;
                            usersigndocument.Ip_Address = meta.Ip_Address;
                            usersigndocument.Latitude = meta.Latitude;
                            usersigndocument.Longitude = meta.Longitude;
                            usersigndocument.Signing_Time = meta.Signing_Time;
                            usersigndocument.Aadhaar_Last_Four_Digit = meta.Aadhaar_Last_Four_Digit;
                        }
                    }

                    usersigndocument.isVerifed = true;

                    _context.SignedDocumentAgreements.Update(usersigndocument);
                    await _context.SaveChangesAsync();



                    // 4. Update user details table
                    var usedetils = _context.payManUsers
                        .FirstOrDefault(t => t.Phone == usersigndocument.UserId);

                    if (usedetils != null)
                    {
                        usedetils.IsAgreement = true;

                        _context.payManUsers.Update(usedetils);
                        await _context.SaveChangesAsync();
                    }
                }

                // 5. Redirect to dashboard
                return RedirectToAction("Dashboard", "Home");
            }
            else
            {
                return View("Error");
            }
        }

        private async Task<DigiLockerResponse> GetESignStatusAsync(string verificationId)
        {
            var requestUrl = $"https://api.cashfree.com/verification/esignature?verification_id={verificationId}";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-client-id", clientId);
                client.DefaultRequestHeaders.Add("x-client-secret", clientSecret);

                var response = await client.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<DigiLockerResponse>(jsonResponse);
                    return result;
                }
            }

            return null;
        }

        private async Task<byte[]> StoreSignedDocumentAsync(string signedDocUrl)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(signedDocUrl);

                if (!response.IsSuccessStatusCode)
                    return null;

                try
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    return fileBytes;   // <-- RETURN HERE
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }



    }

    public class AgreementViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int? Age { get; set; }
        public string PAN { get; set; }
        public string Aadhaar { get; set; }
        public string Address { get; set; }


        public bool IsAgreed { get; set; }
        public DateTime AgreementDate { get; set; } = DateTime.UtcNow;


        public string SignerName { get; set; }
        public string AadhaarLast4 { get; set; }
        public string EsignUid { get; set; }
        public string SignatureImageBase64 { get; set; }
        public DateTime? SignedAt { get; set; }
        public string TransactionId { get; set; }


        public string PdfFilePath { get; set; }
    }


    public class EsignPayload
    {
        public string AgreementId { get; set; }
        public string SignerName { get; set; }
        public string AadhaarLast4 { get; set; }
        public string EsignUid { get; set; }
        public string SignatureImageBase64 { get; set; }
        public string TransactionId { get; set; }
        public DateTime SignedAt { get; set; }
    }

    public class Agreement
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string PAN { get; set; }
        public string Aadhaar { get; set; }
        public string Address { get; set; }
        public string SignerName { get; set; }
        public string AadhaarLast4 { get; set; }
        public string EsignUid { get; set; }
        public string SignatureImageBase64 { get; set; }
        public string TransactionId { get; set; }
        public DateTime? SignedAt { get; set; }
        public string PdfFilePath { get; set; }
    }

    public class KYCRequest
    {
        public string VerificationId { get; set; }
        public string RedirectUrl { get; set; }
        public string[] Documents { get; set; }
    }


    public class DigiLockerCallbackModel
    {
        public string VerificationId { get; set; }
        public string Status { get; set; }
        public UserDetails UserDetails { get; set; }
    }

    public class UserDetails
    {
        public string Name { get; set; }
        public string Dob { get; set; }
        public string Gender { get; set; }
        public string Eaadhar { get; set; }
        public string Mobile { get; set; }
    }
    public class VerificationStatus
    {
        public string Status { get; set; }
    }
    public class Document
    {
        public string DocumentType { get; set; }
        public string DocumentUrl { get; set; }
        public string Error { get; set; }
    }
    public class VerificationRecord
    {
        public int Id { get; set; }
        public string VerificationId { get; set; }
        public string Status { get; set; }
        public string Name { get; set; }
        public string Dob { get; set; }
        public string Gender { get; set; }
        public string Eaadhar { get; set; }
        public string Mobile { get; set; }
    }

    public class DigiLockerCallbackModel1
    {
        [JsonProperty("user_details")]
        public UserDetails1 UserDetails { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("document_requested")]
        public List<string> DocumentRequested { get; set; }

        [JsonProperty("document_consent")]
        public List<string> DocumentConsent { get; set; }

        [JsonProperty("verification_id")]
        public string VerificationId { get; set; }

        [JsonProperty("reference_id")]
        public long ReferenceId { get; set; }
    }

    public class UserDetails1
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("dob")]
        public string Dob { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("eaadhaar")]
        public string Eaadhar { get; set; }

        [JsonProperty("mobile")]
        public string Mobile { get; set; }
    }
    public class DigiLockerDocumentResponse
    {
        [JsonProperty("reference_id")]
        public long ReferenceId { get; set; }

        [JsonProperty("verification_id")]
        public string VerificationId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("uid")]
        public string Uid { get; set; }

        [JsonProperty("dob")]
        public string DateOfBirth { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("care_of")]
        public string CareOf { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("split_address")]
        public SplitAddress SplitAddress { get; set; }

        [JsonProperty("year_of_birth")]
        public int YearOfBirth { get; set; }

        [JsonProperty("photo_link")]
        public string PhotoLink { get; set; }

        [JsonProperty("xml_file")]
        public string XmlFile { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class SplitAddress
    {
        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("dist")]
        public string District { get; set; }

        [JsonProperty("house")]
        public string House { get; set; }

        [JsonProperty("landmark")]
        public string Landmark { get; set; }

        [JsonProperty("pincode")]
        public string Pincode { get; set; }

        [JsonProperty("po")]
        public string PostOffice { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("street")]
        public string Street { get; set; }

        [JsonProperty("subdist")]
        public string SubDistrict { get; set; }

        [JsonProperty("vtc")]
        public string VillageTownCity { get; set; }
    }

    public class PdfUploadRequest
    {
        public List<byte> PdfBytes { get; set; }
        public string userPhone { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }

    }

    public class DigiLockerResponse
    {
        public string Status { get; set; }
        public int Reference_Id { get; set; }
        public string Verification_Id { get; set; }
        public int Document_Id { get; set; }
        public List<Signer> Signers { get; set; }
        public string Signed_Doc_Url { get; set; }
    }

    public class Signer
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public bool Is_Notified { get; set; }
        public MetaData Meta_Data { get; set; }
    }

    public class MetaData
    {
        public string Name { get; set; }
        public string Gender { get; set; }
        public string Year_Of_Birth { get; set; }
        public string Postal_Code { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Serial_Number { get; set; }
        public string Ip_Address { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string Signing_Time { get; set; }
        public string Aadhaar_Last_Four_Digit { get; set; }
    }



}
