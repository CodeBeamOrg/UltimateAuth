using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class HttpContextSessionExtensionsTests
{
    [Fact]
    public void GetSessionContext_WhenSessionContextExists_ShouldReturnSameInstance()
    {
        var httpContext =
            new DefaultHttpContext();

        var sessionId =
            CreateSessionId(
                "session-context-000000000000000000000001");

        var tenant =
            TenantKey.FromExternal("tenant-a");

        var sessionContext =
            SessionContext.FromSessionId(
                sessionId,
                tenant);

        httpContext.Items[
            UAuthConstants.HttpItems.SessionContext
        ] = sessionContext;

        var result =
            httpContext.GetSessionContext();

        result.Should()
            .BeSameAs(sessionContext);

        result.IsAnonymous.Should()
            .BeFalse();

        result.SessionId.Should()
            .Be(sessionId);

        result.Tenant.Should()
            .Be(tenant);
    }

    [Fact]
    public void GetSessionContext_WhenSessionContextDoesNotExist_ShouldReturnAnonymousContext()
    {
        var httpContext =
            new DefaultHttpContext();

        var result =
            httpContext.GetSessionContext();

        result.Should()
            .NotBeNull();

        result.IsAnonymous.Should()
            .BeTrue();

        result.SessionId.Should()
            .BeNull();

        result.Tenant.Should()
            .BeNull();
    }

    [Fact]
    public void GetSessionContext_WhenStoredValueHasWrongType_ShouldReturnAnonymousContext()
    {
        var httpContext =
            new DefaultHttpContext();

        httpContext.Items[
            UAuthConstants.HttpItems.SessionContext
        ] = "not-a-session-context";

        var result =
            httpContext.GetSessionContext();

        result.Should()
            .NotBeNull();

        result.IsAnonymous.Should()
            .BeTrue();

        result.SessionId.Should()
            .BeNull();

        result.Tenant.Should()
            .BeNull();
    }

    private static AuthSessionId CreateSessionId(
        string value)
    {
        AuthSessionId.TryCreate(
                value,
                out var sessionId)
            .Should()
            .BeTrue();

        return sessionId;
    }
}
