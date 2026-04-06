namespace CodeBeam.UltimateAuth.Users.Contracts;

public enum MfaMethod
{
    Totp = 0,
    Sms = 10,
    Email = 20,
    Passkey = 30
}
