using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    public abstract class UAuthChainException : UAuthDomainException
    {
        public ChainId ChainId { get; }

        protected UAuthChainException(
            ChainId chainId,
            string message)
            : base(message)
        {
            ChainId = chainId;
        }
    }
}
