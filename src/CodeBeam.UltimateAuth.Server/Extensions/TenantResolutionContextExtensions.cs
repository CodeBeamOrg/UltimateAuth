//using Microsoft.AspNetCore.Http;
//using CodeBeam.UltimateAuth.Core.MultiTenancy;

//namespace CodeBeam.UltimateAuth.Server.MultiTenancy
//{
//    public static class TenantResolutionContextExtensions
//    {
//        public static TenantResolutionContext FromHttpContext(this HttpContext ctx)
//        {
//            var headers = ctx.Request.Headers
//                .ToDictionary(
//                    h => h.Key,
//                    h => h.Value.ToString(),
//                    StringComparer.OrdinalIgnoreCase);

//            return new TenantResolutionContext
//            {
//                Headers = headers,
//                Host = ctx.Request.Host.Host,
//                Path = ctx.Request.Path.Value,
//                RawContext = ctx
//            };
//        }
//    }
//}
