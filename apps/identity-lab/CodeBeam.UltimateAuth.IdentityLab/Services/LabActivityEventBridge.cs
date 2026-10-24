using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabActivityEventBridge
{
    private IServiceScopeFactory? _scopeFactory;

    public void Initialize(IServiceProvider services)
    {
        _scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
    }

    public async Task RecordAsync(UserKey userKey, string eventType)
    {
        if (_scopeFactory is null)
            return;

        using var scope = _scopeFactory.CreateScope();

        var handler = scope.ServiceProvider
            .GetRequiredService<LabActivityEventHandler>();

        if (eventType == ActivityEvents.Login)
            await handler.RecordLoginAsync(userKey);
        else if (eventType == ActivityEvents.Logout)
            await handler.RecordLogoutAsync(userKey);
    }
}
