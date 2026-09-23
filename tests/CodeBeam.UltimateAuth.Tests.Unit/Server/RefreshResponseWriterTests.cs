using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class RefreshResponseWriterTests
{
    [Theory]
    [InlineData(RefreshOutcome.NoOp, "no-op")]
    [InlineData(RefreshOutcome.Touched, "touched")]
    [InlineData(RefreshOutcome.Rotated, "rotated")]
    [InlineData(RefreshOutcome.ReauthRequired, "reauth-required")]
    [InlineData(RefreshOutcome.Success, "success")]
    public void Write_WhenRefreshDetailsEnabled_WritesExpectedOutcome(RefreshOutcome outcome, string expected)
    {
        var options = Options.Create(new UAuthServerOptions
        {
            Diagnostics = { EnableRefreshDetails = true }
        });

        var writer = new RefreshResponseWriter(options);
        var context = new DefaultHttpContext();

        writer.Write(context, outcome);

        Assert.Equal(
            expected,
            context.Response.Headers[UAuthConstants.Headers.Refresh]);
    }

    [Fact]
    public void Write_WhenRefreshDetailsDisabled_DoesNotWriteHeader()
    {
        var options = Options.Create(new UAuthServerOptions
        {
            Diagnostics =
        {
            EnableRefreshDetails = false
        }
        });

        var writer = new RefreshResponseWriter(options);
        var context = new DefaultHttpContext();

        writer.Write(context, RefreshOutcome.Rotated);

        Assert.False(context.Response.Headers.ContainsKey(UAuthConstants.Headers.Refresh));
    }

    [Fact]
    public void Write_WhenOutcomeIsUnknown_WritesUnknown()
    {
        var options = Options.Create(new UAuthServerOptions
        {
            Diagnostics =
        {
            EnableRefreshDetails = true
        }
        });

        var writer = new RefreshResponseWriter(options);
        var context = new DefaultHttpContext();

        writer.Write(context, (RefreshOutcome)int.MaxValue);

        Assert.Equal("unknown", context.Response.Headers[UAuthConstants.Headers.Refresh]);
    }
}
