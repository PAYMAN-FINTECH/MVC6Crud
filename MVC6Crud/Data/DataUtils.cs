using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MVC6Crud.Controllers;
using MVC6Crud.Models;
using MVC6Crud.Models.App;
using MVC6Crud.Models.CSBPayOut;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PaymanWeb;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Ocsp;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using XAct.Library.Settings;
using static WhatsAppApi.Parser.FMessage;

namespace MVC6Crud.Data
{
    public class DataUtils
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public DataUtils(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public string GenerateOtp()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        public async Task<string> GetPinelabsAmount(string phone)
        {
            string balance = "0.00";
            var ispayoutviaCSB = await _context.PayManGateways.FirstOrDefaultAsync();

            if (ispayoutviaCSB.CsbbankPayout == true)
            {
                var clientid = _configuration["CSBPayOut:client_id"];
                var CSB_KEY = _configuration["CSBPayOut:client_secret"];
                var Scope = _configuration["CSBPayOut:scope"];
                var Grant_type = _configuration["CSBPayOut:grant_type"];
                var accunt = _configuration["CSBPayOut:accountId"];

                var tokenRes = await GetAccessTokenAsync(clientid, CSB_KEY, Scope, Grant_type);
                string token = tokenRes.access_token;

                var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);

                var startdate = istNow.ToString("yyyyMMdd");
                var enddate = istNow.ToString("yyyyMMdd");
                var todaye = istNow.ToString("yyyyMMdd");

                string responseJson = await GetAccountMiniStatementAsync(token, accunt ?? "", startdate, enddate, CSB_KEY, todaye);

                var user1 = new ErrorModel
                {
                    payload = "Csb bank response1",
                    agId = todaye.ToString() ?? "",
                    reqTime = responseJson ?? "",
                    respTime = startdate.ToString() ?? "",
                    requestId = enddate.ToString() ?? "",
                    uid = accunt ?? "",
                    statuscode = true,
                    jsonBody = ""
                };

                _context.errorModels.Add(user1);
                await _context.SaveChangesAsync();

                var result = JsonSerializer.Deserialize<MiniStatementResponse>(responseJson);

                if (result != null)
                {
                    decimal closingBalance = result.closingBalance;

                    var userw1 = new ErrorModel
                    {
                        payload = "Csb bank response1 closing",
                        agId = "",
                        reqTime = closingBalance.ToString(),
                        respTime = "",
                        requestId = "",
                        uid = "",
                        statuscode = true,
                        jsonBody = ""
                    };

                    _context.errorModels.Add(userw1);
                    await _context.SaveChangesAsync();

                    return closingBalance.ToString("0.00");

                }

                return "0.00";

            }

            return balance;
        }


        public async Task<string> GetUserWalletAmount(string phone)
        {
            var ispayoutviaCSB = await _context.PayManGateways.FirstOrDefaultAsync();

            //if (ispayoutviaCSB.CsbbankPayout == true)
            //{
            //    var clientid = _configuration["CSBPayOut:client_id"];
            //    var CSB_KEY = _configuration["CSBPayOut:client_secret"];
            //    var Scope = _configuration["CSBPayOut:scope"];
            //    var Grant_type = _configuration["CSBPayOut:grant_type"];
            //    var accunt = _configuration["CSBPayOut:accountId"];

            //    var tokenRes = await GetAccessTokenAsync(clientid, CSB_KEY, Scope, Grant_type);
            //    string token = tokenRes.access_token;

            //    var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            //    var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);

            //    var startdate = istNow.ToString("yyyyMMdd");
            //    var enddate = istNow.ToString("yyyyMMdd");
            //    var todaye = istNow.ToString("yyyyMMdd");

            //    string responseJson = await GetAccountMiniStatementAsync(token, accunt ?? "", startdate, enddate, CSB_KEY, todaye);

            //    var user1 = new ErrorModel
            //    {
            //        payload = "Csb bank response1",
            //        agId = todaye.ToString() ?? "",
            //        reqTime = responseJson ?? "",
            //        respTime = startdate.ToString() ?? "",
            //        requestId = enddate.ToString() ?? "",
            //        uid = accunt ?? "",
            //        statuscode = true,
            //        jsonBody = ""
            //    };

            //    _context.errorModels.Add(user1);
            //    await _context.SaveChangesAsync();

            //    var result = JsonSerializer.Deserialize<MiniStatementResponse>(responseJson);

            //    if (result != null)
            //    {
            //        decimal closingBalance = result.closingBalance;

            //        var userw1 = new ErrorModel
            //        {
            //            payload = "Csb bank response1 closing",
            //            agId = "",
            //            reqTime = closingBalance.ToString(),
            //            respTime = "",
            //            requestId = "",
            //            uid = "",
            //            statuscode = true,
            //            jsonBody = ""
            //        };

            //        _context.errorModels.Add(userw1);
            //        await _context.SaveChangesAsync();

            //        return closingBalance.ToString("0.00");

            //    }

            //    return "0.00";

            //}
            //else
            //{
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
            //}
 
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

        public async Task<string> BlanceCheck()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Set up headers
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

                    if (!response.IsSuccessStatusCode)
                    {
                        return "0.00";  // Return a default balance on failure
                    }

                    string responseContent = await response.Content.ReadAsStringAsync();
                    var balanceResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<BalanceResponse>(responseContent);

                    // Ensure `Data` and `Balance` exist before accessing them
                    return balanceResponse?.Data?.Balance?.Available ?? "0.00";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching balance: {ex.Message}");
                return "0.00"; // Return default balance if an error occurs
            }
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

