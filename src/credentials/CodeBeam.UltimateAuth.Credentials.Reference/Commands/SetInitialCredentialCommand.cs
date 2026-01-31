using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Credentials.Reference
{
    internal sealed class SetInitialCredentialCommand : IAccessCommand
    {
        private readonly Func<CancellationToken, Task> _execute;

        public SetInitialCredentialCommand(Func<CancellationToken, Task> execute)
        {
            _execute = execute;
        }

        public Task ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
