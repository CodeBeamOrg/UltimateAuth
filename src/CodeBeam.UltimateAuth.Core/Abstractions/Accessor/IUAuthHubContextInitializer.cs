namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IUAuthHubContextInitializer
    {
        Task EnsureInitializedAsync();
    }
}
