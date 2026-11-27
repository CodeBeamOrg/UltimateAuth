using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Builder;

namespace CodeBeam.UltimateAuth.Server.Extensions;

internal static class UAuthEndpointConventionExtensions
{
    public static TBuilder RequireUAuthAuthentication<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(new UAuthRequireAuthenticationMetadata());
        return builder;
    }
}
