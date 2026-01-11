using System.Text.Json;

namespace CodeBeam.UltimateAuth.Client.Contracts
{
    public sealed class BrowserPostRawResult
    {
        public bool Ok { get; init; }
        public int Status { get; init; }
        public string? RefreshOutcome { get; init; }
        public JsonElement? Body { get; init; }
    }
}
