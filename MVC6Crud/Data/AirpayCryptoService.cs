using System.Security.Cryptography;
using System.Text;

namespace MVC6Crud.Data
{
    public class AirpayCryptoService
    {
        /*
         * Airpay sample:
         *
         * MD5(username + "~:~" + password)
         */
        public string GenerateSecretKey(
            string username,
            string password)
        {
            using var md5 = MD5.Create();

            string value =
                username + "~:~" + password;

            byte[] bytes =
                Encoding.UTF8.GetBytes(value);

            byte[] hash =
                md5.ComputeHash(bytes);

            return BitConverter
                .ToString(hash)
                .Replace("-", "")
                .ToLowerInvariant();
        }


        /*
         * Airpay AES encryption.
         *
         * The Airpay sample creates:
         *
         * 8 random bytes
         * -> hex string
         * -> 16 characters IV
         *
         * Final:
         *
         * IV + Base64(ciphertext)
         */
        public string Encrypt(
            string plainText,
            string secretKey)
        {
            using Aes aes = Aes.Create();

            aes.Key =
                Encoding.UTF8.GetBytes(secretKey);

            byte[] ivBytes = new byte[8];

            RandomNumberGenerator.Fill(ivBytes);

            string ivHex =
                BitConverter
                    .ToString(ivBytes)
                    .Replace("-", "")
                    .ToLowerInvariant();

            aes.IV =
                Encoding.UTF8.GetBytes(ivHex);

            aes.Mode = CipherMode.CBC;

            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor =
                aes.CreateEncryptor(
                    aes.Key,
                    aes.IV);

            using MemoryStream ms =
                new MemoryStream();

            using (
                CryptoStream cryptoStream =
                    new CryptoStream(
                        ms,
                        encryptor,
                        CryptoStreamMode.Write))
            {
                byte[] data =
                    Encoding.UTF8.GetBytes(
                        plainText);

                cryptoStream.Write(
                    data,
                    0,
                    data.Length);

                cryptoStream.FlushFinalBlock();
            }

            string encrypted =
                Convert.ToBase64String(
                    ms.ToArray());

            return ivHex + encrypted;
        }


        /*
         * Airpay response decryption.
         */
        public string Decrypt(
            string encryptedData,
            string secretKey)
        {
            if (string.IsNullOrWhiteSpace(
                encryptedData))
            {
                throw new ArgumentException(
                    "Airpay encrypted response is empty.");
            }

            if (encryptedData.Length < 17)
            {
                throw new ArgumentException(
                    "Invalid Airpay encrypted response.");
            }

            string ivHex =
                encryptedData.Substring(
                    0,
                    16);

            string encryptedBase64 =
                encryptedData.Substring(
                    16);

            byte[] encryptedBytes =
                Convert.FromBase64String(
                    encryptedBase64);

            using Aes aes = Aes.Create();

            aes.Key =
                Encoding.UTF8.GetBytes(
                    secretKey);

            aes.IV =
                Encoding.UTF8.GetBytes(
                    ivHex);

            aes.Mode = CipherMode.CBC;

            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform decryptor =
                aes.CreateDecryptor(
                    aes.Key,
                    aes.IV);

            using MemoryStream ms =
                new MemoryStream(
                    encryptedBytes);

            using CryptoStream cryptoStream =
                new CryptoStream(
                    ms,
                    decryptor,
                    CryptoStreamMode.Read);

            using StreamReader reader =
                new StreamReader(
                    cryptoStream,
                    Encoding.UTF8);

            return reader.ReadToEnd();
        }


        /*
         * Airpay checksum:
         *
         * Sort dictionary by key
         * Concatenate values
         * Add UTC date
         * SHA256
         */
        public string GenerateChecksum(
            Dictionary<string, string> data)
        {
            var sortedData =
                data.OrderBy(
                    x => x.Key);

            string concatenated =
                string.Concat(
                    sortedData.Select(
                        x => x.Value));

            string value =
                concatenated +
                DateTime.UtcNow.ToString(
                    "yyyy-MM-dd");

            return Sha256(value);
        }


        /*
         * Airpay private key:
         *
         * SHA256(secret + "@" + username + ":|:" + password)
         */
        public string GeneratePrivateKey(
            string username,
            string password,
            string secret)
        {
            string value =
                username +
                ":|:" +
                password;

            return Sha256(
                secret +
                "@" +
                value);
        }


        public string Sha256(
            string value)
        {
            using SHA256 sha =
                SHA256.Create();

            byte[] hash =
                sha.ComputeHash(
                    Encoding.UTF8.GetBytes(
                        value));

            return BitConverter
                .ToString(hash)
                .Replace("-", "")
                .ToLowerInvariant();
        }
    }

}
