using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    internal sealed class AddUserIdentifierCommand : IAccessCommand
    {
        private readonly Func<CancellationToken, Task> _execute;

        public AddUserIdentifierCommand(Func<CancellationToken, Task> execute)
        {
            _execute = execute;
        }

        public Task ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
