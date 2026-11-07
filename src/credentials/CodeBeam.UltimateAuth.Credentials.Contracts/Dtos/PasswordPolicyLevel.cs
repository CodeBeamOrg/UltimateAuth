namespace CodeBeam.UltimateAuth.Credentials.Contracts;

/// <summary>
/// Defines the scope of password policy enforcement.
/// </summary>
public enum PasswordPolicyLevel
{
    Disabled = 0,
    Basic = 10,
    Standard = 20,
    Strict = 30
}
