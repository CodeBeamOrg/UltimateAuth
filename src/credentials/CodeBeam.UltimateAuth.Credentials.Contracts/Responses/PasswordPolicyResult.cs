namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed class PasswordPolicyResult
{
    public bool Succeeded => Violations.Count == 0;

    public IReadOnlyList<PasswordPolicyViolation> Violations { get; }

    public PasswordPolicyResult(IEnumerable<PasswordPolicyViolation> violations)
    {
        ArgumentNullException.ThrowIfNull(violations);
        Violations = Array.AsReadOnly(violations.ToArray());
    }

    public static PasswordPolicyResult Success() => new(Array.Empty<PasswordPolicyViolation>());

    public static PasswordPolicyResult Failure(params PasswordPolicyViolation[] violations) => new(violations);
}
