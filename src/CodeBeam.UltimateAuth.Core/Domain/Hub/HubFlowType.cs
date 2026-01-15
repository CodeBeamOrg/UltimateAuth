namespace CodeBeam.UltimateAuth.Core.Domain;

public enum HubFlowType
{
    None = 0,

    Login = 1,
    Mfa = 2,
    Reauthentication = 3,
    Consent = 4,

    Custom = 1000
}
