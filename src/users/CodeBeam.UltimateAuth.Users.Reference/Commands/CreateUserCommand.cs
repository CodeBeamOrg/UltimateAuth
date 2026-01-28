using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    internal sealed class CreateUserCommand : IAccessCommand<UserCreateResult>
    {
        private readonly Func<CancellationToken, Task<UserCreateResult>> _execute;

        public CreateUserCommand(Func<CancellationToken, Task<UserCreateResult>> execute)
        {
            _execute = execute;
        }

        public Task<UserCreateResult> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
