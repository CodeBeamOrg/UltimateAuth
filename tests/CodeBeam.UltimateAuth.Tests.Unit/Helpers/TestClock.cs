using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal sealed class TestClock : IClock
{
    private DateTimeOffset _utcNow;

    public TestClock(DateTimeOffset? initial = null)
    {
        _utcNow = initial ?? DateTimeOffset.UtcNow;
    }

    public DateTimeOffset UtcNow => _utcNow;

    public void Advance(TimeSpan duration)
    {
        _utcNow = _utcNow.Add(duration);
    }

    public void Set(DateTimeOffset time)
    {
        _utcNow = time;
    }
}
