namespace CodeBeam.UltimateAuth.Security.Argon2
{
    public sealed class Argon2Options
    {
        // OWASP recommended baseline
        public int MemorySizeKb { get; init; } = 64 * 1024; // 64 MB
        public int Iterations { get; init; } = 3;
        public int Parallelism { get; init; } = Environment.ProcessorCount;

        public int SaltSize { get; init; } = 16;
        public int HashSize { get; init; } = 32;
    }
}
