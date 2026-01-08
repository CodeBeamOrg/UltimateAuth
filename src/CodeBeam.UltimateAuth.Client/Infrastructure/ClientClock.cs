using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal sealed class ClientClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
