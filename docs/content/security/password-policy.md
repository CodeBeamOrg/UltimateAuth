---
title: Password Policy & History
order: 6
group: security
---

# Password Policy & History

UltimateAuth provides configurable password security policies for applications using its reference password credential implementation.

Instead of requiring every application to implement password validation independently, UltimateAuth provides three built-in policy levels: **Basic**, **Standard**, and **Strict**.

Each level defines a starting set of password requirements. Developers can override individual requirements without replacing the entire policy.

Password validation is enforced on the server. Client applications can retrieve the effective policy to display requirements and provide immediate feedback.

## Password Policy Levels

UltimateAuth includes three built-in presets.

**Standard is the default policy level.**

| Requirement | Basic | Standard (Default) | Strict |
|---|---|---|---|
| Minimum length | 8 | 8 | 15 |
| Maximum length | 128 | 128 | 128 |
| Require letter | No | Yes | Yes |
| Require digit | No | Yes | Yes |
| Require uppercase | No | No | Yes |
| Require lowercase | No | No | Yes |
| Require special character | No | No | Yes |
| Common-password detection | Basic | Standard | Strict |
| User-similarity detection | Basic | Standard | Strict |
| Previous passwords remembered | 0 | 0 | 0 |

These values are the built-in defaults, not fixed limits for every application.

### Basic

Basic provides a minimum password length without requiring particular character classes.

It still enables basic common-password and user-similarity checks.

This preset is intended for applications that prefer fewer composition requirements while retaining server-side checks against predictable passwords.

### Standard

Standard is the default UltimateAuth password policy.

It requires at least eight characters, including a letter and a digit, and enables Standard-level common-password and user-similarity checks.

Applications do not need to explicitly select Standard unless they want to make their configuration intentional.

### Strict

Strict increases the minimum length to 15 characters and requires letters, digits, uppercase letters, lowercase letters, and special characters.

It also selects Strict-level common-password and user-similarity checks.

Strict is available for applications that require more restrictive password composition rules. It is not automatically the best policy for every application: usability, password-manager compatibility, account recovery, and the application's threat model should also be considered.

## Configuring Password Policies

Password policies are configured through `UAuthServerOptions.Password`.

### Use a built-in preset

In `Program.cs`:

```csharp
using CodeBeam.UltimateAuth.Credentials.Contracts;

builder.Services.AddUltimateAuthServer(options =>
{
    options.Password.Level = PasswordPolicyLevel.Strict;
});
```

This applies the Strict preset, including its default length, character requirements, and optional security checks.

### Customize individual requirements

You can override any supported requirement without creating a custom policy implementation.

```csharp
using CodeBeam.UltimateAuth.Credentials.Contracts;

builder.Services.AddUltimateAuthServer(options =>
{
    options.Password.Level = PasswordPolicyLevel.Standard;

    options.Password.MinLength = 12;
    options.Password.MaxLength = 64;

    options.Password.RequireUppercase = true;
    options.Password.RequireSpecialCharacter = true;
});
```

The effective policy starts with Standard and applies the explicitly configured overrides.

Unspecified requirements retain their preset values.

For example, this configuration still requires a letter and a digit, even though those properties are not explicitly set.

### Disable an individual security check

