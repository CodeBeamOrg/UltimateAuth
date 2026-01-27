namespace CodeBeam.UltimateAuth.Credentials.Contracts
{
    public static class CredentialTypeParser
    {
        private static readonly Dictionary<string, CredentialType> _map =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["password"] = CredentialType.Password,

                ["otp"] = CredentialType.OneTimeCode,
                ["one-time-code"] = CredentialType.OneTimeCode,

                ["email-otp"] = CredentialType.EmailOtp,
                ["sms-otp"] = CredentialType.SmsOtp,

                ["totp"] = CredentialType.Totp,

                ["passkey"] = CredentialType.Passkey,

                ["certificate"] = CredentialType.Certificate,
                ["cert"] = CredentialType.Certificate,

                ["api-key"] = CredentialType.ApiKey,
                ["apikey"] = CredentialType.ApiKey,

                ["external"] = CredentialType.External
            };

        public static bool TryParse(string value, out CredentialType type) => _map.TryGetValue(value, out type);

        public static CredentialType ParseOrThrow(string value) => TryParse(value, out var type)
            ? type
            : throw new InvalidOperationException($"Unsupported credential type: '{value}'");
    }
}
