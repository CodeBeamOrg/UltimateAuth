namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthInternalException : UAuthDeveloperException
    {
        public UAuthInternalException(string message) : base(message) { }
    }
}
