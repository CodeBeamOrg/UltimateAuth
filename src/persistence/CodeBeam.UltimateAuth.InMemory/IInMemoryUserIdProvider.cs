namespace CodeBeam.UltimateAuth.InMemory;

public interface IInMemoryUserIdProvider<TUserId>
{
    TUserId GetAdminUserId();
    TUserId GetUserUserId();
}
