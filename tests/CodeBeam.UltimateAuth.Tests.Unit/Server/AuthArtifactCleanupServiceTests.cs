using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Stores;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class AuthArtifactCleanupServiceTests
{
    [Fact]
    public async Task HostedService_RemovesExpiredArtifactsWithoutAccess()
    {
        var now = new DateTimeOffset(
            2026, 11, 13, 12, 0, 0, TimeSpan.Zero);

        var clock = new TestClock(now);

        var builder = Host.CreateApplicationBuilder();

        // Must be registered before AddUltimateAuthServer, which uses TryAddSingleton<IClock>.
        builder.Services.AddSingleton<IClock>(clock);

        builder.Services.AddUltimateAuthServer(options =>
        {
            options.AuthArtifacts.MaxEntries = 100;
            options.AuthArtifacts.CleanupInterval = TimeSpan.FromSeconds(1);
        });

        using var host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        var store = Assert.IsType<InMemoryAuthStore>(host.Services.GetRequiredService<IAuthStore>());

        var flow = AuthFlowTestFactory.LoginSuccess();
        var key = new AuthArtifactKey("unread-expired");

        var artifact = new LoginPreviewArtifact(
            flow.Tenant,
            UserKey.New(),
            CredentialType.Password,
            flow.Device.DeviceId!.Value.Value,
            "alice",
            flow.ClientProfile,
            "fingerprint",
            now.AddMinutes(1));

        await store.StoreAsync(key, artifact, TestContext.Current.CancellationToken);

        Assert.Equal(1, store.Count);

        // Expire the artifact without accessing its key.
        clock.UtcNow = now.AddMinutes(2);

        var timeout = TimeSpan.FromSeconds(5);
        var stopwatch = Stopwatch.StartNew();

        while (store.Count != 0 && stopwatch.Elapsed < timeout)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        Assert.Equal(0, store.Count);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    private sealed class TestClock(DateTimeOffset now) : IClock
    {
        private long _ticks = now.UtcDateTime.Ticks;

        public DateTimeOffset UtcNow
        {
            get => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);

            set => Interlocked.Exchange(ref _ticks, value.UtcDateTime.Ticks);
        }
    }
}
