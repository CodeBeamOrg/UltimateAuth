using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit.Core;

public sealed class JsonConverterTests
{
    [Fact]
    public void AuthSessionId_ShouldRoundTrip()
    {
        var original = AuthSessionId.Parse("auth-session-id-12345678901234567890", null);

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<AuthSessionId>(json);

        restored.Should().Be(original);
    }

    [Fact]
    public void SessionChainId_ShouldRoundTripUsingNFormat()
    {
        var original = SessionChainId.New();

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<SessionChainId>(json);

        json.Should().Be($"\"{original.Value:N}\"");
        restored.Should().Be(original);
    }

    [Fact]
    public void SessionRootId_ShouldRoundTripUsingNFormat()
    {
        var original = SessionRootId.New();

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<SessionRootId>(json);

        json.Should().Be($"\"{original.Value:N}\"");
        restored.Should().Be(original);
    }

    [Fact]
    public void TokenId_ShouldRoundTripUsingNFormat()
    {
        var original = TokenId.New();

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<TokenId>(json);

        json.Should().Be($"\"{original.Value:N}\"");
        restored.Should().Be(original);
    }

    [Fact]
    public void DeviceId_ShouldRoundTrip()
    {
        var original = DeviceId.Create(
            "device-identifier-with-sufficient-length");

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<DeviceId>(json);

        json.Should().Be($"\"{original.Value}\"");
        restored.Should().Be(original);
    }

    [Fact]
    public void TenantKey_ShouldRoundTrip()
    {
        var original = TenantKey.FromExternal("tenant-a");

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<TenantKey>(json);

        restored.Should().Be(original);
    }

    [Theory]
    [InlineData("\"invalid\"")]
    [InlineData("\"\"")]
    [InlineData("123")]
    [InlineData("null")]
    public void SessionChainId_ShouldRejectInvalidValues(string json)
    {
        var act = () => JsonSerializer.Deserialize<SessionChainId>(json);

        act.Should().Throw<JsonException>();
    }

    [Theory]
    [InlineData("\"invalid\"")]
    [InlineData("\"\"")]
    [InlineData("123")]
    [InlineData("null")]
    public void SessionRootId_ShouldRejectInvalidValues(string json)
    {
        var act = () => JsonSerializer.Deserialize<SessionRootId>(json);

        act.Should().Throw<JsonException>();
    }

