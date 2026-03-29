using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

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

        var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
            return SessionValidationResult.Invalid(SessionState.NotFound, sessionId: context.SessionId);

        var dto = await response.Content.ReadFromJsonAsync<SessionValidationInfo>(cancellationToken: ct);

        if (dto is null)
            return SessionValidationResult.Invalid(SessionState.NotFound, sessionId: context.SessionId);

        return SessionValidationMapper.ToDomain(dto);
    }
}
