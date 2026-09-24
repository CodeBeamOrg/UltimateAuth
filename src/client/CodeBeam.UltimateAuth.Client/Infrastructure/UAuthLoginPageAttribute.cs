namespace CodeBeam.UltimateAuth.Client;

/// <summary>
/// Indicates that the decorated class is a login page component for the UltimateAuth client.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class UAuthLoginPageAttribute : Attribute
{
}
