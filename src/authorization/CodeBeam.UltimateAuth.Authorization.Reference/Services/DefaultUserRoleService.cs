using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Authorization.Reference
{
    internal sealed class DefaultUserRoleService : IUserRoleService
    {
        private readonly IAccessOrchestrator _accessOrchestrator;
        private readonly IUserRoleStore _store;

        public DefaultUserRoleService(IAccessOrchestrator accessOrchestrator, IUserRoleStore store)
        {
            _accessOrchestrator = accessOrchestrator;
            _store = store;
        }

        public async Task AssignAsync(AccessContext context, UserKey targetUserKey, string role, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("role_empty", nameof(role));

            var cmd = new AssignUserRoleCommand(Array.Empty<IAccessPolicy>(),
                async innerCt =>
                {
                    await _store.AssignAsync(context.ResourceTenant, targetUserKey, role, innerCt);
                });

            await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
        }

        public async Task RemoveAsync(AccessContext context, UserKey targetUserKey, string role, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("role_empty", nameof(role));

            var cmd = new RemoveUserRoleCommand(Array.Empty<IAccessPolicy>(),
                async innerCt =>
                {
                    await _store.RemoveAsync(context.ResourceTenant, targetUserKey, role, innerCt);
                });

            await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
        }


        public async Task<IReadOnlyCollection<string>> GetRolesAsync(AccessContext context, UserKey targetUserKey, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var cmd = new GetUserRolesCommand(Array.Empty<IAccessPolicy>(),
                innerCt => _store.GetRolesAsync(context.ResourceTenant, targetUserKey, innerCt));

            return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
        }
    }
}
