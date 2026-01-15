using System.Security;

namespace CodeBeam.UltimateAuth.Core.Domain;

public readonly record struct DeviceId
{
    public const int MinLength = 16;
    public const int MaxLength = 256;

    private readonly string _value;

    public string Value => _value;

    private DeviceId(string value)
    {
        _value = value;
    }

    public static DeviceId Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new SecurityException("DeviceId is required.");

        raw = raw.Trim();

        if (raw == "undefined" || raw == "null")
            throw new SecurityException("Invalid DeviceId.");

        if (raw.Length < MinLength)
            throw new SecurityException("DeviceId entropy is too low.");

        if (raw.Length > MaxLength)
            throw new SecurityException("DeviceId is too long.");

        return new DeviceId(raw);
    }

    public static DeviceId CreateFromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 32)
            throw new SecurityException("DeviceId entropy is too low.");

        var raw = Convert.ToBase64String(bytes);
        return new DeviceId(raw);
    }

    public override string ToString() => _value;

    public static explicit operator string(DeviceId id) => id._value;
}
