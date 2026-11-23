using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Core.Contracts;
using System.Globalization;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

public static class UAuthLoginCompletion
{
    public static async Task CompleteAsync(UAuthTransportResult response, bool accessTokenRequired, IUAuthTokenStore tokenStore, IUAuthClientEvents events)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!response.Ok || response.Status is < 200 or >= 300)
        {
            throw new UAuthProtocolException("Cannot complete an unsuccessful login.");
        }

        var token = ReadAccessToken(response);

        if (accessTokenRequired && token is null)
        {
            tokenStore.Clear();

            throw new UAuthProtocolException("Login succeeded but the required access token was not returned.");
        }

        if (token is not null && token.IsExpired(DateTimeOffset.UtcNow))
        {
            tokenStore.Clear();

            throw new UAuthProtocolException("Server returned an expired access token.");
        }

        // A cookie-only login must not retain a token belonging to a previous authenticated user.
        if (token is null)
            tokenStore.Clear();
        else
            tokenStore.Replace(token);

        await events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.LoginSucceeded, UAuthStateEventHandlingMode.Validate));
    }

    public static string? GetRedirectUrl(UAuthTransportResult response)
    {
        if (response.Body is not { } body ||
            body.ValueKind != JsonValueKind.Object ||
            !body.TryGetProperty("redirectUrl", out var redirect) ||
            redirect.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return redirect.GetString();
    }

    public static AccessToken? ReadAccessToken(UAuthTransportResult response)
    {
        if (string.IsNullOrWhiteSpace(response.AccessToken))
            return null;

        if (string.IsNullOrWhiteSpace(response.AccessTokenExpiresAt) ||
            !DateTimeOffset.TryParse(response.AccessTokenExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAt))
        {
            throw new UAuthProtocolException("Access token expiration metadata is invalid.");
        }

        var format = response.AccessTokenFormat?.ToLowerInvariant() switch
        {
            "jwt" => TokenFormat.Jwt,
            "opaque" => TokenFormat.Opaque,
            _ => throw new UAuthProtocolException("Access token format metadata is invalid.")
        };

        var token = response.AccessToken;

        switch (response.AccessTokenHeaderFormat?.ToLowerInvariant())
        {
            case "bearer":
                if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    throw new UAuthProtocolException("Invalid Bearer access token header.");
                }

                token = token["Bearer ".Length..];
                break;

            case "raw":
                break;

            default:
                throw new UAuthProtocolException("Access token header format metadata is invalid.");
        }

        if (string.IsNullOrWhiteSpace(token) || token.Contains('\r') || token.Contains('\n'))
        {
            throw new UAuthProtocolException("Invalid access token value.");
        }

        return new AccessToken
        {
            Token = token,
            Format = format,
            ExpiresAt = expiresAt
        };
    }
}
