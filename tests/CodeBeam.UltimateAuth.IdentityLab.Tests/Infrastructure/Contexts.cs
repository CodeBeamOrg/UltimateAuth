using System.Reflection;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Infrastructure;

internal static class Contexts
{
    // AccessContext deliberately has no public constructor. Keep reflection confined
    // to this adapter rather than adding production friend assemblies or public APIs.
    public static AccessContext Create(string action, UserKey? actor = null, UserKey? target = null,
        bool system = false, IReadOnlyDictionary<string, object>? attributes = null,
        IReadOnlyCollection<UserKey>? targets = null)
        => (AccessContext)typeof(AccessContext).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single().Invoke(new object?[] { actor, TenantKeys.Single, actor.HasValue, system,
                null, Array.Empty<AccessActorRole>(), "identitylab.tests", target, TenantKeys.Single,
                action, attributes ?? new Dictionary<string, object>(), targets });
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
