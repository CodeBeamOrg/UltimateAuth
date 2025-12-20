using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    /// <summary>
    /// Resolves <see cref="IUserIdConverter{TUserId}"/> instances from the DI container.
    /// 
    /// If no custom converter is registered for a given TUserId, this resolver falls back
    /// to the default <see cref="UAuthUserIdConverter{TUserId}"/> implementation.
    /// 
    /// This allows applications to optionally plug in specialized converters for certain
    /// user id types while retaining safe defaults for all others.
    /// </summary>
    public sealed class UAuthUserIdConverterResolver : IUserIdConverterResolver
    {
        private readonly IServiceProvider _sp;

        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthUserIdConverterResolver"/> class.
        /// </summary>
        /// <param name="sp">The service provider used to resolve converters from DI.</param>
        public UAuthUserIdConverterResolver(IServiceProvider sp)
        {
            _sp = sp;
        }

        /// <summary>
        /// Returns a converter for the specified TUserId type.
        /// 
        /// Resolution order:
        /// 1. Try to resolve <see cref="IUserIdConverter{TUserId}"/> from DI.
        /// 2. If not found, return a new <see cref="UAuthUserIdConverter{TUserId}"/> instance.
        /// </summary>
        /// <typeparam name="TUserId">The user id type for which to resolve a converter.</typeparam>
        /// <returns>An <see cref="IUserIdConverter{TUserId}"/> instance.</returns>
        public IUserIdConverter<TUserId> GetConverter<TUserId>(string? provider)
        {
            var converter = _sp.GetService<IUserIdConverter<TUserId>>();
            if (converter != null)
                return converter;

            return new UAuthUserIdConverter<TUserId>();
        }

    }
}
