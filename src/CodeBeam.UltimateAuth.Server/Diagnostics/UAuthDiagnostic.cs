namespace CodeBeam.UltimateAuth.Server.Diagnostics;

public sealed record UAuthDiagnostic(string code, string message, UAuthDiagnosticSeverity severity);

public enum UAuthDiagnosticSeverity
{
    Info = 0,
    Warning = 10,
    Error = 20
}
