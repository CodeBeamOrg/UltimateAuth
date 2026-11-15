using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class DeviceTypeResolutionTests
{
    private const string TestDeviceId = "test-device-12345678901234567890";

    private static DeviceResolver CreateResolver() => new(new UAuthUserAgentParser());

    private static DefaultHttpContext CreateContext(string? userAgent = null)
    {
        var context = new DefaultHttpContext();

        context.Request.Headers[UAuthConstants.Headers.DeviceId] = TestDeviceId;

        if (userAgent is not null)
            context.Request.Headers.UserAgent = userAgent;

        return context;
    }

    private static void SetForm(DefaultHttpContext context, string deviceType)
    {
        var body =
            $"{UAuthConstants.Form.DeviceType}=" +
            Uri.EscapeDataString(deviceType);

        var bytes = Encoding.UTF8.GetBytes(body);

        context.Request.ContentType =
            "application/x-www-form-urlencoded";

        context.Request.ContentLength = bytes.Length;
        context.Request.Body = new MemoryStream(bytes);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64)", "desktop")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0)", "mobile")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 17_0)", "tablet")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Mobile)", "mobile")]
    public async Task Browser_should_use_web_type_and_preserve_platform(
        string userAgent,
        string expectedPlatform)
    {
        var context = CreateContext(userAgent);

        var result = await CreateResolver().ResolveAsync(context);

        Assert.Equal("web", result.DeviceType);
        Assert.Equal(expectedPlatform, result.Platform);
    }

    [Theory]
    [InlineData("mobile", "mobile")]
    [InlineData("MOBILE", "mobile")]
    [InlineData(" tablet ", "tablet")]
    [InlineData("desktop", "desktop")]
    [InlineData("kiosk", "kiosk")]
    public async Task Header_type_should_be_normalized(
        string input,
        string expected)
    {
        var context = CreateContext();

        context.Request.Headers[
            UAuthConstants.Headers.DeviceType] = input;

        var result = await CreateResolver().ResolveAsync(context);

        Assert.Equal(expected, result.DeviceType);
    }

    [Fact]
    public async Task Form_type_should_be_resolved()
    {
        var context = CreateContext();
        SetForm(context, "tablet");

        var result = await CreateResolver().ResolveAsync(context);

        Assert.Equal("tablet", result.DeviceType);
    }

    [Fact]
    public async Task Header_should_take_precedence_over_form()
    {
        var context = CreateContext();

        context.Request.Headers[
            UAuthConstants.Headers.DeviceType] = "mobile";

        SetForm(context, "tablet");

        var result = await CreateResolver().ResolveAsync(context);

        Assert.Equal("mobile", result.DeviceType);
    }

    [Fact]
    public async Task Empty_header_should_fallback_to_form()
    {
        var context = CreateContext();

        context.Request.Headers[
            UAuthConstants.Headers.DeviceType] = " ";

        SetForm(context, "tablet");

        var result = await CreateResolver().ResolveAsync(context);

        Assert.Equal("tablet", result.DeviceType);
    }

    [Fact]
    public async Task Missing_type_should_default_to_web()
    {
        var context = CreateContext();

        var result = await CreateResolver().ResolveAsync(context);

        Assert.Equal("web", result.DeviceType);
    }

    [Fact]
    public async Task Declared_type_should_not_override_platform()
    {
        var context = CreateContext(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        context.Request.Headers[
            UAuthConstants.Headers.DeviceType] = "mobile";

        var result = await CreateResolver().ResolveAsync(context);

        Assert.Equal("mobile", result.DeviceType);
        Assert.Equal("desktop", result.Platform);
        Assert.Equal("windows", result.OperatingSystem);
    }

    [Fact]
    public void Parser_should_continue_estimating_device_type()
    {
        var parser = new UAuthUserAgentParser();

        var result = parser.Parse(
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0)");

        Assert.Equal("mobile", result.DeviceType);
        Assert.Equal("mobile", result.Platform);
    }
}
