using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Identity;

namespace CodeBeam.UltimateAuth.Server.Stores
{
    public sealed class AspNetIdentityUserStore // : IUAuthUserStore<string>
    {
        //private readonly UserManager<IdentityUser> _users;

        //public AspNetIdentityUserStore(UserManager<IdentityUser> users)
        //{
        //    _users = users;
        //}

        //public async Task<UAuthUserRecord<string>?> FindByUsernameAsync(
        //    string? tenantId,
        //    string username,
        //    CancellationToken cancellationToken = default)
        //{
        //    var user = await _users.FindByNameAsync(username);
        //    if (user is null)
        //        return null;

        //    var claims = await _users.GetClaimsAsync(user);

        //    return new UAuthUserRecord<string>
        //    {
        //        UserId = user.Id,
        //        Username = user.UserName!,
        //        PasswordHash = user.PasswordHash!,
        //        Claims = ClaimsSnapshot.From(
        //            claims.Select(c => (c.Type, c.Value)).ToArray())
        //    };
        //}
    }

}
