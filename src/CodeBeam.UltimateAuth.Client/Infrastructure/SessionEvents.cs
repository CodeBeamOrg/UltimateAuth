using CodeBeam.UltimateAuth.Client.Abstractions;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal sealed class SessionEvents : ISessionEvents
{
    public event Action? CurrentSessionRevoked;

    public void RaiseCurrentSessionRevoked()
    {
        CurrentSessionRevoked?.Invoke();
    }
}
