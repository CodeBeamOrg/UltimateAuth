using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.InMemory
{
    internal sealed class InMemoryCredentialUser : IUser<UserId>
    {
        public UserId UserId { get; init; }
        public string Username { get; init; }

        public string PasswordHash { get; private set; } = default!;

        public long SecurityVersion { get; private set; }

        public bool IsActive { get; init; } = true;

        IReadOnlyDictionary<string, object>? IUser<UserId>.Claims => null;

        public InMemoryCredentialUser(
            UserId userId,
            string username,
            string passwordHash,
            long securityVersion = 0,
            bool isActive = true)
        {
            UserId = userId;
            Username = username;
            PasswordHash = passwordHash;
            SecurityVersion = securityVersion;
            IsActive = isActive;
        }

        internal void SetPasswordHash(string passwordHash)
        {
            PasswordHash = passwordHash;
            SecurityVersion++;
        }

        internal void IncrementSecurityVersion()
        {
            SecurityVersion++;
        }
    }
}
