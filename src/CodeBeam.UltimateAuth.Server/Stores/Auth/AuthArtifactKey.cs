namespace CodeBeam.UltimateAuth.Server.Stores;

public sealed record AuthArtifactKey(string Value)
{
    public static AuthArtifactKey New() => new(Guid.NewGuid().ToString("N"));
}
