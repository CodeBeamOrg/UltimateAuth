namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IUserIdConverterResolver
    {
        IUserIdConverter<TUserId> GetConverter<TUserId>();
    }
}
