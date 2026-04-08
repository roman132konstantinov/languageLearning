namespace Application.Common.Security
{
    public static class Base64UrlEncoder
    {
        public static string Encode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static byte[] Decode(string input)
        {
            var normalized = input
                .Replace('-', '+')
                .Replace('_', '/');

            var remainder = normalized.Length % 4;
            if (remainder > 0)
            {
                normalized = normalized.PadRight(normalized.Length + (4 - remainder), '=');
            }

            return Convert.FromBase64String(normalized);
        }
    }
}
