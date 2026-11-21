namespace CodeBeam.UltimateAuth.Core.Options;

public interface IApplicationProfileDetector
{
    UAuthApplicationProfile Detect(IServiceProvider services);
}
