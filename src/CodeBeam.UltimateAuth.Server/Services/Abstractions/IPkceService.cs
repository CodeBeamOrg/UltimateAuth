using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Services;

public interface IPkceService
{
    Task<PkceAuthorizeResponse> AuthorizeAsync(PkceAuthorizeCommand command, CancellationToken ct = default);
    Task<PkceCompleteResult> CompleteAsync(AuthFlowContext auth, PkceCompleteRequest request, CancellationToken ct = default);
}