                return "Error: " + response.StatusCode;
            }
            catch (Exception ex)
            {
                // Log ex
                return "Exception: " + ex.Message;
            }
        }

        public string GenerateRequestId()
        {
            return $"{Guid.NewGuid():N}{DateTime.Now:fff}".Substring(0, 35);
        }

        public string Encrypt(string plainText, string key)
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

        public string Decrypt(string encryptedHex, string key)
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

        public string MD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        public byte[] HexToBytes(string hex)
        {
            int length = hex.Length;
            byte[] result = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return result;
        }

        public async Task<BillResponseApp> BillAvenueFetchBill(BillRequestApp request)
        {
            var userDetails = await _context.payManUsers
                                          .FirstOrDefaultAsync(t => t.Phone == request.UserPhone);

            // headers setup...
            var accessCode = _configuration["BillAvenueKeys:accessCode"];
            var workingKey = _configuration["BillAvenueKeys:workingKey"];
            var instituteId = _configuration["BillAvenueKeys:instituteId"];
            var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];


            string requestId = GenerateRequestId();
            string ver = "1.0";

            var billerInfo = await _context.billAvenueCreditCardBillers
    .FirstOrDefaultAsync(t => t.blr_id == request.BillerId);
            if (billerInfo.blr_response == null)
            {
                return new BillResponseApp
                {
                    Success = false,
                    Message = "Bill fetch failed"
                };
            }

            XDocument doc = XDocument.Parse(billerInfo.blr_response);

            var inputs = doc.Descendants("billerInputParams")
                            .Descendants("paramInfo")
                            .Select(x => new
                            {
                                ParamName = (string)x.Element("paramName"),
                                MaxLength = (string)x.Element("maxLength")
                            })
                            .ToList();

            // Build <inputParams> dynamically
            XElement inputParams = new XElement("inputParams");

            foreach (var item in inputs)
            {
                string value = string.Empty;

                // Map ParamName → request value
                if (item.ParamName.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
                {
                    value = request.RegisteredMobile;
                }
                else if (item.ParamName.Contains("credit", StringComparison.OrdinalIgnoreCase))
                {
                    value = request.CreditCardLast4;
                }
                // 👇 you can extend here for other param types as needed
                else
                {
                    value = ""; // default empty
                }

                inputParams.Add(
                    new XElement("input",
                        new XElement("paramName", item.ParamName),
                        new XElement("paramValue", value)
                    )
                );
            }

            // Convert to string
            string additionalInfo = inputParams.ToString();

            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?> 
<billFetchRequest>
    <agentId>{userDetails.BillAvenueAgentId}</agentId>
    <agentDeviceInfo>
        <ip>{endpointIp}</ip>
        <initChannel>INT</initChannel>
        <mac>01-23-45-67-89-ab</mac>
    </agentDeviceInfo>
    <customerInfo>
        <customerMobile>{request.CustomerMobile}</customerMobile>
    </customerInfo>
    <billerId>{request.BillerId}</billerId>
{additionalInfo}
</billFetchRequest>";

            string encryptedPayload = Encrypt(merchantData, workingKey);

            string url = $"https://api.billavenue.com/billpay/extBillCntrl/billFetchRequest/xml" +
                         $"?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}&encRequest={encryptedPayload}";

            using var client = new HttpClient();
            try
            {
                var response = await client.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    return new BillResponseApp
                    {
                        Success = false,
                        Message = "Bill fetch failed"
                    };
                }

                var result = await response.Content.ReadAsStringAsync();

                string decryptedResponse = Decrypt(result, workingKey);

                // Deserialize XML to object
                var serializer = new XmlSerializer(typeof(BillAvenueBillFetchResponse));
                BillAvenueBillFetchResponse billResponse;
                using (var reader = new StringReader(decryptedResponse))
                {
                    billResponse = (BillAvenueBillFetchResponse)serializer.Deserialize(reader);
                }

                var wrapper = ExtractBillerResponse(decryptedResponse);

                if (billResponse?.billerResponse == null)
                {
                    return new BillResponseApp
                    {
                        Success = false,
                        Message = "Invalid response format"
                    };
                }

                // Map to BillResponseApp  Current Outstanding
                var minPayable = billResponse.additionalInfo?
                    .FirstOrDefault(i => i.infoName.Contains("Minimum Amount", StringComparison.OrdinalIgnoreCase))?.infoValue;

                var currentOutStanding = billResponse.additionalInfo?
                    .FirstOrDefault(i => i.infoName.Contains("Current Outstanding", StringComparison.OrdinalIgnoreCase))?.infoValue;


                return new BillResponseApp
                {
                    Success = billResponse.responseCode == "000",
                    Message = billResponse.responseCode == "000" ? "Bill fetched successfully" : "Failed to fetch bill",
                    ConsumerName = billResponse.billerResponse.customerName,
                    BillNumber = billResponse.billerResponse.customerName, // you might want a better unique ref
                    BillDate = wrapper == null ? "" : wrapper.BillerResponse,
                    DueDate = billResponse.billerResponse.dueDate,
                    TotalAmount = Convert.ToDecimal(billResponse.billerResponse.billAmount),
                    MinPayable = string.IsNullOrEmpty(minPayable) ? Convert.ToDecimal(billResponse.billerResponse.billAmount) : Convert.ToDecimal(minPayable),
                    CuurentOutStanding = string.IsNullOrEmpty(currentOutStanding) ? Convert.ToDecimal(billResponse.billerResponse.billAmount) : Convert.ToDecimal(currentOutStanding),
                    Param1 = request.RegisteredMobile,
                    Param2 = request.CreditCardLast4,
                    EnquiryReferenceId = requestId,


                    BillerResponse = wrapper.BillerResponse,
                    AdddditionalInfo = wrapper.AdddditionalInfo,
                    BillFetchResponse = wrapper.BillFetchResponse
                };
            }
            catch (Exception ex)
            {
                return new BillResponseApp
                {
                    Success = false,
                    Message = "Exception: " + ex.Message
                };
            }
        }

        public static BillFetchWrapper ExtractBillerResponse(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var node = doc.SelectSingleNode("//billerResponse");
            var additionalInfo = doc.SelectSingleNode("//additionalInfo");
            var billFetchResponse = doc.SelectSingleNode("//inputParams");

            var wrapper = new BillFetchWrapper
            {
                BillerResponse = node?.OuterXml,
                AdddditionalInfo = additionalInfo?.OuterXml,
                BillFetchResponse = billFetchResponse?.OuterXml
            };
            return wrapper;
        }


        public async Task<bool> CheckBinAsync(string binNumber, string externalRef)
        {
            string url = "https://api.instantpay.in/identity/binChecker";
            string latitude = "0.0";
            string longitude = "0.0";

            // Get current latitude/longitude from IP
            using (HttpClient geoClient = new HttpClient())
            {
                try
                {
                    var geoResponse = await geoClient.GetStringAsync("http://ip-api.com/json/");
                    var geoData = JsonSerializer.Deserialize<GeoResponse>(geoResponse);

                    if (geoData?.Status == "success")
                    {
                        latitude = geoData.Lat.ToString();
                        longitude = geoData.Lon.ToString();
                    }
                }
                catch
                {
                    latitude = "0.0";
                    longitude = "0.0";
                }
            }

            var json = $@"{{
            ""binNumber"": ""{binNumber}"",
            ""latitude"": ""{latitude}"",
            ""longitude"": ""{longitude}"",
            ""externalRef"": ""{externalRef}""
        }}";

            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var clientId = _configuration["Ipay:ClientId"];
                var clientSecret = _configuration["Ipay:ClientSecret"];
                var outletId = _configuration["Ipay:OutletId"];
                var endpointIp = _configuration["Ipay:EndpointIp"];
                var macAddress = _configuration["DeviceInfo:Mac"];
                var ipAddress = _configuration["DeviceInfo:Ip"];

                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("X-Ipay-Auth-Code", "1");
                client.DefaultRequestHeaders.Add("X-Ipay-Client-Id", clientId);
                client.DefaultRequestHeaders.Add("X-Ipay-Client-Secret", clientSecret);
                client.DefaultRequestHeaders.Add("X-Ipay-Endpoint-Ip", endpointIp);
                client.DefaultRequestHeaders.Add("X-Ipay-Outlet-Id", outletId);

               
                var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                // ✅ Parse API response and return true/false
                try
                {
                    var result = JsonSerializer.Deserialize<dynamic>(responseString);
                    return result?.Status?.ToLower() == "success";
                }
                catch
                {
                    return false;
                }
            }
        }

        public async Task<bool> SavePayInProfile(string mobile, string cardnum, string appPhone)
        {
            if (string.IsNullOrWhiteSpace(mobile) || string.IsNullOrWhiteSpace(cardnum) || string.IsNullOrWhiteSpace(appPhone))
                return false;

            var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                             TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            var profiledetail = await _context.payInProfiles
                .Where(x => x.Mobile == mobile && x.UserPhone == appPhone)
                .ToListAsync();

            // If no records found for this mobile-user combination
            if (profiledetail == null || profiledetail.Count == 0)
            {
                return false;
            }

            // Check if card already exists
            var cardProfile = profiledetail.FirstOrDefault(t => t.CardNumber == cardnum);
            if (cardProfile == null && profiledetail.Count > 1)
            {
                // Take existing name from any previous record
                var existingProfile = profiledetail.FirstOrDefault();

                var payInProfiles = new PayInProfiles
                {
                    Mobile = mobile,
                    CardNumber = cardnum,
                    UserPhone = appPhone,
                    CreatedDate = istTime,
                    Name = existingProfile?.Name,
                    Email = existingProfile?.Email
                };

                _context.payInProfiles.Add(payInProfiles);
                await _context.SaveChangesAsync();
                return true;
            }
            else
            {

                var existingProfile1 = profiledetail.FirstOrDefault();
                existingProfile1.CardNumber = cardnum;
                _context.payInProfiles.Update(existingProfile1);
                await _context.SaveChangesAsync();
                return true;

            }
        }

        public async Task SendTextMessage()
        {
            var url = "https://graph.facebook.com/v22.0/829090293624254/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = "919652724937",
                type = "text",
                text = new
                {
                    preview_url = false,
                    body = "text-message-content"
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization",
                    "Bearer EAAgxnTPYvj0BP8Spy2kgWqbGlzVRfNhD2ZAz4NCYLqax4lye3JFjcCQqyZCoLrj2mhWzMlGHBbjGD0hnjLPwlCOyhGgPyX1xOfgRiJHlUehvKtUgkk59TgIPefihGJyyxAjwZB7dSiUdYmXYpf1F09Jq8B6lKZBhJms2tGjlgTShTW5mvYqqbSqhJDe24oTvMAZDZD");

                var response = await client.PostAsync(url, content);
                string result = await response.Content.ReadAsStringAsync();

                Console.WriteLine(result);
            }
        }

        public async Task SendTemplateMessage(string mobile, string otp)
        {
            var url = "https://graph.facebook.com/v22.0/829090293624254/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                to = "919652724937",
                type = "template",
                template = new
                {
                    name = "hello_world",
                    language = new { code = "en_US" }
                }
            };



            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization",
                    "Bearer EAAgxnTPYvj0BQ3QlDZARcUQSVVBwuiqi9KjNYHuyq2sjqyJDYnx0ZASxjNvmQgqLULaX3vD1fMNzN59X2bjdCPlof4H2Y8wIGxCEW0HwZBcWw87yPiI27mdOfigK5WBsp1YSQCLZCGhOn6XASQaf2muWjG9FDk8COyAc0L7M001FDSkjcHhVeU79HB3i3wZDZD");

                var response = await client.PostAsync(url, content);
                string result = await response.Content.ReadAsStringAsync();

                Console.WriteLine(result);
            }
        }

        public async Task<SmsResponse> SendDLTSms(string mobile, string message)
        {
            string apiKey = "rmP2yLRtAuGo3WNsOkzhHFbcXinajZEpBIf9lSxv7Q48qYeUCgO9GAgRDnFe3Va6tmMqvklhBLZcyCzu";
            string senderId = "PMAN";
            string entityId = "1201177213125322574";
            string templateId = "1207177347112364315";

           // string message = $"{otp} is your OTP from PAYMAN to authenticate. Valid for 5 minutes. Never share your OTP or account details with anyone. Regards, PAYMAN";

            string url = $"https://www.fast2sms.com/dev/bulkV2" +
                         $"?authorization={apiKey}" +
                         $"&route=dlt_manual" +
                         $"&sender_id={senderId}" +
                         $"&entity_id={entityId}" +
                         $"&template_id={templateId}" +
                         $"&message={Uri.EscapeDataString(message)}" +
                         $"&numbers={mobile}";

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                var result = await response.Content.ReadAsStringAsync();

                var resultres = Newtonsoft.Json.JsonConvert.DeserializeObject<SmsResponse>(result);
                return resultres;
            }
        }

        public PaymentResponseProcess GenerateFailureResponse(string message, string status = "FAILED")
        {
            return new PaymentResponseProcess
            {
                Success = false,
                Amount = "",
                OrderId = "",
                ReferenceId = "",
                Category = "Credit Card",
                BillerName = "ICICI Credit Card",
                Status = status,
                Message = message
            };
        }

        // Logging helper used in above method - change implementation to use your logger
        private Task LogDebugAsync(string message)
        {
            var error = new ErrorModel
            {
                agId = message,
                payload = "",
                reqTime = "",
                requestId = "",
                respTime = "",
                uid = "",
                jsonBody = "",
                statuscode = false,

            };
            _context.errorModels.Add(error);
            _context.SaveChanges();
            return Task.CompletedTask;
        }

        // Fix LogPayOutAsync - DO NOT call SaveChanges here. Just add to context.
        public Task LogPayOutAsync(PayManUsers user, PaymentRequestApp request,
            DateTime dateTime, string result, string txnId, bool status, string blr_category_name)
        {
            var payout = new PayManPayOut
            {
                UserId = user.Id,
                UserPhone = request.Phone,
                PayOutId = txnId,
                RefId = request.EnquiryReferenceId,
                AccountHolderName = request.customerName ?? "Unknown",
                AccountNo = request.holderMobile,
                IfscCode = request.CustomerMobile,
                Amount = Convert.ToDecimal(request.Amount), // ensure request.Amount valid
                PayoutCommission = 15,
                BeneId = request.BillerId,
                PayOutType = blr_category_name,
                TxnType = request.LastFourDigits,
                Email = user.Email,
                Status = status,
                DateTime = dateTime,
                Result = result,
                Device = "B - " + request.Device
            };

            _context.payManPayOuts.Add(payout);
            return Task.CompletedTask;
        }

        // Fix LogPayInHistoryAsync - DO NOT call SaveChanges here. keep same amount logic (amount param is paise)
        public async Task LogPayInHistoryAsync(PayManUsers user, PaymentRequestApp request,
            DateTime dateTime, string txnId, bool status, decimal amountInPaise, string blr_category_name)
        {
            var userWalletAmount = await GetUserWalletAmount(request.Phone);

            var history = new PayManHistory
            {
                UserId = user.Id,
                UserPhone = request.Phone,
                TxnId = txnId,
                Amount = amountInPaise / 100m,   // keep previous behavior
                CardNumber = request.holderMobile,
                Mode = blr_category_name,
                Status = status,
                Created = dateTime,
                AvlBalance = Convert.ToDecimal(userWalletAmount)
            };

            _context.payManHistories.Add(history);
        }

        // Fix LogFailedTransactionAsync - avoid swallowing exceptions
        public async Task LogFailedTransactionAsync(PayManUsers user, PaymentRequestApp request,
            DateTime dateTime, string reason, string type, bool status = false)
        {
            try
            {
                // create unique ids for failed logs
                var failId = type + "_" + Guid.NewGuid().ToString("N");
                //await LogPayOutAsync(user, request, dateTime, reason, failId, status);
                //await LogPayInHistoryAsync(user, request, dateTime, failId, status, (decimal)(request.Amount * 100)); // pass paise
                //                                                                                                      // Persist the failed log entries immediately (if you want them saved regardless):
                //await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Do not swallow: log to console / logger so you can find the real issue.
                Console.WriteLine("LogFailedTransactionAsync failed: " + ex);
                // rethrow or swallow based on your policy. I will rethrow so caller knows.
                throw;
            }
        }
        public string BuildTransactionStatusXml(string trackValue)
        {
            // Use TRANS_REF_ID (BillAvenue expects TRANS_REF_ID / BILL_RESP_ID etc.)
            return $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<transactionStatusReq>
    <trackType>REQUEST_ID</trackType>
    <trackValue>{SecurityElement.Escape(trackValue ?? "")}</trackValue>
</transactionStatusReq>";
        }

        public async Task<ExtBillPayResponse11?> FetchTransactionStatusAsync(
            string trackValue,
            string accessCode,
            string instituteId,
            string workingKey)
        {
            try
            {
                string xmlData = BuildTransactionStatusXml(trackValue);
                string encryptedPayload = Encrypt(xmlData, workingKey);
                string requestId = "REQ" + DateTime.Now.ToString("yyyyMMddHHmmss");
                string ver = "1.0";

                string endpoint = "https://api.billavenue.com/billpay/transactionStatus/fetchInfo/xml";

                string url = endpoint +
                             $"?accessCode={Uri.EscapeDataString(accessCode)}" +
                             $"&requestId={Uri.EscapeDataString(requestId)}" +
                             $"&ver={Uri.EscapeDataString(ver)}" +
                             $"&instituteId={Uri.EscapeDataString(instituteId)}" +
                             $"&encRequest={Uri.EscapeDataString(encryptedPayload)}";

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(40) };

                var httpResponse = await client.PostAsync(url, null);
                var raw = await httpResponse.Content.ReadAsStringAsync();

                // Log raw HTTP response for debugging
               // await LogDebugAsync($"Status API HTTP {(int)httpResponse.StatusCode} RAW: {raw}");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    await LogDebugAsync($"Status API returned HTTP {(int)httpResponse.StatusCode}");
                    return null;
                }

                // Response typically comes as: encResponse=xxxxx
                var trimmed = (raw ?? "").Trim();
                string encryptedResponse;
                if (trimmed.StartsWith("encResponse=", StringComparison.OrdinalIgnoreCase))
                    encryptedResponse = trimmed.Substring("encResponse=".Length).Trim();
                else
                    encryptedResponse = trimmed; // allow either

                if (string.IsNullOrWhiteSpace(encryptedResponse))
                {
                    await LogDebugAsync("Status API returned empty encResponse");
                    return null;
                }

                string decryptedXml;
                try
                {
                    decryptedXml = Decrypt(encryptedResponse, workingKey)?.Trim() ?? "";
                }
                catch (Exception ex)
                {
                    await LogDebugAsync($"Decrypt failed: {ex.Message}");
                    return null;
                }

                // Log decrypted xml for debugging
                await LogDebugAsync("Decrypted Status XML: " + decryptedXml);

                if (string.IsNullOrWhiteSpace(decryptedXml) || !decryptedXml.TrimStart().StartsWith("<"))
                {
                    await LogDebugAsync("Decrypted content is not valid XML.");
                    return null;
                }

                try
                {
                    var serializer = new XmlSerializer(typeof(ExtBillPayResponse11));
                    using var reader = new StringReader(decryptedXml);
                    var result = (ExtBillPayResponse11)serializer.Deserialize(reader);
                    return result;
                }
                catch (Exception ex)
                {
                    await LogDebugAsync("XML Deserialization failed for status response: " + ex.Message);
                    // optionally save decryptedXml to logs for manual inspection
                    await LogDebugAsync("Bad XML payload: " + decryptedXml);
                    return null;
                }
            }
            catch (Exception ex)
            {
                await LogDebugAsync("FetchTransactionStatusAsync exception: " + ex.Message);
                return null;
            }
        }



        public async Task<BillerRecord?> GetBillerCategoryAsync(string billerId, string userPhone)
        {
            if (string.IsNullOrWhiteSpace(billerId))
                return null;

            billerId = billerId.Trim().ToUpper();


                // Fetch biller
                var biller1 = await _context.billAvenueCreditCardBillers
                    .FirstOrDefaultAsync(b => b.blr_id.Trim().ToUpper() == billerId);

                if (biller1 == null)
                    return null;

                // If not active or missing response → refresh
                if (string.IsNullOrEmpty(biller1.blr_response) || !biller1.IsActive)
                {
                    var status = await GetBillers(biller1.blr_id);  // <-- Your method

                    if (!status)
                {
                    return new BillerRecord
                    {
                        BillerId = biller1.blr_id,
                        BillerName = biller1.blr_name ?? "",
                        CategoryName = biller1.blr_category_name ?? "",
                        CategoryKey = biller1.blr_response ?? "",
                        IsAvailable = biller1.IsActive,
                        userPhone = userPhone
                    };
                }

                    // Reload updated biller after refresh
                    biller1 = await _context.billAvenueCreditCardBillers
                        .FirstOrDefaultAsync(b => b.blr_id.Trim().ToUpper() == billerId);
                }

           
            // Prepare response model
            return new BillerRecord
            {
                BillerId = biller1.blr_id,
                BillerName = biller1.blr_name,
                CategoryName = biller1.blr_category_name,
                CategoryKey = biller1.blr_response,
                IsAvailable = biller1.IsActive,
                userPhone = userPhone
            };
        }
        public async Task<bool> GetBillers(string billerId)
        {
            if (string.IsNullOrEmpty(billerId))
                return false;

            // headers setup...
            var accessCode = _configuration["BillAvenueKeys:accessCode"];
            var workingKey = _configuration["BillAvenueKeys:workingKey"];
            var instituteId = _configuration["BillAvenueKeys:instituteId"];
            var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];

            // string accessCode = "AVCY20UZ93QR58GOXV";
            string requestId = GenerateRequestId();
            //string workingKey = "2EEFEE24E33539706EA1E532D60461C7";
            string ver = "1.0";
            //string instituteId = "RP91";
            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<billerInfoRequest>
  <billerId>{billerId}</billerId>
