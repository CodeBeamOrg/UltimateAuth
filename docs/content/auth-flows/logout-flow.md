---
title: Logout
order: 4
group: auth-flows
---

# 🚪 Logout Flow

Authentication does not end when a client deletes a cookie or forgets a token. In UltimateAuth, ending access is a **server-side lifecycle decision**: the system determines which authentication state must stop being usable, at what scope, and whether the associated device relationship should survive a future login.

That distinction is central to UltimateAuth's design. **Logout ends access; revoke permanently invalidates the selected trust boundary.** Both can prevent a session from being used, but they express different intentions and have different consequences for the session hierarchy.

## Logout is more than clearing credentials

A traditional application may implement logout by removing a browser cookie or deleting a locally stored token. That is useful client cleanup, but by itself it does not necessarily invalidate a credential already issued by the server. A copied token could remain usable until its expiration if the server has no corresponding invalidation mechanism.

UltimateAuth instead models authentication around a hierarchy:

```text
User
└── Root                 Global authentication lineage
    ├── Chain            Device-associated authentication lineage
    │   ├── Session      Individual authentication instance
    │   └── Session      Another instance in the same chain
    └── Chain            Another device
        └── Session
```

This structure gives termination a meaningful scope. An application can end access on the current device, sign out other devices, sign out all devices, or explicitly revoke a session, chain, or root. The correct choice depends on the intent: ordinary sign-out, account security, device compromise, or global trust reset.

> **Mental model:** Logout is a controlled exit from authentication. Revoke is a permanent invalidation of the selected authentication state. A later login may establish new state, but it does not undo a revocation.

## Logout versus revoke

### Logout: end access without necessarily destroying the device lineage

An ordinary logout is designed for situations such as a user selecting **Sign out** in an application. It ends the relevant authenticated access while allowing the device-associated chain to remain available for a subsequent login, subject to its state and lifetime rules.

```text
Before logout
Root (active)
└── Chain (active)
    └── Session (authenticated)

After logout
Root (active)
└── Chain (retained)
    └── Session (no longer usable)

After a later successful login
Root (active)
└── Chain (reusable if still eligible)
    └── New Session
```

**Retaining a chain does not mean retaining an authenticated session.** The user must authenticate again. Reuse is conditional: an expired, revoked, or otherwise ineligible chain must not be treated as active merely because it still exists in storage.

A subtle but important implementation detail: the current HTTP logout flow resolves the authenticated session and invokes the flow service, whose logout command identifies and logs out the **associated chain**. Consequently, the default logout endpoint should not be described as a guarantee that *only one session record* changes. Its practical boundary is the current device chain.

### Revoke: permanently invalidate the selected trust boundary

Revocation is a stronger security operation. A revoked session, chain, or root is not restored by a subsequent login, refresh, or cookie update.

```text
Chain (active)
└── Session (active)

Revoke chain

Chain (revoked — terminal state)
└── Sessions (invalidated)

Future authentication → new eligible chain, not restoration of the old one
```

**Revocation is irreversible for the revoked entity.** This does not mean the user is permanently locked out of the application. A legitimate user may authenticate again and establish a new session lineage if account policy allows it. What cannot happen is bringing the *revoked* session or chain back to life.

This distinction is useful in security-sensitive scenarios: a lost device, suspected credential theft, administrator intervention, or a decision to invalidate all existing authentication relationships.

## Choosing the termination scope

| Operation | Intended scope | What it means |
| --- | --- | --- |
| **Logout current device** | Current session's chain | Ends authenticated access for the current device while preserving the possibility of later eligible chain reuse. |
| **Logout a device** | Selected chain | Ends access on the chosen device; can be initiated by the user or an authorized administrator. |
| **Logout other devices** | Other device sessions | Ends access elsewhere while preserving the specified current device context. |
| **Logout all devices** | All user sessions | Signs the user out across devices without treating this as a root revocation. |
| **Revoke session** | One session | Permanently invalidates the selected session. |
| **Revoke chain** | One device lineage | Permanently invalidates that chain and its sessions. |
| **Revoke other/all chains** | Multiple device lineages | Permanently invalidates selected device relationships. |
| **Revoke root** | Global authentication lineage | Invalidates the root and cascades to its chains and sessions. |

These are not interchangeable aliases. In particular, **logout all devices** and **revoke root** both disrupt broad access, but they do not express the same lifecycle intent.

