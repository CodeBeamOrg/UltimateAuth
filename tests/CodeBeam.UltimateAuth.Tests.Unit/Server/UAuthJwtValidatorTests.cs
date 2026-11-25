using System.Security.Claims;
using System.Security.Cryptography;
using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class UAuthJwtValidatorTests
{
    private const string Issuer = "https://auth.example.test";
    private const string Audience = "ultimateauth-api";
    private const string TokenType = "uauth-access+jwt";
    private const string KeyId = "test-key";

    private static readonly TenantKey Tenant =
        TenantKey.FromExternal("tenant-a");

    private static SymmetricSecurityKey CreateKey(string kid = KeyId)
        => new(RandomNumberGenerator.GetBytes(32))
        {
            KeyId = kid
        };

    private static UAuthJwtValidator CreateValidator(
        params SecurityKey[] keys)
        => CreateValidatorWithOptions(
            keys,
            [SecurityAlgorithms.HmacSha256]);

    private static UAuthJwtValidator CreateValidatorWithOptions(
        SecurityKey[] keys,
        IReadOnlyCollection<string> algorithms,
        int maxTokenBytes = 16 * 1024)
    {
        var provider = new StaticJwtVerificationKeyProvider(keys);

        var options = Microsoft.Extensions.Options.Options.Create(
            new UAuthJwtValidationOptions
            {
                Issuer = Issuer,
                Audience = Audience,
                ValidAlgorithms = algorithms,
                ClockSkew = TimeSpan.Zero,
                MaximumTokenSizeInBytes = maxTokenBytes
            });

        return new UAuthJwtValidator(provider, options);
    }

    private static string CreateToken(
        SecurityKey key,
        string? issuer = null,
        string? audience = null,
        string? tenant = null,
        string? tokenType = null,
        DateTime? notBefore = null,
        DateTime? expires = null,
        string? subject = null,
        string algorithm = SecurityAlgorithms.HmacSha256)
    {
        var now = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            TokenType = tokenType ?? TokenType,
            Issuer = issuer ?? Issuer,
            Audience = audience ?? Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", subject ?? "user-123"),
                new Claim("tenant", tenant ?? Tenant.Value)
            ]),
            NotBefore = notBefore ?? now.AddMinutes(-1),
            IssuedAt = now.AddMinutes(-1),
            Expires = expires ?? now.AddMinutes(10),
            SigningCredentials =
                new SigningCredentials(key, algorithm)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    [Fact]
    public async Task Valid_token_should_return_trusted_identity()
    {
        var key = CreateKey();
        var validator = CreateValidator(key);
        var token = CreateToken(key);

        var identity = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        identity.Should().NotBeNull();
        identity!.Subject.Should().Be("user-123");
        identity.TenantId.Should().Be(Tenant.Value);
        identity.Issuer.Should().Be(Issuer);
        identity.Audience.Should().Be(Audience);
        identity.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Invalid_signature_should_be_rejected()
    {
        var key = CreateKey();
        var validator = CreateValidator(key);
        var token = CreateToken(key);

        var parts = token.Split('.');
        var signature = parts[2].ToCharArray();

        signature[0] = signature[0] == 'A' ? 'B' : 'A';
        parts[2] = new string(signature);

        var tampered = string.Join(".", parts);

        var identity = await validator.ValidateAsync(
            tampered, Tenant, TestContext.Current.CancellationToken);

        identity.Should().BeNull();
    }

    [Fact]
    public async Task Untrusted_signing_key_should_be_rejected()
    {
        var trustedKey = CreateKey();
        var attackerKey = CreateKey();

        var validator = CreateValidator(trustedKey);
        var token = CreateToken(attackerKey);

        var identity = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        identity.Should().BeNull();
    }

    [Fact]
    public async Task Wrong_issuer_should_be_rejected()
    {
        var key = CreateKey();
        var validator = CreateValidator(key);

        var token = CreateToken(
            key, issuer: "https://attacker.example.test");

        var identity = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        identity.Should().BeNull();
    }

    [Fact]
    public async Task Wrong_audience_should_be_rejected()
    {
        var key = CreateKey();
        var validator = CreateValidator(key);

        var token = CreateToken(
            key, audience: "another-api");

        var identity = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        identity.Should().BeNull();
    }

    [Fact]
    public async Task Wrong_tenant_should_be_rejected()
    {
        var key = CreateKey();
        var validator = CreateValidator(key);

        var token = CreateToken(key, tenant: "tenant-b");

        var identity = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        identity.Should().BeNull();
    }

    [Fact]
    public async Task Expired_token_should_be_rejected()
    {
        var key = CreateKey();
        var validator = CreateValidator(key);

        var token = CreateToken(
            key,
            notBefore: DateTime.UtcNow.AddMinutes(-20),
            expires: DateTime.UtcNow.AddMinutes(-10));

        var identity = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        identity.Should().BeNull();
    }

    [Fact]
    public async Task Future_token_should_be_rejected()
    {
        var key = CreateKey();
        var validator = CreateValidator(key);

        var token = CreateToken(
            key,
            notBefore: DateTime.UtcNow.AddMinutes(10),
            expires: DateTime.UtcNow.AddMinutes(20));

        var identity = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        identity.Should().BeNull();
    }

    [Fact]
    public async Task Wrong_token_type_should_be_rejected()
    {
        var key = CreateKey();
        var validator = CreateValidator(key);

        var token = CreateToken(
            key, tokenType: "JWT");

        var identity = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        identity.Should().BeNull();
    }

    [Fact]
    public async Task Unknown_key_id_should_be_rejected()
    {
        var trustedKey = CreateKey("trusted");
        var unknownKey = CreateKey("unknown");

        var validator = CreateValidator(trustedKey);
        var token = CreateToken(unknownKey);

        var identity = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        identity.Should().BeNull();
    }

    [Fact]
    public async Task Disallowed_algorithm_should_be_rejected()
    {
        var key = CreateKey();

        var validator = CreateValidatorWithOptions(
            [key],
            [SecurityAlgorithms.HmacSha512]);

        var token = CreateToken(key);

        var identity = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        identity.Should().BeNull();
    }

    [Fact]
    public async Task Oversized_token_should_be_rejected()
    {
        var key = CreateKey();

        var validator = CreateValidatorWithOptions(
            [key],
            [SecurityAlgorithms.HmacSha256],
            maxTokenBytes: 100);

        var token = CreateToken(key);

        var identity = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        identity.Should().BeNull();
    }

    [Fact]
    public async Task Malformed_token_should_be_rejected()
    {
        var validator = CreateValidator(CreateKey());

        var identity = await validator.ValidateAsync(
            "not-a-jwt",
            Tenant,
            TestContext.Current.CancellationToken);

        identity.Should().BeNull();
    }

    [Fact]
    public async Task Rotated_verification_keys_should_be_accepted()
    {
        var oldKey = CreateKey("old-key");
        var newKey = CreateKey("new-key");

        var validator = CreateValidator(oldKey, newKey);

        var oldToken = CreateToken(oldKey);
        var newToken = CreateToken(newKey);

        var oldIdentity = await validator.ValidateAsync(
            oldToken, Tenant, TestContext.Current.CancellationToken);

        var newIdentity = await validator.ValidateAsync(
            newToken, Tenant, TestContext.Current.CancellationToken);

        oldIdentity.Should().NotBeNull();
        newIdentity.Should().NotBeNull();
    }

    [Fact]
    public async Task Removed_verification_key_should_reject_old_token()
    {
        var oldKey = CreateKey("old-key");
        var newKey = CreateKey("new-key");

        var validator = CreateValidator(newKey);
        var oldToken = CreateToken(oldKey);

        var identity = await validator.ValidateAsync(
            oldToken, Tenant, TestContext.Current.CancellationToken);

        identity.Should().BeNull();
    }

    [Fact]
    public async Task Cancellation_should_be_propagated()
    {
        var validator = CreateValidator(CreateKey());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => validator.ValidateAsync(
            "not-a-jwt", Tenant, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }


    [Theory]
    [InlineData("""
    "sub":"user-1","sub":"user-2","tenant":"tenant-a"
    """)]
    [InlineData("""
    "sub":"user-1","tenant":"tenant-a","tenant":"tenant-b"
    """)]
    public async Task Duplicate_identity_claims_should_be_rejected(
        string claims)
    {
        var key = CreateKey();
        var validator = CreateValidator(key);

        var token = CreateRawToken(
            key, CreateRawPayload(claims));

        var result = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("""
    "tenant":"tenant-a"
    """)]
    [InlineData("""
    "sub":"user-1"
    """)]
    public async Task Missing_identity_claims_should_be_rejected(
        string claims)
    {
        var key = CreateKey();
        var validator = CreateValidator(key);

        var token = CreateRawToken(
            key, CreateRawPayload(claims));

        var result = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("""
    "sub":123,"tenant":"tenant-a"
    """)]
    [InlineData("""
    "sub":"user-1","tenant":123
    """)]
    [InlineData("""
    "sub":null,"tenant":"tenant-a"
    """)]
    [InlineData("""
    "sub":"user-1","tenant":["tenant-a"]
    """)]
    public async Task Invalid_identity_claim_types_should_be_rejected(
        string claims)
    {
        var key = CreateKey();
        var validator = CreateValidator(key);

        var token = CreateRawToken(
            key, CreateRawPayload(claims));

        var result = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Unsigned_token_should_be_rejected()
    {
        var key = CreateKey();
        var validator = CreateValidator(key);

        const string headerJson = """{"alg":"none","typ":"uauth-access+jwt","kid":"test-key"}""";

        var payloadJson = CreateRawPayload(
            """
            "sub":"user-1","tenant":"tenant-a"
            """);

        var header = Base64UrlEncoder.Encode(
            System.Text.Encoding.UTF8.GetBytes(headerJson));

        var payload = Base64UrlEncoder.Encode(
            System.Text.Encoding.UTF8.GetBytes(payloadJson));

        var token = $"{header}.{payload}.";

        var result = await validator.ValidateAsync(
            token, Tenant, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("", "ultimateauth-api", 16384)]
    [InlineData("https://auth.example.test", "", 16384)]
    [InlineData("https://auth.example.test", "ultimateauth-api", 0)]
    [InlineData("https://auth.example.test", "ultimateauth-api", -1)]
    public void Invalid_validation_options_should_be_rejected(
        string issuer,
        string audience,
        int maxTokenBytes)
    {
        var key = CreateKey();

        var provider = new StaticJwtVerificationKeyProvider([key]);

        var options = Microsoft.Extensions.Options.Options.Create(
            new UAuthJwtValidationOptions
            {
                Issuer = issuer,
                Audience = audience,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                MaximumTokenSizeInBytes = maxTokenBytes
            });

        var act = () => new UAuthJwtValidator(provider, options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT validation configuration is invalid.");
    }


    private static string CreateRawToken(SymmetricSecurityKey key, string payloadJson, string algorithm = SecurityAlgorithms.HmacSha256)
    {
        var headerJson = $$"""
            {"alg":"{{algorithm}}","typ":"uauth-access+jwt","kid":"{{key.KeyId}}"}
            """;

        var header = Base64UrlEncoder.Encode(
            System.Text.Encoding.UTF8.GetBytes(headerJson));

        var payload = Base64UrlEncoder.Encode(
            System.Text.Encoding.UTF8.GetBytes(payloadJson));

        var signingInput = $"{header}.{payload}";

        var signature = HMACSHA256.HashData(
            key.Key,
            System.Text.Encoding.ASCII.GetBytes(signingInput));

        return $"{signingInput}.{Base64UrlEncoder.Encode(signature)}";
    }

    private static string CreateRawPayload(string claims)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return $$"""
        {
          "iss": "https://auth.example.test",
          "aud": "ultimateauth-api",
          "iat": {{now - 60}},
          "nbf": {{now - 60}},
          "exp": {{now + 600}},
          {{claims}}
        }
        """;
    }
}
