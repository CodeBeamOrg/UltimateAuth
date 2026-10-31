using AwesomeAssertions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Services;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthResourceClientTests
{
    private readonly Mock<IUAuthResourceRequestClient> _transport = new();

    private UAuthResourceClient CreateClient()
        => new(_transport.Object);

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task Resource_Methods_Should_Forward_Correct_Method(string method)
    {
        var payload = new { Name = "Product" };

        _transport
            .Setup(x => x.SendAsync(
                method,
                "/api/products",
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthResourceTransportResult
            {
                Ok = true,
                Status = 200,
                Body = "{\"id\":1,\"name\":\"Product\"}"
            });

        var client = CreateClient();

        switch (method)
        {
            case "GET":
                await client.GetAsync<TestProduct>("/api/products");
                break;

            case "POST":
                await client.PostAsync<TestProduct>("/api/products", payload);
                break;

            case "PUT":
                await client.PutAsync<TestProduct>("/api/products", payload);
                break;

            case "PATCH":
                await client.PatchAsync<TestProduct>("/api/products", payload);
                break;
        }

        _transport.Verify(x => x.SendAsync(
            method,
            "/api/products",
            It.Is<object?>(p =>
                method == "GET" ? p == null : ReferenceEquals(p, payload)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_Should_Deserialize_Success_Response()
    {
        _transport
            .Setup(x => x.SendAsync(
                "GET",
                "/api/products/1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthResourceTransportResult
            {
                Ok = true,
                Status = 200,
                Body = "{\"id\":1,\"name\":\"Product\"}",
                ContentType = "application/json"
            });

        var result = await CreateClient()
            .GetAsync<TestProduct>("/api/products/1");

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(200);
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(1);
        result.Value.Name.Should().Be("Product");
    }

    [Fact]
    public async Task Get_Should_Return_Default_When_Body_Is_Empty()
    {
        _transport
            .Setup(x => x.SendAsync(
                "GET",
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthResourceTransportResult
            {
                Ok = true,
                Status = 204,
                Body = null
            });

        var result = await CreateClient()
            .GetAsync<TestProduct>("/api/products/1");

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(204);
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Get_Should_Parse_Problem_On_Failure()
    {
        _transport
            .Setup(x => x.SendAsync(
                "GET",
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthResourceTransportResult
            {
                Ok = false,
                Status = 403,
                Body = """
                {
                    "title": "Forbidden",
                    "status": 403,
                    "detail": "Access denied"
                }
                """
            });

        var result = await CreateClient()
            .GetAsync<TestProduct>("/api/products");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(403);
        result.Value.Should().BeNull();
        result.Problem.Should().NotBeNull();
        result.Problem!.Title.Should().Be("Forbidden");
    }

    [Fact]
    public async Task Get_Should_Handle_Invalid_Problem_Json()
    {
        _transport
            .Setup(x => x.SendAsync(
                "GET",
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthResourceTransportResult
            {
                Ok = false,
                Status = 500,
                Body = "<html>Server Error</html>"
            });

        var result = await CreateClient()
            .GetAsync<TestProduct>("/api/products");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(500);
        result.Problem.Should().BeNull();
    }

    [Fact]
    public async Task Delete_Should_Return_Success_Without_Body()
    {
        _transport
            .Setup(x => x.SendAsync(
                "DELETE",
                "/api/products/1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthResourceTransportResult
            {
                Ok = true,
                Status = 204
            });

        var result = await CreateClient()
            .DeleteAsync("/api/products/1");

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(204);
        result.Problem.Should().BeNull();
    }

    [Fact]
    public async Task Delete_Should_Parse_Problem_On_Failure()
    {
        _transport
            .Setup(x => x.SendAsync(
                "DELETE",
                "/api/products/1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthResourceTransportResult
            {
                Ok = false,
                Status = 404,
                Body = """{"title":"Not Found","status":404}"""
            });

        var result = await CreateClient()
            .DeleteAsync("/api/products/1");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(404);
        result.Problem.Should().NotBeNull();
        result.Problem!.Title.Should().Be("Not Found");
    }

    [Fact]
    public async Task Get_Should_Forward_CancellationToken()
    {
        using var cts = new CancellationTokenSource();

        _transport
            .Setup(x => x.SendAsync(
                "GET",
                "/api/products",
                null,
                cts.Token))
            .ReturnsAsync(new UAuthResourceTransportResult
            {
                Ok = true,
                Status = 200,
                Body = "[]"
            });

        await CreateClient()
            .GetAsync<List<TestProduct>>("/api/products", cts.Token);

        _transport.Verify(x => x.SendAsync(
            "GET",
            "/api/products",
            null,
            cts.Token), Times.Once);
    }

    public sealed class TestProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
