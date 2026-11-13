using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options.Validators;

internal sealed class UAuthArtifactStoreOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var store = options.AuthArtifacts;

        if (store is null)
            return ValidateOptionsResult.Fail("AuthArtifacts is required.");

        var errors = new List<string>();

        if (store.MaxEntries <= 0)
            errors.Add("AuthArtifacts.MaxEntries must be positive.");

        if (store.CleanupInterval < TimeSpan.FromSeconds(1))
            errors.Add("AuthArtifacts.CleanupInterval must be at least one second.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
