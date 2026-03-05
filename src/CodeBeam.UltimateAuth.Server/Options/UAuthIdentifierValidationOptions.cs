namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthIdentifierValidationOptions
{
    public UsernameIdentifierRule UserName { get; set; } = new();
    public EmailIdentifierRule Email { get; set; } = new();
    public PhoneIdentifierRule Phone { get; set; } = new();

    internal UAuthIdentifierValidationOptions Clone() => new()
    {
        UserName = UserName.Clone(),
        Email = Email.Clone(),
        Phone = Phone.Clone()
    };
}

public sealed class UsernameIdentifierRule
{
    public bool Enabled { get; set; } = true;
    public int MinLength { get; set; } = 3;
    public int MaxLength { get; set; } = 64;
    public string? AllowedRegex { get; set; } = "^[a-zA-Z0-9._-]+$";

    internal UsernameIdentifierRule Clone() => new()
    {
        Enabled = Enabled,
        MinLength = MinLength,
        MaxLength = MaxLength,
        AllowedRegex = AllowedRegex,
    };
}

public sealed class EmailIdentifierRule
{
    public bool Enabled { get; set; } = true;
    public int MinLength { get; set; } = 3;
    public int MaxLength { get; set; } = 256;

    internal EmailIdentifierRule Clone() => new()
    {
        Enabled = Enabled,
        MinLength = MinLength,
        MaxLength = MaxLength,
    };
}

public sealed class PhoneIdentifierRule
{
    public bool Enabled { get; set; } = true;
    public int MinLength { get; set; } = 3;
    public int MaxLength { get; set; } = 24;

    internal PhoneIdentifierRule Clone() => new()
    {
        Enabled = Enabled,
        MinLength = MinLength,
        MaxLength = MaxLength,
    };
}
