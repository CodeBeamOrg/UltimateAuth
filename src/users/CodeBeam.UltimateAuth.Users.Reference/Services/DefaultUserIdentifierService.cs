using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
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

    public async Task<GetUserIdentifiersResult> GetAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var records = await _store.GetAllAsync(tenantId, userKey, ct);
        var dtos = records.Where(r => r.DeletedAt is null).Select(UserIdentifierMapper.ToDto).ToArray();

        return new GetUserIdentifiersResult
        {
            Identifiers = dtos
        };
    }

    public async Task<IdentifierChangeResult> ChangeAsync(string? tenantId, UserKey userKey, ChangeUserIdentifierRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var ctx = new AccessContext
        {
            TenantId = tenantId,
            ActorUserKey = userKey,
            Action = "users.identifiers.change",
            Resource = "users",
            ResourceId = userKey.Value,
        };

        var policies = new IAccessPolicy[]
        {

        };

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
                // Domain rule: uniqueness
                var exists = await _store.ExistsAsync(tenantId, request.Type, request.NewValue, innerCt);

                if (exists)
                    throw new InvalidOperationException("identifier_already_exists");

                // Main job
                await _store.SetAsync(tenantId, userKey, record, innerCt);
            });

        await _accessOrchestrator.ExecuteAsync(ctx, cmd, ct);

        return IdentifierChangeResult.Success();


        //var exists = await _store.ExistsAsync(tenantId, request.Type, request.NewValue, ct);

        //if (exists)
        //{
        //    return IdentifierChangeResult.Failed("identifier_already_exists");
        //}

        //var record = new UserIdentifierRecord
        //{
        //    Type = request.Type,
        //    Value = request.NewValue,
        //    IsVerified = false,
        //    CreatedAt = _clock.UtcNow,
        //    VerifiedAt = null,
        //    DeletedAt = null
        //};

        //await _store.SetAsync(tenantId, userKey, record, ct);

        //return IdentifierChangeResult.Success();
    }

    public async Task<IdentifierVerificationResult> VerifyAsync(string? tenantId, UserKey userKey, VerifyUserIdentifierRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _store.MarkVerifiedAsync(tenantId, userKey, request.Type, _clock.UtcNow, ct);
        return IdentifierVerificationResult.Success();
    }

    public async Task<IdentifierDeleteResult> DeleteAsync(string? tenantId, UserKey userKey, DeleteUserIdentifierRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var identifiers = await _store.GetByTypeAsync(tenantId, userKey, request.Type, ct);
        var activeCount = identifiers.Count(i => i.DeletedAt is null);

        if (activeCount <= 1 && request.Type == UserIdentifierType.Username)
        {
            return IdentifierDeleteResult.Fail("last_username_cannot_be_deleted");
        }

        await _store.DeleteAsync(tenantId, userKey, request.Type, request.Value, request.Mode,ct);

        return IdentifierDeleteResult.Success();
    }
}
