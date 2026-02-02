using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Services;

public sealed class UAuthSessionQueryService : ISessionQueryService
{
    private readonly ISessionStoreKernelFactory _storeFactory;
    private readonly IAuthFlowContextAccessor _authFlow;

    public UAuthSessionQueryService(
        ISessionStoreKernelFactory storeFactory,
        IAuthFlowContextAccessor authFlow)
    {
        _storeFactory = storeFactory;
        _authFlow = authFlow;
    }

    public Task<UAuthSession?> GetSessionAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        return CreateKernel().GetSessionAsync(sessionId);
    }

    public Task<IReadOnlyList<UAuthSession>> GetSessionsByChainAsync(SessionChainId chainId, CancellationToken ct = default)
    {
        return CreateKernel().GetSessionsByChainAsync(chainId);
    }

    public Task<IReadOnlyList<UAuthSessionChain>> GetChainsByUserAsync(UserKey userKey, CancellationToken ct = default)
    {
        return CreateKernel().GetChainsByUserAsync(userKey);
    }

    public Task<SessionChainId?> ResolveChainIdAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        return CreateKernel().GetChainIdBySessionAsync(sessionId);
    }

    private ISessionStoreKernel CreateKernel()
    {
        var tenantId = _authFlow.Current.Tenant;
        return _storeFactory.Create(tenantId);
    }

}
