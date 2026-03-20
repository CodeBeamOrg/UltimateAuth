using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal sealed class NoOpSessionValidator : ISessionValidator
{
    public Task ValidateSesAsync(SessionValidationContext context, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<SessionValidationResult> ValidateSessionAsync(SessionValidationContext context, CancellationToken ct = default)
    {
        throw new NotSupportedException();
    }
}
