namespace CodeBeam.UltimateAuth.Server.Contracts
{
    public sealed class SessionRefreshResult
    {
        public bool Succeeded { get; }
        public string? NewSessionId { get; }

        private SessionRefreshResult(bool succeeded, string? newSessionId)
        {
            Succeeded = succeeded;
            NewSessionId = newSessionId;
        }

        public static SessionRefreshResult Success(string? newSessionId = null)
            => new(true, newSessionId);

        public static SessionRefreshResult Failed()
            => new(false, null);
    }
}
