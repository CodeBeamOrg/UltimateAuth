using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace CodeBeam.UltimateAuth.Tests.Unit;

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "UltimateAuth.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
