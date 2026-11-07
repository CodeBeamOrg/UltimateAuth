using System.Globalization;
using System.Net;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal static class UAuthRateLimit
{
    internal static TimeSpan? ParseRetryAfter(string? header, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(header)) return null;
        header = header.Trim();
        if (long.TryParse(header, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
            return seconds <= TimeSpan.MaxValue.Ticks / TimeSpan.TicksPerSecond ? TimeSpan.FromTicks(seconds * TimeSpan.TicksPerSecond) : null;
        if (DateTimeOffset.TryParseExact(header, "r", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            return date > now ? date - now : TimeSpan.Zero;
        return null;
    }

    internal static DateTimeOffset NotBefore(DateTimeOffset now, TimeSpan? retryAfter)
    {
        var delay = retryAfter ?? TimeSpan.FromSeconds(30);
        if (delay < TimeSpan.FromSeconds(1)) delay = TimeSpan.FromSeconds(1);
        return delay.Ticks > DateTimeOffset.MaxValue.Ticks - now.UtcTicks ? DateTimeOffset.MaxValue : now.Add(delay);
    }

    internal static UAuthTransportException Exception(string? header) => new("Temporarily rate limited.", HttpStatusCode.TooManyRequests)
    { RetryAfter = ParseRetryAfter(header, DateTimeOffset.UtcNow) };

    internal static UAuthProblem Problem(string? header) => new()
    { Status = 429, Title = "Too many requests", Detail = "rate_limited", RetryAfter = ParseRetryAfter(header, DateTimeOffset.UtcNow) };
}
