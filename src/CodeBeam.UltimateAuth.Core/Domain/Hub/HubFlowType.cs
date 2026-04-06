namespace CodeBeam.UltimateAuth.Core.Domain;

public enum HubFlowType
{
    None = 0,

    Login = 10,
    Mfa = 20,
    Reauthentication = 30,
    Consent = 40,

    Custom = 1000
}
