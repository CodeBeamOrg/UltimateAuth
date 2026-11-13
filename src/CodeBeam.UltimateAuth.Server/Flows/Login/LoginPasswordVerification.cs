using System.Security.Cryptography;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;

namespace CodeBeam.UltimateAuth.Server.Flows;

internal sealed class LoginPasswordVerificationState
{
    private readonly object _sync = new();
    private PasswordHash? _dummyHash;

    internal PasswordHash GetDummyHash(IUAuthPasswordHasher hasher)
    {
        lock (_sync)
        {
            if (_dummyHash is { } existing)
                return existing;

            var dummySecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            var hash = hasher.Hash(dummySecret);

            _dummyHash = hash;
            return hash;
        }
    }
}

internal sealed class LoginPasswordVerification(IUAuthPasswordHasher hasher, LoginPasswordVerificationState state, UAuthPasswordHashLimiter limiter)
{
    public Task<bool> VerifyCredentialAsync(ICredentialProvider provider, ICredential credential, string secret, CancellationToken ct)
    {
        return limiter.ExecuteAsync(() => provider.ValidateAsync(credential, secret, ct), ct);
    }

    public Task VerifyDummyAsync(string secret, CancellationToken ct)
    {
        return limiter.ExecuteAsync(
            () =>
            {
                var dummyHash = state.GetDummyHash(hasher);

                // Never use the result to authorize login.
                _ = hasher.Verify(dummyHash, secret);
            },
            ct);
    }
}

internal sealed class PasswordVerificationCapacityException()
    : UAuthRuntimeException("password_verification_busy", "Password verification capacity is temporarily exhausted.")
{
    public override int StatusCode => 429;
    public override string Title => "Too many requests.";
}
