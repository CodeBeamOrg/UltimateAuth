namespace CodeBeam.UltimateAuth.Client.Blazor;

[AttributeUsage(AttributeTargets.Class)]
public sealed class UAuthAuthorizeAttribute : Attribute
{
    public string? Roles { get; set; }
    public string? Permissions { get; set; }
}
