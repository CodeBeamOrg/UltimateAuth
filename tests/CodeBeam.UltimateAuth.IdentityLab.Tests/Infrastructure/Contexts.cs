using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using System.Reflection;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Infrastructure;

internal static class Contexts
{
    // AccessContext deliberately has no public constructor. Keep reflection confined
    // to this adapter rather than adding production friend assemblies or public APIs.
    public static AccessContext Create(string action, UserKey? actor = null, UserKey? target = null, bool system = false,
                                       IReadOnlyDictionary<string, object>? attributes = null, IReadOnlyCollection<UserKey>? targets = null)
    {
        var constructor = typeof(AccessContext).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();

        var args = constructor.GetParameters()
            .Select(parameter => parameter.Name switch
            {
                "actorUserKey" => (object?)actor,
                "actorTenant" => TenantKeys.Single,
                "isAuthenticated" => actor.HasValue,
                "isSystemActor" => system,
                "actorChainId" => null,
                "actorRoles" => Array.Empty<AccessActorRole>(),
                "resource" => "identitylab.tests",
                "targetUserKey" => target,
                "resourceTenant" => TenantKeys.Single,
                "action" => action,
                "attributes" => attributes ?? new Dictionary<string, object>(),
                "targetUserKeys" => targets,
                "clientProfile" => UAuthClientProfile.NotSpecified,
                _ => throw new InvalidOperationException(
                    $"Unexpected AccessContext parameter: {parameter.Name}")
            })
            .ToArray();

        return (AccessContext)constructor.Invoke(args);
    }

    public static CodeBeam.UltimateAuth.Server.Auth.AuthFlowContext AuthenticatedFlow(UserKey actor)
    {
        var constructor = typeof(CodeBeam.UltimateAuth.Server.Auth.AuthFlowContext)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var args = constructor.GetParameters().Select(parameter => parameter.Name switch
        {
            "tenantKey" => (object)TenantKeys.Single,
            "isAuthenticated" => true,
            "userKey" => actor,
            _ => parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null
        }).ToArray();
        return (CodeBeam.UltimateAuth.Server.Auth.AuthFlowContext)constructor.Invoke(args);
    }
}
