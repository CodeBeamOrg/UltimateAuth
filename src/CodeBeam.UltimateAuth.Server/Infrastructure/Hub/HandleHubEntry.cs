using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal class HandleHub
{
    internal static async Task<IResult> HandleHubEntry(HttpContext ctx, IAuthStore store, IClock clock, IOptions<UAuthServerOptions> options)
    {
        var form = await ctx.GetCachedFormAsync();

        if (form is null)
            return Results.BadRequest("Form content required.");

        var authorizationCode = form["authorization_code"].ToString();
        var codeVerifier = form["code_verifier"].ToString();
        var deviceId = form["device_id"].ToString();
        var returnUrl = form["return_url"].ToString();

        if (!Enum.TryParse<UAuthClientProfile>(form["__uauth_client_profile"], ignoreCase: true, out var clientProfile))
        {
            clientProfile = UAuthClientProfile.NotSpecified;
        }

        var hubSessionId = HubSessionId.New();

        var payload = new HubFlowPayload();
        payload.Set("authorization_code", authorizationCode);
        payload.Set("code_verifier", codeVerifier);

        var tenant = ctx.GetTenant();

        var deviceRaw = form["device"].FirstOrDefault();
        DeviceContext device;

        if (!string.IsNullOrWhiteSpace(deviceRaw))
        {
            try
            {
                var bytes = WebEncoders.Base64UrlDecode(deviceRaw);
                var json = Encoding.UTF8.GetString(bytes);

                device = JsonSerializer.Deserialize<DeviceContext>(json) ?? DeviceContext.Anonymous();
            }
            catch
            {
                device = DeviceContext.Anonymous();
            }
        }
        else
        {
            device = DeviceContext.Anonymous();
        }

        var artifact = new HubFlowArtifact(
            hubSessionId,
            HubFlowType.Login,
            clientProfile,
            tenant,
            device,
            returnUrl,
            payload,
            clock.UtcNow.Add(options.Value.Hub.FlowLifetime));

        await store.StoreAsync(new AuthArtifactKey(hubSessionId.Value), artifact);
        return Results.Redirect($"{options.Value.Hub.LoginPath}?{UAuthConstants.Query.Hub}={hubSessionId.Value}");
    }
}
