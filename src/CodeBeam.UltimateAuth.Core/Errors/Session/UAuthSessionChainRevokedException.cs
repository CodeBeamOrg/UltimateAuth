using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthSessionChainRevokedException : UAuthChainException
    {
        public ChainId ChainId { get; }

        public UAuthSessionChainRevokedException(ChainId chainId)
            : base(chainId, $"Session chain '{chainId}' has been revoked.")
        {
        }
    }
}
