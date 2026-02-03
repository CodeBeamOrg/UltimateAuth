namespace CodeBeam.UltimateAuth.Authorization.InMemory;

public interface IAuthorizationSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}
