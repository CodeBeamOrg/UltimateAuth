namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface ISoftDeletable<T>
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAt { get; }

    T MarkDeleted(DateTimeOffset now);
}
