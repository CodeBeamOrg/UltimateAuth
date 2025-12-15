using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthSessionDeviceMismatchException : UAuthSessionException
    {
        public DeviceInfo Expected { get; }
        public DeviceInfo Actual { get; }

        public UAuthSessionDeviceMismatchException(
            AuthSessionId sessionId,
            DeviceInfo expected,
            DeviceInfo actual)
            : base(
                sessionId,
                $"Session '{sessionId}' device mismatch detected.")
        {
            Expected = expected;
            Actual = actual;
        }
    }
}
