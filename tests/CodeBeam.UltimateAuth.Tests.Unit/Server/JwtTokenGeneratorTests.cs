using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using FluentAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class JwtTokenGeneratorTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private const string Subject = "test-user-123";
    private const string Issuer = "https://auth.example.test";
    private const string Audience = "ultimateauth-api";

    private static readonly TenantKey Tenant =
        TenantKey.FromExternal("tenant-a");

    private static JwtTokenGenerator CreateSut()
    {
        return new JwtTokenGenerator(
            new DevelopmentJwtSigningKeyProvider());
    }

    private static UAuthJwtTokenDescriptor CreateDescriptor(
        IReadOnlyDictionary<string, object>? claims = null)
    {
        return new UAuthJwtTokenDescriptor
        {
            Subject = Subject,
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = Now,
            ExpiresAt = Now.AddMinutes(15),
            Tenant = Tenant,
            Claims = claims,
            KeyId = "dev-uauth"
        };
    }

    private static JsonWebToken CreateToken(
        UAuthJwtTokenDescriptor descriptor)
    {
        var raw = CreateSut().CreateToken(descriptor);

        return new JsonWebTokenHandler().ReadJsonWebToken(raw);
    }

    [Fact]
    public void CreateToken_ShouldPreserveMultipleClaimValues()
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
            Base64UrlEncoder.Decode(
                token.EncodedPayload));

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
    public void CreateToken_ShouldUseTrustedSubjectAndTenant()
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
    public void CreateToken_ShouldPreserveSessionIdAndJwtId()
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
    public void CreateToken_ShouldUseConfiguredIssuerAndAudience()
    {
        var token = CreateToken(CreateDescriptor());

        token.Issuer.Should().Be(Issuer);

        token.Audiences.Should()
            .ContainSingle()
            .Which.Should().Be(Audience);
    }

    [Fact]
    public void CreateToken_ShouldUseConfiguredLifetime()
    {
        var token = CreateToken(CreateDescriptor());

        token.ValidFrom.Should()
            .Be(Now.UtcDateTime);

        token.ValidTo.Should()
            .Be(Now.AddMinutes(15).UtcDateTime);
    }

    [Fact]
    public async Task CreateToken_ShouldProduceValidSignature()
    {
        var keyProvider =
            new DevelopmentJwtSigningKeyProvider();

        var generator = new JwtTokenGenerator(keyProvider);

        var descriptor = CreateDescriptor();

        var raw = generator.CreateToken(descriptor);

        var signingKey = keyProvider.Resolve("dev-uauth");

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
    public void CreateToken_ShouldIgnoreReservedJwtMetadataClaims()
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

        token.ValidFrom.Should().Be(Now.UtcDateTime);

        token.ValidTo.Should()
            .Be(Now.AddMinutes(15).UtcDateTime);
    }

    [Fact]
    public void CreateToken_ShouldIncludeSigningKeyIdInHeader()
    {
        var token = CreateToken(CreateDescriptor());

        token.Kid.Should().Be("dev-uauth");
        token.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
    }
}
