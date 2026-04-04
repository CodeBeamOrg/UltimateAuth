using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal static class TestIds
{
    public static AuthSessionId Session(string raw)
    {
        if (raw.Length < 32)
        {
            raw = raw.PadRight(32, 'x');
        }

        if (!AuthSessionId.TryCreate(raw, out var id))
            throw new InvalidOperationException($"Invalid test AuthSessionId: {raw}");

        return id;
    }
}
