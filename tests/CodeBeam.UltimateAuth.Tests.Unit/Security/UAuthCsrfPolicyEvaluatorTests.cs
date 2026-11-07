using CodeBeam.UltimateAuth.Server.Security;

namespace CodeBeam.UltimateAuth.Tests.Unit.Security;

public sealed class UAuthCsrfPolicyEvaluatorTests
{
    [Theory]
    [InlineData(UAuthCsrfOperation.Login)]
    [InlineData(UAuthCsrfOperation.PkceCompletion)]
    [InlineData(UAuthCsrfOperation.AuthenticatedMutation)]
    public void CookieIssuance_RequiresProtection(UAuthCsrfOperation operation)
    {
        var result = UAuthCsrfPolicyEvaluator.RequiresProtection(
            operation,
            canIssueCookie: true,
            canAcceptCookie: false);

        Assert.True(result);
    }

    [Theory]
    [InlineData(UAuthCsrfOperation.Login)]
    [InlineData(UAuthCsrfOperation.PkceCompletion)]
    [InlineData(UAuthCsrfOperation.AuthenticatedMutation)]
    public void CookieAcceptance_RequiresProtection(UAuthCsrfOperation operation)
    {
        var result = UAuthCsrfPolicyEvaluator.RequiresProtection(
            operation,
            canIssueCookie: false,
            canAcceptCookie: true);

        Assert.True(result);
    }

    [Theory]
    [InlineData(UAuthCsrfOperation.Login)]
    [InlineData(UAuthCsrfOperation.PkceCompletion)]
    [InlineData(UAuthCsrfOperation.AuthenticatedMutation)]
    public void HeaderOnlyTransport_DoesNotRequireProtection(UAuthCsrfOperation operation)
    {
        var result = UAuthCsrfPolicyEvaluator.RequiresProtection(
            operation,
            canIssueCookie: false,
            canAcceptCookie: false);

        Assert.False(result);
    }

    [Theory]
    [InlineData(UAuthCsrfOperation.Login)]
    [InlineData(UAuthCsrfOperation.PkceCompletion)]
    [InlineData(UAuthCsrfOperation.AuthenticatedMutation)]
    public void BothCookieCapabilities_RequireProtection(UAuthCsrfOperation operation)
    {
        var result = UAuthCsrfPolicyEvaluator.RequiresProtection(
            operation,
            canIssueCookie: true,
            canAcceptCookie: true);

        Assert.True(result);
    }

    [Fact]
    public void UnknownOperation_FailsClosed()
    {
        var result = UAuthCsrfPolicyEvaluator.RequiresProtection(
            (UAuthCsrfOperation)999,
            canIssueCookie: false,
            canAcceptCookie: false);

        Assert.True(result);
    }
}
