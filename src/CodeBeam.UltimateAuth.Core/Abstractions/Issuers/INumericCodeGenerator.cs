namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface INumericCodeGenerator
{
    string Generate(int digits = 6);
}