</billerInfoRequest>";

            string apiUrl = $"https://api.billavenue.com/billpay/extMdmCntrl/mdmRequestNew/xml" +
                            $"?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}";

            string encryptedPayload = Encrypt(merchantData, workingKey);

            using var client = new HttpClient();

            try
            {
                var content = new StringContent(encryptedPayload, Encoding.UTF8, "text/plain");
                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    string decryptedResponse = Decrypt(result, workingKey);

                    var xdoc = XDocument.Parse(decryptedResponse);

                    // Get billerStatus
                    var billerStatus = xdoc.Root
                        .Element("biller")?
                        .Element("billerStatus")?.Value;

                    // Get payment modes
                    var paymentModes = xdoc.Root
                        .Element("biller")?
                        .Element("billerPaymentModes")?
                        .Elements("paymentModeInfo")
                        .Select(pm => pm.Element("paymentMode")?.Value)
                        .Where(pm => !string.IsNullOrEmpty(pm))
                        .ToList();

                    // Join payment modes as a comma-separated string
                    var paymentModesStr = string.Join(",", paymentModes);

                    var biller = await _context.billAvenueCreditCardBillers
                        .Where(b => b.blr_id == billerId)
                        .FirstOrDefaultAsync();

                    if (biller != null)
                    {
                        biller.IsActive = billerStatus == "ACTIVE";
                        biller.PaymentModes = paymentModesStr;
                        biller.blr_response = decryptedResponse;
                        _context.billAvenueCreditCardBillers.Update(biller);
                        await _context.SaveChangesAsync();
                    }

                    return true;
                }

                return false; // request failed
            }
            catch (Exception ex)
            {
                // Log ex.Message here
                return false;
            }
        }
        public BillAvenueBillFetchResponse Fail(bool status,string message)
        {
            return new BillAvenueBillFetchResponse { status = status, message = message };
        }


        public async Task<BillAvenueBillFetchResponse> FetchBillAsync(
    string billerId,
    string requestId,
    string registeredMobile,
    string creditCardLast4,
    string customerMobile,
    string userPhone,
    string Category)
        {
            try
            {
                // 1. Fetch biller information
                var billerInfo = await _context.billAvenueCreditCardBillers
                    .FirstOrDefaultAsync(b => b.blr_id == billerId);

                var accessCode = _configuration["BillAvenueKeys:accessCode"];
                var workingKey = _configuration["BillAvenueKeys:workingKey"];
                var instituteId = _configuration["BillAvenueKeys:instituteId"];
                var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];
                string ver = "1.0";

                var testnum = "9652724937";

                var userDetails = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == testnum);
                var panCardNumber = await _context.userDocuments.Where(t => t.Phone == testnum).Select(t => t.PanCardNumber).FirstOrDefaultAsync();

                var aadharDetails = await _context.aadharDetails.FirstOrDefaultAsync(a => a.Phone == testnum);


                //var userDetails = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == customerMobile);
                //var panCardNumber = await _context.userDocuments.Where(t => t.Phone == customerMobile).Select(t => t.PanCardNumber).FirstOrDefaultAsync();

                //var aadharDetails = await _context.aadharDetails.FirstOrDefaultAsync(a => a.Phone == customerMobile);

               

                if (billerInfo == null || string.IsNullOrWhiteSpace(billerInfo.blr_response))
                {
                    return Fail(false,"Invalid BillerId or Biller XML not found");
                }

                // 2. Build <inputParams> dynamically
                var inputParams = BuildInputParams(
                    billerInfo.blr_response,
                    registeredMobile,
                    creditCardLast4,
                    billerInfo.blr_category_name,
                    customerMobile,
                    userPhone
                );

                // 3. Build final merchant XML
                string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?> 
<billFetchRequest>
    <agentId>CC01RP91MOBBAK024661</agentId>
    <agentDeviceInfo> 
        <app>tripozo</app> 
        <imei>000000000000000</imei> 
        <initChannel>MOB</initChannel> 
        <ip>{endpointIp}</ip> 
        <os>android</os> 
    </agentDeviceInfo>

    <customerInfo>
        <customerMobile>{testnum}</customerMobile>
        <customerEmail>{userDetails.Email}</customerEmail>
        <customerAdhaar>{aadharDetails.AadharNo}</customerAdhaar>
        <customerPan>{panCardNumber}</customerPan>
    </customerInfo>

    <billerId>{billerId}</billerId>
    {inputParams}
