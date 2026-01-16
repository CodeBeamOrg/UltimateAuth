using CodeBeam.UltimateAuth.Credentials;

namespace CodeBeam.UltimateAuth.Credentials.InMemory.Models
{
    internal sealed class InMemoryPasswordCredential<TUserId> : ICredential<TUserId> where TUserId : notnull
    {
        public TUserId UserId { get; init; } = default!;
        public string Login { get; init; } = default!;
        public CredentialType Type => CredentialType.Password;

        public string PasswordHash { get; private set; } = default!;
        public long SecurityVersion { get; private set; }

        public bool IsActive { get; set; } = true;

        public void SetPasswordHash(string hash)
        {
            PasswordHash = hash;
            SecurityVersion++;
        }

        public void IncrementSecurityVersion()
        {
            SecurityVersion++;
        }
    }
}
