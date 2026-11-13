namespace CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;

/// <summary>
/// Identifies the password hashing workload whose concurrency policy should be applied.
/// </summary>
public enum UAuthPasswordHashWorkload
{
    Login = 0,
    CredentialManagement = 10
}
