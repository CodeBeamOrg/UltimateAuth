using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Flows;

public interface IRefreshResponseWriter
{
    void Write(HttpContext context, RefreshOutcome outcome);
}
