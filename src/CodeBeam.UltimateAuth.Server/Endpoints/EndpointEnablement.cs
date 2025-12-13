namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    internal static class EndpointEnablement
    {
        public static bool Resolve(bool? overrideValue, bool modeDefault)
            => overrideValue ?? modeDefault;
    }
}
