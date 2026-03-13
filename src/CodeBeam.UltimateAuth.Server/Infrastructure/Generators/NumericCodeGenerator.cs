using CodeBeam.UltimateAuth.Core.Abstractions;
using System.Security.Cryptography;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class NumericCodeGenerator : INumericCodeGenerator
{
    public string Generate(int digits = 6)
    {
        var max = (int)Math.Pow(10, digits);
        var number = RandomNumberGenerator.GetInt32(max);

        return number.ToString($"D{digits}");
    }
}
