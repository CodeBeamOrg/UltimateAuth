---
title: Credentials
order: 3
group: plugin-domains
---

# Credentials Domain

Authentication establishes whether an entity can be trusted to act as a particular identity. A credential provides the evidence used to establish that trust.

UltimateAuth treats credentials as a distinct domain rather than embedding authentication secrets directly into user records or authentication flows.

The **Credentials Plugin Domain** manages the lifecycle, verification, and security of authentication credentials while allowing the authentication engine to remain independent of a particular credential type.

The current reference implementation supports password credentials. Its architecture establishes the separation of responsibilities needed to accommodate additional credential mechanisms without redefining the underlying authentication model.

## Understanding Credentials

A credential represents authentication material or a mechanism through which an identity can demonstrate possession of an accepted authentication factor.

Credentials may take different forms:

| Credential mechanism | Authentication evidence |
|---|---|
| **Password** | Knowledge of a secret verified against a protected representation |
| **One-time code** | Possession of a time-limited or single-use authentication value |
| **Cryptographic credential** | Proof of control over a private key or authenticator |
| **External identity provider** | A trusted authentication assertion established through an external protocol |

These mechanisms do not necessarily share the same storage, verification, or lifecycle requirements.

For example, a password requires secure secret verification, while a cryptographic credential may require signature validation and public-key metadata. An external provider may require validating protocol assertions rather than storing a reusable secret.

UltimateAuth's credential architecture is intended to accommodate these differences through explicit credential-specific behavior.

**Current implementation:** Password credentials are supported. The other mechanisms illustrate the broader credential model and its intended extension boundaries; they are not presented as implemented credential types.

## Credentials and User Identity

A user identity and a credential are separate concepts.

The Users domain manages identity ownership, identifiers, and account lifecycle. The Credentials domain manages the authentication material associated with an identity.

This separation means:

- A user can exist independently of a particular credential.
- Credential state can change without redefining the user's identity.
- An identity can conceptually be associated with multiple authentication mechanisms.
- Credential verification does not replace user lifecycle or account eligibility checks.

A username or email address is generally an **identifier**, not a credential.

An identifier helps resolve which identity is involved in an authentication attempt. A credential provides evidence that the actor is entitled to authenticate as that identity.

For a password-based login, these responsibilities work together:

1. The submitted identifier is resolved to an identity.
2. The applicable password credential is located.
3. The submitted secret is verified.
4. Authentication policies and relevant security conditions are evaluated.
5. The authentication engine establishes authentication state if the flow succeeds.

The credential domain participates in establishing identity proof, but it does not independently determine the entire authentication outcome.

## Responsibilities Across the Framework

Credential support spans several architectural layers, each with a different responsibility.

| Layer | Responsibility |
|---|---|
| **Core** | Foundational authentication contracts and security concepts |
| **Server** | Authentication flow execution, orchestration, and authentication-state management |
| **Credentials Bridge** | Contracts connecting credential capabilities to the authentication server |
| **Credentials Contracts** | Shared credential operation models and results |
| **Credentials Reference Implementation** | Credential management, validation, recovery, and lifecycle behavior |
| **Persistence** | Storage of credential records and related security state |

The distinction is between **using credentials to authenticate** and **managing credentials as domain resources**.

The authentication engine coordinates authentication flows. The Credentials domain owns the rules governing credential records, their usability, and their lifecycle.

An application may integrate a custom credential system through the relevant authentication contracts without necessarily adopting the reference credential management implementation.

## Password Credentials

Password is the first supported credential mechanism in UltimateAuth.

A password credential represents a password-based authentication factor associated with an identity.

The reference implementation separates the submitted secret from its protected representation and the credential's operational state.

### Secret Protection

Passwords must not be stored as plaintext or returned through credential management responses.

The password verification process operates against a protected password representation using the configured password hashing infrastructure.

Credential management APIs should expose only the information required for their intended operations, not the underlying secret.

This separation also allows password verification and password management to evolve without changing the user identity model.

### Credential Security State

A credential is not necessarily usable merely because a matching secret has been supplied.

Credential validation must also account for relevant lifecycle and security conditions, such as:

- Whether the credential is active or revoked.
- Whether an applicable expiration condition has been reached.
- Whether the credential belongs to the intended identity.
- Whether the requested operation is permitted.
- Whether the credential is eligible for the authentication mechanism being executed.

Secret verification and credential usability are related but distinct decisions.

A correctly supplied password must not make a revoked or otherwise unusable credential valid.

## Credential Validation

UltimateAuth separates credential-specific verification from the broader authentication flow.

For password authentication, credential validation includes evaluating the supplied secret against the stored protected representation and enforcing the relevant credential conditions.

The authentication server then uses the result within its authentication flow.

This boundary matters because credential verification is not equivalent to session creation or token issuance.

A successful credential check establishes evidence for authentication. The authentication engine remains responsible for determining whether the flow can complete and what authentication state should be created.

The same architectural distinction applies to other credential mechanisms.

A future cryptographic credential validator, for example, would verify a different kind of proof, but it would still participate in a broader authentication decision rather than independently issuing authentication state.

## Credential Lifecycle

Credentials are managed resources with their own lifecycle.

The reference domain provides operations around credential creation, secret changes, revocation, and recovery.

| Operation | Purpose |
|---|---|
| **Add** | Register a credential for an identity |
| **Change secret** | Replace authentication material through an authorized operation |
| **Revoke** | Make a credential unusable |
| **Begin reset** | Initiate a controlled credential recovery process |
| **Complete reset** | Replace a secret after satisfying recovery requirements |

These operations have different security requirements.

Changing a password while authenticated is not equivalent to recovering access without the existing password. Likewise, revoking a credential is not the same as invalidating every authentication session previously established using that credential.

