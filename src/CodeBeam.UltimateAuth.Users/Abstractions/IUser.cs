namespace CodeBeam.UltimateAuth.Server.Users
{
    public interface IUser<TUserId>
    {
        TUserId UserId { get; }
        bool IsActive { get; }
    }
}
