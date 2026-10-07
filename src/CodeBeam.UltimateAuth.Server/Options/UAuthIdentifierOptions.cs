namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthIdentifierOptions
{
    public UAuthIdentifierBehaviorOptions Behavior { get; set; } = new();

    public UAuthIdentifierValidationOptions Validation { get; set; } = new();

    public UAuthIdentifierNormalizationOptions Normalization { get; set; } = new();

    public UAuthIdentifierUniquenessOptions Uniqueness { get; set; } = new();

    internal UAuthIdentifierOptions Clone() => new()
    {
        Behavior = Behavior.Clone(),
        Validation = Validation.Clone(),
        Normalization = Normalization.Clone(),
        Uniqueness = Uniqueness.Clone()
    };
}
