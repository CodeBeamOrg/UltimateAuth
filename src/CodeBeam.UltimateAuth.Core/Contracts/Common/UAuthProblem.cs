namespace CodeBeam.UltimateAuth.Core.Contracts;

// Based on https://datatracker.ietf.org/doc/html/rfc7807
public sealed class UAuthProblem
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Detail { get; set; }
    public string? TraceId { get; set; }

    public Dictionary<string, object>? Extensions { get; init; }
}