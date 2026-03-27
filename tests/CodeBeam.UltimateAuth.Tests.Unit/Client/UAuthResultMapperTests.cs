using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
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
    public void FromJson_Should_Handle_Empty_Body()
    {
        var raw = new UAuthTransportResult
        {
            Status = 204,
            Body = null
        };

        var result = UAuthResultMapper.FromJson<object>(raw);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
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
    public void FromJson_Should_Handle_NullBody_With_Generic_Type()
    {
        var raw = new UAuthTransportResult
        {
            Status = 200,
            Body = null
        };

        var result = UAuthResultMapper.FromJson<TestDto>(raw);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
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
}
