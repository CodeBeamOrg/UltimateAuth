using Microsoft.AspNetCore.Authorization;

namespace CodeBeam.UltimateAuth.Server.Authorization;

public sealed class UAuthActionRequirement : IAuthorizationRequirement
{
    public string Action { get; }

    public UAuthActionRequirement(string action)
    {
        Action = action;
    }
}
