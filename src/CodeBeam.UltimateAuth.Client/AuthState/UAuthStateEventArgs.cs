namespace CodeBeam.UltimateAuth.Client;

public abstract record UAuthStateEventArgs(
    UAuthStateEvent Type,
    UAuthStateEventHandlingMode RefreshMode);

public sealed record UAuthStateEventArgs<TPayload>(
    UAuthStateEvent Type,
    UAuthStateEventHandlingMode RefreshMode,
    TPayload Payload)
    : UAuthStateEventArgs(Type, RefreshMode);

public sealed record UAuthStateEventArgsEmpty(
    UAuthStateEvent Type,
    UAuthStateEventHandlingMode RefreshMode)
    : UAuthStateEventArgs(Type, RefreshMode);
