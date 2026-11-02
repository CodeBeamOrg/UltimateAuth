using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Credentials.Policies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Credentials.Extensions;

public static class PasswordPolicyServiceCollectionExtensions
{
    public static IServiceCollection AddPasswordStrengthPolicy(this IServiceCollection services)
    {
        services.TryAddScoped<IPasswordStrengthPolicy, UAuthPasswordStrengthPolicy>();
        services.TryAddSingleton<IPasswordCommonPasswordChecker, BuiltInCommonPasswordChecker>();
        services.TryAddSingleton<IPasswordUserSimilarityChecker, UAuthPasswordUserSimilarityChecker>();
        services.TryAddScoped<IPasswordPolicyContextProvider, UAuthPasswordPolicyContextProvider>();

        return services;
    }

    public static IServiceCollection ReplacePasswordStrengthPolicy<TPolicy>(this IServiceCollection services) where TPolicy : class, IPasswordStrengthPolicy
    {
        services.Replace(ServiceDescriptor.Scoped<IPasswordStrengthPolicy, TPolicy>());

        return services;
    }
}
