namespace CodeBeam.UltimateAuth.InMemory;

public sealed class InMemoryAtomicContext
{
    private readonly List<Action> _rollbackActions = new();

    public void RegisterRollback(Action rollback)
    {
        ArgumentNullException.ThrowIfNull(rollback);

        _rollbackActions.Add(rollback);
    }

    public void Rollback()
    {
        List<Exception>? errors = null;

        for (var i = _rollbackActions.Count - 1; i >= 0; i--)
        {
            try
            {
                _rollbackActions[i]();
            }
            catch (Exception ex)
            {
                errors ??= new List<Exception>();
                errors.Add(ex);
            }
        }

        if (errors is not null)
        {
            throw new AggregateException("One or more rollback operations failed.", errors);
        }
    }
}
