namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public interface IInMemoryUserIdProvider<TUserId>
{
    TUserId GetAdminUserId();
    TUserId GetUserUserId();
}