</billFetchRequest>";

                // 4. Encrypt XML
                string encryptedPayload = Encrypt(merchantData, workingKey);

                // 5. Build request URL
                string url =
                    $"https://api.billavenue.com/billpay/extBillCntrl/billFetchRequest/xml" +
                    $"?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}&encRequest={encryptedPayload}";

                // 6. Call API
                using var client = new HttpClient();
                var apiResponse = await client.PostAsync(url, null);

                if (!apiResponse.IsSuccessStatusCode)
                {
                    return Fail(false, $"HTTP Error: {apiResponse.StatusCode}");
                }

                // 7. Read encrypted response
                var encryptedResult = await apiResponse.Content.ReadAsStringAsync();

                // 8. Decrypt response
                var decryptedXml = Decrypt(encryptedResult, workingKey);

                if (string.IsNullOrWhiteSpace(decryptedXml))
                    return Fail(false,"Failed to decrypt BillAvenue response");

                // 9. Deserialize response into your model
                var billResponse = Deserialize<BillAvenueBillFetchResponse>(decryptedXml);


                if (billResponse.responseCode != "000")
                {
                    var response = Deserialize<BillFetchResponseErrorModel>(decryptedXml);
                    billResponse.message = response.ErrorInfo.Error.ErrorMessage;
                }
                else
                {

                    var wrapper = ExtractBillerResponse(decryptedXml);
                    billResponse.BillerResponse1 = wrapper.BillerResponse;
                    billResponse.BillFetchResponse = wrapper.BillFetchResponse;
                    billResponse.AdddditionalInfo = wrapper.AdddditionalInfo;
                    billResponse.status = true;
                }
                    return billResponse;
            }
            catch (Exception ex)
            {
                return Fail(false, "Exception: " + ex.Message);
            }
        }


        private string BuildInputParams(
     string xml,
     string registeredMobile,
     string creditCardLast4,
     string blr_category_name,
     string customerMobile,
     string userPhone)
        {
            XDocument doc = XDocument.Parse(xml);

            var inputs = doc.Descendants("billerInputParams")
                            .Descendants("paramInfo")
                            .Select(x => (string)x.Element("paramName"))
                            .ToList();

            XElement inputParams = new XElement("inputParams");

            foreach (var paramName in inputs)
            {
                string value = string.Empty;
                string key = paramName.ToLower();

                // 🔹 DOB
                if (key.Contains("date of birth") || key.Contains("dob"))
                {
                    value = customerMobile;
                }
                // 🔹 Email
                else if (key.Contains("email"))
                {
                    value = creditCardLast4;
                }
                // 🔹 Policy Number
                else if (key.Contains("policy"))
                {
                    value = registeredMobile;
                }
                // 🔹 Consumer / Unique / Vehicle / Mobile
                else if (key.Contains("consumer") ||
                         key.Contains("unique") ||
                         key.Contains("vehicle") ||
                         key.Contains("mobile"))
                {
                    value =  registeredMobile;
                    // value = customerMobile ?? registeredMobile ?? userPhone;
                }
                // 🔹 Credit Card last 4 (if ever needed)
                else if (key.Contains("card"))
                {
                    value = creditCardLast4;
                }
                else
                {
                    // fallback safety
                    value = string.Empty;
                }

                inputParams.Add(new XElement("input",
                    new XElement("paramName", paramName),
                    new XElement("paramValue", value)
                ));
            }

            return inputParams.ToString(SaveOptions.DisableFormatting);
        }

        //private string BuildInputParams(string xml, string registeredMobile, string creditCardLast4, string blr_category_name, string customerMobile, string userphone)
        //{
        //    XDocument doc = XDocument.Parse(xml);

        //    var inputs = doc.Descendants("billerInputParams")
        //                    .Descendants("paramInfo")
        //                    .Select(x => new
        //                    {
        //                        ParamName = (string)x.Element("paramName"),
        //                    })
        //                    .ToList();

        //    XElement inputParams = new XElement("inputParams");

        //    foreach (var item in inputs)
        //    {
        //        string value = registeredMobile;

        //        if (item.ParamName.Contains("Consumer", StringComparison.OrdinalIgnoreCase))
        //            value = registeredMobile;

        //        if (item.ParamName.Contains("Unique", StringComparison.OrdinalIgnoreCase))
        //            value = registeredMobile;

        //        if (item.ParamName.Contains("Vehicle", StringComparison.OrdinalIgnoreCase))
        //            value = registeredMobile;


        //        inputParams.Add(new XElement("input",
        //            new XElement("paramName", item.ParamName),
        //            new XElement("paramValue", value)
        //        ));
        //    }

        //    return inputParams.ToString();
        //}

        private T Deserialize<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(xml);
            return (T)serializer.Deserialize(reader);
        }



        public async Task<PaymentResponseProcess> ProcessBillPaymentAsync(PaymentRequestApp request)
        {
            // ---- Load Keys ----
            var accessCode = _configuration["BillAvenueKeys:accessCode"];
            var workingKey = _configuration["BillAvenueKeys:workingKey"];
            var instituteId = _configuration["BillAvenueKeys:instituteId"];
            var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];

            // ---- Load User Details ----
            var userDetails = await _context.payManUsers.FirstOrDefaultAsync(u => u.Phone == request.Phone);
            var panCardNumber = await _context.userDocuments.Where(t => t.Phone == request.Phone).Select(t => t.PanCardNumber).FirstOrDefaultAsync();

            var aadharDetails = await _context.aadharDetails.FirstOrDefaultAsync(a => a.Phone == request.Phone);

            if (userDetails == null || aadharDetails == null || panCardNumber == null)
            {
                return new PaymentResponseProcess
                {
                    Success = false,
                    Status = "FAILED",
                    Message = "User details not found",
                    UserPhone = request.Phone,
                    UserName = request.customerName
                };
            }

            // ---- Get Biller ----
            var billerDetails = await _context.billAvenueCreditCardBillers
                .FirstOrDefaultAsync(t => t.blr_id == request.BillerId);

            if (billerDetails == null)
            {
                return new PaymentResponseProcess
                {
                    Success = false,
                    Status = "FAILED",
                    Message = "Biller details not found",
                    UserPhone = request.Phone,
                    UserName = request.customerName
                };
            }

            // ---- Determine Payment Mode ----
            string paymentMode, additionalInfo;
            GetPaymentModeAndInfo(billerDetails.PaymentModes, out paymentMode, out additionalInfo);

            // ---- Build XML ----
            string merchantData = BuildPaymentRequestXml(request, userDetails, aadharDetails, panCardNumber, billerDetails, endpointIp, paymentMode, additionalInfo);

            // ---- Encrypt ----
            var encryptedPayload = Encrypt(merchantData, workingKey);
            var requestId = SecurityElement.Escape(request.EnquiryReferenceId ?? Guid.NewGuid().ToString("N"));

            var url =
                $"https://api.billavenue.com/billpay/extBillPayCntrl/billPayRequest/xml?accessCode={accessCode}" +
                $"&requestId={requestId}&ver=1.0&instituteId={instituteId}&encRequest={HttpUtility.UrlEncode(encryptedPayload)}";

            // ---- Balance Check ----
            var instBalance = await GetInstantPayAmount();
            if (request.Amount > Convert.ToDouble(instBalance))
            {
                return new PaymentResponseProcess
                {
                    Success = false,
                    Status = "FAILED",
                    Message = "Insufficient Balance",
                    UserPhone = request.Phone,
                    UserName = request.customerName
                };
            }

            // ---- Call BillPay API ----
            var initialResponse = await CallBillPayApiAsync(url, workingKey);

            // ---- Save INITIATED logs ----
            var initialStatus = await SaveInitialLogsAsync(initialResponse, request, userDetails, accessCode,instituteId,workingKey);

            // if initial success, return directly
            if (initialStatus.Success)
            {
                return initialStatus;
            }

            // ---- Do Status Check (3 attempts) ----
            var finalResult = await PerformStatusCheckAsync(
                request.EnquiryReferenceId,
                initialResponse,
                accessCode,
                instituteId,
                workingKey,
                request,
                userDetails,
                billerDetails
            );

            return finalResult;
        }

        /* ----------------------------------------------------
         * Below here: helper methods (reusable)
         * ----------------------------------------------------
         */

        private static void GetPaymentModeAndInfo(string modeString, out string pmode, out string info)
        {
            var modes = modeString.Split(',')
                .Select(m => m.Trim().ToLower())
                .ToList();

            pmode = "UPI";
            info = @"<info><infoName>VPA</infoName><infoValue>9652724937@kotak</infoValue></info>";

            if (modes.Contains("cash1"))
            {
                pmode = "Cash";
                info = @"<info><infoName>Remarks</infoName><infoValue>CashPayment</infoValue></info>";
            }
            else if (modes.Contains("wallet1"))
            {
                pmode = "Wallet";
                info = @"
                <info><infoName>WalletName</infoName><infoValue>Forpay</infoValue></info>
                <info><infoName>MobileNo</infoName><infoValue>7286887024</infoValue></info>
            ";
            }
        }

        private string BuildPaymentRequestXml(
            PaymentRequestApp request,
            PayManUsers user,
            AadharDetails aadhar,
            string panNumber,
            BillAvenueCreditCardBillers biller,
            string endpointIp,
            string paymentMode,
            string extraInfo)
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<billPaymentRequest>
  <agentId>CC01RP91MOBBAK024661</agentId>
  <agentDeviceInfo>
    <app>tripozo</app>
    <imei>000000000000000</imei>
    <initChannel>MOB</initChannel>
    <ip>{endpointIp}</ip>
    <os>android</os>
  </agentDeviceInfo>

  <customerInfo>
    <REMITTER_NAME>{aadhar.Name}</REMITTER_NAME>
    <customerMobile>{user.Phone}</customerMobile>
    <customerEmail>{user.Email}</customerEmail>
    <customerAdhaar>{aadhar.AadharNo}</customerAdhaar>
    <customerPan>{panNumber}</customerPan>
  </customerInfo>

  <billerId>{request.BillerId}</billerId>
  {request.BillFetchResponse}
  {request.BillerResponse}
  {request.AdddditionalInfo}

  <amountInfo>
    <amount>{request.Amount * 100}</amount>
    <currency>356</currency>
    <custConvFee>0</custConvFee>
  </amountInfo>

  <paymentMethod>
    <paymentMode>{paymentMode}</paymentMode>
    <quickPay>N</quickPay>
    <splitPay>N</splitPay>
  </paymentMethod>

  <paymentInfo>{extraInfo}</paymentInfo>
