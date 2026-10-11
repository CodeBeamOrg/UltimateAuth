using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface ICredentialSessionPolicy
{
    CredentialSessionBehavior Resolve(CredentialChangeReason reason);
}
