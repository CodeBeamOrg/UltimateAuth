using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    internal sealed class UpdateUserIdentifierCommand : IAccessCommand
    {
        private readonly Func<CancellationToken, Task> _execute;

        public UpdateUserIdentifierCommand(Func<CancellationToken, Task> execute)
        {
            _execute = execute;
        }

        public Task ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
