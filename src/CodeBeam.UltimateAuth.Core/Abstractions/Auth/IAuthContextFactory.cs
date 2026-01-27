using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IAuthContextFactory
{
    AuthContext Create(DateTimeOffset? at = null);
}
