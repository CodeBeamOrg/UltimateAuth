using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Core.Contracts;
using System.Net;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal static class UAuthResultMapper
{
    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public static UAuthResult<T> FromJson<T>(UAuthTransportResult raw)
    {
        EnsureTransport(raw);

        if (raw.Status >= 400 && raw.Status < 500)
        {
            var problem = TryDeserializeProblem(raw);

            return new UAuthResult<T>
            {
                IsSuccess = false,
                Status = raw.Status,
                Problem = problem
            };
        }

        if (raw.Body is null)
        {
            return new UAuthResult<T>
            {
                IsSuccess = true,
                Status = raw.Status,
                Value = default
            };
        }

        try
        {
            var value = raw.Body.Value.Deserialize<T>(_jsonOptions);

            return new UAuthResult<T>
            {
                IsSuccess = true,
                Status = raw.Status,
                Value = value
            };
        }
        catch (JsonException ex)
        {
            throw new UAuthProtocolException("Invalid response format.", ex);
        }
    }

    public static UAuthResult From(UAuthTransportResult raw) => FromJson<object>(raw);

    private static void EnsureTransport(UAuthTransportResult raw)
    {
        if (raw.Status == 0)
            throw new UAuthTransportException("Network error.");

        if (raw.Status >= 500)
            throw new UAuthTransportException($"Server error {raw.Status}", (HttpStatusCode)raw.Status);
    }

    private static UAuthProblem? TryDeserializeProblem(UAuthTransportResult raw)
    {
        if (raw.Body is null)
            return null;

        try
        {
            return raw.Body.Value.Deserialize<UAuthProblem>(_jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
