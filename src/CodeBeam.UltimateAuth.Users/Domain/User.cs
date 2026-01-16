namespace CodeBeam.UltimateAuth.Server.Users;

public sealed record User<TUserId>(
    TUserId UserId,
    bool IsActive) : IUser<TUserId>;
