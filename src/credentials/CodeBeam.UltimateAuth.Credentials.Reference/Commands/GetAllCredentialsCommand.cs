using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Credentials.Reference
{
    internal sealed class GetAllCredentialsCommand : IAccessCommand<GetCredentialsResult>
    {
        private readonly Func<CancellationToken, Task<GetCredentialsResult>> _execute;

        public GetAllCredentialsCommand(Func<CancellationToken, Task<GetCredentialsResult>> execute)
        {
            _execute = execute;
        }

        public Task<GetCredentialsResult> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
