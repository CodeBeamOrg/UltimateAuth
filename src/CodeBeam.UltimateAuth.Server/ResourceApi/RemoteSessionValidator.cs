using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

// TODO: Resource API calls make two calls to here. Investigate.
internal sealed class RemoteSessionValidator : ISessionValidator
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RemoteSessionValidator(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<SessionValidationResult> ValidateSessionAsync(SessionValidationContext context, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/validate")
        {
            Content = JsonContent.Create(new
            {
                sessionId = context.SessionId.Value,
                tenant = context.Tenant.Value
            })
        };

        var httpContext = _httpContextAccessor.HttpContext!;

        if (httpContext.Request.Headers.TryGetValue("Cookie", out var cookie))
        {
            request.Headers.Add("Cookie", cookie.ToString());
        }

        if (context.Device.DeviceId is { } deviceId)
        {
            request.Headers.TryAddWithoutValidation("X-UDID", deviceId.Value);
        }

        var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
            return SessionValidationResult.Invalid(SessionState.NotFound, sessionId: context.SessionId);

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var dto = document.Deserialize<AuthValidationResult>(new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (dto is null)
        {
            return SessionValidationResult.Invalid(SessionState.NotFound, sessionId: context.SessionId);
        }

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

            if (!hasUserStatus || dto.Snapshot?.Identity is null || !Enum.IsDefined(dto.Snapshot.Identity.UserStatus))
            {
                return SessionValidationResult.Invalid(SessionState.Invalid, sessionId: context.SessionId);
            }
        }

        return SessionValidationMapper.ToDomain(dto, context.SessionId);
    }
}
