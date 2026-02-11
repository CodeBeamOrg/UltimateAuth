namespace CodeBeam.UltimateAuth.Core.Runtime;

/// <summary>
/// Marker interface indicating that UltimateAuth is running in a specific runtime context (e.g. server-hosted).
/// Implementations must be provided by integration layers such as UltimateAuth.Server.
/// </summary>
public interface IUAuthRuntimeMarker
{
}
