---
title: Login
order: 2
group: auth-flows
---

# Login Flow

Logging in is often described as a simple exchange: verify a password, issue a token, and consider the user authenticated. That description leaves out the part that matters most over time: **what exactly has been established and how can it be managed or revoked?**

UltimateAuth treats login as a **controlled session-establishment flow**. Credential verification is essential, but it is only one stage. A successful login connects an authenticated user to a tenant, a device context, a session hierarchy, and the security state that governs future requests.

The central idea is straightforward:

> **Login establishes a session. Tokens, when enabled, are credentials issued in connection with that session—not the session itself.**

This distinction shapes how UltimateAuth handles device awareness, session continuity, token issuance, revocation, and authentication policy.

## From a login response to an authentication lifecycle

In a token-centric design, login is frequently treated as the moment a token is minted. The token then becomes the primary artifact developers reason about. This works for simple scenarios, but it can make questions about devices, concurrent logins, global sign-out, and session continuity harder to express consistently.

UltimateAuth starts with the authentication lifecycle instead. It separates four responsibilities:

1. **Identity resolution:** determine which user is attempting to authenticate.
2. **Authentication and security evaluation:** verify the credential and decide whether the attempt is allowed.
3. **Session establishment:** create a managed authentication instance in the appropriate device and user context.
4. **Credential delivery:** optionally issue access and refresh tokens and deliver the applicable credentials according to the server mode and client profile.

These responsibilities are related, but they are not interchangeable. In particular, a valid password does not by itself create a session, and a token is not the sole source of authentication state.

## The session hierarchy

UltimateAuth models authentication through a hierarchy:

```text
User
└── Root
    ├── Chain (Device A)
    │   ├── Session 1
    │   └── Session 2
    └── Chain (Device B)
        └── Session 3
```

Each level answers a different question:

| Level | Responsibility | Why it matters |
| --- | --- | --- |
| **User** | The identity being authenticated | Login identifiers can change without changing the user's internal identity. |
| **Root** | The user-level session hierarchy | Provides a shared context for authentication across devices. |
| **Chain** | A device-associated sequence of sessions | Allows the system to reason about continuity and device-scoped authentication. |
| **Session** | A particular issued authentication instance | Provides the concrete session identity to which authentication credentials can be related. |

The hierarchy is a **logical model**, not a claim that every login inserts a new root or chain. A login can continue an existing active chain for the same user and device. The session subsystem determines how the resulting session is issued and persisted.

A chain is best understood as a **device-associated authentication lifecycle**, rather than as the physical device itself. The quality and stability of device identification depend on the configured device context.

## How a login proceeds

The standard password login flow can be viewed as the following pipeline:

```text
Client request
    │
    ▼
Resolve authentication flow context
    │  Tenant · device · effective mode · client profile
    ▼
Resolve identifier → UserKey
    │
    ▼
Check user eligibility and security state
    │
    ▼
Validate credential using registered providers
    │
    ▼
Find an eligible active chain for this user/device
    │
    ▼
LoginAuthority decision
    ├── Deny ─────────► Failure / lockout handling
    ├── Challenge ────► Continuation required
    └── Allow
          │
          ▼
    Establish session
          │
          ▼
    Optionally issue access/refresh tokens
          │
          ▼
    Dispatch login event
          │
          ▼
    Deliver credentials / redirect or OK
```

This diagram shows the responsibilities in the current flow. Some security-state operations, such as recording failed attempts, happen as part of processing a rejected login. Therefore, **the authority decision precedes session issuance, not every possible state mutation**.

### 1. Resolve the authentication context

Before credential validation, UltimateAuth operates within an `AuthFlowContext`. This context provides the effective tenant, device information, authentication mode, client profile, and applicable server options.

That context matters because the same logical login operation can have different delivery and session requirements depending on how the application is hosted and which kind of client is connecting. Login should not need to be reimplemented for each application style.

A resolvable device ID is required by the current login orchestrator. If device identification cannot be resolved, login cannot proceed to normal session establishment.

### 2. Resolve the login identifier

A user may identify themselves with a username, email address, or another identifier supported by the configured resolver. UltimateAuth translates that external identifier into an internal `UserKey` through `ILoginIdentifierResolver`.

```text
User-provided identifier
          │
          ▼
ILoginIdentifierResolver
          │
          ▼
        UserKey
```

The internal user key, not the original text entered into the login form, becomes the stable reference used by the rest of the authentication flow. This helps keep **how a user signs in** separate from **who that user is**.

The identifiers actually accepted depend on the configured resolver and user data; support for a particular identifier format should not be assumed without configuration.

