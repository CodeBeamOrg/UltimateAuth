namespace CodeBeam.UltimateAuth.Core.Contracts;

public class UAuthResult
{
    public bool IsSuccess { get; init; }
    public int Status { get; init; }
    public string? CorrelationId { get; init; }
    public string? TraceId { get; init; }

    public UAuthProblem? Problem { get; init; }

    public HttpStatusInfo Http => new(Status);

    public string? ErrorText => Problem?.Detail ?? Problem?.Title;

    public sealed class HttpStatusInfo
    {
        private readonly int _status;

        internal HttpStatusInfo(int status)
        {
            _status = status;
        }

        public bool IsBadRequest => _status == 400;
        public bool IsUnauthorized => _status == 401;
        public bool IsForbidden => _status == 403;
        public bool IsConflict => _status == 409;
    }
}

public sealed class UAuthResult<T> : UAuthResult
{
    public T? Value { get; init; }
}
