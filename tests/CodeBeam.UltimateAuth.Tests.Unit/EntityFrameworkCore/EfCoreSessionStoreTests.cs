using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.Data.Sqlite;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class EfCoreSessionStoreTests : EfCoreTestBase
{
    private const string ValidRaw = "session-aaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private static UAuthSessionDbContext CreateDb(SqliteConnection connection)
    {
        return CreateDbContext<UAuthSessionDbContext>(connection, options => new UAuthSessionDbContext(options));
    }

    [Fact]
    public async Task Create_And_Get_Session_Should_Work()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreSessionStore(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var root = UAuthSessionRoot.Create(
            tenant,
            userKey,
            DateTimeOffset.UtcNow);

        var chain = UAuthSessionChain.Create(
            SessionChainId.New(),
            root.RootId,
            tenant,
            userKey,
            DateTimeOffset.UtcNow,
            null,
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            0);

        AuthSessionId.TryCreate(ValidRaw, out var sessionId);

        var session = UAuthSession.Create(
            sessionId,
            tenant,
            userKey,
            chain.ChainId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            0,
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            SessionMetadata.Empty);

        await store.ExecuteAsync(async ct =>
        {
            await store.CreateRootAsync(root, ct);
            await store.CreateChainAsync(chain, ct);
            await store.CreateSessionAsync(session, ct);
        });

        var result = await store.GetSessionAsync(session.SessionId);

        Assert.NotNull(result);
        Assert.Equal(session.SessionId, result!.SessionId);
    }

    [Fact]
    public async Task Session_Should_Persist_DeviceContext()
    {
        using var connection = CreateOpenConnection();
        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var device = DeviceContext.Create(
            DeviceId.Create("1234567890123456"),
            deviceType: "mobile",
            platform: "ios",
            operatingSystem: "ios",
            browser: "safari",
            ipAddress: "127.0.0.1");

        AuthSessionId.TryCreate(ValidRaw, out var sessionId);

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            var root = UAuthSessionRoot.Create(tenant, userKey, DateTimeOffset.UtcNow);

            var chain = UAuthSessionChain.Create(
                SessionChainId.New(),
                root.RootId,
                tenant,
                userKey,
                DateTimeOffset.UtcNow,
                null,
                device,
                ClaimsSnapshot.Empty,
                0);

            var session = UAuthSession.Create(
                sessionId,
                tenant,
                userKey,
                chain.ChainId,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                0,
                device,
                ClaimsSnapshot.Empty,
                SessionMetadata.Empty);

            await store.ExecuteAsync(async ct =>
            {
                await store.CreateRootAsync(root, ct);
                await store.CreateChainAsync(chain, ct);
                await store.CreateSessionAsync(session, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            var result = await store.GetSessionAsync(sessionId);

            Assert.NotNull(result);
            Assert.NotNull(result!.Device.DeviceId);
            Assert.Equal("mobile", result.Device.DeviceType);
        }
    }

    [Fact]
    public async Task Session_Should_Persist_Claims_And_Metadata()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        AuthSessionId.TryCreate(ValidRaw, out var sessionId);

        var claims = ClaimsSnapshot.From(
            (ClaimTypes.Role, "admin"),
            (ClaimTypes.Role, "user"),
            ("uauth:permission", "read"),
            ("uauth:permission", "write"));

        var metadata = new SessionMetadata
        {
            AppVersion = "1.0.0",
            Locale = "en-US",
            CsrfToken = "csrf-token-123",
            Custom = new Dictionary<string, object>
            {
                ["theme"] = "dark",
                ["feature_flag"] = true
            }
        };

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            var root = UAuthSessionRoot.Create(tenant, userKey, DateTimeOffset.UtcNow);

            var chain = UAuthSessionChain.Create(
                SessionChainId.New(),
                root.RootId,
                tenant,
                userKey,
                DateTimeOffset.UtcNow,
                null,
                TestDevice.Default(),
                claims,
                0);

            var session = UAuthSession.Create(
                sessionId,
                tenant,
                userKey,
                chain.ChainId,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                0,
                TestDevice.Default(),
                claims,
                metadata);

            await store.ExecuteAsync(async ct =>
            {
                await store.CreateRootAsync(root, ct);
                await store.CreateChainAsync(chain, ct);
                await store.CreateSessionAsync(session, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            var result = await store.GetSessionAsync(sessionId);

            Assert.NotNull(result);

            Assert.True(result!.Claims.IsInRole("admin"));
            Assert.True(result.Claims.HasPermission("read"));
            Assert.Equal(2, result.Claims.Roles.Count);

            Assert.Equal("1.0.0", result.Metadata.AppVersion);
            Assert.Equal("en-US", result.Metadata.Locale);
            Assert.Equal("csrf-token-123", result.Metadata.CsrfToken);

            Assert.NotNull(result.Metadata.Custom);
            Assert.Equal("dark", result.Metadata.Custom!["theme"].ToString());
        }
    }

    [Fact]
    public async Task Revoke_Session_Should_Work()
    {
        using var connection = CreateOpenConnection();
        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        AuthSessionId.TryCreate(ValidRaw, out var sessionId);

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            var root = UAuthSessionRoot.Create(tenant, userKey, DateTimeOffset.UtcNow);

            var chain = UAuthSessionChain.Create(
                SessionChainId.New(),
                root.RootId,
                tenant,
                userKey,
                DateTimeOffset.UtcNow,
                null,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                0);

            var session = UAuthSession.Create(
                sessionId,
                tenant,
                userKey,
                chain.ChainId,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                0,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                SessionMetadata.Empty);

            await store.ExecuteAsync(async ct =>
            {
                await store.CreateRootAsync(root, ct);
                await store.CreateChainAsync(chain, ct);
                await store.CreateSessionAsync(session, ct);
            });

            var revoked = await store.RevokeSessionAsync(sessionId, DateTimeOffset.UtcNow);

            Assert.True(revoked);
        }
    }

    [Fact]
    public async Task Should_Not_See_Session_From_Other_Tenant()
    {
        using var connection = CreateOpenConnection();

        var tenant1 = TenantKeys.Single;
        var tenant2 = TenantKey.FromInternal("tenant-2");

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        AuthSessionId.TryCreate(ValidRaw, out var sessionId);

        await using (var db = CreateDb(connection))
        {
            var store1 = new EfCoreSessionStore(db, new TenantContext(tenant1));

            var root = UAuthSessionRoot.Create(tenant1, userKey, DateTimeOffset.UtcNow);

            var chain = UAuthSessionChain.Create(
                SessionChainId.New(),
                root.RootId,
                tenant1,
                userKey,
                DateTimeOffset.UtcNow,
                null,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                0);

            var session = UAuthSession.Create(
                sessionId,
                tenant1,
                userKey,
                chain.ChainId,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                0,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                SessionMetadata.Empty);

            await store1.ExecuteAsync(async ct =>
            {
                await store1.CreateRootAsync(root, ct);
                await store1.CreateChainAsync(chain, ct);
                await store1.CreateSessionAsync(session, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store2 = new EfCoreSessionStore(db, new TenantContext(tenant2));

            var result = await store2.GetSessionAsync(sessionId);

            Assert.Null(result);
        }
    }

    [Fact]
    public async Task ExecuteAsync_Should_Rollback_On_Error()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await store.ExecuteAsync(async ct =>
                {
                    throw new InvalidOperationException("boom");
                });
            });
        }
    }

    [Fact]
    public async Task GetSessionsByChain_Should_Return_Sessions()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        AuthSessionId.TryCreate(ValidRaw, out var sessionId);
        SessionChainId chainId;

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));
            var root = UAuthSessionRoot.Create(tenant, userKey, DateTimeOffset.UtcNow);
            chainId = SessionChainId.New();

            var chain = UAuthSessionChain.Create(
                chainId,
                root.RootId,
                tenant,
                userKey,
                DateTimeOffset.UtcNow,
                null,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                0);

            var session = UAuthSession.Create(
                sessionId,
                tenant,
                userKey,
                chainId,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                0,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                SessionMetadata.Empty);

            await store.ExecuteAsync(async ct =>
            {
                await store.CreateRootAsync(root, ct);
                await store.CreateChainAsync(chain, ct);
                await store.CreateSessionAsync(session, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));
            var sessions = await store.GetSessionsByChainAsync(chainId);
            Assert.Single(sessions);
        }
    }

    [Fact]
    public async Task ExecuteAsync_Should_Commit_Multiple_Operations()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        AuthSessionId.TryCreate(ValidRaw, out var sessionId);

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            await store.ExecuteAsync(async ct =>
            {
                var root = UAuthSessionRoot.Create(tenant, userKey, DateTimeOffset.UtcNow);

                var chain = UAuthSessionChain.Create(
                    SessionChainId.New(),
                    root.RootId,
                    tenant,
                    userKey,
                    DateTimeOffset.UtcNow,
                    null,
                    TestDevice.Default(),
                    ClaimsSnapshot.Empty,
                    0);

                var session = UAuthSession.Create(
                    sessionId,
                    tenant,
                    userKey,
                    chain.ChainId,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddHours(1),
                    0,
                    TestDevice.Default(),
                    ClaimsSnapshot.Empty,
                    SessionMetadata.Empty);

                await store.CreateRootAsync(root, ct);
                await store.CreateChainAsync(chain, ct);
                await store.CreateSessionAsync(session, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            var result = await store.GetSessionAsync(sessionId);

            Assert.NotNull(result);
        }
    }

    [Fact]
    public async Task ExecuteAsync_Should_Rollback_All_On_Failure()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        AuthSessionId.TryCreate(ValidRaw, out var sessionId);

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await store.ExecuteAsync(async ct =>
                {
                    var root = UAuthSessionRoot.Create(tenant, userKey, DateTimeOffset.UtcNow);

                    await store.CreateRootAsync(root, ct);

                    // 💥 simulate failure
                    throw new InvalidOperationException("boom");
                });
            });
        }

        await using (var db = CreateDb(connection))
        {
            var count = db.Roots.Count();
            Assert.Equal(0, count);
        }
    }

    [Fact]
    public async Task RevokeChainCascade_Should_Revoke_All_Sessions()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        AuthSessionId.TryCreate(ValidRaw, out var sessionId);
        SessionChainId chainId;

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));
            var root = UAuthSessionRoot.Create(tenant, userKey, DateTimeOffset.UtcNow);

            chainId = SessionChainId.New();

            var chain = UAuthSessionChain.Create(
                chainId,
                root.RootId,
                tenant,
                userKey,
                DateTimeOffset.UtcNow,
                null,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                0);

            var session = UAuthSession.Create(
                sessionId,
                tenant,
                userKey,
                chainId,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                0,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                SessionMetadata.Empty);

            await store.ExecuteAsync(async ct =>
            {
                await store.CreateRootAsync(root, ct);
                await store.CreateChainAsync(chain, ct);
                await store.CreateSessionAsync(session, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            await store.ExecuteAsync(async ct =>
            {
                await store.RevokeChainCascadeAsync(chainId, DateTimeOffset.UtcNow, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));
            var sessions = await store.GetSessionsByChainAsync(chainId);

            Assert.All(sessions, s => Assert.True(s.IsRevoked));
        }
    }

    [Fact]
    public async Task SetActiveSession_Should_Work()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        AuthSessionId.TryCreate(ValidRaw, out var sessionId);
        SessionChainId chainId;

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            var root = UAuthSessionRoot.Create(tenant, userKey, DateTimeOffset.UtcNow);

            chainId = SessionChainId.New();

            var chain = UAuthSessionChain.Create(
                chainId,
                root.RootId,
                tenant,
                userKey,
                DateTimeOffset.UtcNow,
                null,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                0);

            await store.ExecuteAsync(async ct =>
            {
                await store.CreateRootAsync(root, ct);
                await store.CreateChainAsync(chain, ct);
                await store.SetActiveSessionIdAsync(chainId, sessionId);
            });

        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));
            var active = await store.GetActiveSessionIdAsync(chainId);

            Assert.Equal(sessionId, active);
        }
    }

    [Fact]
    public async Task Query_Should_Not_Use_Domain_Computed_Properties()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var ex = await Record.ExceptionAsync(async () =>
        {
            db.Sessions
                .Where(x => x.RevokedAt == null)
                .ToList();
        });

        Assert.Null(ex);
    }

    [Fact]
    public async Task SaveSession_Should_Increment_Version()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        AuthSessionId.TryCreate(ValidRaw, out var sessionId);
        SessionChainId chainId;

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            var root = UAuthSessionRoot.Create(tenant, userKey, DateTimeOffset.UtcNow);
            chainId = SessionChainId.New();

            var chain = UAuthSessionChain.Create(
                chainId,
                root.RootId,
                tenant,
                userKey,
                DateTimeOffset.UtcNow,
                null,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                0);

            var session = UAuthSession.Create(
                sessionId,
                tenant,
                userKey,
                chainId,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                0,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                SessionMetadata.Empty);

            await store.ExecuteAsync(async ct =>
            {
                await store.CreateRootAsync(root, ct);
                await store.CreateChainAsync(chain, ct);
                await store.CreateSessionAsync(session, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            await store.ExecuteAsync(async ct =>
            {
                var existing = await store.GetSessionAsync(sessionId, ct);
                var updated = existing!.Revoke(DateTimeOffset.UtcNow);

                await store.SaveSessionAsync(updated, expectedVersion: 0, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));
            var result = await store.GetSessionAsync(sessionId);

            Assert.Equal(1, result!.Version);
        }
    }

    [Fact]
    public async Task SaveSession_With_Wrong_Version_Should_Throw()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        AuthSessionId.TryCreate(ValidRaw, out var sessionId);
        SessionChainId chainId;

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            var root = UAuthSessionRoot.Create(tenant, userKey, DateTimeOffset.UtcNow);
            chainId = SessionChainId.New();

            var chain = UAuthSessionChain.Create(
                chainId,
                root.RootId,
                tenant,
                userKey,
                DateTimeOffset.UtcNow,
                null,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                0);

            var session = UAuthSession.Create(
                sessionId,
                tenant,
                userKey,
                chainId,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                0,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                SessionMetadata.Empty);

            await store.ExecuteAsync(async ct =>
            {
                await store.CreateRootAsync(root, ct);
                await store.CreateChainAsync(chain, ct);
                await store.CreateSessionAsync(session, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            await Assert.ThrowsAsync<UAuthConcurrencyException>(async () =>
            {
                await store.ExecuteAsync(async ct =>
                {
                    var existing = await store.GetSessionAsync(sessionId, ct);
                    var updated = existing!.Revoke(DateTimeOffset.UtcNow);

                    await store.SaveSessionAsync(updated, expectedVersion: 999, ct);
                });
            });
        }
    }

    [Fact]
    public async Task SaveChain_Should_Increment_Version()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        SessionChainId chainId;

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            var root = UAuthSessionRoot.Create(tenant, userKey, DateTimeOffset.UtcNow);
            chainId = SessionChainId.New();

            var chain = UAuthSessionChain.Create(
                chainId,
                root.RootId,
                tenant,
                userKey,
                DateTimeOffset.UtcNow,
                null,
                TestDevice.Default(),
                ClaimsSnapshot.Empty,
                0);

            await store.ExecuteAsync(async ct =>
            {
                await store.CreateRootAsync(root, ct);
                await store.CreateChainAsync(chain, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            await store.ExecuteAsync(async ct =>
            {
                var existing = await store.GetChainAsync(chainId, ct);
                var updated = existing!.Revoke(DateTimeOffset.UtcNow);

                await store.SaveChainAsync(updated, expectedVersion: 0, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));
            var result = await store.GetChainAsync(chainId);

            Assert.Equal(1, result!.Version);
        }
    }

    [Fact]
    public async Task SaveRoot_Should_Increment_Version()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            var root = UAuthSessionRoot.Create(
                tenant,
                userKey,
                DateTimeOffset.UtcNow);

            await store.ExecuteAsync(async ct =>
            {
                await store.CreateRootAsync(root, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));

            await store.ExecuteAsync(async ct =>
            {
                var existing = await store.GetRootByUserAsync(userKey, ct);
                var updated = existing!.Revoke(DateTimeOffset.UtcNow);

                await store.SaveRootAsync(updated, expectedVersion: 0, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreSessionStore(db, new TenantContext(tenant));
            var result = await store.GetRootByUserAsync(userKey);

            Assert.Equal(1, result!.Version);
        }
    }
}
