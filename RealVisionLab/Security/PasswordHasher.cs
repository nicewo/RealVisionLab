using System;
using System.Security.Cryptography;
using System.Text;
using RealVisionLab.Properties;

namespace RealVisionLab.Security
{
    /// <summary>
    /// PBKDF2 (HMAC-SHA256) password hashing. Stored format: v1$iterations$saltBase64$hashBase64.
    /// </summary>
    public static class PasswordHasher
    {
        public const int Iterations = 100000;
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const string Prefix = "v1";

        public static string Hash(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            byte[] hash = Derive(password, salt, Iterations, HashSize);
            return Prefix + "$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        /// <summary>False for null/empty/malformed stored values (never throws).</summary>
        public static bool Verify(string password, string stored)
        {
            if (password == null || string.IsNullOrEmpty(stored)) return false;
            try
            {
                string[] p = stored.Split('$');
                if (p.Length != 4 || p[0] != Prefix) return false;
                int iter = int.Parse(p[1]);
                if (iter < 1000 || iter > 10000000) return false;
                byte[] salt = Convert.FromBase64String(p[2]);
                byte[] expected = Convert.FromBase64String(p[3]);
                if (salt.Length == 0 || expected.Length == 0) return false;
                byte[] actual = Derive(password, salt, iter, expected.Length);
                return ConstantTimeEquals(actual, expected);
            }
            catch (FormatException) { return false; }
            catch (OverflowException) { return false; }
        }

        public static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            int diff = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static byte[] Derive(string password, byte[] salt, int iterations, int length)
        {
            using (var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                return kdf.GetBytes(length);
        }
    }

    /// <summary>Operator password / service code checks on top of Settings.</summary>
    public static class Credentials
    {
        /// <summary>True when neither a hash nor a legacy plaintext password exists (first use).</summary>
        public static bool NeedsInitialSetup()
        {
            return string.IsNullOrEmpty(Settings.Default.userPasswordHash)
                && string.IsNullOrEmpty(Settings.Default.userPassword);
        }

        /// <summary>
        /// Verifies the operator password. A legacy plaintext userPassword is accepted once and
        /// migrated: hash written, plaintext cleared.
        /// </summary>
        public static bool VerifyUser(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            string hash = Settings.Default.userPasswordHash;
            if (!string.IsNullOrEmpty(hash)) return PasswordHasher.Verify(input, hash);

            string legacy = Settings.Default.userPassword;
            if (string.IsNullOrEmpty(legacy)) return false;
            bool ok = PasswordHasher.ConstantTimeEquals(Encoding.UTF8.GetBytes(input), Encoding.UTF8.GetBytes(legacy));
            if (ok) SetUserPassword(input);
            return ok;
        }

        public static void SetUserPassword(string newPassword)
        {
            Settings.Default.userPasswordHash = PasswordHasher.Hash(newPassword);
            Settings.Default.userPassword = "";
            Settings.Default.Save();
        }

        /// <summary>Service code is disabled (always false) while serviceCodeHash is empty.</summary>
        public static bool VerifyServiceCode(string input)
        {
            string hash = Settings.Default.serviceCodeHash;
            if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(input)) return false;
            return PasswordHasher.Verify(input, hash);
        }
    }
}
