using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class DeviceRequiredInvariant : IAuthorityInvariant
{
    public ValueTask<AccessDecisionResult> DecideAsync(AuthContext context, CancellationToken ct = default)
    {
        if (!RequiresDevice(context))
            return ValueTask.FromResult(AccessDecisionResult.Allow());

        if (!context.Device.HasDeviceId)
            return ValueTask.FromResult(AccessDecisionResult.Deny("DeviceId is required for this operation."));

        return ValueTask.FromResult(AccessDecisionResult.Allow());
    }

    private static bool RequiresDevice(AuthContext context)
    {
        if (context.Operation is AuthOperation.Login or AuthOperation.Refresh)
        {
            return context.ClientProfile switch
            {
                UAuthClientProfile.BlazorWasm => true,
                UAuthClientProfile.BlazorServer => true,
                UAuthClientProfile.Maui => true,
                _ => false // service, system, internal
            };
        }

        return false;
    }
}
