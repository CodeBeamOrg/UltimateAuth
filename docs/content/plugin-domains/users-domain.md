---
title: Users
order: 2
group: plugin-domains
---

# Users Domain

A user is more than a database record, an email address, or a profile.

In UltimateAuth, user identity is modeled through a separation of **authentication identity, account lifecycle, identifiers, and application-specific user information**.

This distinction allows the authentication engine to operate independently of a particular user entity or persistence model, while the Users Plugin Domain provides the capabilities needed to manage identities throughout their lifecycle.

The Users domain builds on the authentication infrastructure provided by UltimateAuth Core and Server. It does not redefine how authentication works; it defines how user identities are represented, maintained, and integrated with that infrastructure.

## Understanding User Identity

UltimateAuth distinguishes between three aspects of a user:

| Component | Responsibility |
|---|---|
| **Lifecycle** | Account existence, status, and security-relevant state |
| **Identifiers** | Values through which a user can be identified or discovered |
| **Profile** | Application-specific information associated with the user |

These components represent different responsibilities and should not be treated as interchangeable.

For example, changing a display name should not affect authentication state. Verifying an email address should not implicitly change account status. Suspending an account should not require deleting its identifiers or profile.

Separating these concerns makes identity operations more predictable and allows security-sensitive changes to be handled explicitly.

## User Identity in Core and Server

UltimateAuth does not require applications to adopt the Users Plugin Domain simply to authenticate users.

Core and Server provide the foundational authentication infrastructure, including the contracts and runtime mechanisms needed to associate authentication operations with user identities.

At this level, a user is primarily an **authentication subject**.

The authentication system needs to establish which identity is involved in an operation, evaluate the relevant security conditions, and maintain the appropriate authentication state.

It does not inherently need to know the user's display name, profile fields, or how the application's user records are stored.

This separation is intentional:

- **Core** defines foundational authentication concepts, contracts, and security semantics.
- **Server** executes authentication flows and enforces the relevant runtime security boundaries.
- **Users** provides higher-level identity management capabilities through a dedicated Plugin Domain.

An application with an existing identity system can integrate that system with UltimateAuth's authentication infrastructure without necessarily adopting the reference user model.

The authentication identity is therefore a framework-level concept, while the complete user management model belongs to the application or the Users domain.

## What the Users Domain Adds

The Users Plugin Domain provides a structured approach to managing user identities beyond the minimum requirements of authentication.

Its responsibilities include:

- Creating and managing user accounts.
- Maintaining account lifecycle and status.
- Registering and managing user identifiers.
- Enforcing identifier ownership and verification rules.
- Applying user management validation and access boundaries.
- Coordinating security-relevant changes with authentication infrastructure.
- Exposing user management operations through application services and shared contracts.

These capabilities are distinct from credential management.

The Users domain determines **which identity an identifier belongs to** and maintains the relevant user state. The Credentials domain manages the authentication credentials associated with identities.

For example, an email address is a user identifier; a password is a credential. They may participate in the same login flow, but they have different ownership and lifecycle rules.

## Account Lifecycle

The lifecycle represents the existence and security-relevant state of a user account.

In the reference model, lifecycle information includes account creation, account status, and security state used to coordinate authentication-related changes.

Account status may represent conditions such as active, suspended, or deleted, according to the supported lifecycle model.

These states are not merely descriptive profile properties. They can influence whether an account is eligible to participate in authentication and other protected operations.

### Security State and Session Invalidation

User lifecycle changes may require existing authentication state to be invalidated.

UltimateAuth separates the decision to change a user's security state from the mechanisms used to enforce that change.

Depending on the configured authentication mode, enforcement may involve server-side session invalidation, token lifecycle rules, or other supported security mechanisms.

This distinction matters because not every authentication mode can invalidate an already-issued credential immediately.

For example, a locally validated JWT may remain valid until expiration unless the protected resource performs additional server-side validation.

The Users domain participates in security-sensitive account operations, while the authentication infrastructure determines how the resulting security state is enforced.

For more information, see [Authentication Modes](/docs/fundamentals/authentication-modes).

## User Identifiers

An identifier is a value associated with a user that allows the identity to be referenced or resolved.

Common examples include:

- Email addresses.
- Usernames.
- Phone numbers.
- Application-defined identifier types.

UltimateAuth treats identifiers as independent identity records rather than assuming that every user has exactly one email address or username.

This supports applications with different identity requirements without changing the underlying authentication model.

### Identifier Properties

The reference identifier model distinguishes several important properties:

| Property | Purpose |
|---|---|
| **Type** | Identifies the kind of identifier, such as email or username |
| **Value** | The identifier value associated with the user |
| **Normalized value** | A representation used for consistent lookup and comparison |
| **Primary state** | Indicates the identifier's designated primary role |
| **Verification state** | Indicates whether ownership has been verified |

