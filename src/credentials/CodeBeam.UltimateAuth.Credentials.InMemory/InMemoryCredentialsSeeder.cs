using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.InMemory
{
    internal static class InMemoryCredentialSeeder
    {
        public static IReadOnlyCollection<InMemoryCredentialUser> CreateDefaultUsers(
            IUAuthPasswordHasher passwordHasher)
        {
            var adminUserId = UserId.New();

            var passwordHash = passwordHasher.Hash("Password!");

            var admin = new InMemoryCredentialUser(
                userId: adminUserId,
                username: "admin",
                passwordHash: passwordHash,
                securityVersion: 0,
                isActive: true
            );

            return new[] { admin };
        }
    }
}
