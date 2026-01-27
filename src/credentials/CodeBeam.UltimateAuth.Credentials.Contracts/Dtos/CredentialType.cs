namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public enum CredentialType
{
    Password = 0,

    // Possession / OTP based
    OneTimeCode = 10,
    EmailOtp = 11,
    SmsOtp = 12,

    Totp = 30,

    // Modern
    Passkey = 40,

    // Machine / system
    Certificate = 50,
    ApiKey = 60,

    // External / Federated // TODO: Add Microsoft, Google, GitHub etc.
    External = 70
}
