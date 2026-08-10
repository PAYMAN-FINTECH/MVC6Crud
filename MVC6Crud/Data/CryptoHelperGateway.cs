using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MVC6Crud.Data
{
    public static class CryptoHelperGateway
    {
        public static string GenerateSignature(
    string orderId,
    string terminalId,
    string password,
    string merchantKey,
    string amount,
    string currency)
        {
            
            // Ensure no nulls
            orderId ??= "";
            terminalId ??= "";
            password ??= "";
            merchantKey ??= "";
            currency ??= "";

            // Pipe string in exact gateway sequence
            string pipeString = string.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1}|{2}|{3}|{4}|{5}",
                orderId, terminalId, password, merchantKey, amount, currency
            );

            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(pipeString);
                byte[] hashBytes = sha.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        public static string ComputeHmacSha256Hex(string message, string secret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret ?? "");
            using var hmac = new HMACSHA256(keyBytes);
            var msgBytes = Encoding.UTF8.GetBytes(message ?? "");
            var hash = hmac.ComputeHash(msgBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        // AES decrypt: assumes input is base64 with IV prepended (first 16 bytes).
        public static string AesDecryptFromBase64(string base64Input, string key)
        {
            if (string.IsNullOrEmpty(base64Input)) return null;
            var combined = Convert.FromBase64String(base64Input);
            if (combined.Length < 17) return null;
            var iv = combined.Take(16).ToArray();
            var cipher = combined.Skip(16).ToArray();

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            var keyBytes = new byte[32];
            var src = Encoding.UTF8.GetBytes(key ?? "");
            Array.Copy(src, keyBytes, Math.Min(src.Length, keyBytes.Length));
            aes.Key = keyBytes;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plain);
        }
    }
}
