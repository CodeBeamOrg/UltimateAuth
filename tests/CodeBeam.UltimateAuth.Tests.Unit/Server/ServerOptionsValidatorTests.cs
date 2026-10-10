using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ServerOptionsValidatorTests
{
    [Fact]
    public void Server_session_options_with_negative_idle_timeout_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddUltimateAuth();
        services.AddUltimateAuthServer(o =>
        {
            o.Session.IdleTimeout = TimeSpan.FromSeconds(-5);
        });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionOptionsValidator>();

        services.AddOptions<UAuthServerOptions>().ValidateOnStart();

        var provider = services.BuildServiceProvider();

        Action act = () =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        };

        act.Should().Throw<OptionsValidationException>().WithMessage("*Session.IdleTimeout*");
    }

    [Fact]
    public void Valid_server_session_options_should_pass()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddUltimateAuth();
        services.AddUltimateAuthServer(o =>
        {
            o.Session.Lifetime = TimeSpan.FromMinutes(30);
            o.Session.IdleTimeout = TimeSpan.FromMinutes(10);
        });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionOptionsValidator>();

        services.AddOptions<UAuthServerOptions>().ValidateOnStart();

        var provider = services.BuildServiceProvider();

        provider.Should().NotBeNull();
    }

    [Fact]
    public void Server_token_options_with_small_opaque_id_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddUltimateAuth();
        services.AddUltimateAuthServer(o =>
        {
            o.Token.IssueOpaque = true;
            o.Token.OpaqueIdBytes = 8;
        });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerTokenOptionsValidator>();

        var provider = services.BuildServiceProvider();

        Action act = () =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        };

        act.Should().Throw<OptionsValidationException>().WithMessage("*OpaqueIdBytes*");
    }

    [Fact]
    public void Valid_server_token_options_should_pass()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddUltimateAuth();
        services.AddUltimateAuthServer(o =>
        {
            o.Token.IssueJwt = true;
            o.Token.IssueOpaque = true;
            o.Token.AccessTokenLifetime = TimeSpan.FromMinutes(5);
            o.Token.RefreshTokenLifetime = TimeSpan.FromDays(1);
            o.Token.OpaqueIdBytes = 32;
        });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerTokenOptionsValidator>();

        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;

        options.Should().NotBeNull();
    }

    [Fact]
    public void Pkce_authorization_code_lifetime_must_be_positive()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.Pkce.AuthorizationCodeLifetimeSeconds = 0;
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerPkceOptionsValidator>();
        var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        });

        Assert.Contains("Pkce.AuthorizationCodeLifetimeSeconds must be > 0", ex.Message);
    }

    [Fact]
    public void MultiTenant_enabled_without_resolver_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.MultiTenant.Enabled = true;
                o.MultiTenant.EnableRoute = false;
                o.MultiTenant.EnableHeader = false;
                o.MultiTenant.EnableDomain = false;
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerMultiTenantOptionsValidator>();

        var provider = services.BuildServiceProvider();

        Action act = () =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        };

        act.Should().Throw<OptionsValidationException>().WithMessage("*no tenant resolver is active*");
    }

    [Fact]
    public void MultiTenant_disabled_with_resolver_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.MultiTenant.Enabled = false;
                o.MultiTenant.EnableRoute = true; // no-meaning if multi-tenancy is disabled
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerMultiTenantOptionsValidator>();

        var provider = services.BuildServiceProvider();

        Action act = () =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        };

        act.Should().Throw<OptionsValidationException>().WithMessage("*Multi-tenancy is disabled*");
    }

    [Fact]
    public void Header_enabled_without_header_name_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.MultiTenant.Enabled = true;
                o.MultiTenant.EnableHeader = true;
                o.MultiTenant.HeaderName = "";
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerMultiTenantOptionsValidator>();

        var provider = services.BuildServiceProvider();

        Action act = () =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        };

        act.Should().Throw<OptionsValidationException>().WithMessage("*HeaderName must be specified*");
    }

    [Fact]
    public void Valid_multi_tenant_route_only_should_pass()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.MultiTenant.Enabled = true;
                o.MultiTenant.EnableRoute = true;
                o.MultiTenant.EnableHeader = false;
                o.MultiTenant.EnableDomain = false;
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerMultiTenantOptionsValidator>();

        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        options.MultiTenant.Enabled.Should().BeTrue();
        options.MultiTenant.EnableRoute.Should().BeTrue();
    }

    [Fact]
    public void UserIdentifiers_both_admin_and_user_override_disabled_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.Identifiers.Behavior.AllowAdminOverride = false;
                o.Identifiers.Behavior.AllowUserOverride = false;
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerUserIdentifierOptionsValidator>();

        var provider = services.BuildServiceProvider();

        Action act = () =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        };

        act.Should().Throw<OptionsValidationException>().WithMessage("*AllowAdminOverride and AllowUserOverride*");
    }

    [Fact]
    public void UserIdentifiers_at_least_one_override_enabled_should_pass()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.Identifiers.Behavior.AllowAdminOverride = true;
                o.Identifiers.Behavior.AllowUserOverride = false;
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerUserIdentifierOptionsValidator>();
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        options.Identifiers.Behavior.AllowAdminOverride.Should().BeTrue();
    }

    [Fact]
    public void No_session_resolver_enabled_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.SessionResolution.EnableBearer = false;
                o.SessionResolution.EnableHeader = false;
                o.SessionResolution.EnableCookie = false;
                o.SessionResolution.EnableQuery = false;
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionResolutionOptionsValidator>();
        var provider = services.BuildServiceProvider();
        Action act = () => _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        act.Should().Throw<OptionsValidationException>().WithMessage("*At least one session resolver must be enabled*");
    }

    [Fact]
    public void Disabled_resolver_in_order_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.SessionResolution.EnableBearer = true;
                o.SessionResolution.EnableQuery = false;
                o.SessionResolution.Order = new() { "Bearer", "Query" };
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionResolutionOptionsValidator>();
        var provider = services.BuildServiceProvider();
        Action act = () => _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        act.Should().Throw<OptionsValidationException>().WithMessage("*not enabled*");
    }

    [Fact]
    public void Header_enabled_without_name_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.SessionResolution.EnableHeader = true;
                o.SessionResolution.HeaderName = "";
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionResolutionOptionsValidator>();
        var provider = services.BuildServiceProvider();
        Action act = () => _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        act.Should().Throw<OptionsValidationException>().WithMessage("*HeaderName*");
    }

    [Fact]
    public void Valid_session_resolution_should_pass()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.SessionResolution.EnableBearer = true;
                o.SessionResolution.EnableHeader = false;
                o.SessionResolution.EnableCookie = false;
                o.SessionResolution.EnableQuery = false;
                o.SessionResolution.Order = new() { "Bearer" };
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionResolutionOptionsValidator>();
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        options.SessionResolution.EnableBearer.Should().BeTrue();
    }


    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Pagination_default_page_size_must_be_positive(int value)
    {
        var services = new ServiceCollection();

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.Pagination.DefaultPageSize = value;
            });

        services.AddSingleton<
            IValidateOptions<UAuthServerOptions>,
            UAuthServerPaginationOptionsValidator>();

        using var provider = services.BuildServiceProvider();

        Action act = () =>
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;

        act.Should()
            .Throw<OptionsValidationException>()
            .WithMessage("*Pagination.DefaultPageSize*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Pagination_max_page_size_must_be_positive(int value)
    {
        var services = new ServiceCollection();

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.Pagination.MaxPageSize = value;
            });

        services.AddSingleton<
            IValidateOptions<UAuthServerOptions>,
            UAuthServerPaginationOptionsValidator>();

        using var provider = services.BuildServiceProvider();

        Action act = () =>
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;

        act.Should()
            .Throw<OptionsValidationException>()
            .WithMessage("*Pagination.MaxPageSize*");
    }

    [Fact]
    public void Pagination_default_page_size_exceeding_maximum_should_fail()
    {
        var services = new ServiceCollection();

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.Pagination.DefaultPageSize = 200;
                o.Pagination.MaxPageSize = 100;
            });

        services.AddSingleton<
            IValidateOptions<UAuthServerOptions>,
            UAuthServerPaginationOptionsValidator>();

        using var provider = services.BuildServiceProvider();

        Action act = () =>
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;

        act.Should()
            .Throw<OptionsValidationException>()
            .WithMessage("*Pagination.DefaultPageSize*");
    }

    [Fact]
    public void Valid_pagination_options_should_pass()
    {
        var services = new ServiceCollection();

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.Pagination.DefaultPageSize = 50;
                o.Pagination.MaxPageSize = 100;
            });

        services.AddSingleton<
            IValidateOptions<UAuthServerOptions>,
            UAuthServerPaginationOptionsValidator>();

        using var provider = services.BuildServiceProvider();

        var options = provider
            .GetRequiredService<IOptions<UAuthServerOptions>>()
            .Value;

        options.Pagination.DefaultPageSize.Should().Be(50);
        options.Pagination.MaxPageSize.Should().Be(100);
    }

    [Fact]
    public void Pagination_default_page_size_equal_to_maximum_should_pass()
    {
        var services = new ServiceCollection();

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.Pagination.DefaultPageSize = 100;
                o.Pagination.MaxPageSize = 100;
            });

        services.AddSingleton<
            IValidateOptions<UAuthServerOptions>,
            UAuthServerPaginationOptionsValidator>();

        using var provider = services.BuildServiceProvider();

        var options = provider
            .GetRequiredService<IOptions<UAuthServerOptions>>()
            .Value;

        options.Pagination.DefaultPageSize.Should().Be(100);
        options.Pagination.MaxPageSize.Should().Be(100);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Login_negative_max_attempts_fails(int attempts) =>
        ShouldReject<UAuthServerLoginOptionsValidator>(o => o.Login.MaxFailedAttempts = attempts, "Login.MaxFailedAttempts");

    [Fact]
    public void Login_negative_lockout_duration_fails() =>
        ShouldReject<UAuthServerLoginOptionsValidator>(o => o.Login.LockoutDuration = TimeSpan.FromTicks(-1), "Login.LockoutMinutes");

    [Fact]
    public void Login_zero_limits_are_accepted() =>
        ShouldAccept<UAuthServerLoginOptionsValidator>(o =>
        {
            o.Login.MaxFailedAttempts = 0;
            o.Login.LockoutDuration = TimeSpan.Zero;
        });

    [Fact]
    public void Login_multiple_errors_are_reported() =>
        ShouldReject<UAuthServerLoginOptionsValidator>(o =>
        {
            o.Login.MaxFailedAttempts = -1;
            o.Login.LockoutDuration = TimeSpan.FromMinutes(-1);
        }, "Login.LockoutMinutes");

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void MultiTenant_disabled_with_any_resolver_fails(bool route, bool header, bool domain) =>
        ShouldReject<UAuthServerMultiTenantOptionsValidator>(o =>
        {
            o.MultiTenant.Enabled = false;
            o.MultiTenant.EnableRoute = route;
            o.MultiTenant.EnableHeader = header;
            o.MultiTenant.EnableDomain = domain;
        }, "Multi-tenancy is disabled");

    [Fact]
    public void MultiTenant_disabled_without_resolvers_passes() =>
        ShouldAccept<UAuthServerMultiTenantOptionsValidator>(o =>
        {
            o.MultiTenant.Enabled = false;
            o.MultiTenant.EnableRoute = false;
            o.MultiTenant.EnableHeader = false;
            o.MultiTenant.EnableDomain = false;
        });

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void MultiTenant_enabled_with_any_resolver_passes(bool route, bool header, bool domain) =>
        ShouldAccept<UAuthServerMultiTenantOptionsValidator>(o =>
        {
            o.MultiTenant.Enabled = true;
            o.MultiTenant.EnableRoute = route;
            o.MultiTenant.EnableHeader = header;
            o.MultiTenant.EnableDomain = domain;
            if (header) o.MultiTenant.HeaderName = "X-Tenant";
        });

    [Fact]
    public void MultiTenant_header_whitespace_fails() =>
        ShouldReject<UAuthServerMultiTenantOptionsValidator>(o =>
        {
            o.MultiTenant.Enabled = true;
            o.MultiTenant.EnableHeader = true;
            o.MultiTenant.HeaderName = "  ";
        }, "MultiTenant.HeaderName");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Pkce_nonpositive_authorization_lifetime_fails(int seconds) =>
        ShouldReject<UAuthServerPkceOptionsValidator>(o => o.Pkce.AuthorizationCodeLifetimeSeconds = seconds,
            "Pkce.AuthorizationCodeLifetimeSeconds");

    [Fact]
    public void Pkce_positive_authorization_lifetime_passes() =>
        ShouldAccept<UAuthServerPkceOptionsValidator>(o => o.Pkce.AuthorizationCodeLifetimeSeconds = 1);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Session_nonpositive_lifetime_fails(int ticks) =>
        ShouldReject<UAuthServerSessionOptionsValidator>(o => o.Session.Lifetime = TimeSpan.FromTicks(ticks),
            "Session.Lifetime");

    [Fact]
    public void Session_nonpositive_max_lifetime_fails() =>
        ShouldReject<UAuthServerSessionOptionsValidator>(o => o.Session.MaxLifetime = TimeSpan.Zero,
            "Session.MaxLifetime");

    [Fact]
    public void Session_max_lifetime_less_than_lifetime_fails() =>
        ShouldReject<UAuthServerSessionOptionsValidator>(o =>
        {
            o.Session.Lifetime = TimeSpan.FromMinutes(30);
            o.Session.MaxLifetime = TimeSpan.FromMinutes(29);
        }, "Session.MaxLifetime");

    [Fact]
    public void Session_equal_max_lifetime_passes() =>
        ShouldAccept<UAuthServerSessionOptionsValidator>(o =>
        {
            o.Session.Lifetime = TimeSpan.FromMinutes(30);
            o.Session.MaxLifetime = TimeSpan.FromMinutes(30);
            o.Session.IdleTimeout = TimeSpan.Zero;
        });

    [Fact]
    public void Session_null_optional_timeouts_pass() =>
        ShouldAccept<UAuthServerSessionOptionsValidator>(o =>
        {
            o.Session.MaxLifetime = null;
            o.Session.IdleTimeout = null;
        });

    [Fact]
    public void Token_negative_concurrent_refresh_window_fails() =>
        ShouldReject<UAuthServerTokenOptionsValidator>(o =>
            o.Token.RefreshTokenConcurrentRequestWindow = TimeSpan.FromTicks(-1),
            "RefreshTokenConcurrentRequestWindow");

    [Fact]
    public void Token_no_issuance_fails_under_current_validator() =>
        ShouldReject<UAuthServerTokenOptionsValidator>(o =>
        {
            o.Token.IssueJwt = false;
            o.Token.IssueOpaque = false;
        }, "IssueJwt or IssueOpaque");

    [Fact]
    public void Token_nonpositive_access_lifetime_fails() =>
        ShouldReject<UAuthServerTokenOptionsValidator>(o => o.Token.AccessTokenLifetime = TimeSpan.Zero,
            "AccessTokenLifetime");

    [Fact]
    public void Token_nonpositive_refresh_lifetime_fails_when_enabled() =>
        ShouldReject<UAuthServerTokenOptionsValidator>(o =>
        {
            o.Token.IssueRefresh = true;
            o.Token.RefreshTokenLifetime = TimeSpan.Zero;
        }, "RefreshTokenLifetime");

    [Fact]
    public void Token_refresh_lifetime_equal_to_access_lifetime_fails() =>
        ShouldReject<UAuthServerTokenOptionsValidator>(o =>
        {
            o.Token.IssueRefresh = true;
            o.Token.AccessTokenLifetime = TimeSpan.FromMinutes(10);
            o.Token.RefreshTokenLifetime = TimeSpan.FromMinutes(10);
        }, "RefreshTokenLifetime");

    [Theory]
    [InlineData(true, " ", "UAuthClient", "Token.Issuer")]
    [InlineData(true, "ab", "UAuthClient", "Token.Issuer")]
    [InlineData(true, "UAuth", " ", "Token.Audience")]
    [InlineData(true, "UAuth", "ab", "Token.Audience")]
    public void Token_invalid_jwt_identifiers_fail(bool issueJwt, string issuer, string audience, string expected) =>
        ShouldReject<UAuthServerTokenOptionsValidator>(o =>
        {
            o.Token.IssueJwt = issueJwt;
            o.Token.Issuer = issuer;
            o.Token.Audience = audience;
        }, expected);

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(129)]
    public void Token_opaque_entropy_outside_limits_fails(int bytes) =>
        ShouldReject<UAuthServerTokenOptionsValidator>(o =>
        {
            o.Token.IssueOpaque = true;
            o.Token.OpaqueIdBytes = bytes;
        }, "OpaqueIdBytes");

    [Theory]
    [InlineData(16)]
    [InlineData(128)]
    public void Token_opaque_entropy_boundary_values_pass(int bytes) =>
        ShouldAccept<UAuthServerTokenOptionsValidator>(o =>
        {
            o.Token.IssueOpaque = true;
            o.Token.OpaqueIdBytes = bytes;
        });

    [Fact]
    public void Token_jwt_fields_are_not_required_when_jwt_disabled() =>
        ShouldAccept<UAuthServerTokenOptionsValidator>(o =>
        {
            o.Token.IssueJwt = false;
            o.Token.IssueOpaque = true;
            o.Token.Issuer = "";
            o.Token.Audience = "";
        });

    [Fact]
    public void Token_refresh_lifetime_is_not_checked_when_refresh_disabled() =>
        ShouldAccept<UAuthServerTokenOptionsValidator>(o =>
        {
            o.Token.IssueRefresh = false;
            o.Token.RefreshTokenLifetime = TimeSpan.Zero;
        });

    [Fact]
    public void Token_multiple_errors_are_reported() =>
        ShouldReject<UAuthServerTokenOptionsValidator>(o =>
        {
            o.Token.AccessTokenLifetime = TimeSpan.Zero;
            o.Token.OpaqueIdBytes = 1;
        }, "OpaqueIdBytes");

    [Fact]
    public void Identifiers_only_user_override_passes() =>
        ShouldAccept<UAuthServerUserIdentifierOptionsValidator>(o =>
        {
            o.Identifiers.Behavior.AllowAdminOverride = false;
            o.Identifiers.Behavior.AllowUserOverride = true;
        });

    [Fact]
    public void Identifiers_both_overrides_enabled_pass() =>
        ShouldAccept<UAuthServerUserIdentifierOptionsValidator>(o =>
        {
            o.Identifiers.Behavior.AllowAdminOverride = true;
            o.Identifiers.Behavior.AllowUserOverride = true;
        });

    [Fact]
    public void SessionResolution_empty_order_fails() =>
        ShouldReject<UAuthServerSessionResolutionOptionsValidator>(o =>
        {
            o.SessionResolution.EnableBearer = true;
            o.SessionResolution.Order = new();
        }, "SessionResolution.Order");

    [Fact]
    public void SessionResolution_unknown_resolver_fails() =>
        ShouldReject<UAuthServerSessionResolutionOptionsValidator>(o =>
        {
            o.SessionResolution.EnableBearer = true;
            o.SessionResolution.Order = new() { "Bearer", "Unknown" };
        }, "Unknown session resolver");

    [Fact]
    public void SessionResolution_query_without_parameter_name_fails() =>
        ShouldReject<UAuthServerSessionResolutionOptionsValidator>(o =>
        {
            o.SessionResolution.EnableBearer = true;
            o.SessionResolution.EnableQuery = true;
            o.SessionResolution.Order = new() { "Bearer", "Query" };
            o.SessionResolution.QueryParameterName = " ";
        }, "QueryParameterName");

    [Theory]
    [InlineData("Bearer")]
    [InlineData("Header")]
    [InlineData("Cookie")]
    [InlineData("Query")]
    public void SessionResolution_enabled_resolver_names_are_case_insensitive(string resolver) =>
        ShouldAccept<UAuthServerSessionResolutionOptionsValidator>(o =>
        {
            o.SessionResolution.EnableBearer = resolver == "Bearer";
            o.SessionResolution.EnableHeader = resolver == "Header";
            o.SessionResolution.EnableCookie = resolver == "Cookie";
            o.SessionResolution.EnableQuery = resolver == "Query";
            o.SessionResolution.HeaderName = "X-Session";
            o.SessionResolution.QueryParameterName = "session";
            o.SessionResolution.Order = new() { resolver.ToLowerInvariant() };
        });

    [Fact]
    public void Pagination_null_configuration_fails() =>
        ShouldReject<UAuthServerPaginationOptionsValidator>(o => o.Pagination = null!,
            "Pagination configuration cannot be null");

    [Fact]
    public void Pagination_multiple_errors_are_reported() =>
        ShouldReject<UAuthServerPaginationOptionsValidator>(o =>
        {
            o.Pagination.DefaultPageSize = -1;
            o.Pagination.MaxPageSize = 0;
        }, "Pagination.MaxPageSize");

    private static void ConfigureValidCrossOptions(UAuthServerOptions o)
    {
        o.AllowedModes = new[] { UAuthMode.Hybrid };
        o.Session.Lifetime = TimeSpan.FromDays(7);
        o.Session.MaxLifetime = TimeSpan.FromDays(10);
        o.Token.AccessTokenLifetime = TimeSpan.FromMinutes(10);
        o.Token.RefreshTokenLifetime = TimeSpan.FromDays(7);
    }

    [Fact]
    public void Server_base_path_missing_fails() =>
        ShouldReject<UAuthServerOptionsValidator>(o =>
        {
            ConfigureValidCrossOptions(o);
            o.Endpoints.BasePath = " ";
        }, "BasePath must be specified");

    [Fact]
    public void Server_base_path_double_slash_fails() =>
        ShouldReject<UAuthServerOptionsValidator>(o =>
        {
            ConfigureValidCrossOptions(o);
            o.Endpoints.BasePath = "/auth//api";
        }, "BasePath cannot contain");

    [Fact]
    public void Server_undefined_mode_fails() =>
        ShouldReject<UAuthServerOptionsValidator>(o =>
        {
            ConfigureValidCrossOptions(o);
            o.AllowedModes = new[] { (UAuthMode)999 };
        }, "Invalid UAuthMode");

    [Theory]
    [InlineData(UAuthMode.SemiHybrid)]
    [InlineData(UAuthMode.PureJwt)]
    public void Server_unimplemented_mode_fails(UAuthMode mode) =>
        ShouldReject<UAuthServerOptionsValidator>(o =>
        {
            ConfigureValidCrossOptions(o);
            o.AllowedModes = new[] { mode };
        }, "not implemented yet");

    [Theory]
    [InlineData(UAuthMode.Hybrid)]
    [InlineData(UAuthMode.PureOpaque)]
    public void Server_implemented_modes_pass(UAuthMode mode) =>
        ShouldAccept<UAuthServerOptionsValidator>(o =>
        {
            ConfigureValidCrossOptions(o);
            o.AllowedModes = new[] { mode };
        });

    [Fact]
    public void Server_null_allowed_modes_pass_under_current_rules() =>
        ShouldAccept<UAuthServerOptionsValidator>(o =>
        {
            ConfigureValidCrossOptions(o);
            o.AllowedModes = null;
        });

    [Fact]
    public void Server_empty_allowed_modes_pass_under_current_rules() =>
        ShouldAccept<UAuthServerOptionsValidator>(o =>
        {
            ConfigureValidCrossOptions(o);
            o.AllowedModes = Array.Empty<UAuthMode>();
        });

    [Fact]
    public void Server_session_lifetime_nonpositive_fails() =>
        ShouldReject<UAuthServerOptionsValidator>(o =>
        {
            ConfigureValidCrossOptions(o);
            o.Session.Lifetime = TimeSpan.Zero;
        }, "Session.Lifetime");

    [Fact]
    public void Server_session_max_lifetime_nonpositive_fails() =>
        ShouldReject<UAuthServerOptionsValidator>(o =>
        {
            ConfigureValidCrossOptions(o);
            o.Session.MaxLifetime = TimeSpan.Zero;
        }, "Session.MaxLifetime");

    [Fact]
    public void Server_access_token_exceeding_session_max_fails() =>
        ShouldReject<UAuthServerOptionsValidator>(o =>
        {
            ConfigureValidCrossOptions(o);
            o.Session.MaxLifetime = TimeSpan.FromMinutes(5);
            o.Token.AccessTokenLifetime = TimeSpan.FromMinutes(6);
            o.Token.RefreshTokenLifetime = TimeSpan.FromMinutes(4);
        }, "Token.AccessTokenLifetime");

    [Fact]
    public void Server_refresh_token_exceeding_session_max_fails() =>
        ShouldReject<UAuthServerOptionsValidator>(o =>
        {
            ConfigureValidCrossOptions(o);
            o.Session.MaxLifetime = TimeSpan.FromMinutes(5);
            o.Token.AccessTokenLifetime = TimeSpan.FromMinutes(1);
            o.Token.RefreshTokenLifetime = TimeSpan.FromMinutes(6);
        }, "Token.RefreshTokenLifetime");

    [Fact]
    public void Server_token_lifetimes_equal_to_session_max_pass() =>
        ShouldAccept<UAuthServerOptionsValidator>(o =>
        {
            ConfigureValidCrossOptions(o);
            o.Session.MaxLifetime = TimeSpan.FromDays(7);
            o.Token.RefreshTokenLifetime = TimeSpan.FromDays(7);
        });

    [Fact]
    public void Server_null_session_max_lifetime_passes_under_current_rules() =>
        ShouldAccept<UAuthServerOptionsValidator>(o =>
        {
            ConfigureValidCrossOptions(o);
            o.Session.MaxLifetime = null;
        });


    private static void ShouldReject<TValidator>(Action<UAuthServerOptions> configure, string expected)
        where TValidator : class, IValidateOptions<UAuthServerOptions>, new()
    {
        var services = new ServiceCollection();
        services.AddOptions<UAuthServerOptions>().Configure(configure);
        services.AddSingleton<IValidateOptions<UAuthServerOptions>, TValidator>();
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value);
        Assert.Contains(expected, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void ShouldAccept<TValidator>(Action<UAuthServerOptions> configure)
        where TValidator : class, IValidateOptions<UAuthServerOptions>, new()
    {
        var services = new ServiceCollection();
        services.AddOptions<UAuthServerOptions>().Configure(configure);
        services.AddSingleton<IValidateOptions<UAuthServerOptions>, TValidator>();
        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
    }
}
