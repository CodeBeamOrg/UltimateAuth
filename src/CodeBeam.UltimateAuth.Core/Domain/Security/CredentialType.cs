namespace CodeBeam.UltimateAuth.Core.Domain;

public enum CredentialType
{
    Password = 0,

    // Possession / OTP based
    OneTimeCode = 10,
    EmailOtp = 11,
    SmsOtp = 12,

    Totp = 30,

    // Modern
    Passkey = 100,

    // Machine / system
    Certificate = 200,
    ApiKey = 210,

    // External / Federated // TODO: Add Microsoft, Google, GitHub etc.
    External = 1000
}
