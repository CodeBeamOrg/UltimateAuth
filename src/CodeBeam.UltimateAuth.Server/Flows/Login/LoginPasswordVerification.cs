using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;

namespace CodeBeam.UltimateAuth.Server.Flows;

internal sealed class LoginPasswordVerification(
    IUAuthPasswordHasher hasher,
    LoginPasswordVerificationState state,
    IUAuthPasswordHashLimiter limiter)
{
    public Task<bool> VerifyCredentialAsync(ICredentialProvider provider, ICredential credential, string secret, CancellationToken ct)
    {
        return limiter.ExecuteAsync(UAuthPasswordHashCategory.Login,
            () => provider.ValidateAsync(credential, secret, ct), ct);
    }

    public Task VerifyDummyAsync(string secret, CancellationToken ct)
    {
        return limiter.ExecuteAsync(
            UAuthPasswordHashCategory.Login,
            () =>
            {
                var dummyHash = state.GetDummyHash(hasher);

                // Dummy verification must never authorize login.
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
