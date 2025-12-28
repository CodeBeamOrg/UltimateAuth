using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    /// <summary>
    /// The single point of truth for accessing the current session context
    /// </summary>
    public interface ISessionContextAccessor
    {
        SessionContext? Current { get; }
    }
}
