using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthSessionChainRevokedException : UAuthChainException
    {
        public SessionChainId ChainId { get; }

        public UAuthSessionChainRevokedException(SessionChainId chainId)
            : base(chainId, $"Session chain '{chainId}' has been revoked.")
        {
        }
    }
}
