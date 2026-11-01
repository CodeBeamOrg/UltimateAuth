using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Credentials.Policies;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class PasswordUserLifecycleIntegration : IUserLifecycleIntegration
{
    private readonly IPasswordCredentialStoreFactory _credentialStoreFactory;
    private readonly IUAuthPasswordHasher _passwordHasher;
    private readonly PasswordCredentialWriter _passwordWriter;
    private readonly IPasswordHistoryStoreFactory _historyStores;
    private readonly IPasswordPolicyContextProvider _passwordContextProvider;
    private readonly IEffectiveServerOptionsProvider _effectiveServerOptionsProvider;
    private readonly IClock _clock;

    public PasswordUserLifecycleIntegration(IPasswordCredentialStoreFactory credentialStoreFactory, IUAuthPasswordHasher passwordHasher, PasswordCredentialWriter passwordWriter, IPasswordHistoryStoreFactory historyStores, IPasswordPolicyContextProvider passwordContextProvider, IEffectiveServerOptionsProvider effectiveServerOptionsProvider, IClock clock)
    {
        _credentialStoreFactory = credentialStoreFactory;
        _passwordHasher = passwordHasher;
        _passwordWriter = passwordWriter;
        _historyStores = historyStores;
        _passwordContextProvider = passwordContextProvider;
        _effectiveServerOptionsProvider = effectiveServerOptionsProvider;
        _clock = clock;
    }

    public async Task OnUserCreatedAsync(TenantKey tenant, UserKey userKey, object request, CancellationToken ct)
    {
        if (request is not CreateUserRequest r)
            return;

        if (string.IsNullOrWhiteSpace(r.Password))
            return;

        var passwordContext = await _passwordContextProvider.CreateAsync(tenant, userKey, r.Password, ct);
        // TODO: This is not correct client profile. Resolve true client profile in the future.
        var effectiveServer = _effectiveServerOptionsProvider.GetEffective(tenant, UAuthClientProfile.NotSpecified);
        var passwordOptions = PasswordPolicyOptionsResolver.Resolve(effectiveServer.Options.Password);
        await _passwordWriter.CreateAsync(
            tenant,
            userKey,
            r.Password,
            passwordContext,
            passwordOptions,
            _clock.UtcNow,
            ct);
    }

    public async Task OnUserDeletedAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, CancellationToken ct)
    {
        var credentialStore = _credentialStoreFactory.Create(tenant);

        await credentialStore.DeleteByUserAsync(userKey, mode, _clock.UtcNow, ct);

        if (mode == DeleteMode.Hard)
        {
            await _historyStores.Create(tenant).DeleteByUserAsync(userKey, ct);
        }
    }
}
