using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Options
{
    internal sealed class SessionOptionsValidator : IValidateOptions<SessionOptions>
    {
        public ValidateOptionsResult Validate(string? name, SessionOptions options)
        {
            var errors = new List<string>();

            if (options.Lifetime <= TimeSpan.Zero)
                errors.Add("Session.Lifetime must be greater than zero.");

            if (options.MaxLifetime < options.Lifetime)
                errors.Add("Session.MaxLifetime must be greater than or equal to Session.Lifetime.");

            if (options.IdleTimeout.HasValue && options.IdleTimeout < TimeSpan.Zero)
                errors.Add("Session.IdleTimeout cannot be negative.");

            if (options.IdleTimeout.HasValue &&
                options.IdleTimeout > TimeSpan.Zero &&
                options.IdleTimeout > options.MaxLifetime)
            {
                errors.Add("Session.IdleTimeout cannot exceed Session.MaxLifetime.");
            }

            if (options.MaxChainsPerUser <= 0)
                errors.Add("Session.MaxChainsPerUser must be at least 1.");

            if (options.MaxSessionsPerChain <= 0)
                errors.Add("Session.MaxSessionsPerChain must be at least 1.");

            if (options.MaxChainsPerPlatform != null)
            {
                foreach (var kv in options.MaxChainsPerPlatform)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key))
                        errors.Add("Session.MaxChainsPerPlatform contains an empty platform key.");

                    if (kv.Value <= 0)
                        errors.Add($"Session.MaxChainsPerPlatform['{kv.Key}'] must be >= 1.");
                }
            }

            if (options.PlatformCategories != null)
            {
                foreach (var cat in options.PlatformCategories)
                {
                    var categoryName = cat.Key;
                    var platforms = cat.Value;

                    if (string.IsNullOrWhiteSpace(categoryName))
                        errors.Add("Session.PlatformCategories contains an empty category name.");

                    if (platforms == null || platforms.Length == 0)
                        errors.Add($"Session.PlatformCategories['{categoryName}'] must contain at least one platform.");

                    var duplicates = platforms?
                        .GroupBy(p => p)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key);
                    if (duplicates?.Any() == true)
                    {
                        errors.Add($"Session.PlatformCategories['{categoryName}'] contains duplicate platforms: {string.Join(", ", duplicates)}");
                    }
                }
            }

            if (options.MaxChainsPerCategory != null)
            {
                foreach (var kv in options.MaxChainsPerCategory)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key))
                        errors.Add("Session.MaxChainsPerCategory contains an empty category key.");

                    if (kv.Value <= 0)
                        errors.Add($"Session.MaxChainsPerCategory['{kv.Key}'] must be >= 1.");
                }
            }

            if (options.PlatformCategories != null && options.MaxChainsPerCategory != null)
            {
                foreach (var category in options.PlatformCategories.Keys)
                {
                    if (!options.MaxChainsPerCategory.ContainsKey(category))
                    {
                        errors.Add(
                            $"Session.MaxChainsPerCategory must define a limit for category '{category}' " +
                            "because it exists in Session.PlatformCategories.");
                    }
                }
            }

            if (errors.Count == 0)
                return ValidateOptionsResult.Success;

            return ValidateOptionsResult.Fail(errors);
        }
    }
}
