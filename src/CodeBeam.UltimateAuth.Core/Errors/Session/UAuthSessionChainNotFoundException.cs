using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthSessionChainNotFoundException : UAuthChainException
    {
        public UAuthSessionChainNotFoundException(ChainId chainId)
            : base(chainId, $"Session chain '{chainId}' was not found.")
        {
        }
    }
}
