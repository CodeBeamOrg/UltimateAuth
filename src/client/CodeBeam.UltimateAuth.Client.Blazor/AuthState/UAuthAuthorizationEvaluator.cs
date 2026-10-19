using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Authorization;

namespace CodeBeam.UltimateAuth.Client.Blazor;

internal sealed class UAuthAuthorizationEvaluator : IUAuthAuthorizationEvaluator
{
    private readonly IAuthorizationService _authorizationService;

    public UAuthAuthorizationEvaluator(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task<UAuthAuthorizationResult> EvaluateAsync(UAuthState state, UAuthAuthorizationRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(requirement);

        if (!state.IsAuthenticated)
            return UAuthAuthorizationResult.Denied;

        // Preserve UAuthStateView's existing session-state semantics.
        if (requirement.RequireActive && state.Identity?.SessionState is { } sessionState && sessionState != SessionState.Active)
        {
            return UAuthAuthorizationResult.Inactive;
        }

        var roles = ParseCsv(requirement.Roles);
        var permissions = ParseCsv(requirement.Permissions);
        var hasPolicy = !string.IsNullOrWhiteSpace(requirement.Policy);

        if (roles.Length == 0 && permissions.Length == 0 && !hasPolicy)
        {
            return UAuthAuthorizationResult.Authorized;
        }

        // Capture synchronous results before the first await.
        var roleResults = roles.Select(state.IsInRole).ToArray();
        var permissionResults = permissions.Select(state.HasPermission).ToArray();
        bool? policyResult = null;

        if (hasPolicy)
        {
            var principal = state.ToClaimsPrincipal();
            var result = await _authorizationService.AuthorizeAsync(principal, requirement.Policy!);
            policyResult = result.Succeeded;
        }

        var authorized = requirement.MatchMode switch
        {
            AuthorizationMatchMode.Any => roleResults.Any(x => x) || permissionResults.Any(x => x) || policyResult == true,
            AuthorizationMatchMode.All => roleResults.All(x => x) && permissionResults.All(x => x) && policyResult != false,
            AuthorizationMatchMode.Category => (roleResults.Length == 0 || roleResults.Any(x => x)) && (permissionResults.Length == 0 || permissionResults.Any(x => x)) && policyResult != false,
            _ => false
        };

        return authorized
            ? UAuthAuthorizationResult.Authorized
            : UAuthAuthorizationResult.Denied;
    }

    private static string[] ParseCsv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