UltimateAuth treats these distinctions explicitly rather than assuming that every credential mutation has the same security effect.

## Credential Ownership and Operation Scope

Credential management operations must be evaluated in the context of the actor, the target identity, and the requested operation.

UltimateAuth distinguishes between self-service, administrative, and anonymous recovery scenarios.

### Self-Service Operations

An authenticated user may manage credentials associated with their own identity, subject to the applicable permissions and validation rules.

Changing a secret may require proving knowledge of the existing secret or satisfying another supported security requirement.

### Administrative Operations

Administrative credential management is performed against an explicitly identified target.

Administrative authority does not eliminate the requirement to preserve target identity consistency.

For example, a credential recovery operation must not modify a different user's credential merely because a supplied identifier resolves to another account.

### Anonymous Recovery

Anonymous recovery allows an identity to regain access without an existing authenticated session.

Because the actor is not authenticated, the recovery process must establish sufficient proof before allowing credential modification.

These operation scopes are security boundaries, not merely different API routes.

## Credential Recovery

Credential recovery is one of the most security-sensitive responsibilities of the Credentials domain.

The password reset process is designed as a controlled, multi-stage operation rather than an unrestricted secret update.

### Beginning Recovery

A recovery request identifies the intended account and initiates a proof-based process.

Depending on the supported recovery configuration, proof may take the form of a token or code delivered through an appropriate notification mechanism.

The proof must be bound to the intended recovery context and subject to server-controlled validity rules.

Recovery material must not be exposed to an anonymous caller simply because that caller initiated the request.

### Completing Recovery

Completing recovery requires validating the supplied proof and its associated security conditions before applying a new secret.

The reference recovery model accounts for:

- Proof validity and expiration.
- Single-use recovery material.
- Consistency between the recovery target and intended identity.
- Rejection of invalid or previously consumed proof.
- Protection against unauthorized credential replacement.
- Security-state updates following successful recovery.

A completed recovery must not leave the same proof available for another successful reset.

### Enumeration Resistance

Anonymous recovery endpoints must avoid unnecessarily revealing whether an identifier belongs to an existing account.

UltimateAuth separates externally observable recovery responses from the internal decisions required to locate and manage the account.

This reduces account enumeration opportunities while preserving the ability to enforce recovery rules.

Recovery behavior should also be combined with appropriate abuse protection and operational monitoring.

## Credentials and Authentication Sessions

Changing or recovering a credential may require action against authentication sessions that were established before the change.

However, **credential lifecycle and session lifecycle are separate responsibilities**.

UltimateAuth supports security policies that determine how credential changes and resets affect existing sessions.

The available policy model includes:

| Policy | Intended behavior |
|---|---|
| `KeepAll` | Preserve existing sessions |
| `KeepCurrent` | Preserve the current session while revoking other applicable sessions |
| `RevokeAll` | Revoke all applicable sessions |

The appropriate policy depends on the operation and the application's security requirements.

A password reset performed through anonymous recovery may warrant different treatment from an authenticated password change.

Enforcement also depends on the authentication mode.

In session-authoritative modes, revocation can be reflected through server-side session validation. In JWT-based modes, previously issued access tokens may remain usable until expiration unless additional validation mechanisms are involved.

Credential changes therefore initiate the applicable security response; they do not imply identical immediate token invalidation across every mode.

See [Authentication Modes](/docs/fundamentals/auth-modes) for the underlying differences.

## Integration with the Users Domain

The Credentials domain does not need to own user records to manage authentication material associated with an identity.

User creation, deletion, or other lifecycle changes may require corresponding credential operations.

UltimateAuth supports coordination through integration contracts rather than requiring the Users and Credentials implementations to depend directly on one another.

For example, an application may coordinate initial credential enrollment with user creation or perform credential cleanup as part of account removal.

These behaviors belong to explicit integration workflows.

Creating a user does not inherently require creating a password credential, and deleting a user should not rely on an undocumented side effect to maintain credential consistency.

This distinction keeps identity lifecycle and credential lifecycle independently understandable.

## Extending Credential Capabilities

Credential mechanisms differ substantially in how they establish trust.

UltimateAuth's architecture is intended to support additional mechanisms by separating credential-specific verification and management from authentication flow orchestration.

Such extensions may require their own:

- Credential representation and metadata.
- Enrollment and verification procedures.
- Security and expiration rules.
- Recovery or revocation behavior.
- Persistence and integration contracts.

Not every credential mechanism needs to implement password-style secret changes or recovery.

The goal is to preserve consistent authentication boundaries while allowing each credential mechanism to enforce its own security requirements.

For most applications using password authentication, the reference implementation provides the recommended starting point.

Applications with specialized requirements can customize supported services and integrations or provide alternative implementations through the relevant contracts.

Detailed extension and replacement strategies belong in the dedicated customization documentation.

## Architectural Perspective

The Credentials domain establishes an important distinction within UltimateAuth:

**A user identifies the account. A credential provides authentication evidence. The authentication engine decides how that evidence establishes authentication state.**

Keeping these responsibilities separate allows the framework to support identity management without tying authentication to one user entity, one secret format, or one credential mechanism.

The current password implementation provides the operational foundation. The broader architecture allows credential capabilities to evolve without turning the authentication engine into a monolithic credential management system.

## Next Steps

- [Users](/docs/plugin-domains/users) — Understand identity ownership, identifiers, and account lifecycle.
- [Authorization](/docs/plugin-domains/authorization) — Explore how authenticated identities are evaluated against access policies.
- [Plugin Domains](/docs/plugin-domains) — Review the architecture and responsibilities of Plugin Domains.

For the authentication flow that consumes credential verification results, see [Flow-Based Authentication](/docs/fundamentals/flow-based-authentication).