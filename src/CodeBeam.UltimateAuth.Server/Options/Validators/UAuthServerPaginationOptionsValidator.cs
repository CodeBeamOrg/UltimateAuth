using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

internal sealed class UAuthServerPaginationOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var errors = new List<string>();

        if (options.Pagination is null)
        {
            errors.Add("Pagination configuration cannot be null.");
            return ValidateOptionsResult.Fail(errors);
        }

        var pagination = options.Pagination;

        if (pagination.DefaultPageSize <= 0)
        {
            errors.Add("Pagination.DefaultPageSize must be greater than zero.");
        }

        if (pagination.MaxPageSize <= 0)
        {
            errors.Add("Pagination.MaxPageSize must be greater than zero.");
        }

        if (pagination.DefaultPageSize > pagination.MaxPageSize)
        {
            errors.Add("Pagination.DefaultPageSize cannot exceed Pagination.MaxPageSize.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
