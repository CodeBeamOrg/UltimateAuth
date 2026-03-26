namespace CodeBeam.UltimateAuth.Sample.Seed;

public interface IUserIdProvider<TUserId>
{
    TUserId GetAdminUserId();
    TUserId GetUserUserId();
}