### Why session-level revoke is separate

UltimateAuth exposes a session revocation operation distinct from the ordinary logout endpoint. A single session can be revoked without necessarily revoking its parent chain. This is useful when an application needs to invalidate a particular authentication instance rather than the entire device relationship.

### Why root revoke is exceptional

The root represents the user's active authentication lineage, not the user's existence in the user directory. Root revocation cascades through the associated authentication hierarchy. A later permitted login may establish a **new** active root; it does not reverse the old root's revocation.

Root revocation should therefore be reserved for an intentional global trust reset rather than ordinary sign-out.

## How the current logout flow executes

### 1. Resolve the flow context

The HTTP endpoint obtains the current `AuthFlowContext`. This carries the relevant authentication state and response configuration, including the resolved session when one is available.

The ordinary logout endpoint does not accept an arbitrary session ID from the browser and blindly invalidate it. It uses the session already resolved for the current flow.

### 2. Request server-side termination

When a `SessionSecurityContext` is present, the endpoint constructs a `LogoutRequest` using that session ID and calls the flow service:

```csharp
var request = new LogoutRequest
{
    SessionId = session.SessionId
};

await _client.Flows.LogoutAsync(request);
```

The logout command resolves the session's chain and invokes the session issuer's `LogoutChainAsync` operation. The session-store implementation performs the chain logout transition.

This is the authoritative part of logout. Removing a client cookie alone is not a substitute for this state transition.

### 3. Clear cookies configured for this flow

After the server-side operation, the HTTP handler attempts to delete credentials whose **effective delivery mode is Cookie**:

- Session ID cookie
- Refresh token cookie
- Access token cookie

The handler uses the configured cookie names through `IUAuthCookieManager`. It does not assume that all three credentials are cookies, and it does not attempt to delete header-delivered credentials from a remote client's storage.

When the flow has no resolved session, the endpoint can still perform cookie cleanup and return a successful logout response. This makes sign-out useful even when a browser holds stale credentials.

### 4. Resolve the response and optional redirect

Once termination and cookie cleanup have been handled, UltimateAuth evaluates the logout response configuration. The result is either a redirect to the configured destination or a normal HTTP success response.

With redirect disabled, the ordinary endpoint returns:

```json
{
  "isSuccess": true
}
```

With redirect enabled, it returns an HTTP redirect to the resolved target instead. Applications should not expect the JSON success body in that case.

## Why redirect matters

A redirect is **not required to invalidate authentication state**. It is a navigation concern that becomes important in browser-based authentication flows.

Consider a server-rendered application with a dedicated sign-out page. Once the session has ended and cookies are cleared, the browser should often return to a public page, a login screen, or the page originally requested by the user. Redirecting completes that user journey without requiring the application to parse a logout response and navigate manually.

Redirects can also help when the authentication endpoint and the client application are on different origins or when a centralized authentication service serves multiple clients. The server can perform the security operation and then send the browser back to the appropriate application destination.

In contrast, a mobile client or API consumer commonly prefers an ordinary HTTP result and controls its own navigation. **Redirect is a response behavior, not a security primitive.**

### Redirect resolution in UltimateAuth

The response resolver selects the effective redirect settings for the logout flow. The redirect resolver then:

1. Checks whether redirect is enabled.
2. On success, considers an allowed return URL override when configured.
3. Otherwise uses the configured success path, resolved against the client base address.
4. If no valid destination is configured, leaves redirect disabled and allows a normal success response.

For absolute return URL overrides, the resolver restricts schemes to HTTP or HTTPS and validates the origin against configured allowed client origins when that allowlist is populated. Relative paths are resolved against the selected client base address.

This is why a return URL should be treated as **validated navigation input**, never as an unrestricted arbitrary destination. Applications should configure allowed origins carefully, especially in multi-client or hosted scenarios.

> **Important:** Do not use a redirect as proof that logout succeeded, or assume that clearing browser cookies invalidates every credential issued to every device. The server-side termination scope determines what access has actually ended.

## Device-aware logout

UltimateAuth treats the device-associated chain as a first-class authentication boundary. That allows applications to build features such as:

- **Sign out of this device** — end the current device's authenticated access.
- **Sign out of another device** — target a specific chain after the appropriate access checks.
- **Sign out of other devices** — preserve the current device while ending other sessions.
- **Sign out everywhere** — end all device sessions for the user.
- **Revoke a lost device** — permanently invalidate the compromised chain instead of merely signing it out.

