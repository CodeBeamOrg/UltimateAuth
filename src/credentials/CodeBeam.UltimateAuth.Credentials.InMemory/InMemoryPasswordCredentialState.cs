using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;

namespace CodeBeam.UltimateAuth.Credentials.InMemory
{
    internal sealed class InMemoryPasswordCredentialState<TUserId>
    {
        public TUserId UserId { get; init; } = default!;
        public CredentialType Type { get; } = CredentialType.Password;
        public string Login { get; init; } = default!;
        public string SecretHash { get; set; } = default!;
        public CredentialStatus Status { get; set; }
        public long SecurityVersion { get; set; }
        public CredentialMetadata Metadata { get; set; } = default!;
    }
}
