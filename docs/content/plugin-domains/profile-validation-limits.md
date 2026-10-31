---
title: Profile Validation & Limits
order: 3
group: plugin-domains
---

# Profile Validation & Limits

UltimateAuth enforces framework-level safety limits on standard user profile fields and metadata.

These limits protect applications from excessively large profile values, unnecessary storage consumption, and resource exhaustion.

They apply consistently to the reference Users domain, regardless of whether InMemory or Entity Framework Core persistence is used.

## Profile Field Limits

The following maximum lengths are enforced:

| Field | Maximum characters |
|---|---:|
| FirstName | 128 |
| LastName | 128 |
| DisplayName | 256 |
| Gender | 128 |
| Bio | 4,096 |
| Language | 64 |
| TimeZone | 128 |
| Culture | 64 |

All fields are optional unless an application introduces additional requirements.

Limits are defined centrally in `UserProfileLimits` and cannot be increased through application configuration.

## Metadata Limits

User profiles support optional key-value metadata.

To prevent unbounded metadata storage, UltimateAuth enforces the following limits:

| Constraint | Maximum |
|---|---:|
| Metadata entries | 32 |
| Key length | 128 characters |
| Value length | 2,048 characters |
| Total metadata size | 8,192 UTF-8 bytes |

The total size includes metadata keys and values.

These limits apply to the complete metadata collection, not individually to each entry.

## Validation Enforcement

Profile safety limits are enforced during:

- User creation.
- Additional profile creation.
- Profile updates.
- Profile cloning.

Validation occurs before invalid data is persisted.

Framework-level safety validation is independent of `IUserProfileValidator`, ensuring that replacing the application validator cannot bypass these limits.

The same restrictions apply to both self-service and administrative operations.

## Custom Validation

Applications can implement `IUserProfileValidator` to introduce additional validation rules.

For example, an application may require a display name, restrict accepted languages, or enforce a shorter biography.

Custom validation can be stricter than the framework limits but cannot override the built-in safety boundaries.

The recommended approach is to use `IUserProfileValidator` for application-specific rules while relying on UltimateAuth for baseline safety validation.

## Validation Errors

When a profile exceeds the supported limits, UltimateAuth rejects the operation with a validation error.

Common error codes include:

| Error code | Meaning |
|---|---|
| `profile_field_too_long` | A standard profile field exceeds its maximum length. |
| `profile_metadata_entries_exceeded` | The metadata collection contains too many entries. |
| `profile_metadata_key_too_long` | A metadata key exceeds its maximum length. |
| `profile_metadata_value_too_long` | A metadata value exceeds its maximum length. |
| `profile_metadata_too_large` | The combined metadata size exceeds the allowed limit. |

Applications should use the error `Code` rather than relying on exception message text.

## Persistence Considerations

The reference Entity Framework Core implementation configures profile column lengths using the same framework limits.

Existing databases may require a migration when upgrading to a version that introduces these constraints.

Before applying restrictive database changes, review existing profile records for values exceeding the new limits.

The InMemory implementation uses the same application-level validation rules.

## Security Considerations

Profile limits are a defense-in-depth measure.

They do not replace HTTP request size limits, rate limiting, or application-specific validation.

Applications should configure these additional protections according to their deployment requirements.

For an overview of user identity and profile management, see [Users Domain](/docs/plugin-domains/users).