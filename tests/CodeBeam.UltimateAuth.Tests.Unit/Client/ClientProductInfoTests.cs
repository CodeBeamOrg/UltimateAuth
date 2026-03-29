using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Runtime;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ClientProductInfoTests
{
    [Fact]
    public void ProductInfo_Should_Be_Created_With_Valid_Data()
    {
        var options = Options.Create(new UAuthClientOptions
        {
            ClientProfile = UAuthClientProfile.BlazorServer,
            AutoRefresh = new UAuthClientAutoRefreshOptions
            {
                Enabled = true,
                Interval = TimeSpan.FromMinutes(5)
            },
            Reauth = new UAuthClientReauthOptions
            {
                Behavior = ReauthBehavior.RaiseEvent
            }
        });

        var provider = new UAuthClientProductInfoProvider(options);

        var info = provider.Get();

        info.Should().NotBeNull();
        info.ProductName.Should().Be("UltimateAuth Client");
        info.ClientProfile.Should().Be(UAuthClientProfile.BlazorServer);
        info.AutoRefreshEnabled.Should().BeTrue();
        info.RefreshInterval.Should().Be(TimeSpan.FromMinutes(5));
        info.ReauthBehavior.Should().Be(ReauthBehavior.RaiseEvent);
    }

    [Fact]
    public void ProductInfo_Should_Set_StartedAt()
    {
        var options = Options.Create(new UAuthClientOptions());
        var before = DateTimeOffset.UtcNow;
        var provider = new UAuthClientProductInfoProvider(options);
        var info = provider.Get();
        var after = DateTimeOffset.UtcNow;

        info.StartedAt.Should().BeAfter(before.AddSeconds(-1));
        info.StartedAt.Should().BeBefore(after.AddSeconds(1));
    }

    [Fact]
    public void ProductInfo_Should_Return_Same_Instance()
    {
        var options = Options.Create(new UAuthClientOptions());
        var provider = new UAuthClientProductInfoProvider(options);
        var info1 = provider.Get();
        var info2 = provider.Get();

        info1.Should().BeSameAs(info2);
    }

    [Fact]
    public void ProductInfo_Should_Have_Version()
    {
        var options = Options.Create(new UAuthClientOptions());
        var provider = new UAuthClientProductInfoProvider(options);
        var info = provider.Get();
        info.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ProductInfo_Should_Have_FrameworkDescription()
    {
        var options = Options.Create(new UAuthClientOptions());
        var provider = new UAuthClientProductInfoProvider(options);
        var info = provider.Get();
        info.FrameworkDescription.Should().NotBeNullOrWhiteSpace();
    }
}
