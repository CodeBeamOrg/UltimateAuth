using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class ApplicationProfileReader : IApplicationProfileReader
{
    public async Task<UAuthApplicationProfile> ReadAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(UAuthConstants.Headers.ClientProfile, out var headerValue) && TryParse(headerValue, out var headerProfile))
        {
            return headerProfile;
        }

        var form = await context.GetCachedFormAsync();

        if (form is not null && form.TryGetValue(UAuthConstants.Form.ClientProfile, out var formValue) && TryParse(formValue, out var formProfile))
        {
            return formProfile;
        }

        return UAuthApplicationProfile.NotSpecified;
    }

    private static bool TryParse(string? value, out UAuthApplicationProfile profile)
    {
        return Enum.TryParse(value, ignoreCase: true, out profile);
    }
}
