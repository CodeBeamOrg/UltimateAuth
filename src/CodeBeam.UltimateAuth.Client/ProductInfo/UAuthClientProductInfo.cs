using CodeBeam.UltimateAuth.Core.Runtime;

namespace CodeBeam.UltimateAuth.Client.Runtime
{
    public sealed class UAuthClientProductInfo
    {
        public string ProductName { get; init; } = "UltimateAuthClient";
        public UAuthProductInfo Core { get; init; } = default!;
    }
}
