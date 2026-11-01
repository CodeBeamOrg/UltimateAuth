using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Credentials.Policies;
using CodeBeam.UltimateAuth.Credentials.Reference;

public sealed class PasswordValidationPipelineTests
{
    private sealed class AcceptAllStrengthPolicy : IPasswordStrengthPolicy
    {
        public Task<PasswordPolicyResult> ValidateAsync(PasswordPolicyContext context, EffectivePasswordPolicyOptions options, CancellationToken ct = default)
        {
            return Task.FromResult(PasswordPolicyResult.Success());
        }
    }

    private sealed class RecordingHistoryValidator : IPasswordHistoryValidator
    {
        public int CallCount { get; private set; }

        public Task<PasswordPolicyResult> ValidateAsync(CredentialKey credentialKey, string password, int rememberLast, CancellationToken ct = default)
        {
            CallCount++;

            return Task.FromResult(PasswordPolicyResult.Failure(new PasswordPolicyViolation(PasswordPolicyErrorCodes.ReusedPassword, "Password was used previously.")));
        }
    }

    private static DefaultPasswordValidationPipeline CreatePipeline(RecordingHistoryValidator history)
    {
        return new DefaultPasswordValidationPipeline(new AcceptAllStrengthPolicy(), history, PasswordPolicyOptionsResolver.Resolve(new()));
    }

    [Fact]
    public async Task AbsoluteLimit_CannotBeBypassedByCustomStrengthPolicy()
    {
        var history = new RecordingHistoryValidator();
        var pipeline = CreatePipeline(history);

        var result = await pipeline.ValidateAsync(
            new PasswordValidationRequest
            {
                Context = new PasswordPolicyContext
                {
                    Password = new string('a', 257)
                }
            });

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Violations,
            x => x.Code == PasswordPolicyErrorCodes.InputTooLong);

        Assert.Equal(0, history.CallCount);
    }

    [Fact]
    public async Task NewCredential_DoesNotRequireHistory()
    {
        var history = new RecordingHistoryValidator();
        var pipeline = CreatePipeline(history);

        var result = await pipeline.ValidateAsync(
            new PasswordValidationRequest
            {
                Context = new PasswordPolicyContext
                {
                    Password = "NewPassword123!"
                }
            });

        Assert.True(result.Succeeded);
        Assert.Equal(0, history.CallCount);
    }
}
