using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class UserAccessorBridge : IUserAccessor
    {
        private readonly IServiceProvider _services;

        public UserAccessorBridge(IServiceProvider services)
        {
            _services = services;
        }

        public async Task ResolveAsync(HttpContext context)
        {
            var accessor = _services.GetRequiredService<IUserAccessor<UserId>>();
            await accessor.ResolveAsync(context);
        }

    }
}
