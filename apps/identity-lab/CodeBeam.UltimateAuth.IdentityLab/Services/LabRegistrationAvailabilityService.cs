using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Users.Contracts;
using System.Threading.RateLimiting;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabRegistrationAvailabilityService(LabAccessContextProvider contextProvider, IUserIdentifierAvailabilityService availabilityService) : IDisposable
{
    private static readonly FixedWindowRateLimiter GlobalLimiter = new(
        new FixedWindowRateLimiterOptions
        {
            PermitLimit = 300,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });

    private readonly FixedWindowRateLimiter _circuitLimiter = new(
        new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });

    public async Task<LabIdentifierAvailability> CheckAsync(UserIdentifierType type, string value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (type is not (UserIdentifierType.Username or UserIdentifierType.Email))
            return LabIdentifierAvailability.Invalid;

        if (string.IsNullOrWhiteSpace(value))
            return LabIdentifierAvailability.Invalid;

        value = value.Trim();

        // Bound the request before reaching normalization and persistence.
        if (value.Length > 256)
            return LabIdentifierAvailability.Invalid;

        using var circuitLease = _circuitLimiter.AttemptAcquire();
        if (!circuitLease.IsAcquired)
            return LabIdentifierAvailability.Unavailable;

        using var globalLease = GlobalLimiter.AttemptAcquire();
        if (!globalLease.IsAcquired)
            return LabIdentifierAvailability.Unavailable;

        try
        {
            var context = await contextProvider.CreateAsync(
                UAuthActions.UserIdentifiers.CheckAvailability,
                "users",
                ct);

            // This is a server-side registration-only call.
            // It does not expose the general UltimateAuth endpoint.
            var result = await availabilityService.CheckAsync(
                context,
                new CheckUserIdentifierAvailabilityRequest
                {
                    Type = type,
                    Value = value
                },
                ct);

            if (!result.IsValid)
                return LabIdentifierAvailability.Invalid;

            return result.IsAvailable
                ? LabIdentifierAvailability.Available
                : LabIdentifierAvailability.Taken;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Never report "Available" when the check failed.
            return LabIdentifierAvailability.Unavailable;
        }
    }

    public void Dispose()
    {
        _circuitLimiter.Dispose();
    }
}

public enum LabIdentifierAvailability
{
    Unknown,
    Checking,
    Available,
    Taken,
    Invalid,
    Unavailable
}
