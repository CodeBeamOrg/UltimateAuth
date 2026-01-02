using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Client.Abstractions
{
    public interface IClientAuthState
    {
        bool IsAuthenticated { get; }

        IReadOnlyCollection<Claim> Claims { get; }
    }
}
