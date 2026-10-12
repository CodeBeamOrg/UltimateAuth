using CodeBeam.UltimateAuth.Users;
using Microsoft.Extensions.Hosting;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Infrastructure;

// TODO: Replace this demo bypass with a real verification challenge flow.
// Never register this implementation in production.
internal sealed class DevelopmentBypassUserIdentifierVerifier(IHostEnvironment environment) : IUserIdentifierVerifier
{
    public const string DemoProof = "demo-verification-bypass";

    public Task<bool> VerifyAsync(UserIdentifierVerificationContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("Development verification bypass cannot run outside Development.");
        }

        return Task.FromResult(string.Equals(context.Proof, DemoProof, StringComparison.Ordinal));
    }
}
