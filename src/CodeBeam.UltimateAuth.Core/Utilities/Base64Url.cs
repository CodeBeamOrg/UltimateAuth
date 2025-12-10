namespace CodeBeam.UltimateAuth.Core.Utilities
{
    public static class Base64Url
    {
        public static string Encode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            return base64
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public static byte[] Decode(string input)
        {
            var padded = input
                .Replace("-", "+")
                .Replace("_", "/");

            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            return Convert.FromBase64String(padded);
        }
    }
}
