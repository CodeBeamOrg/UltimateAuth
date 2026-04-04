using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface IIdentifierNormalizer
{
    NormalizedIdentifier Normalize(UserIdentifierType type, string value);
}
