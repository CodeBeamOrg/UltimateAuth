using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

internal sealed class EffectiveServerOptionsProvider : IEffectiveServerOptionsProvider
{
    private readonly IOptions<UAuthServerOptions> _baseOptions;
    private readonly IEffectiveAuthModeResolver _modeResolver;

    public EffectiveServerOptionsProvider(IOptions<UAuthServerOptions> baseOptions, IEffectiveAuthModeResolver modeResolver)
    {
        _baseOptions = baseOptions;
        _modeResolver = modeResolver;
    }

    public UAuthServerOptions GetOriginal(HttpContext context)
    {
        return _baseOptions.Value;
    }

    public EffectiveUAuthServerOptions GetEffective(HttpContext context, UAuthApplicationProfile applicationProfile)
    {
        return CreateEffective(new AuthModeResolutionContext
        {
            Tenant = context.GetTenant(),
            ApplicationProfile = applicationProfile,
            HttpContext = context
        });
    }

    public EffectiveUAuthServerOptions GetEffective(TenantKey tenant, UAuthApplicationProfile applicationProfile)
    {
        return CreateEffective(new AuthModeResolutionContext
        {
            Tenant = tenant,
            ApplicationProfile = applicationProfile
        });
    }

    private EffectiveUAuthServerOptions CreateEffective(AuthModeResolutionContext context)
    {
        var original = _baseOptions.Value;
        var mode = ResolveMode(original, context);
        var options = original.Clone();

        ConfigureDefaults.ApplyModeDefaults(mode, options);

        if (original.ModeConfigurations.TryGetValue(
            mode, out var configure))
        {
            configure(options);
        }

        return new EffectiveUAuthServerOptions
        {
            Mode = mode,
            Options = options
        };
    }

    private UAuthMode ResolveMode(UAuthServerOptions options, AuthModeResolutionContext context)
    {
        var mode = options.AuthMode
            ?? (options.ApplicationProfileModes.TryGetValue(context.ApplicationProfile, out var mapped)
                    ? mapped
                    : _modeResolver.Resolve(context));

        if (!Enum.IsDefined(mode))
            throw new InvalidOperationException($"Invalid authentication mode: {mode}.");

        if (options.AllowedModes is { Count: > 0 } allowed && !allowed.Contains(mode))
        {
            throw new InvalidOperationException($"Authentication mode '{mode}' is not allowed.");
        }

        return mode;
    }
}
