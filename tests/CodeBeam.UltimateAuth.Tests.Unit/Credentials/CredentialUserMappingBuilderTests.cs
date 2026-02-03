using CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class CredentialUserMappingBuilderTests
{
    private sealed class ConventionUser
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public long SecurityVersion { get; set; }
    }

    private sealed class ExplicitUser
    {
        public Guid UserId { get; set; }
        public string LoginName { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public long SecurityVersion { get; set; }
    }

    private sealed class PlainPasswordUser
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
        public long SecurityVersion { get; set; }
    }


    [Fact]
    public void Build_UsesConventions_WhenExplicitMappingIsNotProvided()
    {
        var options = new CredentialUserMappingOptions<ConventionUser, Guid>();
        var mapping = CredentialUserMappingBuilder.Build(options);
        var user = new ConventionUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            SecurityVersion = 3
        };

        Assert.Equal(user.Id, mapping.UserId(user));
        Assert.Equal(user.Email, mapping.Username(user));
        Assert.Equal(user.PasswordHash, mapping.PasswordHash(user));
        Assert.Equal(user.SecurityVersion, mapping.SecurityVersion(user));
        Assert.True(mapping.CanAuthenticate(user));
    }

    [Fact]
    public void Build_ExplicitMapping_OverridesConvention()
    {
        var options = new CredentialUserMappingOptions<ExplicitUser, Guid>();
        options.MapUsername(u => u.LoginName);
        var mapping = CredentialUserMappingBuilder.Build(options);
        var user = new ExplicitUser
        {
            UserId = Guid.NewGuid(),
            LoginName = "custom-login",
            PasswordHash = "hash",
            SecurityVersion = 1
        };

        Assert.Equal("custom-login", mapping.Username(user));
    }

    [Fact]
    public void Build_DoesNotMap_PlainPassword_Property()
    {
        var options = new CredentialUserMappingOptions<PlainPasswordUser, Guid>();
        var ex = Assert.Throws<InvalidOperationException>(() => CredentialUserMappingBuilder.Build(options));

        Assert.Contains("PasswordHash mapping is required", ex.Message);
    }

    [Fact]
    public void Build_Defaults_CanAuthenticate_ToTrue()
    {
        var options = new CredentialUserMappingOptions<ConventionUser, Guid>();
        var mapping = CredentialUserMappingBuilder.Build(options);
        var user = new ConventionUser
        {
            Id = Guid.NewGuid(),
            Email = "active@example.com",
            PasswordHash = "hash",
            SecurityVersion = 0
        };

        var canAuthenticate = mapping.CanAuthenticate(user);
        Assert.True(canAuthenticate);
    }
}
