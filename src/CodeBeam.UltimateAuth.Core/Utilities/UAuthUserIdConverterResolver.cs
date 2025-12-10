using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Core.Utilities
{
    public sealed class UAuthUserIdConverterResolver : IUserIdConverterResolver
    {
        private readonly IServiceProvider _sp;

        public UAuthUserIdConverterResolver(IServiceProvider sp)
        {
            _sp = sp;
        }

        public IUserIdConverter<TUserId> GetConverter<TUserId>()
        {
            var converter = _sp.GetService<IUserIdConverter<TUserId>>();
            if (converter != null)
                return converter;

            return new UAuthUserIdConverter<TUserId>();
        }
    }

}
