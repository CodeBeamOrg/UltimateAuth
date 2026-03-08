namespace CodeBeam.UltimateAuth.Client;

public abstract record UAuthStateEventArgs(
    UAuthStateEvent Type,
    UAuthStateRefreshMode RefreshMode);

public sealed record UAuthStateEventArgs<TPayload>(
    UAuthStateEvent Type,
    UAuthStateRefreshMode RefreshMode,
    TPayload Payload)
    : UAuthStateEventArgs(Type, RefreshMode);

public sealed record UAuthStateEventArgsEmpty(
    UAuthStateEvent Type,
    UAuthStateRefreshMode RefreshMode)
    : UAuthStateEventArgs(Type, RefreshMode);
