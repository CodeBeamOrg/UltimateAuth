namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface ISoftDeletable
{
    bool IsDeleted { get; }

    void MarkDeleted(DateTimeOffset now);
}
