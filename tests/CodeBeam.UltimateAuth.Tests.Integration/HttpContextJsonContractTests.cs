using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Server.Extensions;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class HttpContextJsonContractTests
{
    private const string Endpoint = "/test/json-contract";

    public static TheoryData<string, string, string> InvalidJsonRequests => new()
    {
        {
            "text/plain",
            """{"name":"test"}""",
            "request_content_type_invalid"
        },
        {
            "application/json",
            "",
            "request_body_required"
        },
        {
            "application/json",
            "null",
            "request_body_invalid"
        },
        {
            "application/json",
            "{",
            "request_json_invalid"
        },
        {
            "application/json",
            "{}",
            "request_json_invalid"
        }
    };

    [Theory]
    [MemberData(nameof(InvalidJsonRequests))]
    public async Task InvalidJsonRequest_ShouldReturn400ProblemDetails(
        string contentType,
        string body,
        string expectedCode)
    {
        await using var host = await JsonTestHost.CreateAsync();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            Endpoint);

        request.Content = new StringContent(
            body,
            Encoding.UTF8);

        request.Content.Headers.ContentType =
            MediaTypeHeaderValue.Parse(contentType);

        using var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        response.Content.Headers.ContentType!
            .MediaType.Should().Be("application/problem+json");

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        problem.Should().NotBeNull();
        problem!.Status.Should().Be(400);
        problem.Detail.Should().Be(expectedCode);
        problem.Type.Should().Contain(expectedCode);
        problem.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public async Task ValidJsonRequest_ShouldReturn200WithTypedResult()
    {
        await using var host = await JsonTestHost.CreateAsync();

        using var response = await host.Client.PostAsJsonAsync(
            Endpoint,
            new { name = "UltimateAuth" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<JsonTestResult>();

        result.Should().NotBeNull();
        result!.Name.Should().Be("UltimateAuth");
    }

    [Fact]
    public async Task MissingContentType_ShouldReturn400ProblemDetails()
    {
        await using var host = await JsonTestHost.CreateAsync();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            Endpoint)
        {
            Content = new ByteArrayContent(
                Encoding.UTF8.GetBytes("""{"name":"test"}"""))
        };

        using var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        problem!.Detail.Should().Be("request_content_type_invalid");
    }

    [Fact]
    public async Task JsonReader_ShouldRewindBodyAfterSuccess()
    {
        await using var host = await JsonTestHost.CreateAsync();

        using var response = await host.Client.PostAsJsonAsync(
            "/test/json-rewind",
            new { name = "UltimateAuth" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<BodyRewindResult>();

        result.Should().NotBeNull();
        result!.Name.Should().Be("UltimateAuth");
        result.BodyStartsWithJson.Should().BeTrue();
    }

    [Fact]
    public async Task InvalidPermission_ShouldReturn400ProblemDetails()
    {
        await using var host = await JsonTestHost.CreateAsync();

        using var content = new StringContent(
            """
        {
            "name": "TestRole",
            "permissions": [
                { "value": "" }
            ]
        }
        """,
            Encoding.UTF8,
            "application/json");

        using var response = await host.Client.PostAsync(
            "/test/json-permission",
            content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        response.Content.Headers.ContentType!
            .MediaType.Should().Be("application/problem+json");

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        problem.Should().NotBeNull();
        problem!.Status.Should().Be(400);
        problem.Detail.Should().Be("permission_required");
    }


    private sealed class JsonTestHost : IAsyncDisposable
    {
        private readonly WebApplication _app;

        public HttpClient Client { get; }

        private JsonTestHost(WebApplication app)
        {
            _app = app;
            Client = app.GetTestClient();
        }

        public static async Task<JsonTestHost> CreateAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();

            var app = builder.Build();

            app.UseUAuthExceptionHandling();

            app.MapPost(Endpoint, async (HttpContext ctx) =>
            {
                var request = await ctx.ReadJsonAsync<JsonTestRequest>(
                    ctx.RequestAborted);

                return Results.Ok(new JsonTestResult(request.Name));
            });

            app.MapPost("/test/json-rewind", async (HttpContext ctx) =>
            {
                var request = await ctx.ReadJsonAsync<JsonTestRequest>(
                    ctx.RequestAborted);

                var firstByte = ctx.Request.Body.ReadByte();

                return Results.Ok(new BodyRewindResult(
                    request.Name,
                    firstByte == '{'));
            });


            app.MapPost("/test/json-permission", async (HttpContext ctx) =>
            {
                var request = await ctx.ReadJsonAsync<PermissionTestRequest>(
                    ctx.RequestAborted);

                return Results.Ok(request.Name);
            });

            await app.StartAsync();

            return new JsonTestHost(app);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private sealed class JsonTestRequest
    {
        public required string Name { get; init; }
    }

    private sealed record JsonTestResult(string Name);

    private sealed class PermissionTestRequest
    {
        public required string Name { get; init; }

        public required Permission[] Permissions { get; init; }
    }

    private sealed record BodyRewindResult(
        string Name,
        bool BodyStartsWithJson);
}
