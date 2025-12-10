namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IUserIdConverter<TUserId>
    {
        string ToString(TUserId id);
        byte[] ToBytes(TUserId id);

        TUserId FromString(string value);
        TUserId FromBytes(byte[] binary);
    }
}
