namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

internal static class CredentialUserMappingBuilder
{
    public static CredentialUserMapping<TUser, TUserId> Build<TUser, TUserId>(CredentialUserMappingOptions<TUser, TUserId> options)
    {
        if (options.UserId is null)
        {
            var expr = ConventionResolver.TryResolve<TUser, TUserId>("Id", "UserId");
            if (expr != null)
                options.ApplyUserId(expr);
        }

        if (options.Username is null)
        {
            var expr = ConventionResolver.TryResolve<TUser, string>(
                "Username",
                "UserName",
                "Email",
                "EmailAddress",
                "Login");

            if (expr != null)
                options.ApplyUsername(expr);
        }

        // Never add "Password" as a convention to avoid accidental mapping to plaintext password properties
        if (options.PasswordHash is null)
        {
            var expr = ConventionResolver.TryResolve<TUser, string>(
                "PasswordHash",
                "Passwordhash",
                "PasswordHashV2");

            if (expr != null)
                options.ApplyPasswordHash(expr);
        }

        if (options.SecurityVersion is null)
        {
            var expr = ConventionResolver.TryResolve<TUser, long>(
                "SecurityVersion",
                "SecurityStamp",
                "AuthVersion");

            if (expr != null)
                options.ApplySecurityVersion(expr);
        }


        if (options.UserId is null)
            throw new InvalidOperationException("UserId mapping is required. Use MapUserId(...) or ensure a conventional property exists.");

        if (options.Username is null)
            throw new InvalidOperationException("Username mapping is required. Use MapUsername(...) or ensure a conventional property exists.");

        if (options.PasswordHash is null)
            throw new InvalidOperationException("PasswordHash mapping is required. Use MapPasswordHash(...) or ensure a conventional property exists.");

        if (options.SecurityVersion is null)
            throw new InvalidOperationException("SecurityVersion mapping is required. Use MapSecurityVersion(...) or ensure a conventional property exists.");

        var canAuthenticateExpr = options.CanAuthenticate ?? (_ => true);

        return new CredentialUserMapping<TUser, TUserId>
        {
            UserId = options.UserId.Compile(),
            Username = options.Username.Compile(),
            PasswordHash = options.PasswordHash.Compile(),
            SecurityVersion = options.SecurityVersion.Compile(),
            CanAuthenticate = canAuthenticateExpr.Compile()
        };
    }
}
