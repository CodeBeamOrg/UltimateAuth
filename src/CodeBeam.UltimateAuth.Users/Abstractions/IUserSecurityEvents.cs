namespace CodeBeam.UltimateAuth.Server.Users;

public interface IUserSecurityEvents<TUserId>
{
    Task OnUserActivatedAsync(TUserId userId);
    Task OnUserDeactivatedAsync(TUserId userId);
    Task OnSecurityInvalidatedAsync(TUserId userId);
}
