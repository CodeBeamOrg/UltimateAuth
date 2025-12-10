namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthDeviceLimitException : UAuthDomainException
    {
        public string Platform { get; }

        public UAuthDeviceLimitException(string platform) : base($"Device limit exceeded for platform '{platform}'.")
        {
            Platform = platform;
        }

    }
}
