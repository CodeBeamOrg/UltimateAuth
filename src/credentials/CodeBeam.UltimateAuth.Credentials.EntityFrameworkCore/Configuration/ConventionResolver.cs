using System.Linq.Expressions;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore
{
    internal static class ConventionResolver
    {
        public static Expression<Func<TUser, TProp>>? TryResolve<TUser, TProp>(params string[] names)
        {
            var prop = typeof(TUser)
                .GetProperties()
                .FirstOrDefault(p =>
                    names.Contains(p.Name, StringComparer.OrdinalIgnoreCase) &&
                    typeof(TProp).IsAssignableFrom(p.PropertyType));

            if (prop is null)
                return null;

            var param = Expression.Parameter(typeof(TUser), "u");
            var body = Expression.Property(param, prop);

            return Expression.Lambda<Func<TUser, TProp>>(body, param);
        }
    }
}
