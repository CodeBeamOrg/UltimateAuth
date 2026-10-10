using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthClientEventConsistencyTests
{
    private readonly Mock<IUAuthRequestClient> _request = new();
    private readonly Mock<IUAuthClientEvents> _events = new();

    private readonly IOptions<UAuthClientOptions> _options =
        Options.Create(new UAuthClientOptions
        {
            Endpoints = new UAuthClientEndpointOptions
            {
                BasePath = "/auth"
            }
        });

    private void Respond(int status, bool ok = true)
    {
        _request.Setup(x => x.SendJsonAsync(
                It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = ok,
                Status = status
            });
    }

    private void AssertNoEvents()
    {
        _events.Verify(
            x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()),
            Times.Never);
    }

    [Theory]
    [InlineData("Add")]
    [InlineData("Update")]
    [InlineData("SetPrimary")]
    [InlineData("UnsetPrimary")]
    [InlineData("Verify")]
    [InlineData("Delete")]
    public async Task Identifier_Should_Not_Publish_When_Response_Is_Invalid(
        string operation)
    {
        Respond(302);

        var client = new UAuthUserIdentifierClient(
            _request.Object, _events.Object, _options);

        // Requests are deliberately omitted: this test only
        // exercises HTTP response validation and event ordering.
        Func<Task> act = operation switch
        {
            "Add" => () => client.AddMyAsync(null!),
            "Update" => () => client.UpdateMyAsync(null!),
            "SetPrimary" => () => client.SetMyPrimaryAsync(null!),
            "UnsetPrimary" => () => client.UnsetMyPrimaryAsync(null!),
            "Verify" => () => client.VerifyMyAsync(null!),
            "Delete" => () => client.DeleteMyAsync(null!),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        await act.Should().ThrowAsync<UAuthProtocolException>();
        AssertNoEvents();
    }

    [Theory]
    [InlineData("UpdateProfile")]
    [InlineData("DeleteMe")]
    [InlineData("CreateProfile")]
    [InlineData("DeleteProfile")]
    public async Task User_Should_Not_Publish_When_Response_Is_Invalid(string operation)
    {
        Respond(302);

        var client = new UAuthUserClient(_request.Object, _events.Object, _options);

        Func<Task> act = operation switch
        {
            "UpdateProfile" => () => client.UpdateMyProfileAsync(null!),
            "DeleteMe" => () => client.DeleteMeAsync(),
            "CreateProfile" => () => client.CreateMyProfileAsync(null!),
            "DeleteProfile" => () => client.DeleteMyProfileAsync(null!),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        await act.Should().ThrowAsync<UAuthProtocolException>();
        AssertNoEvents();
    }

    [Theory]
    [InlineData("Change")]
    [InlineData("Revoke")]
    [InlineData("CompleteReset")]
    public async Task Credential_Should_Not_Publish_When_Response_Is_Invalid(string operation)
    {
        Respond(302);

        var client = new UAuthCredentialClient(
            _request.Object, _events.Object, _options);

        Func<Task> act = operation switch
        {
            "Change" => () => client.ChangeMyAsync(null!),
            "Revoke" => () => client.RevokeMyAsync(null!),
            "CompleteReset" => () => client.CompleteResetMyAsync(null!),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        await act.Should().ThrowAsync<UAuthProtocolException>();
        AssertNoEvents();
    }

    [Theory]
    [InlineData("Change")]
    [InlineData("CompleteReset")]
    public async Task Typed_Credential_Response_Should_Not_Publish_When_Body_Missing(
        string operation)
    {
        Respond(200);

        var client = new UAuthCredentialClient(
            _request.Object, _events.Object, _options);

        Func<Task> act = operation switch
        {
            "Change" => () => client.ChangeMyAsync(null!),
            "CompleteReset" => () => client.CompleteResetMyAsync(null!),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        await act.Should().ThrowAsync<UAuthProtocolException>();
        AssertNoEvents();
    }

    [Fact]
    public async Task Identifier_Should_Not_Publish_On_Server_Error()
    {
        Respond(500, ok: false);

        var client = new UAuthUserIdentifierClient(
            _request.Object, _events.Object, _options);

        Func<Task> act = () => client.AddMyAsync(null!);

        await act.Should().ThrowAsync<UAuthTransportException>();
        AssertNoEvents();
    }

    [Fact]
    public async Task Identifier_Should_Publish_On_Valid_Success()
    {
        Respond(204);

        var request = new AddUserIdentifierRequest
        {
            Value = "test@example.com",
            Type = UserIdentifierType.Email,
            IsPrimary = false
        };

        var client = new UAuthUserIdentifierClient(
            _request.Object, _events.Object, _options);

        var result = await client.AddMyAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(204);

        _events.Verify(x => x.PublishAsync(
            It.Is<UAuthStateEventArgs<AddUserIdentifierRequest>>(e =>
                e.Type == UAuthStateEvent.IdentifiersChanged &&
                ReferenceEquals(e.Payload, request))),
            Times.Once);
    }

    [Fact]
    public async Task Identifier_Should_Return_Failure_Without_Event_On_4xx()
    {
        Respond(409, ok: false);

        var client = new UAuthUserIdentifierClient(
            _request.Object, _events.Object, _options);

        var result = await client.AddMyAsync(null!);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(409);

        AssertNoEvents();
    }
}
