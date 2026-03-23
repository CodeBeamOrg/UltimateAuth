using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Sample.UAuthHub.Controllers;

[Route("auth/uauthhub")]
[IgnoreAntiforgeryToken]
public sealed class HubLoginController : Controller
{
    private readonly IAuthStore _authStore;
    private readonly UAuthServerOptions _options;
    private readonly IClock _clock;

    public HubLoginController(IAuthStore authStore, IOptions<UAuthServerOptions> options, IClock clock)
    {
        _authStore = authStore;
        _options = options.Value;
        _clock = clock;
    }

    [HttpPost("login")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> BeginLogin(
        [FromForm] string authorization_code,
        [FromForm] string code_verifier,
        [FromForm] UAuthClientProfile client_profile,
        [FromForm] string? return_url,
        [FromForm] string device_id)
    {
        var hubSessionId = HubSessionId.New();

        var payload = new HubFlowPayload();
        payload.Set("authorization_code", authorization_code);
        payload.Set("code_verifier", code_verifier);
        payload.Set("device_id", device_id);

        var artifact = new HubFlowArtifact(
            hubSessionId: hubSessionId,
            flowType: HubFlowType.Login,
            clientProfile: client_profile,
            tenant: TenantKeys.System, // TODO: Think about multi tenant scenarios
            returnUrl: return_url,
            payload: payload,
            expiresAt: _clock.UtcNow.Add(_options.Hub.FlowLifetime));

        await _authStore.StoreAsync(new AuthArtifactKey(hubSessionId.Value), artifact, HttpContext.RequestAborted);

        return Redirect($"{_options.Hub.LoginPath}?hub={hubSessionId.Value}");
    }
}
