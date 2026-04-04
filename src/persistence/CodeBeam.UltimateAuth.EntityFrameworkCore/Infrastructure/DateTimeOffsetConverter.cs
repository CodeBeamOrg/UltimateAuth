using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore;

public static class DateTimeOffsetConverter
{
    public static PropertyBuilder<DateTimeOffset> HasUtcDateTimeOffsetConverter(this PropertyBuilder<DateTimeOffset> property)
    {
        return property.HasConversion(UtcDateTimeOffsetConverter);
    }

    public static PropertyBuilder<DateTimeOffset?> HasNullableUtcDateTimeOffsetConverter(this PropertyBuilder<DateTimeOffset?> property)
    {
        return property.HasConversion(NullableUtcDateTimeOffsetConverter);
    }

    private static readonly ValueConverter<DateTimeOffset, DateTime> UtcDateTimeOffsetConverter =
        new(
            v => v.UtcDateTime,
            v => new DateTimeOffset(DateTime.SpecifyKind(v, DateTimeKind.Utc))
        );

    private static readonly ValueConverter<DateTimeOffset?, DateTime?> NullableUtcDateTimeOffsetConverter =
        new(
            v => v.HasValue ? v.Value.UtcDateTime : null,
            v => v.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                : null
        );
}
