using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal static class SessionValidationMapper
    {
        public static SessionSecurityContext? ToSecurityContext(SessionValidationResult result)
        {
            if (!result.IsValid)
            {
                if (result?.SessionId is null)
                    return null;

                return new SessionSecurityContext
                {
                    SessionId = result.SessionId.Value,
                    State = result.State,
                    ChainId = result.ChainId,
                    UserKey = result.UserKey,
                    BoundDeviceId = result.BoundDeviceId
                };
            }

            return new SessionSecurityContext
            {
                SessionId = result.SessionId!.Value,
                State = SessionState.Active,
                ChainId = result.ChainId,
                UserKey = result.UserKey,
                BoundDeviceId = result.BoundDeviceId
            };
        }
    }

}