### 3. Check eligibility and security state

Once the user is resolved, the login orchestrator retrieves their runtime state. The current implementation requires the user to exist, be permitted to authenticate (`CanAuthenticate`), and not be deleted (`IsDeleted`).

It also examines two security scopes:

- **Account state:** security conditions associated with the account as a whole.
- **Factor state:** security conditions associated with the credential type being used.

An existing account or factor lock can stop the attempt before credential validation proceeds. Failed attempts, lockout duration, failure windows, and lock extension are governed by login options.

This separation is intentional: a credential can be correct while the account is not currently eligible to authenticate.

### 4. Validate the credential

UltimateAuth delegates credential verification to registered credential providers rather than embedding password comparison directly into the login orchestrator.

For the current standard HTTP login endpoint, the credential type is **Password**. The orchestrator also rejects unsupported credential types in this path. The provider architecture is extensible, but that extensibility should not be confused with a promise that OTP, passkeys, or external identity providers all use this exact password-login endpoint in v0.1.0.

For eligible credentials, the orchestrator checks that the credential matches the requested type, has not been deleted, is usable under its security rules, and validates the submitted secret through its provider.

This gives the login flow a stable orchestration model while keeping credential-specific verification behind dedicated abstractions.

### 5. Resolve device-associated session continuity

Before the authority decision, the orchestrator looks for a session chain associated with the resolved user and device. If a chain exists and its state is active under the configured idle timeout, its `ChainId` can be carried into session issuance.

Conceptually:

```text
Same user + known device + active chain
                  │
                  ▼
       Continue with that ChainId

No eligible active chain
                  │
                  ▼
       Session subsystem establishes
       the appropriate new chain
```

This enables device-aware continuity without reducing a login to “issue another unrelated token.” The session subsystem remains responsible for the exact creation and persistence behavior.

### 6. Make the authority decision

`LoginAuthority` evaluates the login attempt and returns one of three outcomes:

| Decision | Meaning | Session issued? |
| --- | --- | --- |
| **Allow** | The attempt may proceed to session establishment. | Yes, if the remaining issuance steps succeed. |
| **Deny** | Authentication is rejected. | No. |
| **Challenge** | An additional step or continuation is required. | No, not from this login attempt. |

The current default authority considers user existence, lockout, reauthentication requirements, and credential validity. The broader flow also performs eligibility and lockout checks before invoking the authority.

A challenge is represented through a `LoginContinuation`; the current orchestrator uses an MFA continuation type for its challenge branch. This represents a requirement to continue authentication, **not proof that an MFA exchange has already completed**.

For rejected attempts, the orchestrator may record a failed factor attempt and return lockout or remaining-attempt information when the corresponding disclosure option is enabled. Applications should treat these details as policy-controlled rather than universally available.

### 7. Establish the session

Only an allowed, successfully validated **commit** login proceeds to session issuance.

The orchestrator first records a successful factor authentication (unless explicitly suppressed by an internal execution option), then obtains user claims through `IUserClaimsProvider`.

It constructs a `SessionIssuanceContext` containing the tenant, user key, device, claims, effective mode, session options, and any eligible existing chain ID. The context is passed to `ISessionOrchestrator` using a login-session creation command.

This is the decisive transition:

> The user has not merely supplied a valid secret; the system has established a managed authentication session.

The resulting session has its own identity. That identity is available to the later token-issuance and response-delivery stages.

### 8. Issue tokens when permitted

After session issuance, access and refresh tokens may be requested from the token issuer. The current orchestrator issues them only when `LoginRequest.RequestTokens` is true.

For the HTTP login endpoint, however, the server derives that setting from `AuthFlowContext.AllowsTokenIssuance`. A client cannot force token issuance merely by placing `RequestTokens: true` in its submitted payload.

When issuance is enabled:

- The access token is issued in the context of the authenticated user and issued session.
- Refresh-token issuance is requested with persistence enabled.
- Both issuance paths operate with the tenant, user key, session ID, chain ID, and claims context.

Depending on the effective mode and issuer behavior, an individual token type may be absent. **Successful login and token issuance are deliberately separate concepts.**

### 9. Dispatch the login event and deliver the response

Following successful session and optional token issuance, UltimateAuth dispatches a `UserLoggedInContext` event containing the tenant, user key, time, device, and session ID.

The HTTP endpoint then delegates credential delivery to `ICredentialResponseWriter`. Session ID, access token, and refresh token are handled as distinct grant kinds. Delivery is resolved using the effective mode, authentication flow, client profile, and server configuration.

