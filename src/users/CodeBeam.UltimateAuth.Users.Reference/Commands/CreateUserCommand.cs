using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    internal sealed class CreateUserCommand : IAccessCommand
    {
        private readonly Func<CancellationToken, Task> _execute;

        public CreateUserCommand(Func<CancellationToken, Task> execute)
        {
            _execute = execute;
        }

        public Task ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
