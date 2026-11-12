using System.Security.Claims;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Authorization;

internal sealed class UAuthActiveUserHandler : AuthorizationHandler<UAuthActiveUserRequirement>
{
    private readonly IUserRuntimeStateProvider _runtime;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UAuthActiveUserHandler(IUserRuntimeStateProvider runtime, IHttpContextAccessor httpContextAccessor)
    {
        _runtime = runtime;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UAuthActiveUserRequirement requirement)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http is null)
            return;

        // Only a validated UltimateAuth identity is eligible.
        var identity = context.User.Identities.FirstOrDefault(x =>
            x.IsAuthenticated &&
            x.AuthenticationType == UAuthConstants.SchemeDefaults.GlobalScheme);

        if (identity is null)
            return;

        var userKeyValue = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userKeyValue))
            return;

        if (!UserKey.TryParse(userKeyValue, null, out var userKey))
            return;

        var state = await _runtime.GetAsync(http.GetTenant(), userKey, http.RequestAborted);

        if (state is null || !state.Exists || state.IsDeleted || !state.CanAuthenticate)
        {
            return;
        }

        if (state.IsActive)
        {
            context.Succeed(requirement);
            return;
        }

        // Explicit exception for authenticated but inactive users.
        var allowInactive = http.GetEndpoint()?.Metadata.GetMetadata<UAuthAllowInactiveAttribute>();

        if (allowInactive is not null)
            context.Succeed(requirement);
    }
}
