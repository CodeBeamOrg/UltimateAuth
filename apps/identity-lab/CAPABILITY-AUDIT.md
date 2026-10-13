# Identity Lab Phase 1 capability audit

This audit records the public API boundary used by the three-screen Identity Lab application. Identity Lab is a live application that does not access an UltimateAuth store or persistence model directly.

| Screen / UI feature | UltimateAuth capability | Public API / type | Usable? | Ownership / decision |
|---|---|---|---|---|
| Landing: isolated playground copy and transition | None required | — | Yes | Identity Lab presentation. The environment uses the real in-memory UltimateAuth bundle. |
| Sign in | Flow-based login | `UAuthLoginForm`, `IUAuthClient.Flows`, `LoginRequest` | Yes | UltimateAuth owns validation, login, session establishment and errors. |
| Create account | User creation | `IUAuthClient.Users.CreateAsync(CreateUserRequest)` | Yes | UltimateAuth owns users and credentials. Identity Lab only collects input. |
| Remember me | No matching public semantic was found | — | No | Omitted rather than emulated. |
| GitHub login | No configured external-provider public flow in the inspected samples | — | No | Omitted rather than presenting a non-functional control. |
| Current identity and authorization state | Auth state snapshot | `UAuthPageBase.AuthState`, `UAuthStateView`, `UAuthAuthorize` | Yes | UltimateAuth-backed identity, tenant, roles and session state. |
| Device/session overview | Root → Chain → Session model | `IUAuthClient.Sessions.GetMyChainsAsync`, `SessionChainSummary` | Yes | A chain is labelled as a device/client context; it is not presented as a raw session. |
| Sign out the current session | Current-session logout | `IUAuthClient.Flows.LogoutAsync()` | Yes | The normal current-device action ends the current session without explicitly revoking its chain. |
| Log out another device | Device logout | `IUAuthClient.Flows.LogoutMyDeviceAsync(LogoutDeviceRequest)` | Yes | Used for non-current chains; UltimateAuth owns the logout and resulting chain/session state. |
| Log out other/all devices | Multi-device logout | `IUAuthClient.Flows.LogoutMyOtherDevicesAsync()`, `LogoutAllMyDevicesAsync()` | Yes, not surfaced in Phase 1 | Public APIs exist, but no corresponding approved Phase 1 control is present. |
| Explicit chain revocation | Chain revocation | `IUAuthClient.Sessions.RevokeMyChainAsync(SessionChainId)` | Yes, not surfaced | Reserved for a future advanced security experience; it is not disguised as everyday logout. |
| Validate session | Session flow validation | `IUAuthClient.Flows.ValidateAsync()` | Yes | UltimateAuth-backed. |
| Client profile/runtime | Client product metadata | `IUAuthClientProductInfoProvider.Get()` | Yes | UltimateAuth-backed. |
| Playground label and storage description | Environment metadata | app configuration | Yes | Identity Lab-owned presentation metadata. No security meaning. |
| All-user count/list | Admin query exists but should only appear after permission-aware product design | `IUAuthClient.Users.QueryAsync(UserQuery)` | Deferred | Not queried in Phase 1; avoids conflating the current-user dashboard with admin access. |
| Global active-session count | No unrestricted aggregate API identified | — | No | Omitted; current user's chain count is shown instead. |
| Authentication/security event feed | No public query API identified | — | No | Omitted; the landing preview describes capabilities rather than displaying fabricated events. |
| Playground creation, expiry and quotas | No Identity Lab persistence/service exists in Phase 1 | — | No | Omitted until genuine playground metadata storage is introduced. |
