
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MVC6Crud.Models.PaymanApp
{
    public class Getepay
    {
        public static GetepayOrderResponse generateRequest(GetepayConfig config, GetepayRequest request)
        {
            GetepayRequestWrapper obj = new GetepayRequestWrapper
            {
                mid = config.mid,
                terminalId = config.terminalId
            };
            string requestString = JsonConvert.SerializeObject(request);
            obj.req = encryptRequest(requestString, config);
            string data = JsonConvert.SerializeObject(obj);

            using (WebClient webClient = new WebClient())
            {
                webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
                string text = webClient.UploadString(config.url, data);
                if (!string.IsNullOrEmpty(text))
                {
                    GetepayResponseWrapper wrapper = JsonConvert.DeserializeObject<GetepayResponseWrapper>(text);
                    if (wrapper != null)
                    {
                        return JsonConvert.DeserializeObject<GetepayOrderResponse>(decryptRequest(wrapper.response, config));
                    }
                }
            }
            return null;
        }

        public static GetepayRequeryResponse getepayResponse(GetepayConfig config, string responseString)
        {
            string decrypted = decryptRequest(responseString, config);

            // Remove extra quotes if needed
            if (decrypted.StartsWith("\"") && decrypted.EndsWith("\""))
            {
                decrypted = decrypted.Substring(1, decrypted.Length - 2);
                decrypted = decrypted.Replace("\\\"", "\""); // unescape inner quotes
            }

            return JsonConvert.DeserializeObject<GetepayRequeryResponse>(decrypted);
        }


        public static GetepayRequeryResponse requeryRequest(GetepayConfig config, GetepayRequery request)
        {
            GetepayRequestWrapper obj = new GetepayRequestWrapper
            {
                mid = config.mid,
                terminalId = config.terminalId
            };
            string requestString = JsonConvert.SerializeObject(request);
            obj.req = encryptRequest(requestString, config);
            string data = JsonConvert.SerializeObject(obj);

            using (WebClient webClient = new WebClient())
            {
                webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
                string text = webClient.UploadString(config.url, data);
                if (!string.IsNullOrEmpty(text))
                {
                    GetepayResponseWrapper wrapper = JsonConvert.DeserializeObject<GetepayResponseWrapper>(text);
                    if (wrapper != null)
                    {
                        return JsonConvert.DeserializeObject<GetepayRequeryResponse>(decryptRequest(wrapper.response, config));
                    }
                }
            }
            return null;
        }

        public static string decryptRequest(string requestString, GetepayConfig config)
        {
            byte[] key = Convert.FromBase64String(config.key);
            byte[] iv = Convert.FromBase64String(config.iv);
            byte[] bytes = aesDecrypt(StringToByteArray(requestString), key, iv);
            return Encoding.UTF8.GetString(bytes);
        }

        private static byte[] aesEncrypt(byte[] request, byte[] key, byte[] iv)
        {
            using Aes aes = Aes.Create();
            using ICryptoTransform transform = aes.CreateEncryptor(key, iv);
            using MemoryStream memoryStream = new MemoryStream();
            using CryptoStream stream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(Encoding.UTF8.GetString(request));
            }
            return memoryStream.ToArray();
        }

        private static byte[] aesDecrypt(byte[] request, byte[] key, byte[] iv)
        {
            using Aes aes = Aes.Create();
            using ICryptoTransform transform = aes.CreateDecryptor(key, iv);
            string s;
            using (MemoryStream stream = new MemoryStream(request))
            using (CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read))
            using (StreamReader reader = new StreamReader(cryptoStream))
            {
                s = reader.ReadToEnd();
            }
            return Encoding.UTF8.GetBytes(s);
        }

        public static string encryptRequest(string requestString, GetepayConfig config)
        {
            byte[] key = Convert.FromBase64String(config.key);
            byte[] iv = Convert.FromBase64String(config.iv);
            byte[] array = aesEncrypt(Encoding.UTF8.GetBytes(requestString), key, iv);
            return ByteArrayToString(array);
        }

        private static string ByteArrayToString(byte[] ba) => BitConverter.ToString(ba).Replace("-", "");

        private static byte[] StringToByteArray(string hex)
        {
            int length = hex.Length;
            byte[] array = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return array;
        }
    }
}
