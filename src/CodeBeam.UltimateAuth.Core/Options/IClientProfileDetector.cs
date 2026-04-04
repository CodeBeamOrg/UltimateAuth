namespace CodeBeam.UltimateAuth.Core.Options;

public interface IClientProfileDetector
{
    UAuthClientProfile Detect(IServiceProvider services);
}
