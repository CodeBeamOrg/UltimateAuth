using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using FluentAssertions;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthResultMapperTests
{
    [Fact]
    public void FromJson_Should_Map_Success_Response()
    {
        var raw = new UAuthTransportResult
        {
            Status = 200,
            Body = JsonDocument.Parse("{\"name\":\"test\"}").RootElement
        };

        var result = UAuthResultMapper.FromJson<TestDto>(raw);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("test");
    }

    [Fact]
    public void FromJson_Should_Reject_Empty_Body()
    {
        var raw = new UAuthTransportResult
        {
            Status = 204,
            Body = null
        };

        Action act = () => UAuthResultMapper.FromJson<object>(raw);

        act.Should().Throw<UAuthProtocolException>();
    }

    [Fact]
    public void From_Should_Handle_NoContent()
    {
        var raw = new UAuthTransportResult
        {
            Status = 204,
            Body = null
        };

        var result = UAuthResultMapper.From(raw);

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(204);
    }

    [Fact]
    public void FromJson_Should_Map_Problem_On_4xx()
    {
        var raw = new UAuthTransportResult
        {
            Status = 401,
            Body = JsonDocument.Parse("{\"title\":\"Unauthorized\"}").RootElement
        };

        var result = UAuthResultMapper.FromJson<object>(raw);

        result.IsSuccess.Should().BeFalse();
        result.Problem.Should().NotBeNull();
    }

    [Fact]
    public void FromJson_Should_Throw_On_500()
    {
        var raw = new UAuthTransportResult
        {
            Status = 500
        };

        Action act = () => UAuthResultMapper.FromJson<object>(raw);
        act.Should().Throw<UAuthTransportException>();
    }

    [Fact]
    public void FromJson_Should_Throw_On_Invalid_Json()
    {
        var raw = new UAuthTransportResult
        {
            Status = 200,
            Body = JsonDocument.Parse("\"invalid\"").RootElement
        };

        Action act = () => UAuthResultMapper.FromJson<TestDto>(raw);
        act.Should().Throw<UAuthProtocolException>();
    }

    [Fact]
    public void FromJson_Should_Be_Case_Insensitive()
    {
        var raw = new UAuthTransportResult
        {
            Status = 200,
            Body = JsonDocument.Parse("{\"NAME\":\"test\"}").RootElement
        };

        var result = UAuthResultMapper.FromJson<TestDto>(raw);
        result.Value!.Name.Should().Be("test");
    }

    [Fact]
    public void FromJson_Should_Reject_NullBody_With_Generic_Type()
    {
        var raw = new UAuthTransportResult
        {
            Status = 200,
            Body = null
        };

        Action act = () => UAuthResultMapper.FromJson<TestDto>(raw);

        act.Should().Throw<UAuthProtocolException>();
    }

    [Fact]
    public void FromJson_Should_Not_Throw_When_Problem_Invalid()
    {
        var raw = new UAuthTransportResult
        {
            Status = 400,
            Body = JsonDocument.Parse("\"invalid\"").RootElement
        };

        var result = UAuthResultMapper.FromJson<object>(raw);

        result.IsSuccess.Should().BeFalse();
        result.Problem.Should().BeNull();
    }

    [Fact]
    public void FromJson_Should_Throw_On_Status_Zero()
    {
        var raw = new UAuthTransportResult
        {
            Status = 0
        };

        Action act = () => UAuthResultMapper.FromJson<object>(raw);
        act.Should().Throw<UAuthTransportException>();
    }

    [Fact]
    public void FromJson_ShouldDeserializeValidBody()
    {
        var raw = Response(200, new
        {
            name = "UltimateAuth",
            count = 5
        });

        var result = UAuthResultMapper.FromJson<TestDto>(raw);

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(200);
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("UltimateAuth");
        result.Value.Count.Should().Be(5);
    }

    [Fact]
    public void From_ShouldAccept4xxWithoutProblemBody()
    {
        var raw = Response(401);

        var result = UAuthResultMapper.From(raw);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(401);
        result.Problem.Should().BeNull();
    }

    [Theory]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    public void FromJson_ShouldThrowTransportExceptionFor5xx(int status)
    {
        var raw = Response(status);

        Action act = () => UAuthResultMapper.FromJson<TestDto>(raw);

        act.Should().Throw<UAuthTransportException>();
    }

    [Theory]
    [InlineData(301)]
    [InlineData(302)]
    [InlineData(307)]
    [InlineData(308)]
    public void FromJson_ShouldRejectRedirectResponses(int status)
    {
        var raw = Response(status);

        Action act = () =>
            UAuthResultMapper.FromJson<TestDto>(raw);

        act.Should().Throw<UAuthProtocolException>();
    }

    [Fact]
    public void From_ShouldRejectInvalidHttpStatus()
    {
        var raw = Response(600);

        Action act = () => UAuthResultMapper.From(raw);

        act.Should().Throw<UAuthProtocolException>();
    }


    private sealed class TestDto
    {
        public string? Name { get; set; }
        public int Count { get; set; }
    }

    private static UAuthTransportResult Response(int status, object? body = null)
    {
        return new UAuthTransportResult
        {
            Ok = status is >= 200 and < 300,
            Status = status,
            Body = body is null
                ? null
                : JsonSerializer.SerializeToElement(body)
        };
    }
}
