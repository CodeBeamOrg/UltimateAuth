using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal sealed class ResourceAuthContextFactory : IAuthContextFactory
{
    private readonly IHttpContextAccessor _http;
    private readonly IClock _clock;

    public ResourceAuthContextFactory(IHttpContextAccessor http, IClock clock)
    {
        _http = http;
        _clock = clock;
    }

    public AuthContext Create(DateTimeOffset? at = null)
    {
        var ctx = _http.HttpContext!;

        var result = ctx.Items[UAuthConstants.HttpItems.SessionValidationResult] as SessionValidationResult;

        if (result is null || !result.IsValid)
        {
            return new AuthContext
            {
                Tenant = default!,
                Operation = AuthOperation.ResourceAccess,
                Mode = UAuthMode.PureOpaque,
                ClientProfile = UAuthClientProfile.Api,
                Device = DeviceContext.Create(DeviceId.Create(result.BoundDeviceId.Value.Value)),
                At = at ?? _clock.UtcNow,
                Session = null
            };
        }

        return new AuthContext
        {
            Tenant = result.Tenant,
            Operation = AuthOperation.ResourceAccess,
            Mode = UAuthMode.PureOpaque, // sonra resolver yapılabilir
            ClientProfile = UAuthClientProfile.Api,
            Device = DeviceContext.Create(DeviceId.Create(result.BoundDeviceId.Value.Value)),
            At = at ?? _clock.UtcNow,

            Session = new SessionSecurityContext
            {
                UserKey = result.UserKey,
                SessionId = result.SessionId.Value,
                State = result.State,
                ChainId = result.ChainId,
                BoundDeviceId = result.BoundDeviceId
            }
        };
    }
}