namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthConcurrencyLimitingOptions
{
    /// <summary>
    /// Enables concurrency limiting for expensive password hashing operations.
    /// Enabled by default.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Shared concurrency budget for all password hashing operations.
    /// </summary>
    public UAuthConcurrencyRule PasswordHashing { get; set; } = new();

    /// <summary>
    /// Concurrency policy for login operations, including dummy verification.
    /// </summary>
    public UAuthConcurrencyRule Login { get; set; } = new();

    /// <summary>
    /// Concurrency policy for password creation, changes, resets and history verification.
    /// </summary>
    public UAuthConcurrencyRule CredentialManagement { get; set; } = new();

    internal UAuthConcurrencyLimitingOptions Clone() => new()
    {
        Enabled = Enabled,
        PasswordHashing = PasswordHashing.Clone(),
        Login = Login.Clone(),
        CredentialManagement = CredentialManagement.Clone()
    };
}

public sealed class UAuthConcurrencyRule
{
    /// <summary>
    /// Maximum number of concurrently executing operations.
    /// </summary>
    public int PermitLimit { get; set; }

    /// <summary>
    /// Maximum number of queued operations.
    /// Null represents an unbounded queue.
    /// Zero disables queuing.
    /// </summary>
    public int? QueueLimit { get; set; }

    /// <summary>
    /// Maximum time an operation may wait to acquire a permit.
    /// </summary>
    public TimeSpan QueueTimeout { get; set; } = TimeSpan.FromSeconds(3);

    public UAuthConcurrencyRule()
    {
        PermitLimit = Math.Max(2, Environment.ProcessorCount);
        QueueLimit = checked(PermitLimit * 5);
    }

    internal UAuthConcurrencyRule Clone() => new()
    {
        PermitLimit = PermitLimit,
        QueueLimit = QueueLimit,
        QueueTimeout = QueueTimeout
    };
}
