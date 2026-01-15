using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    public abstract class UAuthChainException : UAuthDomainException
    {
        public SessionChainId ChainId { get; }

        protected UAuthChainException(
            SessionChainId chainId,
            string message)
            : base(message)
        {
            ChainId = chainId;
        }
    }
}
