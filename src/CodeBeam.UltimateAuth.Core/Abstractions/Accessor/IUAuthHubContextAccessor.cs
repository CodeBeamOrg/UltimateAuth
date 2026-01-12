using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IUAuthHubContextAccessor
{
    UAuthHubContext? Current { get; }
    bool HasActiveFlow { get; }
}
