using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;

namespace CodeBeam.UltimateAuth.Sample.ResourceApi;

public static class AppActions
{
    public static class Products
    {
        public static readonly string Read = UAuthActions.Create("products", "read", ActionScope.Self);

        public static readonly string Create = UAuthActions.Create("products", "create", ActionScope.Admin);

        public static readonly string Update = UAuthActions.Create("products", "update", ActionScope.Admin);

        public static readonly string Delete = UAuthActions.Create("products", "delete", ActionScope.Admin);
    }
}
