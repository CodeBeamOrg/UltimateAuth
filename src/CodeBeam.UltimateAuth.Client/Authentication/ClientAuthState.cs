using CodeBeam.UltimateAuth.Client.Abstractions;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Client.Authentication
{
    internal sealed class ClientAuthState : IClientAuthState
    {
        private readonly List<Claim> _claims = new();

        public bool IsAuthenticated { get; private set; }

        public IReadOnlyCollection<Claim> Claims => _claims;

        public void SetAuthenticated(IEnumerable<Claim> claims)
        {
            _claims.Clear();
            _claims.AddRange(claims);
            IsAuthenticated = true;
        }

        public void Clear()
        {
            _claims.Clear();
            IsAuthenticated = false;
        }

    }
}
