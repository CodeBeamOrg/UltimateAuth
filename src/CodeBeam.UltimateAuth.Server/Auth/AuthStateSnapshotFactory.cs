using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    internal sealed class AuthStateSnapshotFactory : IAuthStateSnapshotFactory
    {
        private readonly IPrimaryUserIdentifierProvider _identifier;
        private readonly IUserProfileSnapshotProvider _profile;
        private readonly IUserLifecycleSnapshotProvider _lifecycle;

        public AuthStateSnapshotFactory(IPrimaryUserIdentifierProvider identifier, IUserProfileSnapshotProvider profile, IUserLifecycleSnapshotProvider lifecycle)
        {
            _identifier = identifier;
            _profile = profile;
            _lifecycle = lifecycle;
        }

        public async Task<AuthStateSnapshot?> CreateAsync(SessionValidationResult validation, CancellationToken ct = default)
        {
            if (!validation.IsValid || validation.UserKey is null)
                return null;

            var identifiers = await _identifier.GetAsync(validation.Tenant, validation.UserKey.Value, ct);
            var profile = await _profile.GetAsync(validation.Tenant, validation.UserKey.Value, ProfileKey.Default, ct);
            var lifecycle = await _lifecycle.GetAsync(validation.Tenant, validation.UserKey.Value, ct);

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
                SessionState = validation.State,
                UserStatus = lifecycle?.Status ?? UserStatus.Unknown
            };

            return new AuthStateSnapshot
            {
                Identity = identity,
                Claims = validation.Claims
            };
        }
    }

}
