namespace CodeBeam.UltimateAuth.Users.Contracts
{
    public enum MfaMethod
    {
        Totp = 10,
        Sms = 20,
        Email = 30,
        Passkey = 40
    }
}
