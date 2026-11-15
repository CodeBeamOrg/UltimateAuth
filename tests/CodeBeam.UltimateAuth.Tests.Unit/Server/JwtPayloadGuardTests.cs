using System.Text;
using AwesomeAssertions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class JwtPayloadGuardTests
{
    [Fact]
    public void Valid_payload_is_accepted()
    {
        var token = CreateToken(
            """{"alg":"HS256","typ":"uauth-access+jwt","kid":"key-1"}""",
            """{"sub":"user-1","tenant":"tenant-a","exp":2000000000}""");

        JwtPayloadGuard.IsValid(token).Should().BeTrue();
    }

    [Fact]
    public void Duplicate_subject_is_rejected()
    {
        var token = CreateToken(
            """{"alg":"HS256","kid":"key-1"}""",
            """{"sub":"user-1","sub":"user-2","tenant":"tenant-a"}""");

        JwtPayloadGuard.IsValid(token).Should().BeFalse();
    }

    [Fact]
    public void Duplicate_tenant_is_rejected()
    {
        var token = CreateToken(
            """{"alg":"HS256","kid":"key-1"}""",
            """{"sub":"user-1","tenant":"tenant-a","tenant":"tenant-b"}""");

        JwtPayloadGuard.IsValid(token).Should().BeFalse();
    }

    [Fact]
    public void Duplicate_header_key_id_is_rejected()
    {
        var token = CreateToken(
            """{"alg":"HS256","kid":"key-1","kid":"key-2"}""",
            """{"sub":"user-1","tenant":"tenant-a"}""");

        JwtPayloadGuard.IsValid(token).Should().BeFalse();
    }

    [Fact]
    public void Non_string_subject_is_rejected()
    {
        var token = CreateToken(
            """{"alg":"HS256","kid":"key-1"}""",
            """{"sub":123,"tenant":"tenant-a"}""");

        JwtPayloadGuard.IsValid(token).Should().BeFalse();
    }

    [Fact]
    public void String_expiration_is_rejected()
    {
        var token = CreateToken(
            """{"alg":"HS256","kid":"key-1"}""",
            """{"sub":"user-1","tenant":"tenant-a","exp":"2000000000"}""");

        JwtPayloadGuard.IsValid(token).Should().BeFalse();
    }

    [Fact]
    public void Malformed_payload_is_rejected()
    {
        var token = CreateToken(
            """{"alg":"HS256"}""",
            """{"sub":""");

        JwtPayloadGuard.IsValid(token).Should().BeFalse();
    }

    private static string CreateToken(
        string header,
        string payload)
    {
        return string.Join(".",
            Base64UrlEncoder.Encode(
                Encoding.UTF8.GetBytes(header)),
            Base64UrlEncoder.Encode(
                Encoding.UTF8.GetBytes(payload)),
            Base64UrlEncoder.Encode(
                Encoding.UTF8.GetBytes("test-signature")));
    }
}
