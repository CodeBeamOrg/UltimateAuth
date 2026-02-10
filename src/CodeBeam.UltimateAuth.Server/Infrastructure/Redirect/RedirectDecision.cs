namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class RedirectDecision
{
    public bool Enabled { get; }
    public string? TargetUrl { get; }

    private RedirectDecision(bool enabled, string? targetUrl)
    {
        Enabled = enabled;
        TargetUrl = targetUrl;
    }

    public static RedirectDecision None() => new(false, null);

    public static RedirectDecision To(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Redirect target URL cannot be empty.", nameof(url));

        return new RedirectDecision(true, url);
    }
}
