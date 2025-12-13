namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Default session store factory that throws until a real store implementation is registered.
    /// </summary>
    public sealed class DefaultSessionStoreFactory : ISessionStoreFactory
    {
        /// <summary>Creates a session store instance for the given user ID type, but always throws because no store has been registered.</summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c> in single-tenant mode.</param>
        /// <typeparam name="TUserId">The type used to uniquely identify the user.</typeparam>
        /// <returns>Never returns; always throws.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no session store implementation has been configured.</exception>
        public ISessionStoreKernel<TUserId> Create<TUserId>(string? tenantId)
        {
            throw new InvalidOperationException(
                "No session store has been configured." +
                "Call AddUltimateAuthServer().AddSessionStore(...) to register one."
            );
        }
    }
}