</billPaymentRequest>";
        }

        private async Task<ExtBillPayResponse> CallBillPayApiAsync(string url, string workingKey)
        {
            using var client = new HttpClient();
            var httpResponse = await client.PostAsync(url, null);
            var responseContent = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                return null;

            string enc = responseContent.Replace("encResponse=", "");
            string decrypted = Decrypt(enc, workingKey);

            var serializer = new XmlSerializer(typeof(ExtBillPayResponse));
            using var reader = new StringReader(decrypted);
            return (ExtBillPayResponse)serializer.Deserialize(reader);
        }

        private async Task<PaymentResponseProcess> SaveInitialLogsAsync(
            ExtBillPayResponse billResponse,
            PaymentRequestApp request,
            PayManUsers user, string accessCode, string instituteId, string workingKey)
        {
            // Save your payout and history logs here (same as your existing logic)
            // ...
            PaymentResponseProcess jlkj = new PaymentResponseProcess();
            try
            {
                var rsu = await ProcessCreditCardTransactionAsync(billResponse, request, user, accessCode, instituteId, workingKey);

                return new PaymentResponseProcess
                {
                    Success = rsu.Success,
                    Status = rsu.Status,
                    Amount = request.Amount.ToString(),
                    OrderId = "",//request.EnquiryReferenceId
                    UserPhone = request.Phone,
                    UserName = request.customerName
                };
            }catch(Exception ex)
            {

            }
            return jlkj;


        }

        private async Task<PaymentResponseProcess> PerformStatusCheckAsync(
            string enquiryRef,
            ExtBillPayResponse initialResponse,
            string accessCode,
            string instituteId,
            string workingKey,
            PaymentRequestApp request,
            PayManUsers user,
            BillAvenueCreditCardBillers biller)
        {
            // 3 attempts loop
            ExtBillPayResponse11? finalStatus = null;

            for (int i = 0; i < 3; i++)
            {
                finalStatus = await FetchTransactionStatusAsync(enquiryRef, accessCode, instituteId, workingKey);
                if (finalStatus?.TxnList?.TxnStatus?.ToLower() == "success") break;
                await Task.Delay(3000);
            }

            return new PaymentResponseProcess
            {
                Success = finalStatus?.TxnList?.TxnStatus?.ToLower() == "success",
                Status = finalStatus?.TxnList?.TxnStatus?.ToUpper() ?? "FAILED",
                Amount = request.Amount.ToString(),
                OrderId = enquiryRef,
                BillerName = biller.blr_name,
                UserPhone = request.Phone,
                UserName = request.customerName
            };
        }



        public async Task<PaymentResponseProcess> ProcessCreditCardTransactionAsync(
            ExtBillPayResponse billResponse,
             PaymentRequestApp request,
    PayManUsers userDetails, string accessCode, string instituteId, string workingKey)
        {

            IDbContextTransaction? dbTransaction = null;
           
            DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                              TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
            var billerDetails = await _context.billAvenueCreditCardBillers.FirstOrDefaultAsync(t => t.blr_id == request.BillerId);

            try
            {
               
                dbTransaction = await _context.Database.BeginTransactionAsync();


                // -----------------------------
                // 1. INITIAL LOGGING
                // -----------------------------
                var initialResultMsg = !string.IsNullOrWhiteSpace(billResponse?.ResponseReason)
                    ? billResponse.ResponseReason
                    : "INITIATED";

                var initialTxnId = billResponse?.TxnRefId
                    ?? request.EnquiryReferenceId
                    ?? Guid.NewGuid().ToString("N");

               
                bool initialIsSuccess = string.Equals(initialResultMsg, "Successful", StringComparison.OrdinalIgnoreCase);

                await LogPayOutAsync(userDetails, request, istDateTime, initialResultMsg, initialTxnId, initialIsSuccess, billerDetails.blr_category_name);
                await LogPayInHistoryAsync(userDetails, request, istDateTime, initialTxnId, initialIsSuccess, billResponse.RespAmount, billerDetails.blr_category_name);

                await _context.SaveChangesAsync();


                // -----------------------------
                // 2. EARLY SUCCESS
                // -----------------------------
                if (initialIsSuccess)
                {
                    await dbTransaction.CommitAsync();

                    return new PaymentResponseProcess
                    {
                        Success = true,
                        Amount = request.Amount.ToString(),
                        OrderId = billResponse?.TxnRefId ?? initialTxnId,
                        ReferenceId = billResponse?.ApprovalRefNumber ?? "NA",
                        Category = "Credit Card",
                        BillerName = billerDetails?.blr_name ?? "Unknown",
                        Status = "SUCCESS"
                    };
                }


                // -----------------------------
                // 3. STATUS CHECK (3 attempts)
                // -----------------------------
                string finalStatus = "PENDING";
                ExtBillPayResponse11? statusResponse = null;

                for (int attempt = 0; attempt < 3; attempt++)
                {
                    statusResponse = await FetchTransactionStatusAsync(
                        request.EnquiryReferenceId,
                        accessCode,
                        instituteId,
                        workingKey);

                    var txnStatus = statusResponse?.TxnList?.TxnStatus?.Trim()?.ToLower();

                    if (txnStatus == "success")
                    {
                        finalStatus = "SUCCESS";
                        break;
                    }
                    if (txnStatus == "failed" || txnStatus == "rejected")
                    {
                        finalStatus = txnStatus.ToUpper();
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(3));  // retry delay
                }


                // -----------------------------
                // 4. UPDATE EXISTING LOG RECORDS
                // -----------------------------
                var getPayOut = await _context.payManPayOuts
                    .FirstOrDefaultAsync(x => x.RefId == request.EnquiryReferenceId);

                var getPayOuthistory = await _context.payManHistories
                    .FirstOrDefaultAsync(x => x.TxnId == request.EnquiryReferenceId);

                if (getPayOut != null)
                {
                    getPayOut.Result = finalStatus;
                    getPayOut.Status = finalStatus == "SUCCESS";

                    var txn = statusResponse?.TxnList;

                    if (txn != null)
                    {
                        getPayOut.PayOutId = txn.TxnReferenceId ?? txn.ApprovalRefNumber ?? getPayOut.PayOutId;
                        getPayOut.AccountNo = txn.Mobile ?? getPayOut.AccountNo;
                        getPayOut.AccountHolderName = txn.RespCustomerName ?? getPayOut.AccountHolderName;

                        var inputParams = txn.InputParams ?? new List<InputParams11>();

                        getPayOut.TxnType = inputParams.FirstOrDefault(x =>
                            x.ParamName?.ToLower().Contains("last 4") == true ||
                            x.ParamName?.ToLower().Contains("primary") == true)?.ParamValue
                            ?? getPayOut.TxnType;

                        getPayOut.IfscCode = inputParams.FirstOrDefault(x =>
                            x.ParamName?.ToLower().Contains("registered") == true)?.ParamValue
                            ?? getPayOut.IfscCode;
                    }

                    _context.payManPayOuts.Update(getPayOut);
                }

                if (getPayOuthistory != null)
                {
                    getPayOuthistory.Status = finalStatus == "SUCCESS";
                    _context.payManHistories.Update(getPayOuthistory);
                }

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();


                // -----------------------------
                // 5. FINAL RETURN RESPONSE
                // -----------------------------
                return new PaymentResponseProcess
                {
                    Success = finalStatus == "SUCCESS",
                    Amount = request.Amount.ToString(),
                    OrderId = statusResponse?.TxnList?.TxnReferenceId
                        ?? billResponse?.TxnRefId
                        ?? request.EnquiryReferenceId,

                    ReferenceId = statusResponse?.TxnList?.ApprovalRefNumber
                        ?? billResponse?.ApprovalRefNumber
                        ?? "NA",

                    Category = "Credit Card",
                    BillerName = billerDetails?.blr_name ?? "Unknown",
                    Status = finalStatus
                };
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();

                var errMessage = $"DB save error: {ex.Message} - STACK: {ex.StackTrace}";
                Console.WriteLine(errMessage);

                try
                {
                    await LogFailedTransactionAsync(userDetails, request, istDateTime, errMessage, "DB");
                }
                catch { }

                return new PaymentResponseProcess
                {
                    Success = false,
                    Status = "FAILED",
                    Amount = request.Amount.ToString(),
                    OrderId = request.EnquiryReferenceId,
                    ReferenceId = "NA",
                    Category = "Credit Card",
                    BillerName = billerDetails?.blr_name ?? "Unknown"
                };
            }
        }



        //CSB Payout methods
        public async Task<string> CSBGetAccessTokenAsync()
        {

            using var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post,
                "https://uatfusion.csbuat.bank.in/uat-ext/external/api/v1/oauth2/token");

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        { "client_id", "fb1c6ea797620a9f9db02e6d4df98809" },
        { "client_secret", "78fa58bd76a8546d0b6479251822b1c6" },
        { "scope", "/v1" },
        { "grant_type", "client_credentials" }
    });

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("access_token").GetString();
        }

        private string CleanBankString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            // Remove special characters (keep only A-Z, a-z, 0-9 and space)
            string cleaned = System.Text.RegularExpressions.Regex
                .Replace(input, @"[^a-zA-Z0-9 ]", "");

            // Remove multiple spaces
            cleaned = System.Text.RegularExpressions.Regex
                .Replace(cleaned, @"\s+", " ");

            return cleaned.Trim();
        }

        public async Task<string> ProcessImpsAsync(string token, string CSB_KEY,
    PayManBeneficiaryAccounts payManBeneficiaryAccounts,
    decimal amount, string AccountNo)
        {
            try
            {
                var request = new ImpsPaymentRequest
                {
                    header = new header
                    {
                        entityId = "ENTITY_ID1",
                        functionId = "PJDOTONL",
                        action = "NEW",
                        channel = "MANL",
                        source = "EPAY",
                        moduleId = "PJ",
                        userId = "CSBAPI",
                        hostCode = "INDIA",
                        transactionId = Guid.NewGuid().ToString("N")
                    },
                    txnDet = new txnDet
                    {
                        benAcNo = payManBeneficiaryAccounts.AccountNo,
                        benAcType = "10",
                        benIfsc = payManBeneficiaryAccounts.IfscCode,
                        benName = CleanBankString(payManBeneficiaryAccounts.ContactName),
                        drAcNo = AccountNo,
                        drAcType = "11",
                        hostCode = "INDIA",
                        instructionDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        networkCode = "IMPS",
                        pmtType = "J",
                        remarks = "bill payments",
                        sourceCode = "EPAY",
                        txnAmount = amount,
                        txnCcy = "INR",
                        txnType = "P2A",
                        sourceRefNo = "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmssfff")
                    }
                };

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                string encrypted = CSBEncrypt(json, CSB_KEY);

                // ✅ Log Request
                _context.errorModels.Add(new ErrorModel
                {
                    payload = "Csb bank json imps",
                    agId = json,
                    reqTime = encrypted,
                    respTime ="",
                    uid = "",
                    requestId ="",
                    statuscode = true,
                    jsonBody =""
                });

                await _context.SaveChangesAsync();

                var payload = new { encData = encrypted };

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                    client.DefaultRequestHeaders.Add("Channel", "EPAY");
                    client.DefaultRequestHeaders.Add("X-Request-Id", Guid.NewGuid().ToString());

                    var content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json");

                    var response = await client.PostAsync(
                        "https://fusion.csb.bank.in/prod-ext/external/api/obpm/v1/indiapay/imps-corp",
                        content);

                    string responseText = await response.Content.ReadAsStringAsync();

                    // ✅ Log Raw Response
                    _context.errorModels.Add(new ErrorModel
                    {
                        payload = "Csb bank response imps",
                        agId = json,
                        reqTime = responseText,
                        respTime = "",
                        uid = "",
                        requestId = "",
                        jsonBody = "",
                        statuscode = response.IsSuccessStatusCode
                    });

                    await _context.SaveChangesAsync();

                    // ❌ HTTP Error Handling
                    if (!response.IsSuccessStatusCode)
                    {
                        _context.errorModels.Add(new ErrorModel
                        {
                            payload = "Csb bank HTTP failure",
                            agId = json,
                            reqTime = responseText,
                            respTime ="",
                            uid = "",
                            requestId = "",
                            jsonBody = "",
                            statuscode = false
                        });

                        await _context.SaveChangesAsync();

                        throw new Exception("Bank HTTP Error: " + responseText);
                    }

                    // 🔓 Encrypted Response Handling
                    if (responseText.Contains("encData"))
                    {
                        try
                        {
                            var encObj = JsonSerializer.Deserialize<JsonElement>(responseText);
                            string decrypted = CSBDecrypt(encObj.GetProperty("encData").GetString(), CSB_KEY);

                            _context.errorModels.Add(new ErrorModel
                            {
                                payload = "Csb bank decrypted response",
                                agId = json,
                                reqTime = decrypted,
                                respTime = "",
                                uid = "",
                                requestId = "",
                                jsonBody = "",
                                statuscode = true
                            });

                            await _context.SaveChangesAsync();

                            return decrypted;
                        }
                        catch (Exception decryptEx)
                        {
                            _context.errorModels.Add(new ErrorModel
                            {
                                payload = "Csb bank decryption failure",
                                agId = decryptEx.Message,
                                reqTime = decryptEx.StackTrace,
                                respTime = "",
                                uid = "",
                                requestId = "",
                                jsonBody = "",
                                statuscode = false
                            });

                            await _context.SaveChangesAsync();

                            throw;
                        }
                    }

                    return responseText;
                }
            }
            catch (Exception ex)
            {
                // ✅ Final Catch – Log Any Failure
                _context.errorModels.Add(new ErrorModel
                {
                    payload = "Csb bank exception final",
                    agId = ex.Message,
                    reqTime = ex.StackTrace,
                    respTime = "",
                    uid = "",
                    requestId = "",
                    jsonBody = "",
                    statuscode = false
                });

                await _context.SaveChangesAsync();

                throw;
            }
        }

        public async Task<string> ProcessRTGSAsync(
      string token,
      string CSB_KEY,
      PayManBeneficiaryAccounts payManBeneficiaryAccounts,
      decimal amount,
      string AccountNo)
        {
            try
            {
                var request = new RtgsRequest
                {
                    header = new Header1
                    {
                        entityId = "ENTITY_ID1",
                        functionId = "PJDOTONL",
                        action = "NEW",
                        source = "EPAY",
                        moduleId = "PJ",
                        userId = "CSBAPI",
                        hostCode = "INDIA",
                    },
                    txnDet = new RtgsTxnDet
                    {
                        hostCode = "INDIA",
                        sourceCode = "EPAY",
                        networkCode = "RTGS",
                        pmtType = "L",
                        activationDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        remarks = "Bill payments",
                        prefundedPayments = "N",
                        transferType = "C",

                        custcrdtrfinitPmtinfDto = new CustCrdTrfInitPmtInf
                        {
                            dbtraccothrid = AccountNo,
                            dbtraccnm = "Payman",
                            reqdexctndt = DateTime.UtcNow.ToString("yyyy-MM-dd")
                        },

                        custcrdtrfinitCdttxinfDto = new CustCrdTrfInitCdtTxInf
                        {
                            cdtragtclrsysmmbid = payManBeneficiaryAccounts.IfscCode,
                            cdtrnm = CleanBankString(payManBeneficiaryAccounts.ContactName),
                            cdtraccothrid = payManBeneficiaryAccounts.AccountNo,
                            instrid = "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                            instdamt = amount,
                            instdamtccy = "INR"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                string encrypted = CSBEncrypt(json, CSB_KEY);

                // Log Request
                _context.errorModels.Add(new ErrorModel
                {
                    payload = "Csb bank json rtgs",
                    agId = json,
                    reqTime = encrypted,
                    respTime = "",
                    uid = "",
                    requestId = "",
                    statuscode = true,
                    jsonBody = ""
                });

                await _context.SaveChangesAsync();

                var payload = new { encData = encrypted };

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                    client.DefaultRequestHeaders.Add("Channel", "EPAY");
                    client.DefaultRequestHeaders.Add("X-Request-Id", Guid.NewGuid().ToString());

                    var content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json");

                    var response = await client.PostAsync(
                        "https://fusion.csb.bank.in/prod-ext/external/api/obpm/v1/indiapay/rtgs-corp",
                        content);

                    string responseText = await response.Content.ReadAsStringAsync();

                    // Log Raw Response
                    _context.errorModels.Add(new ErrorModel
                    {
                        payload = "Csb bank response rtgs",
                        agId = json,
                        reqTime = responseText,
                        respTime = "",
                        uid = "",
                        requestId = "",
                        jsonBody = "",
                        statuscode = response.IsSuccessStatusCode
                    });

                    await _context.SaveChangesAsync();

                    // HTTP Failure
                    if (!response.IsSuccessStatusCode)
                    {
                        _context.errorModels.Add(new ErrorModel
                        {
                            payload = "Csb bank HTTP failure rtgs",
                            agId = json,
                            reqTime = responseText,
                            respTime = "",
                            uid = "",
                            requestId = "",
                            jsonBody = "",
                            statuscode = false
                        });

                        await _context.SaveChangesAsync();

                        throw new Exception("Bank HTTP Error: " + responseText);
                    }

                    // Decrypt response
                    if (responseText.Contains("encData"))
                    {
                        try
                        {
                            var encObj = JsonSerializer.Deserialize<JsonElement>(responseText);
                            string decrypted = CSBDecrypt(
                                encObj.GetProperty("encData").GetString(),
                                CSB_KEY);

                            _context.errorModels.Add(new ErrorModel
                            {
                                payload = "Csb bank decrypted response rtgs",
                                agId = json,
                                reqTime = decrypted,
                                respTime = "",
                                uid = "",
                                requestId = "",
                                jsonBody = "",
                                statuscode = true
                            });

                            await _context.SaveChangesAsync();

                            return decrypted;
                        }
                        catch (Exception decryptEx)
                        {
                            _context.errorModels.Add(new ErrorModel
                            {
                                payload = "Csb bank decryption failure rtgs",
                                agId = decryptEx.Message,
                                reqTime = decryptEx.StackTrace,
                                respTime = "",
                                uid = "",
                                requestId = "",
                                jsonBody = "",
                                statuscode = false
                            });

                            await _context.SaveChangesAsync();

                            throw;
                        }
                    }

                    return responseText;
                }
            }
            catch (Exception ex)
            {
                _context.errorModels.Add(new ErrorModel
                {
                    payload = "Csb bank exception final rtgs",
                    agId = ex.Message,
                    reqTime = ex.StackTrace,
                    respTime = "",
                    uid = "",
                    requestId = "",
                    jsonBody = "",
                    statuscode = false
                });

                await _context.SaveChangesAsync();

                throw;
            }
        }
        public async Task<string> ProcessNEFTAsync(
    string token,
    string CSB_KEY,
    PayManBeneficiaryAccounts payManBeneficiaryAccounts,
    decimal amount,
    string AccountNo)
        {
            try
            {
                var request = new RtgsRequest
                {
                    header = new Header1
                    {
                        entityId = "ENTITY_ID1",
                        functionId = "PJDOTONL",
                        action = "NEW",
                        source = "EPAY",
                        moduleId = "PJ",
                        userId = "CSBAPI",
                        hostCode = "INDIA",
                    },
                    txnDet = new RtgsTxnDet
                    {
                        hostCode = "INDIA",
                        sourceCode = "EPAY",
                        networkCode = "NEFT",
                        pmtType = "T",
                        activationDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        remarks = "Bill Payments",
                        prefundedPayments = "N",
                        transferType = "C",

                        custcrdtrfinitPmtinfDto = new CustCrdTrfInitPmtInf
                        {
                            dbtraccothrid = AccountNo,
                            dbtraccnm = "Payman",
                            reqdexctndt = DateTime.UtcNow.ToString("yyyy-MM-dd")
                        },

                        custcrdtrfinitCdttxinfDto = new CustCrdTrfInitCdtTxInf
                        {
                            cdtragtclrsysmmbid = payManBeneficiaryAccounts.IfscCode,
                            cdtrnm = CleanBankString(payManBeneficiaryAccounts.ContactName),
                            cdtraccothrid = payManBeneficiaryAccounts.AccountNo,
                            instrid = "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                            instdamt = amount,
                            instdamtccy = "INR"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                string encrypted = CSBEncrypt(json, CSB_KEY);

                // Log Request
                _context.errorModels.Add(new ErrorModel
                {
                    payload = "Csb bank json neft",
                    agId = json,
                    reqTime = encrypted,
                    respTime = "",
                    uid = "",
                    requestId = "",
                    jsonBody = "",
                    statuscode = true
                });

                await _context.SaveChangesAsync();

                var payload = new { encData = encrypted };

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                    client.DefaultRequestHeaders.Add("Channel", "EPAY");
                    client.DefaultRequestHeaders.Add("X-Request-Id", Guid.NewGuid().ToString());

                    var content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json");

                    var response = await client.PostAsync(
                        "https://fusion.csb.bank.in/prod-ext/external/api/obpm/v1/indiapay/neft-corp",
                        content);

                    string responseText = await response.Content.ReadAsStringAsync();

                    // Log raw response
                    _context.errorModels.Add(new ErrorModel
                    {
                        payload = "Csb bank response neft",
                        agId = json,
                        reqTime = responseText,
                        respTime = "",
                        uid = "",
                        requestId = "",
                        jsonBody = "",
                        statuscode = response.IsSuccessStatusCode
                    });

                    await _context.SaveChangesAsync();

                    // HTTP failure
                    if (!response.IsSuccessStatusCode)
                    {
                        _context.errorModels.Add(new ErrorModel
                        {
                            payload = "Csb bank HTTP failure neft",
                            agId = json,
                            reqTime = responseText,
                            respTime = "",
                            uid = "",
                            requestId = "",
                            jsonBody = "",
                            statuscode = false
                        });

                        await _context.SaveChangesAsync();

                        throw new Exception("Bank HTTP Error: " + responseText);
                    }

                    // Decrypt response
                    if (responseText.Contains("encData"))
                    {
                        try
                        {
                            var encObj = JsonSerializer.Deserialize<JsonElement>(responseText);

                            string decrypted = CSBDecrypt(
                                encObj.GetProperty("encData").GetString(),
                                CSB_KEY);

                            _context.errorModels.Add(new ErrorModel
                            {
                                payload = "Csb bank decrypted response neft",
                                agId = json,
                                reqTime = decrypted,
                                respTime = "",
                                uid = "",
                                requestId = "",
                                jsonBody = "",
                                statuscode = true
                            });

                            await _context.SaveChangesAsync();

                            return decrypted;
                        }
                        catch (Exception decryptEx)
                        {
                            _context.errorModels.Add(new ErrorModel
                            {
                                payload = "Csb bank decryption failure neft",
                                agId = decryptEx.Message,
                                reqTime = decryptEx.StackTrace,
                                respTime = "",
                                uid = "",
                                requestId = "",
                                jsonBody = "",
                                statuscode = false
                            });

                            await _context.SaveChangesAsync();

                            throw;
                        }
                    }

                    return responseText;
                }
            }
            catch (Exception ex)
            {
                _context.errorModels.Add(new ErrorModel
                {
                    payload = "Csb bank exception final neft",
                    agId = ex.Message,
                    reqTime = ex.StackTrace,
                    respTime = "",
                    uid = "",
                    requestId = "",
                    jsonBody = "",
                    statuscode = false
                });

                await _context.SaveChangesAsync();

                throw;
            }
        }
        public async Task<string> TransactionInquiryAsync(string token,string sourceRefNo,string networkId,string CSB_KEY)
        {
            // networkId like IMPS,RTGS,NEFT
            var request = new
            {
                header = new
                {
                    entityId = "ENTITY_ID1"
                },
                txnDet = new
                {
                    sourceRefNo = sourceRefNo,
                    pmType = "OP",              // OP = Outward Payment
                    userId = "SYSTEM",
                    networkId = networkId
                }
            };

            // Serialize (NO indentation)
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Encrypt request
            string encrypted = CSBEncrypt(json, CSB_KEY);

            var payload = new
            {
                encData = encrypted
            };

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
          HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                client.DefaultRequestHeaders.Add("Channel", "EPAY");

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(
                    "https://uatfusion.csbuat.bank.in/uat-ext/external/api/obpm/v1/transaction-inq",
                    content);

                var responseText = await response.Content.ReadAsStringAsync();

                // 1️⃣ Deserialize
                var encResponse = JsonSerializer.Deserialize<CSBEncryptedResponse>(responseText);

                // 2️⃣ Decrypt ONLY encData
                //string decryptedJson = CSBTranDecrypt(encResponse.encData, CSB_KEY);

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Transaction Inquiry Failed: {responseText}");

                // Response is encrypted
                return CSBTranDecrypt(encResponse.encData, CSB_KEY);
            }
        }

        public async Task<string> GetAccountMiniStatementAsync(string token,string accountId,string fromDate,string toDate,string CSB_KEY, string todaye)
        {
            var request = new StatementRequest
            {
                args0 = new Args0
                {
                    bankCode = 47,
                    transactionBranch = 20,
                    localDateTimeText = todaye,
                    externalReferenceNo = "PAYMAN" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    postingDateText = todaye,
                    valueDateText = todaye,
                    channel = "EPAY",
                    userId = "CSBAPI",
                    serviceCode = "173",
                    supervisorContext = new SupervisorContext
                    {
                        userId = "CSBAPI",
                        primaryPassword = ""
                    },
                    tellerContext = new TellerContext
                    {
                        userId = "CSBAPI",
                        primaryPassword = ""
                    },
                    reason = new Reason
                    {
                        reasonCode = 0,
                        comment = ""
                    }
                },
                args1 = new Args1
                {
                    accountID = accountId,
                    fromDate = fromDate,
                    toDate = toDate,
                    pageSize =0,
                    pageNumber=0,
                    noOftransactions =0,
                    txnReferenceNumber ="",
                    transactionDate ="",
                }
            };
            // Serialize
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Encrypt
            string encryptedPayload = CSBEncrypt(json, CSB_KEY);

            var user = new ErrorModel
            {
                payload = "Csb bank json",
                agId = json,
                reqTime = encryptedPayload,
                respTime = "",
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(user);
            await _context.SaveChangesAsync();

            var payload = new
            {
                encData = encryptedPayload
            };

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
           HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                client.DefaultRequestHeaders.Add("Channel", "EPAY");

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                //var response = await client.PostAsync(
                //    "https://uatfusion.csbuat.bank.in/uat-ext/external/api/fc/v1/acnt/stmt-corp",
                //    content);
                var response = await client.PostAsync(
                    "https://fusion.csb.bank.in/prod-ext/external/api/fc/v1/acnt/stmt-corp",
                    content);

                var responseText = await response.Content.ReadAsStringAsync();

                var user1 = new ErrorModel
                {
                    payload = "Csb bank response",
                    agId = json,
                    reqTime = responseText,
                    respTime = "",
                    requestId = "",
                    uid = "",
                    statuscode = true,
                    jsonBody = ""
                };

                _context.errorModels.Add(user1);
                await _context.SaveChangesAsync();

                // 1️⃣ Deserialize
                var encResponse = JsonSerializer.Deserialize<CSBEncryptedResponse>(responseText);

                // 2️⃣ Decrypt ONLY encData
                //string decryptedJson = CSBTranDecrypt(encResponse.encData, CSB_KEY);

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Transaction Inquiry Failed: {responseText}");

                // Response is encrypted
                return CSBTranDecrypt(encResponse.encData, CSB_KEY);
            }
        }


        public async Task<CSBOAuthTokenResponse> GetAccessTokenAsync(string clientid,string CSB_KEY,string Scope,string Grant_type)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var client = new HttpClient(handler);

            //var url = "https://uatfusion.csbuat.bank.in/uat-ext/external/api/v1/oauth2/token";
            var url = "https://fusion.csb.bank.in/prod-ext/external/api/v1/oauth2/token";

            var formData = new Dictionary<string, string>
    {
        { "client_id", clientid },
        { "client_secret", CSB_KEY },
        { "scope", Scope },
        { "grant_type", Grant_type }
    };
            try
            {
                using var content = new FormUrlEncodedContent(formData);

                using var response = await client.PostAsync(url, content);

                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Token API failed: {response.StatusCode} - {responseBody}");

                return JsonSerializer.Deserialize<CSBOAuthTokenResponse>(
                    responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (Exception ex)
            {

            }
            return JsonSerializer.Deserialize<CSBOAuthTokenResponse>(
                    "responseBody",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

        }

        public static string CSBEncrypt(string plainText,string CSB_KEY)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(CSB_KEY);

            // Generate 16-byte IV
            byte[] ivBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(ivBytes);
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7; // Java PKCS5 compatible

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] cipherBytes = encryptor.TransformFinalBlock(
                        plainBytes, 0, plainBytes.Length);

                    // IV + CipherText
                    byte[] finalBytes = new byte[ivBytes.Length + cipherBytes.Length];
                    Buffer.BlockCopy(ivBytes, 0, finalBytes, 0, ivBytes.Length);
                    Buffer.BlockCopy(cipherBytes, 0, finalBytes, ivBytes.Length, cipherBytes.Length);

                    return Convert.ToBase64String(finalBytes);
                }
            }
        }
        public static string CSBDecrypt(string res, string CSB_KEY)
        {
            byte[] allBytes = Convert.FromBase64String(res);

            byte[] ivBytes = allBytes.Take(16).ToArray();
            byte[] cipherBytes = allBytes.Skip(16).ToArray();
            byte[] keyBytes = Encoding.UTF8.GetBytes(CSB_KEY);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] plainBytes = decryptor.TransformFinalBlock(
                        cipherBytes, 0, cipherBytes.Length);

                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }

        public static string CSBTranDecrypt(string base64Encrypted, string CSB_KEY)
        {
            byte[] allBytes = Convert.FromBase64String(base64Encrypted);

            byte[] ivBytes = allBytes.Take(16).ToArray();
            byte[] cipherBytes = allBytes.Skip(16).ToArray();
            byte[] keyBytes = Encoding.UTF8.GetBytes(CSB_KEY);

            if (keyBytes.Length != 16 && keyBytes.Length != 24 && keyBytes.Length != 32)
                throw new Exception("Invalid AES key length");

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return Encoding.UTF8.GetString(plainBytes);
            }
        }

        public async Task<PaymentStatusViewModel> PayInDbCall(PaymentDecryptResponse paymentDecryptResponse)
        {
            // 🔐 Safety checks
            if (paymentDecryptResponse == null)
                throw new ArgumentNullException(nameof(paymentDecryptResponse));



            var easebuzzGateway = await _context.PayManGateways
                .FirstOrDefaultAsync(t => t.GatewayName == "Easebuzz");

            // ✅ Idempotency check
            var existingPayIn = await _context.payManPayIns
                .FirstOrDefaultAsync(t =>
                    t.TxnId == paymentDecryptResponse.OrderDetails.OrderId);

            var user = await _context.payManUsers
                .FirstOrDefaultAsync(u => u.Phone == existingPayIn.UserPhone);

            if (user == null)
                throw new Exception("User not found");

            var istTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            if (existingPayIn != null)
            {
                var gatewayId = await _context.gateways
                    .Where(g => g.StoreName == paymentDecryptResponse.Gatewayname)
                    .Select(g => g.Id)
                    .FirstOrDefaultAsync();

                var getMargin = await _context.userLookUps
                    .Where(m => m.GatewayId == gatewayId && m.UserPhone == existingPayIn.UserPhone)
                    .Select(m => m.GatewayMargin)
                    .FirstOrDefaultAsync();

                decimal amount =
                    Convert.ToDecimal(paymentDecryptResponse.AmountDetails?.Amount ?? "0");

                decimal margin = getMargin > 0 ? getMargin : 0;

                // Card based margin
                if (paymentDecryptResponse.CardDetails?.CardBrand?
                    .Equals("mastercard", StringComparison.OrdinalIgnoreCase) == true)
                {
                    margin = user.MasterMarigin ?? margin;
                }

                //if(paymentDecryptResponse.PaymentMethod
                //    .Equals("CCI", StringComparison.OrdinalIgnoreCase) == true)
                //{
                //    margin = user.CarporateCardMarigin ?? margin;
                //}

                //var mm = 1.50m;

                existingPayIn.EasePayId = paymentDecryptResponse.TransactionId;
                existingPayIn.Email = paymentDecryptResponse.CustomerEmail;
                existingPayIn.CardNumber = paymentDecryptResponse.CardDetails?.MaskedCard;
                existingPayIn.EaseCardNum = user.Email;
                existingPayIn.IsCorporate = paymentDecryptResponse.PaymentMethod;
                existingPayIn.Amount = amount;
                existingPayIn.Gateway = existingPayIn.Gateway == "fastag" ? "fastag" :  paymentDecryptResponse.Gatewayname;
                existingPayIn.CardBrand = paymentDecryptResponse.CardDetails?.CardBrand;
                existingPayIn.PayInCommission = amount * margin / 100;
                existingPayIn.PaymanCommission = amount *
                        Convert.ToDecimal(easebuzzGateway?.PaymanComm ?? 0) / 100;
                existingPayIn.Created = istTime;
                existingPayIn.Status = paymentDecryptResponse.Result == "SUCCESS";
                existingPayIn.Result = paymentDecryptResponse.Result;

                _context.payManPayIns.Update(existingPayIn);
                await _context.SaveChangesAsync();

                // ✅ Wallet & history ONLY if success
                if (paymentDecryptResponse.Result == "SUCCESS")
                {
                    var avlAmount = await GetUserWalletAmount(existingPayIn.UserPhone);

                    var payInHistory = new PayManHistory
                    {
                        UserId = user.Id,
                        UserPhone = existingPayIn.UserPhone,
                        TxnId = paymentDecryptResponse.TransactionId,
                        Amount = amount,
                        Mode = "PayIn",
                        Status = paymentDecryptResponse.Result == "SUCCESS",
                        Created = istTime,
                        AvlBalance = Convert.ToDecimal(avlAmount),
                        CardNumber = paymentDecryptResponse.CardDetails?.MaskedCard,
                        PayInId = existingPayIn.Id
                    };

                    _context.payManHistories.Add(payInHistory);
                    await _context.SaveChangesAsync();
                }
            }

            // ✅ RETURN VIEW MODEL (FIXED)
            return new PaymentStatusViewModel
            {
                IsSuccess = paymentDecryptResponse.Result == "SUCCESS",
                Amount = Convert.ToDouble(
                    paymentDecryptResponse.AmountDetails?.Amount ?? "0"),
                TransactionId = paymentDecryptResponse.TransactionId,
                CardNumber = paymentDecryptResponse.CardDetails?.MaskedCard,
                Gateway = existingPayIn.Gateway
            };
        }

        public async Task<PaymentStatusViewModel> JioPayInDbCall(JiopayResponse paymentDecryptResponse)
        {
            // 🔐 Safety checks
            if (paymentDecryptResponse == null)
                throw new ArgumentNullException(nameof(paymentDecryptResponse));

            var user11 = new ErrorModel
            {
                payload = "Jio Pay response2",
                agId = paymentDecryptResponse.merchantTxnNo,
                reqTime =  "",
                respTime = "",
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(user11);
            await _context.SaveChangesAsync();



            var easebuzzGateway = await _context.PayManGateways
                .FirstOrDefaultAsync(t => t.GatewayName == "Easebuzz");

            // ✅ Idempotency check
            var existingPayIn = await _context.payManPayIns
                .FirstOrDefaultAsync(t =>
                    t.TxnId == paymentDecryptResponse.merchantTxnNo);

           

            var istTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            string jsonData = System.Text.Json.JsonSerializer.Serialize(paymentDecryptResponse);

            var user1a1 = new ErrorModel
            {
                payload = "Jio Pay response3",
                agId =  "",
                reqTime =  "",
                respTime = jsonData,
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(user1a1);
            await _context.SaveChangesAsync();

            if (existingPayIn != null)
            {
                var user = await _context.payManUsers
               .FirstOrDefaultAsync(u => u.Phone == existingPayIn.UserPhone);

                if (user == null)
                    throw new Exception("User not found");

                var gatewayId = await _context.gateways
                    .Where(g => g.StoreName == "JioPay")
                    .Select(g => g.Id)
                    .FirstOrDefaultAsync();
                var getMargin = await _context.userLookUps
                    .Where(m => m.GatewayId == gatewayId && m.UserPhone == existingPayIn.UserPhone)
                    .Select(m => m.GatewayMargin)
                    .FirstOrDefaultAsync();

                decimal amount =
                    Convert.ToDecimal(paymentDecryptResponse.amount ?? "0");

                decimal margin = getMargin > 0 ? getMargin : 0;

                // Card based margin
                //if (paymentDecryptResponse.cardNetwork?
                //    .Equals("mastercard", StringComparison.OrdinalIgnoreCase) == true)
                //{
                //    margin = user.MasterMarigin ?? margin;
                //}

                //var mm = 1.50m;

                existingPayIn.EasePayId = paymentDecryptResponse.txnID;
                existingPayIn.Email = paymentDecryptResponse.customerEmailID;
                existingPayIn.CardNumber = paymentDecryptResponse.paymentInstId;
                existingPayIn.EaseCardNum = user.Email;
                existingPayIn.Amount = amount;
                existingPayIn.Gateway = "JioPay";
                existingPayIn.CardBrand = paymentDecryptResponse.cardNetwork;
                existingPayIn.PayInCommission = amount * margin / 100;
                existingPayIn.PaymanCommission = amount *
                        Convert.ToDecimal(easebuzzGateway?.PaymanComm ?? 0) / 100;
                existingPayIn.Created = istTime;
                existingPayIn.Status = paymentDecryptResponse.respDescription == "Transaction successful";
                existingPayIn.Result = paymentDecryptResponse.respDescription;

                _context.payManPayIns.Update(existingPayIn);
                await _context.SaveChangesAsync();

                // ✅ Wallet & history ONLY if success
                if (paymentDecryptResponse.respDescription == "Transaction successful")
                {
                    var avlAmount = await GetUserWalletAmount(existingPayIn.UserPhone);

                    var payInHistory = new PayManHistory
                    {
                        UserId = user.Id,
                        UserPhone = existingPayIn.UserPhone,
                        TxnId = paymentDecryptResponse.merchantTxnNo,
                        Amount = amount,
                        Mode = "PayIn",
                        Status = paymentDecryptResponse.respDescription == "Transaction successful",
                        Created = istTime,
                        AvlBalance = Convert.ToDecimal(avlAmount),
                        CardNumber = paymentDecryptResponse.paymentInstId,
                        PayInId = existingPayIn.Id
                    };

                    _context.payManHistories.Add(payInHistory);
                    await _context.SaveChangesAsync();
                }
            }

            var existingbbpsPayIn = await _context.bbpsPayIns
                .FirstOrDefaultAsync(t =>
                    t.TxnId == paymentDecryptResponse.merchantTxnNo);

            string jsonData1 = System.Text.Json.JsonSerializer.Serialize(existingbbpsPayIn);

            var user1a11 = new ErrorModel
            {
                payload = "Jio Pay response4",
                agId = "",
                reqTime = "",
                respTime = jsonData1,
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(user1a11);
            await _context.SaveChangesAsync();

            //var bbpsuser = await _context.pMUsers
            //    .FirstOrDefaultAsync(u => u.Phone == existingbbpsPayIn.UserPhone);

            if (existingbbpsPayIn != null && existingbbpsPayIn.Device == "mobile")
            {
                decimal amount =
                    Convert.ToDecimal(paymentDecryptResponse.amount ?? "0"); 

                existingbbpsPayIn.PaymentTxnId = paymentDecryptResponse.txnID ?? "";
                existingbbpsPayIn.CustomerEmail = paymentDecryptResponse.customerEmailID ?? "";
                existingbbpsPayIn.CardNumber = paymentDecryptResponse.paymentInstId?? "";
               // existingbbpsPayIn.EaseCardNum = user.Email;
                existingbbpsPayIn.Amount = amount;
                existingbbpsPayIn.Created = istTime;
                existingbbpsPayIn.Status = paymentDecryptResponse.respDescription == "Transaction successful" || paymentDecryptResponse.respDescription == "Request processed successfully";
                existingbbpsPayIn.Result = paymentDecryptResponse.respDescription;

                _context.bbpsPayIns.Update(existingbbpsPayIn);
                await _context.SaveChangesAsync();
            }
            

            // ✅ RETURN VIEW MODEL (FIXED)
            return new PaymentStatusViewModel
            {
                IsSuccess = paymentDecryptResponse.respDescription == "Transaction successful" || paymentDecryptResponse.respDescription == "Request processed successfully",
                Amount = Convert.ToDouble(
                    paymentDecryptResponse.amount ?? "0"),
                TransactionId = paymentDecryptResponse.merchantTxnNo,
                CardNumber = paymentDecryptResponse?.paymentInstId ?? "",
                Gateway = existingPayIn?.CreditCardHolderNum ?? ""
            };
        }


        public async Task<PaymentStatusViewModel> PaytmInDbCall(PaymentTransaction paymentDecryptResponse)
        {
            // 🔐 Safety checks
            if (paymentDecryptResponse == null)
                throw new ArgumentNullException(nameof(paymentDecryptResponse));



            var easebuzzGateway = await _context.PayManGateways
                .FirstOrDefaultAsync(t => t.GatewayName == "Easebuzz");

            // ✅ Idempotency check
            var existingPayIn = await _context.payManPayIns
                .FirstOrDefaultAsync(t =>
                    t.TxnId == paymentDecryptResponse.OrderId);

            var user = await _context.payManUsers
                .FirstOrDefaultAsync(u => u.Phone == existingPayIn.UserPhone);

            if (user == null)
                throw new Exception("User not found");

            var istTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            if (existingPayIn != null)
            {
                var gatewayId = await _context.gateways
                    .Where(g => g.StoreName == "PaytmEdu")
                    .Select(g => g.Id)
                    .FirstOrDefaultAsync();
                var getMargin = await _context.userLookUps
                    .Where(m => m.GatewayId == gatewayId && m.UserPhone == existingPayIn.UserPhone)
                    .Select(m => m.GatewayMargin)
                    .FirstOrDefaultAsync();

                decimal amount = paymentDecryptResponse.Amount;

                decimal margin = getMargin > 0 ? getMargin : 0;

                // Card based margin
                //if (paymentDecryptResponse.cardNetwork?
                //    .Equals("mastercard", StringComparison.OrdinalIgnoreCase) == true)
                //{
                //    margin = user.MasterMarigin ?? margin;
                //}

                //var mm = 1.50m;

                existingPayIn.EasePayId = paymentDecryptResponse.TxnId;
                existingPayIn.CardNumber = paymentDecryptResponse.BankTxnId;
                existingPayIn.EaseCardNum = user.Email;
                existingPayIn.Amount = amount;
                existingPayIn.CardBrand = paymentDecryptResponse.BankName;
                existingPayIn.PayInCommission = amount * margin / 100;
                existingPayIn.PaymanCommission = amount *
                        Convert.ToDecimal(easebuzzGateway?.PaymanComm ?? 0) / 100;
                existingPayIn.Created = istTime;
                existingPayIn.Status = paymentDecryptResponse.Status == "TXN_SUCCESS";
                existingPayIn.Result = paymentDecryptResponse.Status;

                _context.payManPayIns.Update(existingPayIn);
                await _context.SaveChangesAsync();

                // ✅ Wallet & history ONLY if success
                if (paymentDecryptResponse.Status == "TXN_SUCCESS")
                {
                    var avlAmount = await GetUserWalletAmount(existingPayIn.UserPhone);

                    var payInHistory = new PayManHistory
                    {
                        UserId = user.Id,
                        UserPhone = existingPayIn.UserPhone,
                        TxnId = paymentDecryptResponse.OrderId,
                        Amount = amount,
                        Mode = "PayIn",
                        Status = paymentDecryptResponse.Status == "TXN_SUCCESS",
                        Created = istTime,
                        AvlBalance = Convert.ToDecimal(avlAmount),
                        CardNumber = paymentDecryptResponse.BankTxnId,
                        PayInId = existingPayIn.Id
                    };

                    _context.payManHistories.Add(payInHistory);
                    await _context.SaveChangesAsync();
                }
            }

            // ✅ RETURN VIEW MODEL (FIXED)
            return new PaymentStatusViewModel
            {
                IsSuccess = paymentDecryptResponse.Status == "TXN_SUCCESS",
                Amount = Convert.ToDouble(
                    paymentDecryptResponse.Amount),
                TransactionId = paymentDecryptResponse.OrderId,
                CardNumber = paymentDecryptResponse.BankTxnId
            };
        }


        // Fix LogPayOutAsync - DO NOT call SaveChanges here. Just add to context.
        public Task<Guid> LogForPayOutAsync(
     PayManUsers user,
     PaymentRequestApp request,
     DateTime dateTime,
     string result,
     string txnId,
     bool status)
        {

            var amountDouble = request?.Amount ?? 0.0; // request.Amount is double (nullable via ?)
            decimal payoutCommission = amountDouble > 100000d ? 30m : 15m;

            var payout = new PayManPayOut
            {
                UserId = user?.Id ?? Guid.Empty,
                UserPhone = request?.Phone ?? "",

                PayOutId = txnId ?? "",                     // Bank TXNID
                RefId = request?.EnquiryReferenceId ?? "",  // External Ref

                AccountHolderName = request?.customerName ?? "Unknown",
                AccountNo = request?.holderMobile ?? "",
                IfscCode = request?.BillerResponse ?? "",

                Amount = Convert.ToDecimal( request?.Amount),
                PayoutCommission = payoutCommission, // Change if dynamic

                BeneId = request?.BillerId ?? "",
                PayOutType = "CSB PAYOUT",

                TxnType = request?.LastFourDigits ?? "",
                Email = user?.Email ?? "",

                Status = status,   // true = success
                DateTime = dateTime,

                Result = result ?? "PENDING",
                Device = "B - " + (request?.Device ?? "WEB")
            };

            _context.payManPayOuts.Add(payout);

            return Task.FromResult(payout.Id);
        }

        // Fix LogPayInHistoryAsync - DO NOT call SaveChanges here. keep same amount logic (amount param is paise)
        public async Task LogForPaoutPayInHistoryAsync(
     PayManUsers user,
     PaymentRequestApp request,
     DateTime dateTime,
     string txnId,
     bool status,
     decimal amountInPaise,
     Guid payOutDbId)
        {
            decimal finalAmount = amountInPaise;

            var userWalletAmount = await GetUserWalletAmount(request?.Phone ?? "");

            decimal updatedBalance = Convert.ToDecimal(userWalletAmount) - finalAmount;

            var history = new PayManHistory
            {

                UserId = user?.Id ?? Guid.Empty,
                UserPhone = request?.Phone ?? "",

                TxnId = txnId ?? "",

                Amount = finalAmount,
                CardNumber = request?.holderMobile ?? "",

                Mode = "CSB PAYOUT",
                Status = status,

                Created = dateTime,
                AvlBalance = updatedBalance,

                PayInId = payOutDbId
            };

            _context.payManHistories.Add(history);
        }



        public async Task<BillResponseApp> BillAvenueFetchBillMOBCard(BillRequestApp request)
        {

            // headers setup...
            var accessCode = _configuration["BillAvenueKeys:accessCode"];
            var workingKey = _configuration["BillAvenueKeys:workingKey"];
            var instituteId = _configuration["BillAvenueKeys:instituteId"];
            var endpointIp = _configuration["BillAvenueKeys:EndpointIp"];


            string requestId = GenerateRequestId();
            string ver = "1.0";

            var billerInfo = await _context.billAvenueCreditCardBillers
    .FirstOrDefaultAsync(t => t.blr_id == request.BillerId);
            if (billerInfo.blr_response == null)
            {
                return new BillResponseApp
                {
                    Success = false,
                    Message = "Bill fetch failed"
                };
            }

            XDocument doc = XDocument.Parse(billerInfo.blr_response);

            var inputs = doc.Descendants("billerInputParams")
                            .Descendants("paramInfo")
                            .Select(x => new
                            {
                                ParamName = (string)x.Element("paramName"),
                                MaxLength = (string)x.Element("maxLength")
                            })
                            .ToList();

            string mobile = request.RegisteredMobile.Length == 10 ? request.RegisteredMobile : request.CreditCardLast4;
            string lastfour = request.CreditCardLast4.Length == 4 ? request.CreditCardLast4 : request.RegisteredMobile;

            // Build <inputParams> dynamically
            XElement inputParams = new XElement("inputParams");

            foreach (var item in inputs)
            {
                string value = string.Empty;

                // Map ParamName → request value
                if (item.ParamName.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
                {
                    value = mobile.Trim();
                }
                else if (item.ParamName.Contains("credit", StringComparison.OrdinalIgnoreCase))
                {
                    value = lastfour.Trim();
                }
                // 👇 you can extend here for other param types as needed
                else
                {
                    value = ""; // default empty
                }

                inputParams.Add(
                    new XElement("input",
                        new XElement("paramName", item.ParamName),
                        new XElement("paramValue", value)
                    )
                );
            }

            // Convert to string
            string additionalInfo = inputParams.ToString();
            var userDetails = _context.payManUsers.Where(t => t.Phone == request.UserPhone).FirstOrDefault();


            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?> 
<billFetchRequest>
    <agentId>{userDetails.BillAvenueAgentId}</agentId>
                <agentDeviceInfo>
                  <ip>{endpointIp}</ip>
                  <initChannel>AGT</initChannel>
                  <mac>01-23-45-67-89-ab</mac>
                </agentDeviceInfo >
    <customerInfo>
        <customerMobile>{request.CustomerMobile}</customerMobile>
    </customerInfo>
    <billerId>{request.BillerId}</billerId>
{additionalInfo}
</billFetchRequest>";

            string encryptedPayload = Encrypt(merchantData, workingKey);

            string url = $"https://api.billavenue.com/billpay/extBillCntrl/billFetchRequest/xml" +
                         $"?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}&encRequest={encryptedPayload}";

            using var client = new HttpClient();
            try
            {
                var response = await client.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    return new BillResponseApp
                    {
                        Success = false,
                        Message = "Bill fetch failed"
                    };
                }

                var result = await response.Content.ReadAsStringAsync();

                string decryptedResponse = Decrypt(result, workingKey);

                // Deserialize XML to object
                var serializer = new XmlSerializer(typeof(BillAvenueBillFetchResponse));
                BillAvenueBillFetchResponse billResponse;
                using (var reader = new StringReader(decryptedResponse))
                {
                    billResponse = (BillAvenueBillFetchResponse)serializer.Deserialize(reader);
                }

                var wrapper = ExtractBillerResponseMOBCard(decryptedResponse);

                if (billResponse?.billerResponse == null)
                {
                    return new BillResponseApp
                    {
                        Success = false,
                        Message = billResponse.errorInfo.Error.ErrorMessage ?? "Invalid response format"
                    };
                }

                // Map to BillResponseApp  Current Outstanding
                var minPayable = billResponse.additionalInfo?
                    .FirstOrDefault(i => i.infoName.Contains("Minimum Amount", StringComparison.OrdinalIgnoreCase))?.infoValue;

                var currentOutStanding = billResponse.additionalInfo?
                    .FirstOrDefault(i => i.infoName.Contains("Current Outstanding", StringComparison.OrdinalIgnoreCase))?.infoValue;


                return new BillResponseApp
                {
                    Success = billResponse.responseCode == "000",
                    Message = billResponse.responseCode == "000" ? "Bill fetched successfully" : "Failed to fetch bill",
                    ConsumerName = billResponse.billerResponse.customerName,
                    BillNumber = billResponse.billerResponse.customerName, // you might want a better unique ref
                    BillDate = wrapper == null ? "" : wrapper.BillerResponse,
                    DueDate = billResponse.billerResponse.dueDate,
                    TotalAmount = Convert.ToDecimal(billResponse.billerResponse.billAmount),
                    MinPayable = string.IsNullOrEmpty(minPayable) ? Convert.ToDecimal(billResponse.billerResponse.billAmount) : Convert.ToDecimal(minPayable),
                    CuurentOutStanding = string.IsNullOrEmpty(currentOutStanding) ? Convert.ToDecimal(billResponse.billerResponse.billAmount) : Convert.ToDecimal(currentOutStanding),
                    Param1 = mobile.Trim(),
                    Param2 = lastfour.Trim(),
                    EnquiryReferenceId = requestId,


                    BillerResponse = wrapper.BillerResponse,
                    AdddditionalInfo = wrapper.AdddditionalInfo,
                    BillFetchResponse = wrapper.BillFetchResponse
                };
            }
            catch (Exception ex)
            {
                return new BillResponseApp
                {
                    Success = false,
                    Message = "Exception: " + ex.Message
                };
            }
        }

        public static BillFetchWrapper ExtractBillerResponseMOBCard(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var node = doc.SelectSingleNode("//billerResponse");
            var additionalInfo = doc.SelectSingleNode("//additionalInfo");
            var billFetchResponse = doc.SelectSingleNode("//inputParams");

            var wrapper = new BillFetchWrapper
            {
                BillerResponse = node?.OuterXml,
                AdddditionalInfo = additionalInfo?.OuterXml,
                BillFetchResponse = billFetchResponse?.OuterXml
            };

            return wrapper;
        }



    }
}
