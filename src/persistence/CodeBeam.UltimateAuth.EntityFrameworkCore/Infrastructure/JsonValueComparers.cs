using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore;

internal static class JsonValueComparerHelpers
{
    public static bool AreEqual<T>(T left, T right)
    {
        return string.Equals(
            JsonSerializerWrapper.Serialize(left),
            JsonSerializerWrapper.Serialize(right),
            StringComparison.Ordinal);
    }

    public static int GetHashCodeSafe<T>(T value)
    {
        return JsonSerializerWrapper.Serialize(value).GetHashCode();
    }

    public static T Snapshot<T>(T value)
    {
        var json = JsonSerializerWrapper.Serialize(value);
        return JsonSerializerWrapper.Deserialize<T>(json);
    }

    public static bool AreEqualNullable<T>(T? left, T? right)
    {
        return string.Equals(
            JsonSerializerWrapper.SerializeNullable(left),
            JsonSerializerWrapper.SerializeNullable(right),
            StringComparison.Ordinal);
    }

    public static int GetHashCodeSafeNullable<T>(T? value)
    {
        var json = JsonSerializerWrapper.SerializeNullable(value);
        return json == null ? 0 : json.GetHashCode();
    }

    public static T? SnapshotNullable<T>(T? value)
    {
        var json = JsonSerializerWrapper.SerializeNullable(value);
        return json == null ? default : JsonSerializerWrapper.Deserialize<T>(json);
    }
}

public static class JsonValueComparers
{
    public static ValueComparer<T> Create<T>()
    {
        return new ValueComparer<T>(
            (l, r) => JsonValueComparerHelpers.AreEqual(l, r),
            v => JsonValueComparerHelpers.GetHashCodeSafe(v),
            v => JsonValueComparerHelpers.Snapshot(v));
    }

    public static ValueComparer<T?> CreateNullable<T>()
    {
        return new ValueComparer<T?>(
            (l, r) => JsonValueComparerHelpers.AreEqualNullable(l, r),
            v => JsonValueComparerHelpers.GetHashCodeSafeNullable(v),
            v => JsonValueComparerHelpers.SnapshotNullable(v));
    }
}