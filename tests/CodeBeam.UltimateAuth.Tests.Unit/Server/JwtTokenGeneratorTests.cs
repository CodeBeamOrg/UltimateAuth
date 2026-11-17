using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class JwtTokenGeneratorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private const string Subject = "test-user-123";
    private const string Issuer = "https://auth.example.test";
    private const string Audience = "ultimateauth-api";

    private static readonly TenantKey Tenant = TenantKey.FromExternal("tenant-a");

    private static ConfiguredJwtSigningKeyProvider CreateKeyProvider()
    {
        var options = new UAuthServerOptions();

        return new ConfiguredJwtSigningKeyProvider(Options.Create(options));
    }

    private static JwtTokenGenerator CreateSut()
    {
        return new JwtTokenGenerator(CreateKeyProvider());
    }

    private static UAuthJwtTokenDescriptor CreateDescriptor(
        IReadOnlyDictionary<string, object>? claims = null,
        string? keyId = null,
        string subject = Subject,
        string issuer = Issuer,
        string audience = Audience,
        DateTimeOffset? expiresAt = null)
    {
        return new UAuthJwtTokenDescriptor
        {
            Subject = subject,
            Issuer = issuer,
            Audience = audience,
            IssuedAt = Now,
            ExpiresAt = expiresAt ?? Now.AddMinutes(15),
            Tenant = Tenant,
            Claims = claims,
            KeyId = keyId
        };
    }

    private static JsonWebToken CreateToken(
        UAuthJwtTokenDescriptor descriptor)
    {
        var raw = CreateSut().CreateToken(descriptor);

        return new JsonWebTokenHandler()
            .ReadJsonWebToken(raw);
    }

    [Fact]
    public void CreateToken_PreservesMultipleClaimValues()
    {
        var descriptor = CreateDescriptor(
            new Dictionary<string, object>
            {
                ["role"] = new[] { "Admin", "Editor" },
                ["uauth:permission"] = new[]
                {
                    "users.read",
                    "users.write"
                }
            });

        var token = CreateToken(descriptor);

        using var payload = JsonDocument.Parse(
            Base64UrlEncoder.Decode(token.EncodedPayload));

        var root = payload.RootElement;

        root.GetProperty("role")
            .EnumerateArray()
            .Select(x => x.GetString())
            .Should()
            .BeEquivalentTo("Admin", "Editor");

        root.GetProperty("uauth:permission")
            .EnumerateArray()
            .Select(x => x.GetString())
            .Should()
            .BeEquivalentTo("users.read", "users.write");
    }

    [Fact]
    public void CreateToken_UsesTrustedSubjectAndTenant()
    {
        var descriptor = CreateDescriptor(
            new Dictionary<string, object>
            {
                ["sub"] = "attacker-user",
                ["tenant"] = "attacker-tenant",
                ["department"] = "legal"
            });

        var token = CreateToken(descriptor);

        token.Subject.Should().Be(Subject);

        token.GetClaim("tenant")!.Value
            .Should().Be(Tenant.Value);

        token.GetClaim("department")!.Value
            .Should().Be("legal");
    }

    [Fact]
    public void CreateToken_PreservesSessionIdAndJwtId()
    {
        var descriptor = CreateDescriptor(
            new Dictionary<string, object>
            {
                ["sid"] = "session-123",
                ["jti"] = "jwt-123"
            });

        var token = CreateToken(descriptor);

        token.GetClaim("sid")!.Value
            .Should().Be("session-123");

        token.GetClaim("jti")!.Value
            .Should().Be("jwt-123");
    }

    [Fact]
    public void CreateToken_UsesConfiguredIssuerAndAudience()
    {
        var token = CreateToken(CreateDescriptor());

        token.Issuer.Should().Be(Issuer);

        token.Audiences.Should()
            .ContainSingle()
            .Which.Should().Be(Audience);
    }

    [Fact]
    public void CreateToken_UsesConfiguredLifetime()
    {
        var token = CreateToken(CreateDescriptor());

        token.ValidFrom.Should()
            .Be(Now.UtcDateTime);

        token.ValidTo.Should()
            .Be(Now.AddMinutes(15).UtcDateTime);
    }

    [Fact]
    public async Task CreateToken_ProducesValidSignature()
    {
        var provider = CreateKeyProvider();
        var generator = new JwtTokenGenerator(provider);

        var raw = generator.CreateToken(
            CreateDescriptor());

        var signingKey = provider.Resolve(null);

        var handler = new JsonWebTokenHandler();

        var validation = await handler.ValidateTokenAsync(
            raw,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,

                ValidateAudience = true,
                ValidAudience = Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey.Key,

                RequireSignedTokens = true,
                ValidateLifetime = false,

                ValidAlgorithms = new[]
                {
                    SecurityAlgorithms.HmacSha256
                }
            });

        validation.IsValid.Should().BeTrue(
            validation.Exception?.ToString());
    }

    [Fact]
    public async Task CreateToken_RejectsValidationWithDifferentKey()
    {
        var generator = CreateSut();

        var raw = generator.CreateToken(
            CreateDescriptor());

        var differentKey = new SymmetricSecurityKey(
            Enumerable.Repeat((byte)0x7A, 32).ToArray())
        {
            KeyId = UAuthCryptographyConstants.DefaultJwtKeyId
        };

        var validation = await new JsonWebTokenHandler()
            .ValidateTokenAsync(
                raw,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,

                    ValidateAudience = true,
                    ValidAudience = Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = differentKey,

                    RequireSignedTokens = true,
                    ValidateLifetime = false,

                    ValidAlgorithms = new[]
                    {
                        SecurityAlgorithms.HmacSha256
                    }
                });

        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateToken_IgnoresReservedJwtMetadataClaims()
    {
        var descriptor = CreateDescriptor(
            new Dictionary<string, object>
            {
                ["iss"] = "attacker-issuer",
                ["aud"] = "attacker-audience",
                ["exp"] = 1L,
                ["nbf"] = 1L,
                ["iat"] = 1L
            });

        var token = CreateToken(descriptor);

        token.Issuer.Should().Be(Issuer);

        token.Audiences.Should().Contain(Audience);

        token.ValidFrom.Should()
            .Be(Now.UtcDateTime);

        token.ValidTo.Should()
            .Be(Now.AddMinutes(15).UtcDateTime);
    }

    [Fact]
    public void CreateToken_IncludesSigningKeyIdInHeader()
    {
        var token = CreateToken(CreateDescriptor());

        token.Kid.Should().Be(
            UAuthCryptographyConstants.DefaultJwtKeyId);

        token.Alg.Should().Be(
            SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public void CreateToken_AcceptsConfiguredSigningKeyId()
    {
        var descriptor = CreateDescriptor(
            keyId: UAuthCryptographyConstants.DefaultJwtKeyId);

        var token = CreateToken(descriptor);

        token.Kid.Should().Be(
            UAuthCryptographyConstants.DefaultJwtKeyId);
    }

    [Fact]
    public void CreateToken_RejectsUnknownSigningKeyId()
    {
        var provider = CreateKeyProvider();

        var act = () => provider.Resolve("unknown-key");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateToken_AllowsDefaultSigningKey()
    {
        var provider = CreateKeyProvider();

        var key = provider.Resolve(null);

        key.KeyId.Should().Be(
            UAuthCryptographyConstants.DefaultJwtKeyId);

        key.Algorithm.Should().Be(
            SecurityAlgorithms.HmacSha256);

        key.Key.Should().BeOfType<SymmetricSecurityKey>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateToken_RejectsEmptySubject(string subject)
    {
        var descriptor = CreateDescriptor(subject: subject);

        var act = () => CreateSut().CreateToken(descriptor);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateToken_RejectsEmptyIssuer(string issuer)
    {
        var descriptor = CreateDescriptor(issuer: issuer);

        var act = () => CreateSut().CreateToken(descriptor);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateToken_RejectsEmptyAudience(string audience)
    {
        var descriptor = CreateDescriptor(audience: audience);

        var act = () => CreateSut().CreateToken(descriptor);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateToken_RejectsInvalidLifetime()
    {
        var descriptor = CreateDescriptor(expiresAt: Now);

        var act = () => CreateSut().CreateToken(descriptor);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateToken_RejectsUnknownDescriptorKeyId()
    {
        var descriptor = CreateDescriptor(
            keyId: "unknown-key");

        var act = () => CreateSut().CreateToken(descriptor);

        act.Should().Throw<InvalidOperationException>();
    }
}
