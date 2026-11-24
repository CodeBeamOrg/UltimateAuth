using System.Text.Json;
using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Http;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class UAuthLoginTransportEndpointTests
{
    [Theory]
    [InlineData(GrantDeliveryMode.Cookie, GrantDeliveryMode.None, true, true, false)]
    [InlineData(GrantDeliveryMode.Cookie, GrantDeliveryMode.Header, true, false, true)]
    [InlineData(GrantDeliveryMode.Header, GrantDeliveryMode.None, false, false, false)]
    [InlineData(GrantDeliveryMode.Header, GrantDeliveryMode.Header, false, false, true)]
    public void Get_ReportsEffectiveTransportCapabilities(GrantDeliveryMode session, GrantDeliveryMode access, bool supported, bool navigationSupported, bool accessTokenInHeader)
    {
        var response = new EffectiveAuthResponse(
            Delivery(GrantKind.Session, session),
            Delivery(GrantKind.AccessToken, access),
            EffectiveGrantDelivery.Disabled(GrantKind.RefreshToken),
            EffectiveRedirectResponse.Disabled);

        var flow = AuthFlowTestFactory.New(response: response);

        var accessor = new Mock<IAuthFlowContextAccessor>();
        accessor.SetupGet(x => x.Current).Returns(flow);

        var context = new DefaultHttpContext();

        var result = UAuthLoginTransportEndpoint.Get(context, accessor.Object);

        ((IStatusCodeHttpResult)result).StatusCode.Should().Be(StatusCodes.Status200OK);

        context.Response.Headers.CacheControl.ToString().Should().Be("no-store");

        var json = JsonSerializer.SerializeToElement(((IValueHttpResult)result).Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.GetProperty("supported").GetBoolean().Should().Be(supported);
        json.GetProperty("navigationSupported").GetBoolean().Should().Be(navigationSupported);
        json.GetProperty("accessTokenInHeader").GetBoolean().Should().Be(accessTokenInHeader);
    }

    private static EffectiveGrantDelivery Delivery(GrantKind kind,GrantDeliveryMode mode)
    {
        if (mode == GrantDeliveryMode.None)
            return EffectiveGrantDelivery.Disabled(kind);

        return new EffectiveGrantDelivery(
            kind,
            kind == GrantKind.AccessToken
                ? TokenFormat.Jwt
                : TokenFormat.Opaque,
            mode);
    }
}
