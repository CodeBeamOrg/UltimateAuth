using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class UAuthCredentialSessionPolicy : ICredentialSessionPolicy
{
    private readonly UAuthCredentialSessionPolicyOptions _options;

    public UAuthCredentialSessionPolicy(IOptions<UAuthServerOptions> options)
    {
        _options = options.Value.CredentialSessionPolicy;
    }

    public CredentialSessionBehavior Resolve(CredentialChangeReason reason)
    {
        return reason switch
        {
            CredentialChangeReason.SelfChange => _options.OnSelfChange,

            CredentialChangeReason.AdminChange => _options.OnAdminChange,

            CredentialChangeReason.Recovery => _options.OnRecovery,

            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
        };
    }
}
