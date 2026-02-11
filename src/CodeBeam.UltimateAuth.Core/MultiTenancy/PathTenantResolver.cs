namespace CodeBeam.UltimateAuth.Core.MultiTenancy;

public sealed class PathTenantResolver : ITenantIdResolver
{
    /// <summary>
    /// Extracts the tenant id from the request path, if present.
    /// Returns null when the prefix is not matched or the path is insufficient.
    /// </summary>
    public Task<string?> ResolveTenantIdAsync(TenantResolutionContext context)
    {
        var path = context.Path;
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult<string?>(null);

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Format: /{tenant}/...
        if (segments.Length >= 1)
            return Task.FromResult<string?>(segments[0]);

        return Task.FromResult<string?>(null);
    }
}
