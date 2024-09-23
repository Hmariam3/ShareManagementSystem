using System.Security.Cryptography;
using System.Text;

namespace Shareholder_Management_System.commons
{
    public class PasswordHash
    {
        public string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convert byte array to a string
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    stringBuilder.Append(bytes[i].ToString("x2")); // Convert each byte to a hexadecimal string
                }
                return stringBuilder.ToString();
            }
        }
        // Method to verify if the provided password matches the stored hash
        public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
        {
            // Hash the provided password
            string hashedProvidedPassword = HashPassword(providedPassword);

            // Compare the stored hashed password with the provided hashed password
            return hashedPassword == hashedProvidedPassword;
        }
    }
}