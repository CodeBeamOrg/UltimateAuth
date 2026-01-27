namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public class UAuthResult
    {
        public bool Ok { get; init; }
        public int Status { get; init; }

        public string? Error { get; init; }
        public string? ErrorCode { get; init; }

        public bool IsUnauthorized => Status == 401;
        public bool IsForbidden => Status == 403;
    }

    public sealed class UAuthResult<T> : UAuthResult
    {
        public T? Value { get; init; }
    }

}
