using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Options
{
    internal sealed class UAuthOptionsValidator : IValidateOptions<UAuthOptions>
    {
        public ValidateOptionsResult Validate(string? name, UAuthOptions options)
        {
            var errors = new List<string>();


            if (options.Login is null)
                errors.Add("UltimateAuth.Login configuration section is missing.");

            if (options.Session is null)
                errors.Add("UltimateAuth.Session configuration section is missing.");

            if (options.Token is null)
                errors.Add("UltimateAuth.Token configuration section is missing.");

            if (options.Pkce is null)
                errors.Add("UltimateAuth.Pkce configuration section is missing.");

            if (errors.Count > 0)
                return ValidateOptionsResult.Fail(errors);


            // Only add cross-option validation beyond this point, individual options should validate in their own validators.
            if (options.Token!.AccessTokenLifetime > options.Session!.MaxLifetime)
            {
                errors.Add("Token.AccessTokenLifetime cannot exceed Session.MaxLifetime.");
            }

            if (options.Token.RefreshTokenLifetime > options.Session.MaxLifetime)
            {
                errors.Add("Token.RefreshTokenLifetime cannot exceed Session.MaxLifetime.");
            }

            return errors.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(errors);
        }
    }
}