    [Theory]
    [InlineData("\"short\"")]
    [InlineData("\"\"")]
    [InlineData("\"undefined\"")]
    public void DeviceId_ShouldRejectInvalidValues(string json)
    {
        var act = () => JsonSerializer.Deserialize<DeviceId>(json);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void NullableTypedIds_ShouldAcceptNull()
    {
        var json = """
        {
            "ChainId": null,
            "RootId": null,
            "BoundDeviceId": null
        }
        """;

        var restored = JsonSerializer.Deserialize<NullableIdsDto>(json);

        restored.Should().NotBeNull();
        restored!.ChainId.Should().BeNull();
        restored.RootId.Should().BeNull();
        restored.BoundDeviceId.Should().BeNull();
    }

    [Fact]
    public void AuthValidationResult_ShouldSerializeTypedIdsAsStrings()
    {
        var chainId = SessionChainId.New();
        var rootId = SessionRootId.New();
        var deviceId = DeviceId.Create("device-identifier-with-sufficient-length");

        var result = new AuthValidationResult
        {
            State = SessionState.Active,
            ChainId = chainId,
            RootId = rootId,
            BoundDeviceId = deviceId
        };

        var json = JsonSerializer.Serialize(result);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("ChainId").GetString()
            .Should().Be(chainId.Value.ToString("N"));

        root.GetProperty("RootId").GetString()
            .Should().Be(rootId.Value.ToString("N"));

        root.GetProperty("BoundDeviceId").GetString()
            .Should().Be(deviceId.Value);

        var restored = JsonSerializer.Deserialize<AuthValidationResult>(json);

        restored.Should().NotBeNull();
        restored!.ChainId.Should().Be(chainId);
        restored.RootId.Should().Be(rootId);
        restored.BoundDeviceId.Should().Be(deviceId);
    }

    [Fact]
    public void PasswordHash_ShouldRoundTrip()
    {
        var original = PasswordHash.Create(
            "argon2id",
            "encoded-hash-value");

        var json = JsonSerializer.Serialize(
            original);

        json.Should().Be("\"argon2id$encoded-hash-value\"");

        var restored = JsonSerializer.Deserialize<PasswordHash>(
            json);

        restored.Should().Be(original);
    }

    [Fact]
    public void PasswordHash_ShouldSupportLegacyFormat()
    {
        const string json = "\"legacy-hash-value\"";

        var restored = JsonSerializer.Deserialize<PasswordHash>(
            json);

        restored.Algorithm.Should().Be("legacy");
        restored.Hash.Should().Be("legacy-hash-value");

        JsonSerializer.Serialize(restored)
            .Should().Be("\"legacy$legacy-hash-value\"");
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    [InlineData("123")]
    [InlineData("true")]
    [InlineData("{}")]
    public void PasswordHash_ShouldRejectInvalidValues(string json)
    {
        var act = () => JsonSerializer.Deserialize<PasswordHash>(
            json);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void ProfileKey_ShouldRoundTripAndNormalize()
    {
        var original = ProfileKey.Parse("  WORK  ", null);

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<ProfileKey>(json);

        json.Should().Be("\"work\"");
        restored.Should().Be(original);
        restored.Value.Should().Be("work");
    }

    [Fact]
    public void ProfileKey_ShouldRoundTripDefault()
    {
        var original = ProfileKey.Default;

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<ProfileKey>(json);

        json.Should().Be("\"default\"");
        restored.Should().Be(original);
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    [InlineData("123")]
    [InlineData("true")]
    public void ProfileKey_ShouldRejectInvalidValues(string json)
    {
        var act = () => JsonSerializer.Deserialize<ProfileKey>(json);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void ProfileKey_ShouldRejectValuesLongerThan64Characters()
    {
        var json = JsonSerializer.Serialize(new string('a', 65));

        var act = () => JsonSerializer.Deserialize<ProfileKey>(json);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void ProfileKey_ShouldAcceptNullAsDefault()
    {
        // This behavior is explicitly supported by ProfileKeyJsonConverter.
        var restored = JsonSerializer.Deserialize<ProfileKey>("null");

        restored.Should().Be(default(ProfileKey));
    }

    [Fact]
    public void DeviceContext_ShouldRoundTrip()
    {
        var deviceId = DeviceId.Create(
            "device-identifier-with-sufficient-length");

        var original = DeviceContext.Create(
            deviceId,
            deviceType: " Desktop ",
            platform: " Windows ",
            operatingSystem: " Windows 11 ",
            browser: " Edge ",
            ipAddress: " 127.0.0.1 ");

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<DeviceContext>(json);

        restored.Should().NotBeNull();

        restored!.DeviceId.Should().Be(deviceId);
        restored.DeviceType.Should().Be("desktop");
        restored.Platform.Should().Be("windows");
        restored.OperatingSystem.Should().Be("windows 11");
        restored.Browser.Should().Be("edge");
        restored.IpAddress.Should().Be("127.0.0.1");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("deviceId").GetString()
            .Should().Be(deviceId.Value);

        root.GetProperty("deviceType").GetString()
            .Should().Be("desktop");
    }

    [Fact]
    public void DeviceContext_WithoutDeviceId_ShouldBeAnonymous()
    {
        const string json = """
    {
        "deviceType": "desktop",
        "platform": "windows"
    }
    """;

        var restored = JsonSerializer.Deserialize<DeviceContext>(json);

        restored.Should().NotBeNull();
        restored!.HasDeviceId.Should().BeFalse();

        restored.DeviceId.Should().BeNull();
        restored.DeviceType.Should().BeNull();
        restored.Platform.Should().BeNull();
    }

    [Fact]
    public void DeviceContext_Anonymous_ShouldRoundTrip()
    {
        var original = DeviceContext.Anonymous();

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<DeviceContext>(json);

        restored.Should().NotBeNull();
        restored!.HasDeviceId.Should().BeFalse();
        restored.DeviceId.Should().BeNull();
    }

    [Fact]
    public void DeviceContext_ShouldRejectInvalidDeviceId()
    {
        const string json = """
    {
        "deviceId": "short",
        "deviceType": "desktop"
    }
    """;

        var act = () => JsonSerializer.Deserialize<DeviceContext>(json);

        act.Should().Throw<JsonException>()
            .WithMessage("*Invalid DeviceId*");
    }

    [Theory]
    [InlineData("\"device\"")]
    [InlineData("123")]
    [InlineData("[]")]
    public void DeviceContext_ShouldRejectNonObjectValues(string json)
    {
        var act = () => JsonSerializer.Deserialize<DeviceContext>(json);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void UserKey_ShouldRoundTrip()
    {
        var original = UserKey.FromString("external-user-123");

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<UserKey>(json);

        json.Should().Be("\"external-user-123\"");
        restored.Should().Be(original);
    }

    [Fact]
    public void UserKey_GuidBased_ShouldRoundTrip()
    {
        var original = UserKey.New();

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<UserKey>(json);

        json.Should().Be($"\"{original.Value}\"");
        restored.Should().Be(original);
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    [InlineData("123")]
    [InlineData("true")]
    public void UserKey_ShouldRejectInvalidValues(string json)
    {
        var act = () => JsonSerializer.Deserialize<UserKey>(json);

        act.Should().Throw<JsonException>();
    }

    private sealed class NullableIdsDto
    {
        public SessionChainId? ChainId { get; init; }
        public SessionRootId? RootId { get; init; }
        public DeviceId? BoundDeviceId { get; init; }
    }
}
