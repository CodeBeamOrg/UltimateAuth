using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IUserAgentParser
{
    UserAgentInfo Parse(string? userAgent);
}
