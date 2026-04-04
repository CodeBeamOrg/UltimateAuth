using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users;

public interface IUserSecurityEvents
{
    Task OnUserActivatedAsync(UserKey userKey);
    Task OnUserDeactivatedAsync(UserKey userKey);
    Task OnSecurityInvalidatedAsync(UserKey userKey);
}
