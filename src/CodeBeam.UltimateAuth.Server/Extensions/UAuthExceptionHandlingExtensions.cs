using CodeBeam.UltimateAuth.Core.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class UAuthExceptionHandlingExtensions
{
    public static IApplicationBuilder UseUAuthExceptionHandling(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (UAuthRuntimeException ex)
            {
                if (context.Response.HasStarted)
                    throw;

                await WriteProblemDetails(context, ex);
            }
        });
    }

    private static Task WriteProblemDetails(HttpContext context, UAuthRuntimeException ex)
    {
        var problem = new ProblemDetails
        {
            Title = ex.Title,
            Detail = ex.Code,
            Status = MapStatusCode(ex),
            Type = $"{ex.TypePrefix}/{ex.Code}"
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = problem.Status ?? 500;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(problem);
    }

    private static int MapStatusCode(UAuthRuntimeException ex) =>
        ex switch
        {
            UAuthConflictException => StatusCodes.Status409Conflict,
            UAuthValidationException => StatusCodes.Status400BadRequest,
            UAuthUnauthorizedException => StatusCodes.Status401Unauthorized,
            UAuthForbiddenException => StatusCodes.Status403Forbidden,
            UAuthNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };
}
