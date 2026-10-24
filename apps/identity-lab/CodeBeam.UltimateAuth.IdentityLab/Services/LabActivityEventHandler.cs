using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabActivityEventHandler(SandboxService sandboxService, ActivityLogService activityLogService)
{
    public async Task RecordLoginAsync(UserKey userKey)
    {
        await RecordAsync(userKey, ActivityEvents.Login);
    }

    public async Task RecordLogoutAsync(UserKey userKey)
    {
        await RecordAsync(userKey, ActivityEvents.Logout);
    }

    private async Task RecordAsync(UserKey userKey, string eventType)
    {
        var sandbox = await sandboxService.GetByUserAsync(userKey);

        if (sandbox is null)
            return;

        await activityLogService.TryRecordAsync(sandbox.Id, userKey.Value, eventType);
    }
}
