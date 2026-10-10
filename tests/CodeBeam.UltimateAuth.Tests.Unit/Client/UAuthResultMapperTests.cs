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

    [Fact]
    public void FromJson_WhenProblemStatusDiffers_UsesHttpStatus()
    {
        var raw = new UAuthTransportResult
        {
            Status = 400,
            Body = JsonSerializer.SerializeToElement(new
            {
                status = 200,
                title = "Validation failed",
                traceId = "trace-123"
            })
        };

        var result = UAuthResultMapper.FromJson<string>(raw);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Status);
        Assert.NotNull(result.Problem);
        Assert.Equal(400, result.Problem.Status);
        Assert.Equal("trace-123", result.TraceId);
        Assert.Equal("trace-123", result.Problem.TraceId);
    }

    [Fact]
    public void FromJson_WhenErrorBodyIsMissing_ReturnsNullProblem()
    {
        var raw = new UAuthTransportResult
        {
            Status = 403
        };

        var result = UAuthResultMapper.FromJson<string>(raw);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.Status);
        Assert.Null(result.Problem);
    }

    [Fact]
    public void From_WhenErrorBodyIsInvalid_ReturnsNullProblem()
    {
        var raw = new UAuthTransportResult
        {
            Status = 400,
            Body = JsonSerializer.SerializeToElement("Invalid request")
        };

        var result = UAuthResultMapper.From(raw);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Status);
        Assert.Null(result.Problem);
    }

    [Fact]
    public void From_WhenSuccessfulWithoutBody_ReturnsSuccess()
    {
        var raw = new UAuthTransportResult
        {
            Status = 204
        };

        var result = UAuthResultMapper.From(raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(204, result.Status);
        Assert.Null(result.Problem);
    }

    [Fact]
    public void FromJson_WhenSuccessfulWithoutBody_ThrowsProtocolException()
    {
        var raw = new UAuthTransportResult
        {
            Status = 200
        };

        Assert.Throws<UAuthProtocolException>(
            () => UAuthResultMapper.FromJson<string>(raw));
    }

    [Fact]
    public void FromJson_Should_Use_Http_Status_In_Problem()
    {
        var raw = new UAuthTransportResult
        {
            Status = 403,
            Body = JsonSerializer.SerializeToElement(new
            {
                title = "Forbidden",
                status = 200,
                traceId = "trace-123"
            })
        };

        var result = UAuthResultMapper.FromJson<object>(raw);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(403);
        result.Problem.Should().NotBeNull();
        result.Problem!.Status.Should().Be(403);
        result.TraceId.Should().Be("trace-123");
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
