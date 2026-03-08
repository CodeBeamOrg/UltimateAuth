//using CodeBeam.UltimateAuth.Authorization.Contracts;
//using CodeBeam.UltimateAuth.Core.Abstractions;
//using CodeBeam.UltimateAuth.Core.Contracts;

//namespace CodeBeam.UltimateAuth.Authorization;

//public sealed class PermissionAccessPolicy : IAccessPolicy
//{
//    private readonly IReadOnlySet<string> _permissions;
//    private readonly string _operation;

//    public PermissionAccessPolicy(IEnumerable<Permission> permissions, string operation)
//    {
//        _permissions = permissions.Select(p => p.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
//        _operation = operation;
//    }

//    public bool AppliesTo(AccessContext context) => context.ActorUserKey is not null;

//    public AccessDecision Decide(AccessContext context)
//    {
//        if (context.ActorUserKey is null)
//            return AccessDecision.Deny("unauthenticated");

//        return _permissions.Contains(_operation)
//            ? AccessDecision.Allow()
//            : AccessDecision.Deny("missing_permission");
//    }
//}
