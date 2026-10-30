using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabProvisioningRollbackGate
{
    internal const string AttributeKey = "identitylab.internal.provisioning.rollback";

    private readonly Dictionary<object, (UserKey Actor, UserKey Target)>
        _pending = new(ReferenceEqualityComparer.Instance);

    internal object Register(UserKey actor, UserKey target)
    {
        var token = new object();
        _pending.Add(token, (actor, target));
        return token;
    }

    internal bool Consume(
        IReadOnlyDictionary<string, object> attributes,
        UserKey actor,
        UserKey target)
    {
        if (!attributes.TryGetValue(AttributeKey, out var token))
            return false;

        if (!_pending.TryGetValue(token, out var expected))
            return false;

        if (expected.Actor != actor || expected.Target != target)
            return false;

        _pending.Remove(token);
        return true;
    }

    internal void Revoke(object token)
    {
        _pending.Remove(token);
    }
}
