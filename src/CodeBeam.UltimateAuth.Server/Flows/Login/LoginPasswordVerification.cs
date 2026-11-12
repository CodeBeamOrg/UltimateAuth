using System.Security.Cryptography;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Credentials;

namespace CodeBeam.UltimateAuth.Server.Flows;

internal sealed class LoginPasswordVerificationState : IDisposable
{
    // Conservative initial limit for memory-hard hashers.
    // Shared by real and dummy verifications.
    internal const int MaxConcurrentVerifications = 2;

    internal readonly SemaphoreSlim Semaphore = new(MaxConcurrentVerifications, MaxConcurrentVerifications);

    private readonly object _sync = new();
    private PasswordHash? _dummyHash;

    internal PasswordHash GetDummyHash(IUAuthPasswordHasher hasher)
    {
        lock (_sync)
        {
            if (_dummyHash is { } existing)
                return existing;

            // A random password, never exposed and never used to log in.
            // Use the configured hasher and its current cost options.
            var dummySecret = Convert.ToHexString(
                RandomNumberGenerator.GetBytes(32));

            var hash = hasher.Hash(dummySecret);

            _dummyHash = hash;
            return hash;
        }
    }

    public void Dispose() => Semaphore.Dispose();
}

internal sealed class LoginPasswordVerification(IUAuthPasswordHasher hasher, LoginPasswordVerificationState state)
{
    public async Task<bool> VerifyCredentialAsync(ICredentialProvider provider, ICredential credential, string secret, CancellationToken ct)
    {
        if (!await state.Semaphore.WaitAsync(0, ct))
            throw new PasswordVerificationCapacityException();

        try
        {
            return await provider.ValidateAsync(credential, secret, ct);
        }
        finally
        {
            state.Semaphore.Release();
        }
    }

    public async Task VerifyDummyAsync(string secret, CancellationToken ct)
    {
        if (!await state.Semaphore.WaitAsync(0, ct))
            throw new PasswordVerificationCapacityException();

        try
        {
            var dummyHash = state.GetDummyHash(hasher);

            // Perform the actual hash verification.
            // Its result is deliberately ignored.
            _ = hasher.Verify(dummyHash, secret);
        }
        finally
        {
            state.Semaphore.Release();
        }
    }
}

internal sealed class PasswordVerificationCapacityException() : UAuthRuntimeException(
        "password_verification_busy",
        "Password verification capacity is temporarily exhausted.")
{
    public override int StatusCode => 429;

    public override string Title => "Too many requests.";
}
