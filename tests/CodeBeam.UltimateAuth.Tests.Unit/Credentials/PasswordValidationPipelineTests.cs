using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Credentials.Policies;
using CodeBeam.UltimateAuth.Credentials.Reference;
using Xunit;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class PasswordValidationPipelineTests
{
    private sealed class AcceptAllStrengthPolicy : IPasswordStrengthPolicy
    {
        public int CallCount { get; private set; }

        public Task<PasswordPolicyResult> ValidateAsync(
            PasswordPolicyContext context,
            EffectivePasswordPolicyOptions options,
            CancellationToken ct = default)
        {
            CallCount++;

            return Task.FromResult(PasswordPolicyResult.Success());
        }
    }

    private sealed class RecordingHistoryValidator : IPasswordHistoryValidator
    {
        public int CallCount { get; private set; }

        public CredentialKey? LastCredentialKey { get; private set; }

        public string? LastPassword { get; private set; }

        public int? LastRememberLast { get; private set; }

        public PasswordPolicyResult Result { get; set; }
            = PasswordPolicyResult.Success();

        public Task<PasswordPolicyResult> ValidateAsync(
            CredentialKey credentialKey,
            string password,
            int rememberLast,
            CancellationToken ct = default)
        {
            CallCount++;
            LastCredentialKey = credentialKey;
            LastPassword = password;
            LastRememberLast = rememberLast;

            return Task.FromResult(Result);
        }
    }

    private static EffectivePasswordPolicyOptions CreateOptions(
        int rememberLast = 0)
    {
        return PasswordPolicyOptionsResolver.Resolve(
            new PasswordPolicyOptions
            {
                Level = PasswordPolicyLevel.Standard,
                History = new PasswordHistoryOptions
                {
                    RememberLast = rememberLast
                }
            });
    }

    private static DefaultPasswordValidationPipeline CreatePipeline(
        RecordingHistoryValidator history,
        AcceptAllStrengthPolicy? strength = null)
    {
        return new DefaultPasswordValidationPipeline(
            strength ?? new AcceptAllStrengthPolicy(),
            history);
    }

    private static PasswordValidationRequest CreateRequest(
        string password,
        CredentialKey? existingCredentialKey = null)
    {
        return new PasswordValidationRequest
        {
            Context = new PasswordPolicyContext
            {
                Password = password
            },
            ExistingCredentialKey = existingCredentialKey
        };
    }

    [Fact]
    public async Task AbsoluteLimit_CannotBeBypassedByCustomStrengthPolicy()
    {
        var history = new RecordingHistoryValidator();
        var strength = new AcceptAllStrengthPolicy();
        var pipeline = CreatePipeline(history, strength);

        var result = await pipeline.ValidateAsync(
            CreateRequest(new string('a', 257)),
            CreateOptions());

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Violations,
            x => x.Code == PasswordPolicyErrorCodes.InputTooLong);

        Assert.Equal(0, strength.CallCount);
        Assert.Equal(0, history.CallCount);
    }

    [Fact]
    public async Task NewCredential_DoesNotRequireHistory()
    {
        var history = new RecordingHistoryValidator();
        var strength = new AcceptAllStrengthPolicy();
        var pipeline = CreatePipeline(history, strength);

        var result = await pipeline.ValidateAsync(
            CreateRequest("NewPassword123!"),
            CreateOptions(rememberLast: 5));

        Assert.True(result.Succeeded);
        Assert.Equal(1, strength.CallCount);
        Assert.Equal(0, history.CallCount);
    }

    [Fact]
    public async Task ExistingCredential_AlwaysValidatesHistory_WhenRememberLastIsZero()
    {
        var history = new RecordingHistoryValidator
        {
            Result = PasswordPolicyResult.Failure(
                new PasswordPolicyViolation(
                    PasswordPolicyErrorCodes.ReusedPassword,
                    "Password matches the current password."))
        };

        var pipeline = CreatePipeline(history);
        var key = new CredentialKey(TenantKey.Single, Guid.NewGuid());

        var result = await pipeline.ValidateAsync(
            CreateRequest("CurrentPassword123!", key),
            CreateOptions(rememberLast: 0));

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Violations,
            x => x.Code == PasswordPolicyErrorCodes.ReusedPassword);

        Assert.Equal(1, history.CallCount);
        Assert.Equal(0, history.LastRememberLast);
        Assert.Equal(key, history.LastCredentialKey);
    }

    [Fact]
    public async Task ExistingCredential_PassesRememberLastToHistoryValidator()
    {
        var history = new RecordingHistoryValidator();
        var pipeline = CreatePipeline(history);
        var key = new CredentialKey(TenantKey.Single, Guid.NewGuid());

        var result = await pipeline.ValidateAsync(
            CreateRequest("NewPassword123!", key),
            CreateOptions(rememberLast: 5));

        Assert.True(result.Succeeded);
        Assert.Equal(1, history.CallCount);
        Assert.Equal(key, history.LastCredentialKey);
        Assert.Equal("NewPassword123!", history.LastPassword);
        Assert.Equal(5, history.LastRememberLast);
    }

    [Fact]
    public async Task ExistingCredential_PropagatesHistoryValidationFailure()
    {
        var history = new RecordingHistoryValidator
        {
            Result = PasswordPolicyResult.Failure(
                new PasswordPolicyViolation(
                    PasswordPolicyErrorCodes.ReusedPassword,
                    "Password was used previously."))
        };

        var pipeline = CreatePipeline(history);
        var key = new CredentialKey(TenantKey.Single, Guid.NewGuid());

        var result = await pipeline.ValidateAsync(
            CreateRequest("PreviouslyUsed123!", key),
            CreateOptions(rememberLast: 3));

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Violations,
            x => x.Code == PasswordPolicyErrorCodes.ReusedPassword);

        Assert.Equal(1, history.CallCount);
        Assert.Equal(3, history.LastRememberLast);
    }
}
