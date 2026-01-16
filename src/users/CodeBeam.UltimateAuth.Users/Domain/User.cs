namespace CodeBeam.UltimateAuth.Users;

public sealed record User<TUserId>(
    TUserId UserId,
    bool IsActive) : IUser<TUserId>;