Common-password detection and user-similarity detection are configured independently from the main preset.

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Password.Level = PasswordPolicyLevel.Standard;

    options.Password.CommonPasswords =
        PasswordPolicyLevel.Strict;

    options.Password.UserSimilarity =
        PasswordPolicyLevel.Disabled;
});
```

This keeps Standard password composition rules, increases common-password detection to Strict, and disables user-similarity detection.

Disabling security checks should be a deliberate application decision.

## Password Length Limits

UltimateAuth defines the following limits:

| Setting | Value |
|---|---|
| Minimum configurable password length | 6 |
| Default maximum password length | 128 |
| Absolute input length limit | 256 |

The built-in presets use a maximum length of 128 characters.

Applications can customize the configured maximum within the supported limits. The absolute limit is a defensive boundary and is not intended to replace application-level password policy configuration.

Invalid password policy options are rejected during server options validation.

## Common-Password Detection

A password may satisfy every character requirement and still be easy to guess.

For example, a password such as `qwerty123` may satisfy Standard's letter, digit, and length requirements but still be rejected by the server's common-password checker.

UltimateAuth supports independently configurable detection levels:

- `Disabled`
- `Basic`
- `Standard`
- `Strict`

The selected level determines the checker configuration used by the reference implementation.

Common-password detection is a server-side operation. A client-side character validator cannot establish whether a password passes this check.

## User-Similarity Detection

Passwords that closely resemble account identifiers or other available user information may be predictable.

UltimateAuth provides user-similarity checking with the same configurable levels:

- `Disabled`
- `Basic`
- `Standard`
- `Strict`

The server evaluates similarity using the available password-policy context.

Client applications should not attempt to reproduce the complete similarity decision from public policy information alone.

## Password History

Password history helps prevent users from repeatedly selecting passwords they have used before.

UltimateAuth can remember a configurable number of previous passwords and reject reuse during applicable password credential operations.

### Enable password history

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Password.History.RememberLast = 5;
});
```

`RememberLast` controls how many previous passwords are checked.

| Value | Behavior |
|---|---|
| `0` | Previous-password history checking is disabled |
| `1–10` | Check up to the configured number of previous passwords |

The default value is `0`.

The reference password-history validator also checks whether the submitted password matches the current credential, independently of the configured previous-password count.

Password history compares password candidates against protected password representations; it does not require retaining previous passwords in plaintext.

### Performance Considerations

Password history validation requires a separate password hash verification for the current credential and each stored historical credential. These checks can be CPU and memory-intensive, particularly with Argon2.

For example, `RememberLast = 3` may require four password verifications, followed by hashing the new password if validation succeeds.

Choose the history depth according to your application's security requirements and expected workload rather than automatically selecting the maximum. UltimateAuth applies [Concurrency Limiting](/docs/security/concurrency-limiting) to these hashing operations to help protect shared server resources.

### Persistence

Password history requires an appropriate history store.

UltimateAuth provides reference storage implementations for in-memory and Entity Framework Core persistence.

For persistent deployments, apply the required database migrations before enabling password-history functionality.

An in-memory store is useful for development and testing but does not provide durable history across application restarts.

## Server Enforcement and Client Feedback

UltimateAuth distinguishes between authoritative password validation and client-side guidance.

**The server is the security boundary.**

Client applications may retrieve the effective policy and validate its publicly expressible requirements, including:

- Minimum and maximum length
- Letter and digit requirements
- Uppercase and lowercase requirements
- Special-character requirements

Other checks remain server-authoritative:

- Common-password detection
- User-similarity detection
- Password history

A password passing client-side validation is not guaranteed to be accepted by the server.

Applications should handle server-side password-policy violations as expected validation errors, rather than displaying them as unexpected application failures.

For client API usage and examples, see [Client SDK — Credentials](/docs/client/credentials).

## Security Considerations

Password policies are one part of credential security, not a replacement for the broader authentication system.

Applications should also consider secure password hashing, transport security, login throttling, account recovery, session revocation, and appropriate authorization for credential-management operations.

Changing a password may also affect existing authentication sessions, depending on the configured credential session policy.

See [Session Security Model](/docs/security/session-security) for the relevant session security boundaries.

## Next Steps

- [Credentials Domain](/docs/plugin-domains/credentials) — Understand credential ownership, validation, and lifecycle.
- [Client SDK — Credentials](/docs/client/credentials) — Retrieve password requirements and implement credential-management flows.
- [Real-World Setup](/docs/getting-started/real-world-setup) — Configure UltimateAuth in a real application.
- [Session Security Model](/docs/security/session-security) — Understand the security consequences of credential changes.
