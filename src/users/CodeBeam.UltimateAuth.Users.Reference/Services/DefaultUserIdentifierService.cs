using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class DefaultUserIdentifierService : IUserIdentifierService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly IUserIdentifierStore _store;
    private readonly UAuthServerOptions _serverOptions;
    private readonly IClock _clock;

    public DefaultUserIdentifierService(IAccessOrchestrator accessOrchestrator, IUserIdentifierStore store, IOptions<UAuthServerOptions> serverOptions, IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _store = store;
        _serverOptions = serverOptions.Value;
        _clock = clock;
    }

    public async Task<GetUserIdentifiersResult> GetAsync(AccessContext context, UserKey targetUserKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var policies = Array.Empty<IAccessPolicy>();

        var cmd = new GetUserIdentifiersCommand(
            policies,
            async innerCt =>
            {
                var records = await _store.GetAllAsync(
                    context.ResourceTenantId,
                    targetUserKey,
                    innerCt);

                var dtos = records
                    .Where(r => r.DeletedAt is null)
                    .Select(UserIdentifierMapper.ToDto)
                    .ToArray();

                return new GetUserIdentifiersResult
                {
                    Identifiers = dtos
                };
            });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<IdentifierChangeResult> ChangeAsync(AccessContext context, UserKey targetUserKey, ChangeUserIdentifierRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var policies = Array.Empty<IAccessPolicy>();

        var record = new UserIdentifierRecord
        {
            Type = request.Type,
            Value = request.NewValue,
            IsVerified = false,
            CreatedAt = _clock.UtcNow,
            VerifiedAt = null,
            DeletedAt = null
        };

        var cmd = new ChangeUserIdentifierCommand(
            policies,
            async innerCt =>
            {
                var exists = await _store.ExistsAsync(context.ResourceTenantId, request.Type, request.NewValue, innerCt);

                if (exists)
                    throw new InvalidOperationException("identifier_already_exists");

                await _store.SetAsync(context.ResourceTenantId, targetUserKey, record, innerCt);
            });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);

        return IdentifierChangeResult.Success();
    }

    public async Task<IdentifierVerificationResult> VerifyAsync(AccessContext context, UserKey targetUserKey, VerifyUserIdentifierRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var policies = Array.Empty<IAccessPolicy>();

        var cmd = new VerifyUserIdentifierCommand(
            policies,
            async innerCt =>
            {
                await _store.MarkVerifiedAsync(context.ResourceTenantId, targetUserKey, request.Type, _clock.UtcNow, innerCt);
            });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);

        return IdentifierVerificationResult.Success();
    }

    public async Task<IdentifierDeleteResult> DeleteAsync(AccessContext context, UserKey targetUserKey, DeleteUserIdentifierRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var policies = Array.Empty<IAccessPolicy>();

        var cmd = new DeleteUserIdentifierCommand(
            policies,
            async innerCt =>
            {
                var identifiers = await _store.GetByTypeAsync(context.ResourceTenantId, targetUserKey, request.Type, innerCt);

                var activeCount = identifiers.Count(i => i.DeletedAt is null);

                if (activeCount <= 1 && request.Type == UserIdentifierType.Username)
                    throw new InvalidOperationException("last_username_cannot_be_deleted");

                await _store.DeleteAsync(context.ResourceTenantId, targetUserKey, request.Type, request.Value, request.Mode, innerCt);
            });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);

        return IdentifierDeleteResult.Success();
    }
}
