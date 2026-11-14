using CodeBeam.UltimateAuth.Core.Errors;

namespace CodeBeam.UltimateAuth.Server.Stores;

internal sealed class UAuthArtifactStoreFullException : UAuthRuntimeException
{
    public override int StatusCode => 503;

    public override string Title => "Authentication service temporarily unavailable.";

    public UAuthArtifactStoreFullException()
        : base("auth_artifact_capacity_exceeded", "The authentication artifact store has reached its capacity.")
    {

    }
}
