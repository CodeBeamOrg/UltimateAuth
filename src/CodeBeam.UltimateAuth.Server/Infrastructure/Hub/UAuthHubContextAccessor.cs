using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UAuthHubContextAccessor : IUAuthHubContextAccessor
{
    private UAuthHubContext? _current;

    public UAuthHubContext? Current => _current;

    public bool HasActiveFlow => _current is not null && !_current.IsCompleted;

    public void Initialize(UAuthHubContext context)
    {
        _current = context;
    }

    public void Clear()
    {
        _current = null;
    }
}
