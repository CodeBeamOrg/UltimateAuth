namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IUser<TUserId>
    {
        TUserId UserId { get; }
        IReadOnlyDictionary<string, object>? Claims { get; }
    }
}
