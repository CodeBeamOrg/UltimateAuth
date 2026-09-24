using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class IntegrationTestClock : IClock
{
    private readonly object _sync = new();

    private DateTimeOffset _utcNow = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public DateTimeOffset UtcNow
    {
        get
        {
            lock (_sync)
            {
                return _utcNow;
            }
        }
    }

    public void Advance(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration));

        lock (_sync)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }

    public void Set(DateTimeOffset value)
    {
        lock (_sync)
        {
            _utcNow = value.ToUniversalTime();
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _utcNow = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
