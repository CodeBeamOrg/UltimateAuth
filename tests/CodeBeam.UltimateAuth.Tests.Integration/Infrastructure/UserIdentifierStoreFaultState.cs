using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;

internal sealed class UserIdentifierStoreFaultState
{
    private int _addAttempts;

    public bool Enabled { get; private set; }

    public int FailOnAddAttempt { get; private set; }

    public int AddAttempts => _addAttempts;

    public UserIdentifierType? LastAttemptedType { get; private set; }

    public UserKey? LastAttemptedUserKey { get; private set; }

    public void Enable(int failOnAddAttempt)
    {
        if (failOnAddAttempt <= 0)
            throw new ArgumentOutOfRangeException(nameof(failOnAddAttempt));

        _addAttempts = 0;
        LastAttemptedType = null;
        LastAttemptedUserKey = null;

        FailOnAddAttempt = failOnAddAttempt;
        Enabled = true;
    }

    public void Disable()
    {
        Enabled = false;
    }

    public bool ShouldFail(UserIdentifier identifier)
    {
        if (!Enabled)
            return false;

        var attempt = Interlocked.Increment(ref _addAttempts);

        LastAttemptedType = identifier.Type;
        LastAttemptedUserKey = identifier.UserKey;

        return attempt == FailOnAddAttempt;
    }
}
