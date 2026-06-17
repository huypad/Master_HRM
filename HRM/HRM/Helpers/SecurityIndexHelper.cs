using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HRM.Common
{
    public static class SecurityIndexHelper
    {
        private const string Salt = "AppFixedSalt_2026";

        public static byte[] ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

        public static byte[] ComputeHashWithSalt(string value)
        {
            value ??= string.Empty;
            return ComputeSha256(Salt + value.Trim().ToLowerInvariant());
        }

        public static byte[] ComputeHashForGram(string gram)
        {
            return ComputeSha256(Salt + gram);
        }

        public static string NormalizeForSearch(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.Trim().ToLowerInvariant();
            value = Regex.Replace(value, @"\s+", " ");
            return value;
        }

        public static List<string> BuildNgrams(string value, int n = 3)
        {
            var normalized = NormalizeForSearch(value);
            var grams = new List<string>();

            if (string.IsNullOrEmpty(normalized))
                return grams;

            if (normalized.Length < n)
            {
                grams.Add(normalized);
                return grams;
            }

            for (int i = 0; i <= normalized.Length - n; i++)
            {
                grams.Add(normalized.Substring(i, n));
            }

            return grams;
        }

        public static List<byte[]> BuildNgramHashes(string value, int n = 3)
        {
            return BuildNgrams(value, n)
                .Select(ComputeHashForGram)
                .ToList();
        }
    }
}