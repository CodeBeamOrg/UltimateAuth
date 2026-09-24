using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tests.Integration;

internal sealed record IntegrationTestUser(
    UserKey UserKey,
    string Identifier,
    string Secret);
