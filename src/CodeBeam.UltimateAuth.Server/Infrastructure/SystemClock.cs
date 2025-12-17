using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
