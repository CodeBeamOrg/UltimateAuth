namespace CodeBeam.UltimateAuth.Server.RateLimiting;

/// <summary>Startup-only endpoint rate limiting configuration. Counters are process-local.</summary>
public sealed class UAuthRateLimitingOptions
{
    public bool Enabled { get; set; } = true;
    public UAuthRateLimitRule Sensitive { get; set; } = new() { PermitLimit = 30 };
    public UAuthRateLimitRule Standard { get; set; } = new() { PermitLimit = 120 };
    public UAuthRateLimitRule Frequent { get; set; } = new() { PermitLimit = 600 };
    /// <summary>Shared by Login, TryLogin, PkceComplete and PkceTryComplete unless explicitly overridden.</summary>
    public UAuthRateLimitRule Login { get; set; } = new() { PermitLimit = 20 };
    /// <summary>Complete replacement rules. Disabling removes only UltimateAuth's contribution.</summary>
    public Dictionary<UAuthEndpoint, UAuthRateLimitRule> Endpoints { get; set; } = new();

    internal (UAuthRateLimitRule Rule, string Budget) Resolve(UAuthRateLimitMetadata metadata)
    {
        if (Endpoints.TryGetValue(metadata.Endpoint, out var rule))
            return (rule, metadata.Endpoint.ToString());
        if (metadata.Endpoint is UAuthEndpoint.Login or UAuthEndpoint.TryLogin or UAuthEndpoint.PkceComplete or UAuthEndpoint.PkceTryComplete)
            return (Login, "login-verification");
        rule = metadata.Group switch
        {
            UAuthRateLimitGroup.Sensitive => Sensitive,
            UAuthRateLimitGroup.Standard => Standard,
            _ => Frequent
        };
        // Explicit endpoint overrides replace these security defaults as well.
        rule = metadata.Endpoint switch
        {
            UAuthEndpoint.UserCreateAnonymous => new() { PermitLimit = 10, Window = TimeSpan.FromMinutes(10), Enabled = rule.Enabled, PolicyName = rule.PolicyName },
            UAuthEndpoint.CredentialResetBeginAnonymous => new() { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), Enabled = rule.Enabled, PolicyName = rule.PolicyName },
            UAuthEndpoint.CredentialResetCompleteAnonymous or UAuthEndpoint.VerifyUserIdentifierSelf => new() { PermitLimit = 10, Window = TimeSpan.FromMinutes(10), Enabled = rule.Enabled, PolicyName = rule.PolicyName },
            _ => rule
        };
        return (rule, metadata.Endpoint.ToString());
    }

    internal UAuthRateLimitingOptions Clone() => new()
    {
        Enabled = Enabled,
        Sensitive = Sensitive.Clone(),
        Standard = Standard.Clone(),
        Frequent = Frequent.Clone(),
        Login = Login.Clone(),
        Endpoints = Endpoints.ToDictionary(x => x.Key, x => x.Value.Clone())
    };
}

public sealed class UAuthRateLimitRule
{
    public bool Enabled { get; set; } = true;
    public int PermitLimit { get; set; } = 30;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
    /// <summary>Optional application-owned named native policy; numeric settings are unused. Native policy/middleware rejection precedence applies. Built-in inline policies otherwise own their 429 response.</summary>
    public string? PolicyName { get; set; }
    internal UAuthRateLimitRule Clone() => (UAuthRateLimitRule)MemberwiseClone();
}
