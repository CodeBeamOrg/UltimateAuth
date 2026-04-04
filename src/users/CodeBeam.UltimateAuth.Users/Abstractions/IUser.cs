namespace CodeBeam.UltimateAuth.Users;

public interface IUser<TUserId>
{
    TUserId UserId { get; }
    bool IsActive { get; }
}
