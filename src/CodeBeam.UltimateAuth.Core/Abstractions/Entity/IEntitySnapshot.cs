namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IEntitySnapshot<T>
{
    T Snapshot();
}
