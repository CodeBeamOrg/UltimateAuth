using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using System.Security;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public class RefreshStrategyResolver
    {
        public static RefreshStrategy Resolve(UAuthMode mode)
        {
            return mode switch
            {
                UAuthMode.PureOpaque => RefreshStrategy.SessionOnly,
                UAuthMode.PureJwt => RefreshStrategy.TokenOnly,
                UAuthMode.SemiHybrid => RefreshStrategy.TokenWithSessionCheck,
                UAuthMode.Hybrid => RefreshStrategy.SessionAndToken,
                _ => throw new SecurityException("Unsupported refresh mode")
            };
        }
    }
}
