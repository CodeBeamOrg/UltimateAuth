using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Extensions;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class AuthContextFactory : IAuthContextFactory
{
    private readonly IAuthFlowContextAccessor _flow;
    private readonly IClock _clock;

    public AuthContextFactory(IAuthFlowContextAccessor flow, IClock clock)
    {
        _flow = flow;
        _clock = clock;
    }

    public AuthContext Create(DateTimeOffset? at = null)
    {
        var flow = _flow.Current;
        return flow.ToAuthContext(at ?? _clock.UtcNow);
    }
}
