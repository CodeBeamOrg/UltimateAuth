---
title: Client HTTP 429
order: 9
group: security
---

# Client HTTP 429 compatibility

HTTP 429 is a temporary request throttle. It is not evidence that a password is wrong, a session is revoked, a token has expired, or access is forbidden. The client does not clear credentials or turn authenticated state into anonymous solely because of a 429.

## Existing contracts

The SDK reuses `UAuthTransportException`, `TryLoginResult`, `TryPkceLoginResult`, `RefreshResult`, `UAuthResult` and `UAuthProblem`. There is no new exception hierarchy. Method signatures on `IFlowClient` are unchanged.

- The stable error code is `rate_limited`. Exceptions expose it as `Code`; login/refresh results as `ErrorCode`; mapped service results as `Problem.Detail`.
- HTTP status remains 429. `UAuthResult.Http.IsTooManyRequests` distinguishes it from `IsUnauthorized` and `IsForbidden`.
- `RetryAfter` is a nullable `TimeSpan` on the exception and application results/problems. Transport DTOs carry the raw `retryAfter` header string across the JS bridge.
- The existing `IUAuthTryResult` interface has default metadata properties, so existing implementers need not add members. The new observed-form transport overload has a default implementation delegating to the original transport method; existing adapters keep their original signatures.

## Behavior by flow

| Flow | HTTP 429 behavior |
|---|---|
| Validation (`ValidateAsync`) | Throws `UAuthTransportException` with status 429, code and optional retry delay before trying to deserialize an authentication result. |
| Blazor state validation (`EnsureAsync`) | Catches that throttle, preserves identity/claims and the last successful validation time, and defers subsequent validation attempts, including force calls. It does not mark the snapshot freshly validated. |
| Refresh | Returns an unsuccessful `RefreshResult` with status 429, `ErrorCode = rate_limited`, and `Outcome = None`. It ignores misleading authentication-outcome headers on a throttle. Repeated refresh calls during the cooldown return the throttle without another request. |
| Automatic refresh | Keeps the existing coordinator loop. The next attempt occurs no earlier than both its configured interval and throttle deadline. No reauthentication event, redirect or termination is caused by 429. |
| Login/PKCE preview | Returns an unsuccessful, distinguishable result with status/code/retry delay and no invalid-credential reason. Try-and-commit does not submit the commit form after a throttled preview. |
| Logout (`LogoutAsync`) | Observes the POST response. A 429 throws the existing transport exception, publishes no successful logout event and performs no success navigation. A confirmed success can clear client state through the existing logout event. |
| Logout/revocation variants, authorization, profile, identifier and credential/recovery operations | Return `UAuthResult` failures with status 429 and a generic rate-limited problem. They do not publish successful mutation events or automatically repeat requests. Authorization throttling is not a Forbidden result. |
| Resource requests | Preserve status/code/retry information without retrying or changing authentication state. HTTP status remains authoritative even if an adapter supplies an inconsistent success flag. |

Genuine refresh 401 still produces `ReauthRequired`, and genuine invalid/expired/revoked validation outcomes still clear state. Existing explicit local actions (`Clear`, `OnLogoutAsync`) and authoritative revocation/mutation events retain their semantics. Throttling does not extend token expiry, update stored credentials, renew validation timestamps or create trusted identity.

## Retry guidance

Delta seconds and canonical HTTP-date Retry-After values are parsed using invariant culture and UTC. Missing, inaccessible or malformed guidance remains null in the application result; it is not protocol corruption. Past dates yield zero delay.

Maintenance scheduling uses a conservative 30-second fallback when guidance is absent, and a one-second minimum to prevent a zero-delay loop. Long guidance is honored; the timer waits in bounded chunks without making early HTTP attempts. The SDK does not add a generic retry service or automatically retry login, logout, recovery, credential changes or administrative mutations.

Cross-origin hosts must expose `Retry-After` through their CORS configuration if browser clients should read it. When it is not exposed, the normal absent-header fallback applies. This change does not modify server CORS or proxy trust settings.

## Navigation and adapter boundaries

`LoginAsync`, DirectCommit, `CompletePkceLoginAsync`, and the final commit of TryAndCommit retain full-page form navigation. The browser owns the eventual HTTP response and displays a 429 page if the committed POST is throttled. Submission completion/preview success is not confirmation that a new session was issued. These navigation APIs cannot return the final page's Retry-After through JS interop; observing that response would require changing their navigation/protocol contract. No client authentication-state transition is inferred from navigation completion.

Logout is different: its observed form request follows a successful server redirect, then navigates to the final page without replaying the POST. It does not navigate after 429. Cross-origin redirects still require appropriate browser/CORS access. A network or redirect failure can leave the server outcome uncertain; the SDK reports an error and does not claim success or automatically replay logout. Revalidate explicitly when connectivity permits. Applications that intentionally choose local logout may still invoke their existing explicit local-clear action.

Adapter authors should preserve status and Retry-After in transport results and implement the observed `SendFormAsync(endpoint, form, navigateOnSuccess, ct)` overload when platform navigation is supported. The default overload uses the existing form request implementation and does not invent navigation capabilities.

## Audited state and retry paths

The HTTP result mapper gates mutation success on status. Service-level credential, session, identifier, user and authorization events remain gated on successful results. `UAuthStateManager` owns state clearing for invalid validation results and explicit authoritative events. `SessionCoordinator` owns automatic refresh scheduling and genuine reauthentication handling. `UAuthAuthenticationStateProvider` only projects that retained state. Browser storage removal remains an explicit operation; the request/preview/resource helpers never clear storage on an HTTP failure. The ASP.NET Core client shim has no separate HTTP retry or token-removal path.
