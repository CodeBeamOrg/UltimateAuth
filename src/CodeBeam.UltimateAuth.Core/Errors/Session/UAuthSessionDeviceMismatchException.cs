using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthSessionDeviceMismatchException : UAuthSessionException
{
    public DeviceContext Expected { get; }
    public DeviceContext Actual { get; }

    public UAuthSessionDeviceMismatchException(AuthSessionId sessionId, DeviceContext expected, DeviceContext actual)
        : base(sessionId, $"Session '{sessionId}' device mismatch detected.")
    {
        Expected = expected;
        Actual = actual;
    }
}
