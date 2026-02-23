using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    internal sealed class AuthStateSnapshotFactory : IAuthStateSnapshotFactory
    {
        private readonly IPrimaryUserIdentifierProvider _identifierProvider;
        private readonly IUserProfileSnapshotProvider _profileSnapshotProvider;

        public AuthStateSnapshotFactory(IPrimaryUserIdentifierProvider identifierProvider, IUserProfileSnapshotProvider profileSnapshotProvider)
        {
            _identifierProvider = identifierProvider;
            _profileSnapshotProvider = profileSnapshotProvider;
        }

        public async Task<AuthStateSnapshot?> CreateAsync(SessionValidationResult validation, CancellationToken ct = default)
        {
            if (!validation.IsValid || validation.UserKey is null)
                return null;

            var identifiers = await _identifierProvider.GetAsync(validation.Tenant, validation.UserKey.Value, ct);
            var profile = await _profileSnapshotProvider.GetAsync(validation.Tenant, validation.UserKey.Value, ct);

            var identity = new AuthIdentitySnapshot
            {
                UserKey = validation.UserKey.Value,
                Tenant = validation.Tenant,
                PrimaryUserName = identifiers?.UserName,
                PrimaryEmail = identifiers?.Email,
                PrimaryPhone = identifiers?.Phone,
                DisplayName = profile?.DisplayName,
                TimeZone = profile?.TimeZone,
                AuthenticatedAt = validation.AuthenticatedAt,
                SessionState = validation.State
            };

            return new AuthStateSnapshot
            {
                Identity = identity,
                Claims = validation.Claims
            };
        }
    }

}
