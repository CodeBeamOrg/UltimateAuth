namespace CodeBeam.UltimateAuth.Server.Diagnostics;

public sealed record UAuthDiagnostic(string Code, string Message, UAuthDiagnosticSeverity Severity);

public enum UAuthDiagnosticSeverity
{
    Info,
    Warning,
    Error
}
