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
        services.TryAddScoped<IPasswordStrengthPolicy, DefaultPasswordStrengthPolicy>();
        services.TryAddSingleton<IPasswordCommonPasswordChecker, BuiltInCommonPasswordChecker>();
        services.TryAddSingleton<IPasswordUserSimilarityChecker, DefaultPasswordUserSimilarityChecker>();
        services.TryAddScoped<IPasswordPolicyContextProvider, DefaultPasswordPolicyContextProvider>();

        return services;
    }

    public static IServiceCollection AddPasswordPolicyOptions(this IServiceCollection services, Action<PasswordPolicyOptions>? configure = null)
    {
        var builder = services.AddOptions<PasswordPolicyOptions>();

        if (configure is not null)
            builder.Configure(configure);

        builder.ValidateOnStart();

        services.TryAddSingleton<IValidateOptions<PasswordPolicyOptions>, PasswordPolicyOptionsValidator>();

        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<PasswordPolicyOptions>>().Value;

            return PasswordPolicyOptionsResolver.Resolve(options);
        });

        return services;
    }

    public static IServiceCollection ReplacePasswordStrengthPolicy<TPolicy>(this IServiceCollection services) where TPolicy : class, IPasswordStrengthPolicy
    {
        services.Replace(ServiceDescriptor.Scoped<IPasswordStrengthPolicy, TPolicy>());

        return services;
    }
}
