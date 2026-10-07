namespace CodeBeam.UltimateAuth.Core.Contracts;

public enum RefreshTokenValidationState
{
    Valid = 0,
    NotFound = 10,
    Expired = 20,
    Consumed = 30,
    Invalid = 40
}
