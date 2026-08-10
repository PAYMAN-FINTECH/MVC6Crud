using MVC6Crud.Controllers;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace MVC6Crud.Models.DTOS
{
    public class kyc
    {
    }
    public class AdharNumberRequest1
    {
        public string Phone { get; set; }
        public string AdharNumber { get; set; }
    }

    public class VerifyAdharOtpRequest1
    {
        public string Phone { get; set; }
        public string Otp { get; set; }
    }

    public class PanRequest1
    {
        public string Phone { get; set; }
        public string PanNumber { get; set; }
    }
    public class AadhaarVerificationKyc
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string VerificationId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AadhaarDocumentKyc
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public byte[] EncryptedFile { get; set; }
        public string FileType { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class AesEncryption
    {
        public static byte[] Encrypt(byte[] data, string key, string iv)
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(key);
            aes.IV = Convert.FromBase64String(iv);

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }
    }

    public class DigiLockerCreateResponseKyc 
    {
        public string verification_id { get; set; }
        public long reference_id { get; set; }
        public string url { get; set; }
        public string status { get; set; }
        public string redirect_url { get; set; }
        public string user_flow { get; set; }
    }

    public class StartDigiLockerRequestKyc 
    {
        // Your internal user reference (phone / userId etc.)
        public string Phone { get; set; }

        // Unique ID per verification attempt
        // Example: DLKYC_9876543210_20260115
        public string VerificationId { get; set; }
    }

    public class DigiLockerStatusResponseKyc 
    {
        [JsonProperty("verification_id")]
        public string VerificationId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }   // INITIATED | VERIFIED | FAILED

        [JsonProperty("documents")]
        public List<DigiLockerDocumentKyc>? Documents { get; set; }
    }

    public class DigiLockerDocumentKyc 
    {
        [JsonProperty("doc_type")]
        public string DocType { get; set; }  // AADHAAR | PAN

        [JsonProperty("doc_url")]
        public string DocUrl { get; set; }
    }

    public class DigiLockerStatusRequestKyc1
    {
        public string Phone { get; set; } = string.Empty;
        public string VerificationId { get; set; } = string.Empty;
    }

    public class DigiLockerStatusResponseKyc1
    {
        public string Status { get; set; } = string.Empty; // PENDING / VERIFIED / FAILED
        public string Message { get; set; } = string.Empty;
        public string ReferenceId { get; set; } = string.Empty;
    }

    public class DigiLockerStatusResponse
    {
        public bool IsSuccess { get; set; }
        public string Status { get; set; }     // AUTHENTICATED / FAILED / PENDING
        public string Message { get; set; }
    }

    public class DigiLockerPanResponse
    {
        [JsonProperty("reference_id")]
        public string ReferenceId { get; set; }

        [JsonProperty("verification_id")]
        public string VerificationId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("pan")]
        public string Pan { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("dob")]
        public string Dob { get; set; }

        [JsonProperty("name_pan_card")]
        public string NameOnPan { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("xml_file")]
        public string XmlFile { get; set; }
    }

    public class DigiLockerFetchResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string DocumentType { get; set; }

        public DigiLockerDocumentResponse AadhaarData { get; set; }
        public DigiLockerPanResponse PanData { get; set; }
    }


}
