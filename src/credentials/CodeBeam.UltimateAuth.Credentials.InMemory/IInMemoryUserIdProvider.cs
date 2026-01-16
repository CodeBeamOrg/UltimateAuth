namespace CodeBeam.UltimateAuth.Credentials.InMemory
{
    public interface IInMemoryUserIdProvider<TUserId>
    {
        TUserId GetAdminUserId();
    }
}
