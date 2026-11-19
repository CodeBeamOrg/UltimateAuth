using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

/// <summary>
/// Provides the effective authentication mode for the Resource API.
/// </summary>
/// <remarks>
/// The Resource API has a fixed application profile.
/// Its effective mode is resolved once and shared by the authentication pipeline and supporting services.
/// </remarks>
internal sealed class ResourceApiAuthModeProvider
{
    private readonly Lazy<UAuthMode> _mode;

    public ResourceApiAuthModeProvider(IOptions<UAuthResourceApiOptions> options, IEffectiveAuthModeResolver resolver)
    {
        _mode = new Lazy<UAuthMode>(() => Resolve(options.Value, resolver));
    }

    public UAuthMode Mode => _mode.Value;

    internal static UAuthMode Resolve(UAuthResourceApiOptions options, IEffectiveAuthModeResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(resolver);

        var mode = options.Auth.Mode ?? resolver.Resolve(
            new AuthModeResolutionContext
            {
                ClientProfile = UAuthApplicationProfile.ResourceApi,
                Tenant = TenantKey.Single
            });

        if (!Enum.IsDefined(mode))
        {
            throw new InvalidOperationException($"Invalid Resource API authentication mode: {mode}.");
        }

        return mode;
    }
}
