using System.Reflection;
using CodeBeam.UltimateAuth.Client;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public static class LabVersionInfo
{
    public static string DotNetVersion { get; } = Environment.Version.Major.ToString() + "." + Environment.Version.Minor.ToString() + "." + Environment.Version.Build.ToString();

    private static string GetVersion(Assembly assembly)
    {
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
            return informationalVersion.Split('+')[0];

        var version = assembly.GetName().Version;

        return version is null
            ? "Unknown"
            : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
