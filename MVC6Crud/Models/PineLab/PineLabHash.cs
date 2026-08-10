using System.Security.Cryptography;
using System.Text;

namespace MVC6Crud.Models.PineLab
{
    public class PineLabHash
    {
        public static string GenerateCreateOrderHash(string request, string secret)
        {
            try
            {
                using (var hmac = new HMACSHA256(HexToByteArray(secret)))
                {
                    var data = Encoding.UTF8.GetBytes(request);
                    var hashBytes = hmac.ComputeHash(data);
                    var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
                    return hash;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public static string GenerateFetchOrderHash(object request, string secret)
        {
            try
            {
                var sortedKeys = request.GetType().GetProperties()
                    .Select(property => new { Key = property.Name, Value = property.GetValue(request) })
                    .OrderBy(item => item.Key)
                    .Select(item => $"{item.Key}={item.Value}")
                    .ToArray();

                var dataString = string.Join("&", sortedKeys);
                using (var hmac = new HMACSHA256(HexToByteArray(secret)))
                {
                    var dataBytes = Encoding.UTF8.GetBytes(dataString);
                    var hashBytes = hmac.ComputeHash(dataBytes);
                    var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
                    return hash;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public static bool VerifyHash(string hash, object request, string merchantSecret)
        {
            try
            {
                var sortedKeys = request.GetType().GetProperties()
                    .Select(property => new { Key = property.Name, Value = property.GetValue(request) })
                    .OrderBy(item => item.Key, StringComparer.Ordinal)
                    .Select(item => $"{item.Key}={item.Value}")
                    .ToArray();

                var dataString = string.Join("&", sortedKeys);

                using (var hmac = new HMACSHA256(HexToByteArray(merchantSecret)))
                {
                    var dataBytes = Encoding.UTF8.GetBytes(dataString);
                    var newHashBytes = hmac.ComputeHash(dataBytes);
                    var newHash = BitConverter.ToString(newHashBytes).Replace("-", "").ToUpper();
                    return newHash == hash;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new Exception("Failed to verify hash");
            }
        }
        private static byte[] HexToByteArray(string hex)
        {
            var length = hex.Length;
            var bytes = new byte[length / 2];

            for (var i = 0; i < length; i += 2) bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return bytes;
        }
    }
}
