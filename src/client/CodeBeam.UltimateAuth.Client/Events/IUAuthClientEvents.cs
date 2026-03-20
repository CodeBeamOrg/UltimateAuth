namespace CodeBeam.UltimateAuth.Client.Events;

public interface IUAuthClientEvents
{
    event Func<UAuthStateEventArgs, Task>? StateChanged;
    Task PublishAsync<TPayload>(UAuthStateEventArgs<TPayload> args);
    Task PublishAsync(UAuthStateEventArgs args);
}
