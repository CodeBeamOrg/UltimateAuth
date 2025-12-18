namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface ITokenStoreFactory
    {
        ITokenStoreKernel Create(string? tenantId);
    }
}
