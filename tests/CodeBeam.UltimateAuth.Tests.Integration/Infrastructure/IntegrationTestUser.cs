using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;

internal sealed record IntegrationTestUser(
    UserKey UserKey,
    string Identifier,
    string Secret);
