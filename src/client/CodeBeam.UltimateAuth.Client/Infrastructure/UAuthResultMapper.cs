using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Core.Contracts;
using System.Net;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal static class UAuthResultMapper
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static UAuthResult<T> FromJson<T>(UAuthTransportResult raw)
    {
        EnsureTransport(raw);

        if (!IsSuccessStatus(raw.Status))
        {
            var problem = CreateProblem(raw);

            return new UAuthResult<T>
            {
                IsSuccess = false,
                Status = raw.Status,
                Problem = problem,
                TraceId = problem?.TraceId
            };
        }

        if (raw.Body is null || raw.Body.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            throw new UAuthProtocolException($"Expected response body for {typeof(T).Name}, but received none.");
        }

        try
        {
            var value = raw.Body.Value.Deserialize<T>(_jsonOptions);

            if (value is null)
            {
                throw new UAuthProtocolException($"Response body for {typeof(T).Name} was null.");
            }

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

    public static UAuthResult From(UAuthTransportResult raw)
    {
        EnsureTransport(raw);

        if (IsSuccessStatus(raw.Status))
        {
            return new UAuthResult
            {
                IsSuccess = true,
                Status = raw.Status
            };
        }

        var problem = CreateProblem(raw);

        return new UAuthResult
        {
            IsSuccess = false,
            Status = raw.Status,
            Problem = problem,
            TraceId = problem?.TraceId
        };
    }

    private static bool IsSuccessStatus(int status) => status is >= 200 and < 300;

    private static void EnsureTransport(UAuthTransportResult raw)
    {
        if (raw.Status == 0)
            throw new UAuthTransportException("Network error.");

        if (raw.Status < 200 || raw.Status is >= 300 and < 400 || raw.Status >= 600)
            throw new UAuthProtocolException($"Unexpected HTTP status code: {raw.Status}");

        if (raw.Status >= 500)
            throw new UAuthTransportException($"Server error {raw.Status}", (HttpStatusCode)raw.Status);
    }

    private static UAuthProblem? CreateProblem(UAuthTransportResult raw)
    {
        var parsed = TryDeserializeProblem(raw);

        if (parsed is null)
            return null;

        return new UAuthProblem
        {
            Type = parsed?.Type,
            Title = string.IsNullOrWhiteSpace(parsed?.Title) ? "HTTP request failed" : parsed.Title,
            Detail = parsed?.Detail,
            Status = raw.Status,
            TraceId = parsed?.TraceId,
            Extensions = parsed?.Extensions
        };
    }

    private static UAuthProblem? TryDeserializeProblem(UAuthTransportResult raw)
    {
        if (raw.Body is null || raw.Body.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        try
        {
            var problem = raw.Body.Value.Deserialize<UAuthProblem>(_jsonOptions);

            if (problem is null)
                return null;

            if (string.IsNullOrWhiteSpace(problem.Type) && string.IsNullOrWhiteSpace(problem.Title) && string.IsNullOrWhiteSpace(problem.Detail))
            {
                return null;
            }

            return problem;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
