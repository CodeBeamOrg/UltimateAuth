using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

internal sealed class EfCoreAuthUser<TUserId> : IAuthSubject<TUserId>
{
    public TUserId UserId { get; }

    IReadOnlyDictionary<string, object>? IAuthSubject<TUserId>.Claims => null;

    public EfCoreAuthUser(TUserId userId)
    {
        UserId = userId;
    }
}
