namespace CodeBeam.UltimateAuth.Security.Argon2;

public sealed class Argon2Options
{
    // OWASP recommended baseline
    public int MemorySizeKb { get; set; } = 64 * 1024; // 64 MB
    public int Iterations { get; set; } = 3;
    public int Parallelism { get; set; } = Environment.ProcessorCount;

    public int SaltSize { get; set; } = 16;
    public int HashSize { get; set; } = 32;
}
