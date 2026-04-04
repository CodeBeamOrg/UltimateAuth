namespace CodeBeam.UltimateAuth.Core.Contracts;

// This is for AuthFlowContext, with minimal data and no db access
/// <summary>
/// Represents the minimal authentication state of the current request.
/// This type is request-scoped and contains no domain or persistence data.
///
/// AuthUserSnapshot answers only the question:
/// "Is there an authenticated user associated with this execution context?"
///
/// It must not be used for user discovery, lifecycle decisions,
/// or authorization policies.
/// </summary>
public sealed class AuthUserSnapshot<TUserId>
{
    public bool IsAuthenticated { get; }
    public TUserId? UserId { get; }

    private AuthUserSnapshot(bool isAuthenticated, TUserId? userId)
    {
        IsAuthenticated = isAuthenticated;
        UserId = userId;
    }

    public static AuthUserSnapshot<TUserId> Authenticated(TUserId userId) => new(true, userId);
    public static AuthUserSnapshot<TUserId> Anonymous() => new(false, default);
}