Depending on configuration, the HTTP response may complete with `200 OK` or a success redirect. A rejected login may produce `401 Unauthorized` or a configured failure redirect.

**Important for v0.1.0:** `TokenResponseMode.Body` is currently unsupported and rejected during response resolution. Do not assume that the HTTP login endpoint returns a JSON object containing the issued tokens. Consult the configured delivery modes and client profile instead.

## TryLogin: evaluate without establishing a session

UltimateAuth distinguishes **previewing a login** from **committing a login**.

`TryLogin` runs the authentication evaluation in preview mode. It can indicate whether credentials would pass the current checks, report permitted failure information, and return a continuation requirement. It does **not** issue a session or tokens.

| Behavior | TryLogin (Preview) | Login (Commit) |
| --- | --- | --- |
| Resolve identifier and evaluate credentials | Yes | Yes |
| Apply applicable failure-attempt handling | Yes | Yes |
| Reset factor failure state on success | No | Yes, normally |
| Create a session | No | Yes, on success |
| Issue tokens | No | Optional |
| Return a preview receipt | Possible | Consumes a matching receipt, if supplied |

On a successful preview, the server can create a short-lived `PreviewReceipt`. That receipt is associated with the user, tenant, credential type, device, identifier, client profile, and a fingerprint of the submitted login material.

When a subsequent commit request includes a matching receipt, the server consumes it and can suppress a duplicate failed-attempt registration for that commit. **The receipt is not a replacement for the password**: the commit path still performs credential validation.

This separation is useful when an application needs to assess login readiness or guide a user through an authentication experience without establishing a session prematurely.

## Device awareness and session management

Because login produces a managed session within a device-associated chain, UltimateAuth can model authentication beyond a single browser token:

- **Multiple devices:** a user can have separate chains associated with distinct device contexts.
- **Session continuity:** an active chain can be considered when the same user signs in again from the same device.
- **Targeted session management:** session and chain identities give downstream operations a way to distinguish authentication instances and device-associated lifecycles.
- **Broader revocation:** the hierarchy provides the foundation for operations that affect more than one session.

The exact scope and availability of logout and revocation operations are defined by their own APIs and policies. Login establishes the identities those operations can act upon; it does not by itself specify every revocation behavior.

## Security is part of the flow, not an afterthought

Several security properties follow from this design:

**User eligibility is separate from credential correctness.** A valid secret does not override a disabled, deleted, or locked account.

**Failed attempts are tracked as security state.** Lockout behavior is configurable and can be evaluated before session issuance.

**Device context participates in session establishment.** The system can distinguish device-associated chains rather than treating all logins as anonymous token issuance events.

**Session issuance follows an explicit decision.** A denied or challenged login does not proceed to normal session creation.

**Response delivery is policy-driven.** Whether a session credential or token is delivered—and by which supported mechanism—is determined by the effective server flow, not solely by a client request.

**The hierarchy supports later lifecycle enforcement.** Token validation, session validity, security-version checks, refresh behavior, and revocation are related concerns, but their exact enforcement belongs to those respective flows. They should not be mistaken for checks all performed inside the login method itself.

## A useful mental model

Think of login as the moment UltimateAuth establishes a **managed, device-aware authentication relationship** between a user and an application.

```text
Identity + Valid Credential + Security Decision
                       │
                       ▼
               Managed Session
                       │
               ┌───────┴────────┐
               ▼                ▼
         Session identity   Optional tokens
               │                │
               └───────┬────────┘
                       ▼
             Ongoing auth lifecycle
```

The session is the managed authentication instance. Tokens, where used, are credentials associated with that instance. They can be issued, refreshed, and validated without redefining what a login means.

This is the shift UltimateAuth is designed to make: **from login as token generation to login as lifecycle-aware session establishment**.

## Key takeaways

- **Login is a flow, not a single credential-check function.** Identity resolution, eligibility, security evaluation, session issuance, and delivery are separate stages.
- **The hierarchy is `User → Root → Chain → Session`.** It expresses user-level, device-associated, and individual session lifecycles.
- **A successful login establishes a session.** It does not necessarily create a new root or chain each time.
- **Tokens are optional.** Issuance is governed by the effective mode and server policy.
- **TryLogin is not Login.** Preview evaluates an attempt without issuing a session; commit establishes one.
- **The authority controls admission to session issuance.** Security-state handling can also occur before and after that decision.
- **Device context matters.** Session continuity and downstream session management are built around it.

## Next step

Continue to **Refresh Flow** to learn how UltimateAuth extends or rotates authentication credentials while preserving the relationship to the managed session lifecycle.
