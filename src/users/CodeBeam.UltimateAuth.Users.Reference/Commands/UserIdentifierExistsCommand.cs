using CodeBeam.UltimateAuth.Server.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBeam.UltimateAuth.Users.Reference.Commands
{
    internal sealed class UserIdentifierExistsCommand : IAccessCommand<bool>
    {
        private readonly Func<CancellationToken, Task<bool>> _execute;

        public UserIdentifierExistsCommand(Func<CancellationToken, Task<bool>> execute)
        {
            _execute = execute;
        }

        public Task<bool> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
