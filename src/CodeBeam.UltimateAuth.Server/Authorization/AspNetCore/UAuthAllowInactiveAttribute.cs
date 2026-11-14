namespace CodeBeam.UltimateAuth.Server.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class UAuthAllowInactiveAttribute : Attribute
{

}
