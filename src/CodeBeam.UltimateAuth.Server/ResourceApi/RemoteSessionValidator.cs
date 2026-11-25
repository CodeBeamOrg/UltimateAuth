using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

// TODO: Resource API calls make two calls to here. Investigate.
internal sealed class RemoteSessionValidator : ISessionValidator
{
    private readonly HttpClient _http;
    private readonly UAuthResourceApiOptions _options;

    public RemoteSessionValidator(HttpClient http, IOptions<UAuthResourceApiOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<SessionValidationResult> ValidateSessionAsync(SessionValidationContext context, CancellationToken ct = default)
    {
        SessionValidationResult Invalid(SessionState state = SessionState.Invalid)
            => SessionValidationResult.Invalid(state, sessionId: context.SessionId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/validate");

        var cookieName = _options.Hub.SessionCookieName;
        var sessionValue = context.SessionId.Value;

        if (string.IsNullOrWhiteSpace(cookieName) ||
            string.IsNullOrWhiteSpace(sessionValue) ||
            sessionValue.Any(c =>
                char.IsWhiteSpace(c) ||
                char.IsControl(c) ||
                c is ';' or ',' or '"' or '\\'))
        {
            return Invalid();
        }

        request.Headers.Add(
            "Cookie",
            $"{cookieName}={sessionValue}");

        if (context.Device.DeviceId is { } deviceId)
        {
            request.Headers.Add("X-UDID", deviceId.Value);
        }

        try
        {
            using var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
                return Invalid(SessionState.NotFound);

            using var stream =
                await response.Content.ReadAsStreamAsync(ct);

            using var document =
                await JsonDocument.ParseAsync(
                    stream,
                    cancellationToken: ct);

            var dto = document.Deserialize<AuthValidationResult>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (dto is null)
                return Invalid();

            if (dto.IsValid)
            {
                var hasUserStatus =
                    document.RootElement.ValueKind == JsonValueKind.Object &&
                    document.RootElement.TryGetProperty("snapshot", out var snapshot) &&
                    snapshot.ValueKind == JsonValueKind.Object &&
                    snapshot.TryGetProperty("identity", out var identity) &&
                    identity.ValueKind == JsonValueKind.Object &&
                    identity.TryGetProperty("userStatus", out var status) &&
                    status.ValueKind != JsonValueKind.Null &&
                    status.ValueKind != JsonValueKind.Undefined;

                if (!hasUserStatus ||
                    dto.Snapshot?.Identity is null ||
                    !Enum.IsDefined(dto.Snapshot.Identity.UserStatus) ||
                    dto.Snapshot.Identity.Tenant != context.Tenant)
                {
                    return Invalid();
                }
            }

            return SessionValidationMapper.ToDomain(
                dto,
                context.SessionId);
        }
        catch (HttpRequestException)
        {
            return Invalid();
        }
        catch (OperationCanceledException)
            when (!ct.IsCancellationRequested)
        {
            return Invalid();
        }
        catch (JsonException)
        {
            return Invalid();
        }
    }
}
