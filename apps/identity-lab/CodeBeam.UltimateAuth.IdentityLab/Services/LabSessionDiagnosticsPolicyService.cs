using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed record LabSessionDiagnosticsPolicy(
    TimeSpan SessionLifetime,
    TimeSpan? TouchInterval,
    TimeSpan? IdleTimeout,
    TimeSpan? MaxLifetime,
    bool SlidingExpiration,
    bool AutoRefreshEnabled,
    TimeSpan? AutoRefreshInterval);

public sealed class LabSessionDiagnosticsPolicyService(IOptions<UAuthServerOptions> serverOptions, IOptions<UAuthClientOptions> clientOptions)
{
    public LabSessionDiagnosticsPolicy Get()
    {
        var session = serverOptions.Value.Session;
        var autoRefresh = clientOptions.Value.AutoRefresh;

        return new LabSessionDiagnosticsPolicy(
            session.Lifetime,
            session.TouchInterval,
            session.IdleTimeout,
            session.MaxLifetime,
            session.SlidingExpiration,
            autoRefresh.Enabled,
            autoRefresh.Interval);
    }
}
