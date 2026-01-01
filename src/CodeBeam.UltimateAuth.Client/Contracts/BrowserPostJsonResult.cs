namespace CodeBeam.UltimateAuth.Client.Contracts
{
    public sealed record BrowserPostJsonResult<T>
    {
        public bool Ok { get; init; }
        public int Status { get; init; }
        public T? Body { get; init; }
    }
}
