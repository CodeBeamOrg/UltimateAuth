using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Integration;

public sealed class ManagementHttpTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private const string Password = "Http-test-password-8!";

    private static async Task<UserKey> RegisterAsync(LabHttpHost host, LabDatabase db, string name)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var registration = new LabRegistrationService(ActorProvider.For(null), sp.GetRequiredService<IUserApplicationService>(),
            sp.GetRequiredService<IRoleStoreFactory>(),
            sp.GetRequiredService<IUserRoleStoreFactory>(),
            new(db), new(db, NullLogger<ActivityLogService>.Instance));
        var registered = await registration.RegisterAsync(new(name, $"{name}@example.test", Password), TestContext.Current.CancellationToken);
        Assert.True(registered.IsSuccess, registered.ErrorMessage);
        await using var verify = db.CreateDbContext();
        return (await verify.Sandboxes.SingleAsync(x => x.Name == $"{name}'s Sandbox")).OwnerUserKey;
    }

    [Fact]
    public async Task Browser_request_provisions_as_validated_owner_enforces_capacity_membership_and_target_isolation()
    {
        await using var db = await postgres.CreateAsync();
        await using var host = await LabHttpHost.CreateAsync(db);
        var owner = await RegisterAsync(host, db, "owner-http");
        var foreign = await RegisterAsync(host, db, "foreign-http");
        using var browser = host.Browser();
        await LabHttpHost.LoginAsync(browser, "owner-http", Password);

        // Unknown actor/tenant fields are ignored: only the validated session is authoritative.
        using var created = await LabHttpHost.PostAsync(browser, "/api/lab/manage/users", new
        {
            userName = "member-http", email = "member-http@example.test", password = Password,
            actorUserKey = foreign.Value, tenant = "forged-tenant"
        });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        Assert.True((await created.Content.ReadFromJsonAsync<CreateUserDialogResult>(cancellationToken: TestContext.Current.CancellationToken))!.IsSuccess);
        UserKey member;
        await using (var verify = db.CreateDbContext())
        {
            var membership = await verify.SandboxMembers.Include(x => x.Sandbox)
                .SingleAsync(x => x.Role == SandboxMemberRole.Member, TestContext.Current.CancellationToken);
            Assert.Equal(owner, membership.Sandbox.OwnerUserKey);
            member = membership.UserKey;
            var settings = await verify.SandboxSettings.SingleAsync(x => x.SandboxId == membership.SandboxId, TestContext.Current.CancellationToken);
            settings.MaxUsers = 2;
            await verify.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var roles = await scope.ServiceProvider.GetRequiredService<IUserRoleStoreFactory>()
                .Create(TenantKeys.Single).GetAssignmentsAsync(member, TestContext.Current.CancellationToken);
            Assert.Contains(roles, x => x.RoleId == LabRoleIds.User);
        }
        using var capacity = await LabHttpHost.PostAsync(browser, "/api/lab/manage/users", new CreateUserRequest
            { UserName = "over-capacity", Email = "capacity@example.test", Password = Password });
        var capacityResult = await capacity.Content.ReadFromJsonAsync<CreateUserDialogResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(capacityResult!.IsSuccess);
        Assert.Contains("limit", capacityResult.ErrorMessage);

        using var memberBrowser = host.Browser("member-device-0001");
        await LabHttpHost.LoginAsync(memberBrowser, "member-http", Password);
        using var denied = await LabHttpHost.PostAsync(memberBrowser, "/api/lab/manage/users", new CreateUserRequest
            { UserName = "forbidden", Email = "forbidden@example.test", Password = Password });
        Assert.False((await denied.Content.ReadFromJsonAsync<CreateUserDialogResult>(cancellationToken: TestContext.Current.CancellationToken))!.IsSuccess);
        using var memberRepair = await LabHttpHost.PostAsync(memberBrowser, "/api/lab/admin/repair/scan");
        Assert.Equal(HttpStatusCode.Forbidden, memberRepair.StatusCode);
        using var ownerRepair = await LabHttpHost.PostAsync(browser, "/api/lab/admin/repair/scan");
        Assert.Equal(HttpStatusCode.Forbidden, ownerRepair.StatusCode);

        using var foreignBrowser = host.Browser("foreign-device-0001");
        await LabHttpHost.LoginAsync(foreignBrowser, "foreign-http", Password);
        using var foreignTarget = await LabHttpHost.PostAsync(foreignBrowser, "/api/lab/manage/demo-roles", new
            { userKey = member.Value, role = "Manager", assigned = true });
        Assert.Equal(HttpStatusCode.Forbidden, foreignTarget.StatusCode);
    }

    [Fact]
    public async Task Repair_requires_live_session_matching_device_ladmin_and_csrf()
    {
        await using var db = await postgres.CreateAsync();
        await using var host = await LabHttpHost.CreateAsync(db);
        using var anonymous = host.Browser();
        using var anonymousResult = await LabHttpHost.PostAsync(anonymous, "/api/lab/admin/repair/scan");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResult.StatusCode);
        using var anonymousCreate = await LabHttpHost.PostAsync(anonymous, "/api/lab/manage/users", new CreateUserRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousCreate.StatusCode);

        using var browser = host.Browser();
        await LabHttpHost.LoginAsync(browser, "lab-admin", "Lab-admin-test-password-8!");
        using var scan = await LabHttpHost.PostAsync(browser, "/api/lab/admin/repair/scan");
        Assert.Equal(HttpStatusCode.OK, scan.StatusCode);
        Assert.NotNull(await scan.Content.ReadFromJsonAsync<RepairSnapshot>(cancellationToken: TestContext.Current.CancellationToken));
        using var noCsrf = await browser.PostAsJsonAsync("/api/lab/admin/repair/sandbox/00000000-0000-0000-0000-000000000001/delete", new { }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using var noCreateCsrf = await browser.PostAsJsonAsync("/api/lab/manage/users", new CreateUserRequest(), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, noCreateCsrf.StatusCode);

        browser.DefaultRequestHeaders.Remove("X-UDID");
        using var missing = await LabHttpHost.PostAsync(browser, "/api/lab/admin/repair/scan");
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        browser.DefaultRequestHeaders.Add("X-UDID", "different-device");
        using var mismatch = await LabHttpHost.PostAsync(browser, "/api/lab/admin/repair/scan");
        Assert.Equal(HttpStatusCode.Unauthorized, mismatch.StatusCode);

        browser.DefaultRequestHeaders.Remove("X-UDID");
        browser.DefaultRequestHeaders.Add("X-UDID", "identitylab-browser");
        var oldSession = await LabHttpHost.LoginAsync(browser, "lab-admin", "Lab-admin-test-password-8!");
        using var logout = await LabHttpHost.PostAsync(browser, "/auth/logout");
        Assert.True((int)logout.StatusCode is >= 200 and < 400);
        using var replay = host.Browser();
        replay.DefaultRequestHeaders.Add("Cookie", oldSession);
        using var revoked = await LabHttpHost.PostAsync(replay, "/api/lab/admin/repair/scan");
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        using var revokedCreate = await LabHttpHost.PostAsync(replay, "/api/lab/manage/users", new CreateUserRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, revokedCreate.StatusCode);
        using var invalid = host.Browser();
        invalid.DefaultRequestHeaders.Add("Cookie", "uas=invalid-session");
        using var invalidScan = await LabHttpHost.PostAsync(invalid, "/api/lab/admin/repair/scan");
        Assert.Equal(HttpStatusCode.Unauthorized, invalidScan.StatusCode);
    }
}
