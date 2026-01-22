using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    internal sealed class GetUserIdentifiersCommand : IAccessCommand<GetUserIdentifiersResult>
    {
        private readonly Func<CancellationToken, Task<GetUserIdentifiersResult>> _execute;

        public GetUserIdentifiersCommand(Func<CancellationToken, Task<GetUserIdentifiersResult>> execute)
        {
            _execute = execute;
        }

        public Task<GetUserIdentifiersResult> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
