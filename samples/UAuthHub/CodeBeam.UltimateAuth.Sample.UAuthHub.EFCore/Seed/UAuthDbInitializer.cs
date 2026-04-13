using CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Users.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Sample.UAuthHub.EFCore;

public static class UAuthDbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, bool reset = false)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var bundleDb = sp.GetService<UAuthDbContext>();

        if (bundleDb != null)
        {
            if (reset)
                await bundleDb.Database.EnsureDeletedAsync();

            await bundleDb.Database.MigrateAsync();
            return;
        }

        var contexts = new DbContext[]
        {
            sp.GetRequiredService<UAuthSessionDbContext>(),
            sp.GetRequiredService<UAuthUserDbContext>(),
            sp.GetRequiredService<UAuthCredentialDbContext>(),
            sp.GetRequiredService<UAuthAuthorizationDbContext>(),
            sp.GetRequiredService<UAuthTokenDbContext>(),
            sp.GetRequiredService<UAuthAuthenticationDbContext>()
        };

        if (reset)
            await contexts[0].Database.EnsureDeletedAsync();

        foreach (var db in contexts)
            await db.Database.MigrateAsync();
    }
}
