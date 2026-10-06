using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class SessionContextAccessorTests
{
    [Fact]
    public void Current_WhenHttpContextDoesNotExist_ShouldReturnNull()
    {
        var httpContextAccessor =
            new Mock<IHttpContextAccessor>();

        httpContextAccessor
            .SetupGet(x => x.HttpContext)
            .Returns((HttpContext?)null);

        var sut =
            new SessionContextAccessor(
                httpContextAccessor.Object);

        var result = sut.Current;

        result.Should().BeNull();
    }

    [Fact]
    public void Current_WhenSessionContextExists_ShouldReturnSameInstance()
    {
        var httpContext = new DefaultHttpContext();

        var sessionContext =
            SessionContext.Anonymous();

        httpContext.Items[
            UAuthConstants.HttpItems.SessionContext
        ] = sessionContext;

        var httpContextAccessor =
            new Mock<IHttpContextAccessor>();

        httpContextAccessor
            .SetupGet(x => x.HttpContext)
            .Returns(httpContext);

        var sut =
            new SessionContextAccessor(
                httpContextAccessor.Object);

        var result = sut.Current;

        result.Should().BeSameAs(sessionContext);
    }

    [Fact]
    public void Current_WhenSessionContextDoesNotExist_ShouldReturnNull()
    {
        var httpContext = new DefaultHttpContext();

        var httpContextAccessor =
            new Mock<IHttpContextAccessor>();

        httpContextAccessor
            .SetupGet(x => x.HttpContext)
            .Returns(httpContext);

        var sut =
            new SessionContextAccessor(
                httpContextAccessor.Object);

        var result = sut.Current;

        result.Should().BeNull();
    }

    [Fact]
    public void Current_WhenStoredValueHasWrongType_ShouldReturnNull()
    {
        var httpContext = new DefaultHttpContext();

        httpContext.Items[
            UAuthConstants.HttpItems.SessionContext
        ] = "not-a-session-context";

        var httpContextAccessor =
            new Mock<IHttpContextAccessor>();

        httpContextAccessor
            .SetupGet(x => x.HttpContext)
            .Returns(httpContext);

        var sut =
            new SessionContextAccessor(
                httpContextAccessor.Object);

        var result = sut.Current;

        result.Should().BeNull();
    }
}
