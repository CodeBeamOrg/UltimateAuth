using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Server.Stores;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class PkceTests
{
    [Fact]
    public void Pkce_Should_Succeed_With_Valid_Data()
    {
        var validator = new PkceAuthorizationValidator();
        var (artifact, verifier) = TestPkceFactory.Create();
        var result = validator.Validate(artifact, verifier, artifact.Context, DateTimeOffset.UtcNow);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Pkce_Should_Fail_With_Invalid_Verifier()
    {
        var validator = new PkceAuthorizationValidator();
        var (artifact, _) = TestPkceFactory.Create();
        var result = validator.Validate(artifact, "wrong_verifier", artifact.Context, DateTimeOffset.UtcNow);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be(PkceValidationFailureReason.InvalidVerifier);
    }

    [Fact]
    public void Pkce_Should_Fail_On_Device_Mismatch()
    {
        var validator = new PkceAuthorizationValidator();
        var (artifact, verifier) = TestPkceFactory.Create();

        var wrongContext = new PkceContextSnapshot(
            artifact.Context.ClientProfile,
            artifact.Context.Tenant,
            artifact.Context.RedirectUri,
            device: TestDevice.Alternative()
        );

        var result = validator.Validate(artifact, verifier, wrongContext, DateTimeOffset.UtcNow);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be(PkceValidationFailureReason.ContextMismatch);
    }

    [Fact]
    public async Task Refresh_Should_Generate_New_AuthorizationCode()
    {
        var runtime = new TestAuthRuntime<string>();

        using var scope = runtime.Services.CreateScope();

        var store = scope.ServiceProvider.GetRequiredService<IAuthStore>();
        var pkceService = scope.ServiceProvider.GetRequiredService<IPkceService>();

        var (artifact, _) = TestPkceFactory.Create();

        await store.StoreAsync(artifact.AuthorizationCode, artifact);

        var hub = TestHubFactory.Create(artifact);

        var refreshed = await pkceService.RefreshAsync(hub);

        refreshed.AuthorizationCode.Should().NotBe(artifact.AuthorizationCode.Value);
    }

    [Fact]
    public async Task Refresh_Should_Store_New_PkceArtifact()
    {
        var runtime = new TestAuthRuntime<string>();

        using var scope = runtime.Services.CreateScope();

        var store = scope.ServiceProvider.GetRequiredService<IAuthStore>();
        var pkceService = scope.ServiceProvider.GetRequiredService<IPkceService>();

        var (artifact, _) = TestPkceFactory.Create();

        await store.StoreAsync(artifact.AuthorizationCode, artifact);

        var hub = TestHubFactory.Create(artifact);

        var refreshed = await pkceService.RefreshAsync(hub);

        var stored = await store.GetAsync(new AuthArtifactKey(refreshed.AuthorizationCode));

        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task Refresh_Should_Invalidate_Old_Code()
    {
        var runtime = new TestAuthRuntime<string>();
        using var scope = runtime.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAuthStore>();
        var pkceService = scope.ServiceProvider.GetRequiredService<IPkceService>();

        var (artifact, _) = TestPkceFactory.Create();
        await store.StoreAsync(artifact.AuthorizationCode, artifact);
        var hub = TestHubFactory.Create(artifact);
        var refreshed = await pkceService.RefreshAsync(hub);
        var old = await store.GetAsync(artifact.AuthorizationCode);

        old.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_Should_Preserve_Context()
    {
        var runtime = new TestAuthRuntime<string>();
        using var scope = runtime.Services.CreateScope();
        var pkceService = scope.ServiceProvider.GetRequiredService<IPkceService>();

        var (artifact, _) = TestPkceFactory.Create();
        var hub = TestHubFactory.Create(artifact);
        var refreshed = await pkceService.RefreshAsync(hub);

        var store = scope.ServiceProvider.GetRequiredService<IAuthStore>();
        var newArtifact = await store.GetAsync(new AuthArtifactKey(refreshed.AuthorizationCode)) as PkceAuthorizationArtifact;

        newArtifact!.Context.Device.Should().BeEquivalentTo(artifact.Context.Device);
    }
}
