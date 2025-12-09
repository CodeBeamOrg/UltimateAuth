using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Options
{
    internal sealed class PkceOptionsValidator : IValidateOptions<PkceOptions>
    {
        public ValidateOptionsResult Validate(string? name, PkceOptions options)
        {
            var errors = new List<string>();

            if (options.AuthorizationCodeLifetimeSeconds <= 0)
            {
                errors.Add("Pkce.AuthorizationCodeLifetimeSeconds must be > 0.");
            }

            return errors.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(errors);
        }
    }
}
