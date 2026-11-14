---
title: Concurrency Limiting
order: 10
group: security
---

# Concurrency Limiting

UltimateAuth provides built-in concurrency limiting to protect server resources during expensive password hashing operations.

Unlike [Rate Limiting](/docs/security/rate-limiting), which restricts how frequently requests may be made, concurrency limiting controls how many password hashing operations may execute at the same time.

This is particularly important for memory-hard password hashing algorithms such as Argon2, where excessive parallel execution can place significant pressure on CPU and memory resources.

Concurrency limiting is enabled by default and requires no additional middleware.

## Getting Started

The default configuration automatically derives concurrency limits from the processor count available to the application.

Developers can override these limits through `UAuthServerOptions.ConcurrencyLimiting`:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.ConcurrencyLimiting.PasswordHashing.PermitLimit = 8;
    options.ConcurrencyLimiting.PasswordHashing.QueueLimit = 40;
    options.ConcurrencyLimiting.PasswordHashing.QueueTimeout = TimeSpan.FromSeconds(3);
});
```

No additional registration is required when using UltimateAuth Server.

## Concurrency Policies

UltimateAuth uses three coordinated concurrency policies:

| Policy | Responsibility |
|---|---|
| `PasswordHashing` | Global limit shared by all protected password hashing operations |
| `Login` | Additional limit for login credential verification, including dummy verification |
| `CredentialManagement` | Additional limit for password creation, changes, resets, and history validation |

Every protected operation must acquire capacity from both its workload-specific policy and the global `PasswordHashing` policy.

For example, with a global limit of 8, a login limit of 6, and a credential-management limit of 4:

- At most 8 protected operations execute simultaneously across both workloads.
- Login operations cannot exceed 6 concurrent executions.
- Credential-management operations cannot exceed 4 concurrent executions.

These are upper bounds, not reserved capacities. A workload-specific limit does not guarantee dedicated capacity when the global budget is exhausted.

## Defaults

Each policy initially uses the following defaults:

| Setting | Default |
|---|---|
| `Enabled` | `true` (global setting) |
| `PermitLimit` | `Math.Max(2, Environment.ProcessorCount)` |
| `QueueLimit` | Five times the initial permit limit |
| `QueueTimeout` | 3 seconds |
| Queue processing | Oldest first |

For example:

| Available processors | Concurrent operations per policy | Waiting operations per policy |
|---|---|---|
| 2 | 2 | 10 |
| 4 | 4 | 20 |
| 8 | 8 | 40 |
| 16 | 16 | 80 |

The processor count is evaluated when the options are initialized. In supported .NET hosting environments, it can reflect container CPU restrictions.

These defaults are starting points rather than performance guarantees. Optimal concurrency depends on the configured password hasher, its memory and parallelism settings, hardware resources, and application traffic.

The queue size is initialized from the permit limit. **Changing `PermitLimit` later does not automatically recalculate `QueueLimit`.** Configure both when customizing capacity.

## Queue Behavior

Each concurrency policy has an independent waiting queue.

```csharp
options.ConcurrencyLimiting.Login.PermitLimit = 4;
options.ConcurrencyLimiting.Login.QueueLimit = 20;
options.ConcurrencyLimiting.Login.QueueTimeout = TimeSpan.FromSeconds(3);
```

When all permits are in use, additional operations may wait in the configured queue.

`QueueLimit` supports three behaviors:

| Value | Behavior |
|---|---|
| `0` | Disable waiting; reject when capacity is exhausted |
| Positive integer | Allow up to the specified number of waiting operations |
| `null` | Use an effectively unbounded queue |

An unbounded queue is an advanced setting. Although waiting operations do not execute password hashing, they still consume request and application resources. A timeout does not eliminate the risk of resource exhaustion under sustained load.

### Queue Timeout

`QueueTimeout` limits how long an operation may wait to acquire its required permits.

When an operation needs both a global and workload-specific permit, the current implementation uses the shorter of the two configured timeouts as a shared acquisition deadline.

The timeout applies only while acquiring permits. It does not interrupt a password hashing operation that has already started.

When capacity cannot be acquired, the operation fails without executing the protected password hash.

## Password Hashing Workloads

Concurrency limiting applies to expensive password hashing operations in the built-in authentication and reference credential-management paths.

This includes:

- Real and dummy password verification during login.
- Password hash creation.
- Current-password verification during credential changes.
- Password history comparisons.
- Password hashing during password changes and resets.

Password history comparisons execute sequentially. Each comparison acquires and releases its permits individually rather than reserving capacity for the entire history-validation workflow.

Database operations and session-management actions are not intended to consume password hashing permits.

Custom credential providers and direct calls to a password hasher are not automatically protected merely because concurrency limiting is enabled. Custom implementations must coordinate their own expensive work when appropriate.

## Handling Capacity Exhaustion

When an operation cannot obtain the required capacity, UltimateAuth raises a capacity error with HTTP status `429 Too Many Requests` when handled by the UltimateAuth exception-handling pipeline.

The error code is:

```text
password_verification_busy
```

A capacity rejection is not evidence of invalid credentials, session expiration, or account revocation.

Applications should treat it as a temporary resource-capacity condition rather than an authentication failure.

Request cancellation is preserved separately from queue-capacity failures.

Unlike endpoint rate limiting, password hashing concurrency limiting does not depend on ASP.NET Core's `UseRateLimiter()` middleware.

## Disabling Concurrency Limiting

Concurrency limiting is enabled by default.

To disable UltimateAuth's built-in password hashing concurrency controls:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.ConcurrencyLimiting.Enabled = false;
});
```

Disabling these controls removes UltimateAuth's built-in concurrency protection for the covered operations. It does not disable HTTP rate limiting, authentication lockout policies, or application-owned resource controls.

Use this option only when resource scheduling is handled elsewhere or when the deployment has a specific reason to disable it.

## Deployment Considerations

Concurrency limits are process-local. Each application instance maintains its own budgets and queues.

For example, deploying multiple authentication server replicas increases aggregate capacity because each replica has its own configured limits.

Concurrency limiting protects against excessive simultaneous hashing work, but it is not a substitute for request rate limiting, load testing, memory limits, or infrastructure-level traffic protection.

The configured password hashing algorithm also matters. Previously stored password hashes may retain older cost parameters, so changing current hashing options does not necessarily change the cost of verifying every existing credential.

## Related Documentation

- [Rate Limiting](/docs/security/rate-limiting) — Control request frequency and endpoint budgets.
- [Password Policy & History](/docs/security/password-policy) — Configure password rules and password reuse protection.
- [Login Flow](/docs/auth-flows/login-flow) — Understand credential verification and login security decisions.
- [Server Options](/docs/configuration/server-options) — Configure UltimateAuth Server.
