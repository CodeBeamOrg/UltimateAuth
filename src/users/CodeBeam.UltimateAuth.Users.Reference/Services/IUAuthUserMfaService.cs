using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public interface IUAuthUserMfaService
    {
        Task<UserMfaStatusDto> GetStatusAsync(CancellationToken ct = default);
        Task<BeginMfaSetupResult> BeginSetupAsync(BeginMfaSetupRequest request, CancellationToken ct = default);
        Task CompleteSetupAsync(CompleteMfaSetupRequest request, CancellationToken ct = default);
        Task DisableAsync(DisableMfaRequest request, CancellationToken ct = default);
    }
}
