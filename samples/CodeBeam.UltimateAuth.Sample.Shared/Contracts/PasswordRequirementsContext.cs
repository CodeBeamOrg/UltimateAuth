using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public sealed record PasswordRequirementsContext(UAuthPasswordPolicyInfo Policy, string? Password);
