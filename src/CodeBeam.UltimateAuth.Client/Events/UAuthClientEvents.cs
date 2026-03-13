namespace CodeBeam.UltimateAuth.Client.Events;

internal sealed class UAuthClientEvents : IUAuthClientEvents
{
    public event Func<UAuthStateEventArgs, Task>? StateChanged;

    public Task PublishAsync<TPayload>(UAuthStateEventArgs<TPayload> args)
        => PublishAsync((UAuthStateEventArgs)args);

    public async Task PublishAsync(UAuthStateEventArgs args)
    {
        var handlers = StateChanged;
        if (handlers == null)
            return;

        foreach (var handler in handlers.GetInvocationList())
        {
            await ((Func<UAuthStateEventArgs, Task>)handler)(args);
        }
    }
}
