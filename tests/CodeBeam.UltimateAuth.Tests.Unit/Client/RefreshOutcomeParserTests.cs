using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Core.Domain;
using Xunit;

namespace CodeBeam.UltimateAuth.Tests.Unit
{
    public sealed class RefreshOutcomeParserTests
    {
        [Theory]
        [InlineData("no-op", RefreshOutcome.NoOp)]
        [InlineData("touched", RefreshOutcome.Touched)]
        [InlineData("reauth-required", RefreshOutcome.ReauthRequired)]
        public void Parse_KnownValues_ReturnsExpectedOutcome(string input, RefreshOutcome expected)
        {
            var result = RefreshOutcomeParser.Parse(input);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("unknown")]
        [InlineData("NO-OP")]
        [InlineData("Touched")]
        public void Parse_UnknownOrInvalidValues_ReturnsNone(string? input)
        {
            var result = RefreshOutcomeParser.Parse(input);

            Assert.Equal(RefreshOutcome.None, result);
        }
    }
}