These operations are exposed through distinct application-service methods, rather than overloading a single logout call with ambiguous behavior.

### Self-service and administrative actions

Device and all-device HTTP endpoints use access contexts carrying explicit action identifiers. They are executed through the access orchestrator, which enforces the applicable authorization decision before the requested state mutation.

For example, `LogoutDeviceSelfAsync` targets the authenticated user's device, whereas `LogoutDeviceAdminAsync` accepts a target user and executes under an administrative action context. Both ultimately call the device logout application service; authorization and target ownership are not left to the client.

The default current-device logout endpoint follows its own flow-based path. Do not assume every termination operation shares the same exact handler or authorization pipeline.

## Security properties and boundaries

### Server-side invalidation comes first

The server must stop recognizing the affected authentication state. Browser cleanup improves user experience and reduces accidental credential reuse, but it is not the source of truth.

### Revoked state is terminal

Revocation cannot be undone by setting `IsRevoked` back to false, presenting an old refresh token, or logging in again with the same device. A new authentication path must establish new eligible state.

### Scope must be explicit

A session revoke should not be casually described as a chain revoke. A logout-all operation should not be described as a root revoke. These distinctions determine whether existing device lineages are retained, ended, or permanently invalidated.

### Authorization and ownership matter

Self-service operations must stay within the authenticated user's permitted scope. Administrative operations must be evaluated under their corresponding action and target context. A client-provided chain identifier is not sufficient authorization to terminate someone else's session.

### Token delivery affects cleanup, not the termination model

Whether a client receives credentials through cookies or headers, the server-side hierarchy still governs session-backed authentication. Clients using non-cookie delivery must also clear their own local credentials after successful termination.

The precise behavior of already-issued, independently validated tokens depends on the active authentication mode and token validation strategy. Avoid assuming every token format provides instantaneous server-side revocation without checking its validation model.

## Practical examples

### A user signs out at the end of the day

Use the ordinary logout flow. The current device's authenticated access ends, configured cookies are cleared, and the browser may be redirected to a public page. A later successful login can reuse the retained device chain if it remains eligible.

### A user loses a phone

Use **chain revoke**, not merely a soft device logout, when the goal is to permanently invalidate that device's existing trust lineage. The user may later authenticate on a replacement device, but the revoked chain stays revoked.

### A user wants to keep this device but sign out elsewhere

Use **logout other devices**. The current device's chain is excluded while other device sessions are terminated. This is different from permanently revoking every other chain.

### An account is believed to be compromised

Depending on the security policy, revoke all chains or the root. This is a stronger response than ordinary logout because it deliberately invalidates existing authentication lineages. Credential rotation and additional account recovery steps may also be required; revocation alone does not prove that compromised credentials have been changed.

## Common misconceptions

**“Logout just deletes the cookie.”** No. The ordinary endpoint performs server-side termination when a session is resolved, then clears applicable cookies.

**“Logout and revoke are the same operation.”** No. Both can stop access, but revocation is permanent for the selected entity; soft logout can retain eligible device lineage.

**“Logout always means exactly one session.”** Not in the current default HTTP flow. The resolved session identifies a chain, and the logout command operates on that chain.

**“Revocation permanently bans the user.”** No. It permanently invalidates existing authentication state at the selected scope. A permitted future login may create new state.

**“Redirect makes logout secure.”** No. Redirect controls where the browser goes after logout; the session-store transition is what ends server-recognized access.

**“Logout all devices is the same as revoke root.”** No. They are different lifecycle operations with different implications for the authentication hierarchy.

## Key takeaways

- **Logout is a lifecycle operation**, not merely client-side credential deletion.
- **Revoke is irreversible** for the revoked session, chain, or root; a future login never restores that entity.
- **Root → Chain → Session** makes termination scope explicit and device-aware.
- **Current-device logout operates through the associated chain** in the current server implementation.
- **Other-device and all-device logout** are separate from hard chain/root revocation.
- **Redirect is optional navigation behavior**, especially useful for browser-based flows; it does not perform revocation.
- **Server-side state is authoritative** for session-backed access; client credential cleanup remains important.

## ➡️ Next step

Continue to **Session Lifecycle** to understand how roots, chains, and sessions are created, rotated, expired, logged out, and revoked over time.
