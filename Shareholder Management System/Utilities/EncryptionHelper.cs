using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class TripleDESEncryptionHelper
{
    private static readonly string key = "your-24-byte-key"; // Must be 24 bytes for Triple DES

    public static string Encrypt(string plainText)
    {
        using (TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider())
        {
            tdes.Key = Encoding.UTF8.GetBytes(key);
            tdes.Mode = CipherMode.ECB; // You can also use CBC or CFB
            tdes.Padding = PaddingMode.PKCS7;

            using (var encryptor = tdes.CreateEncryptor())
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                }
                byte[] encryptedBytes = ms.ToArray();
                return UrlSafeBase64Encode(encryptedBytes);
            }
        }
    }

    public static string UrlSafeBase64Encode(byte[] input)
    {
        string base64 = Convert.ToBase64String(input);
        // Replace + with - and / with _ to make it URL-safe
        return base64.Replace('+', '-').Replace('/', '_').Replace("=", "");
    }


    public static string Decrypt(string cipherText)
    {
        using (TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider())
        {
            tdes.Key = Encoding.UTF8.GetBytes(key);
            tdes.Mode = CipherMode.ECB; // Ensure this matches the mode used during encryption
            tdes.Padding = PaddingMode.PKCS7;

            // Decode the URL-safe Base64 string back to the original Base64 string
            string base64 = cipherText.Replace('-', '+').Replace('_', '/');
            // Add padding back if necessary
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            byte[] cipherBytes = Convert.FromBase64String(base64);
            using (var decryptor = tdes.CreateDecryptor())
            using (var ms = new MemoryStream(cipherBytes))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }

}