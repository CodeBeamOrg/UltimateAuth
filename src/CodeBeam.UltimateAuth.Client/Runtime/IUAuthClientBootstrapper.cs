using System.Threading.Tasks;

namespace CodeBeam.UltimateAuth.Client.Runtime
{
    public interface IUAuthClientBootstrapper
    {
        Task EnsureStartedAsync();
    }
}
