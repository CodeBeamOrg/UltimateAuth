using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    internal sealed class GetUserIdentifierCommand : IAccessCommand<UserIdentifierDto?>
    {
        private readonly Func<CancellationToken, Task<UserIdentifierDto?>> _execute;

        public GetUserIdentifierCommand(Func<CancellationToken, Task<UserIdentifierDto?>> execute)
        {
            _execute = execute;
        }

        public Task<UserIdentifierDto?> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