These properties address separate questions.

An identifier may exist without being verified. A verified identifier does not automatically become primary. A primary identifier does not necessarily satisfy every authentication policy.

### Login Identifier Resolution

Identifier resolution determines how an incoming login identifier is associated with a user identity.

In the reference approach, primary identifiers define the intended login surface, subject to the configured identifier resolution and verification policies.

Applications can control which identifier types are accepted and how identifiers are resolved.

This allows authentication to support different identity strategies without embedding email-specific or username-specific assumptions into Core.

The distinction is important: **an identifier is identity data; whether it is acceptable for login is an authentication policy decision.**

### Identifier Ownership and Verification

Identifier verification is a security operation, not a simple update to a Boolean property.

Marking an identifier as verified must be based on an appropriate ownership proof or an explicitly authorized administrative operation.

The reference domain therefore distinguishes between:

- Registering an identifier.
- Establishing ownership of an identifier.
- Verifying that ownership.
- Changing an identifier's role or status.

An anonymous or self-service request must not be able to claim verified ownership merely by supplying a verification flag.

Likewise, operations targeting another user's identifiers must respect the applicable authorization and ownership boundaries.

This prevents identifier management from becoming an alternative route around authentication security.

## User Profiles

A profile contains information about the user that is not itself an authentication credential or an identity verification proof.

Typical profile information may include:

- Display name and personal information.
- Localization preferences.
- Application metadata.
- Other domain-specific user attributes.

UltimateAuth deliberately separates profile data from the security-critical identity model.

Applications should be able to evolve their profile structure without redesigning authentication flows or changing how identifiers are verified.

A profile attribute may still be relevant to application authorization or business rules, but it should not automatically be treated as proof of identity.

The reference model provides a starting point; applications remain responsible for deciding which additional user attributes belong in their own domain.

## Reference Implementation

The Users Plugin Domain includes a reference implementation that demonstrates and provides a practical user management model.

It brings together lifecycle, identifiers, and profile management through application services and supporting abstractions.

Rather than exposing persistence operations as the primary application interface, the reference implementation places user management rules within the application service layer.

This layer coordinates operations such as:

- User creation and account management.
- Identifier registration and updates.
- Identifier verification and ownership validation.
- Account status changes.
- Integration and lifecycle notifications.
- Security-related coordination with the authentication infrastructure.

This is an important architectural boundary.

A request to verify an identifier, for example, is not equivalent to directly updating a database field. The application service is responsible for enforcing the rules that make the operation valid.

The reference implementation provides a recommended starting point for these behaviors without requiring every application to use the same persistence technology or user profile structure.

## Domain Integration

User management frequently affects other parts of an identity platform.

Creating a user may be followed by credential enrollment. Changing account security state may require session invalidation. Deleting an account may require coordinated cleanup.

UltimateAuth supports these scenarios through explicit integration contracts rather than direct dependencies between Plugin Domain implementations.

For example, `IUserLifecycleIntegration` provides an integration boundary for user lifecycle operations.

This allows other capabilities to participate in user-related workflows while keeping the Users domain responsible for its own state and rules.

The distinction between domain ownership and cross-domain coordination is central to UltimateAuth's modular architecture.

## Customizing User Management

Applications are not required to accept every detail of the reference implementation.

Depending on their requirements and the available extension contracts, they may:

- Provide custom identifier resolution.
- Configure supported identifier and verification policies.
- Replace storage implementations.
- Integrate existing user repositories.
- Extend supported lifecycle and application service integrations.

For most applications, the recommended approach is to begin with the reference implementation and customize specific behavior rather than replacing the entire domain.

More advanced scenarios, including custom domain implementations and integration with existing identity systems, will be covered separately in the customization documentation.

## Choosing Your User Model

There are two broad integration approaches.

**Use the reference Users domain** when the application needs a ready-made foundation for account lifecycle, identifiers, verification, and user management.

**Integrate an existing identity model** when the application already owns these responsibilities and needs UltimateAuth primarily for its authentication infrastructure.

Neither approach changes the fundamental security requirements.

Identity resolution must be trustworthy, identifier ownership must be protected, and authentication state must be managed according to the configured security model.

The difference is which part of the application owns the user management implementation.

## Next Steps

The Users domain establishes identity ownership and account lifecycle, but it does not independently provide every capability of an identity platform.

- [Credentials](/docs/plugin-domains/credentials) — Learn how authentication credentials are managed independently of user identifiers.
- [Authorization](/docs/plugin-domains/authorization) — Explore how identities are associated with permissions and access policies.
- [Plugin Domains](/docs/plugin-domains) — Return to the overall Plugin Domain architecture.

For the distinction between identity and authentication infrastructure, see [Authentication Model](/docs/fundamentals/authentication-model).