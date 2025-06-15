using Org.BouncyCastle.Utilities.Encoders;
using System.Buffers.Text;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Crowbar.Encryption
{
    /// <summary>
    /// All text fields go through this to the database, so even if attacker
    /// gets access to the database, he won't be able to decrypt the content
    /// without having access to the encryption key in the forum directory.
    /// </summary>
    public static class EncryptionLayer
    {
        private static byte[]? _key;
        /// <summary>
        /// Encryption key is stored in xorred form in case an attacker
        /// manages to dump the memory. At least this prevents the key
        /// from being found with a simple 'cat file |strings'.
        /// </summary>
        private static byte[]? key {
            get => xorDeobfuscate(_key);
            set => _key = xorObfuscate(value);
        }
        private static byte[]? iv;

        /// <summary>
        /// Obfuscates input data by xorring it with a predefined string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Cipher</returns>
        private static byte[]? xorObfuscate(byte[]? data)
        {
            data ??= [];
            var xorWith = Encoding.UTF8.GetBytes("CROWBAR");
            var xorred = new byte[data.Length];
            for (int i = 0; i < data.Length; i++) 
                xorred[i] = (byte)(data[i] ^ xorWith[i%xorWith.Length]);
            return xorred;
        }

        /// <summary>
        /// Deobfuscates input data by xorring it with a predefined string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Cleartext</returns>
        private static byte[]? xorDeobfuscate(byte[]? cipher)
        {
            cipher ??= [];
            var xorWith = Encoding.UTF8.GetBytes("CROWBAR");
            var xorred = new byte[cipher.Length];
            for (int i = 0; i < cipher.Length; i++)
                xorred[i] = (byte)(cipher[i] ^ xorWith[i % xorWith.Length]);
            return xorred;
        }

        /// <summary>
        /// Generates random string of text with the length specified.
        /// </summary>
        /// <param name="length"></param>
        /// <returns>Random string</returns>
        public static string RandomText(int length)
        {
            var characters = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM0123456789";
            var output = new StringBuilder();
            for (var i = 0; i < length; i++)
                output.Append(characters[new Random().Next(characters.Length)]);
            return output.ToString();
        }

        /// <summary>
        /// Makes the encrypted base64 unique each time
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Obfuscate(string data) => $"{data}{RandomText(16)}";

        /// <summary>
        /// Makes the encrypted base64 unique each time
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Obfuscate(byte[] data) => data.Concat(Encoding.UTF8.GetBytes(RandomText(16))).ToArray();

        /// <summary>
        /// Removes the randomized last 16 characters.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Deobfuscate(string data) => data[..^16];

        /// <summary>
        /// Removes the randomized last 16 characters.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Deobfuscate(byte[] data) => data[..^16];


        /// <summary>
        /// Sets the encryption password and IV (called at startup)
        /// </summary>
        /// <param name="password"></param>
        public static void SetPassword(string password)
        {
            key = Sha256Hash(password);
            iv = Sha256Hash($"IV_{password}").ToList().GetRange(0, 16).ToArray();
        }

        /// <summary>
        /// Does a simple AES-256 encryption on the data provided.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Encrypted cipher in the form of base64 encoded string.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string? Encrypt(string? data)
        {
            if (key is null) 
                throw new ArgumentNullException(nameof(key));
            if (iv is null) 
                throw new ArgumentNullException(nameof(iv));
            if (data is null) return null;

            data = Obfuscate(data);

            var outputStream = new MemoryStream();
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                using CryptoStream cryptoStream = new(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                using StreamWriter cryptoWriter = new StreamWriter(cryptoStream);
                cryptoWriter.Write(data);
            }

            return Convert.ToBase64String(outputStream.ToArray());
        }

        /// <summary>
        /// Does a simple AES-256 encryption on the data provided.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Encrypted cipher in the form of base64 encoded string./returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string? Encrypt(byte[]? data)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));
            if (data is null) return null;

            data = Obfuscate(data);

            var outputStream = new MemoryStream();
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                using CryptoStream cryptoStream = new(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                cryptoStream.Write(data);
            }

            return Convert.ToBase64String(outputStream.ToArray());
        }

        /// <summary>
        /// Does a simple AES-256 encryption on the data provided.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Encrypted cipher in the form of base64 encoded string./returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string[]? Encrypt(string[]? data)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));

            List<string> output = new();
            foreach (var item in data ?? [])
                output.Add(Encrypt(item) ?? "");

            return output.ToArray();
        }


        /// <summary>
        /// Decrypts AES-256 cipher.
        /// </summary>
        /// <param name="cipher"></param>
        /// <returns>Plaintext</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string? DecryptString(string? cipher)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));
            if (cipher is null) return null;

            var cipherStream = new MemoryStream(Convert.FromBase64String(cipher));
            var outputStream = new MemoryStream();
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using CryptoStream cryptoStream = new(cipherStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader cryptoReader = new StreamReader(cryptoStream);
            return Deobfuscate(cryptoReader.ReadToEnd());   
        }

        /// <summary>
        /// Decrypts AES-256 cipher.
        /// </summary>
        /// <param name="cipher"></param>
        /// <returns>Plaintext bytes</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static byte[]? DecryptData(string? cipher)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));
            if (cipher is null) return null;

            var cipherStream = new MemoryStream(Convert.FromBase64String(cipher));
            var outputStream = new MemoryStream();
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using CryptoStream cryptoStream = new(cipherStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using MemoryStream cryptoReader = new MemoryStream();
            cryptoStream.CopyTo(cryptoReader);
            return Deobfuscate(cryptoReader.ToArray());
        }

        /// <summary>
        /// Decrypts AES-256 cipher.
        /// </summary>
        /// <param name="cipher"></param>
        /// <returns>Plaintext bytes</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string[]? DecryptStringList(string[]? ciphers)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));

            List<string> output = new();
            foreach (var cipher in ciphers ?? [])
                output.Add(DecryptString(cipher) ?? "");
            return output.ToArray();
        }


        /// <summary>
        /// Calculates a SHA-256 value.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] Sha256Hash(string data)
            => SHA256.HashDataAsync(new MemoryStream(Encoding.UTF8.GetBytes(data))).GetAwaiter().GetResult();
    }
}
