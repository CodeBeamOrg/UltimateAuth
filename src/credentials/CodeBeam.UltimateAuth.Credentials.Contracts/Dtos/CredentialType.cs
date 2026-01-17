namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public enum CredentialType
{
    Password = 0,
    /// <summary>
    /// Username / email / phone used for login.
    /// </summary>
    IdentifierUsername = 10,
    IdentifierEmail = 11,
    IdentifierPhone = 12,

    // Possession / OTP based
    OneTimeCode = 30,
    EmailOtp = 31,
    SmsOtp = 32,
    Totp = 50,

    // Modern
    Passkey = 60,

    // Machine / system
    Certificate = 70,
    ApiKey = 80,

    // External / Federated // TODO: Add Microsoft, Google, GitHub etc.
    External = 100
}
