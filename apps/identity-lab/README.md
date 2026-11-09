# Identity Lab management requests

Interactive Blazor Server events do not have a fresh browser HTTP request.
`IAuthFlow.BeginAsync` uses `IHttpContextAccessor`, and session validation resolves
the device from that HTTP request. A circuit event can therefore have a session
but no current `X-UDID`, causing `DeviceMismatch` and an unauthenticated access
context. Do not construct authenticated access contexts in component events.

Identity Lab uses `LabManagementClient` and `IUAuthRequestClient.SendJsonAsync`
to initiate same-origin browser requests. The existing UltimateAuth transport
bootstraps the device and sends cookies, client profile, device header and an
antiforgery token. Read operations also use POST to reuse this transport without
expanding the public client API. Initial browser calls run after interactive
rendering; mutations retain their UI confirmation steps.

`LabManagementAuthorizationFilter` checks antiforgery, validates the request's
session and device, and creates a request-scoped access context. Endpoints pass
that context to the application services. Actor, tenant, roles and sandbox
ownership are never accepted from the payload. Admin routes additionally use
`LabAdminAuthorizationFilter` and the existing Ladmin guard. Services retain
ownership, capacity, target, tenant and role checks.

Provisioning rollback reuses the flow validated in the same request to create a
delete context bound to the created user. It verifies the actor again and retains
the single-use rollback gate, independent cleanup timeout and authorization
policy. It does not start another session validation or use a system actor.

## Context usage audit

- **HTTP request usage:** management and admin endpoint filters. Downstream
  services receive the validated context; rollback retargets the validated flow.
- **Interactive usages migrated:** member creation, admin repair scan/owner
  repair/empty sandbox deletion, owner recovery status/execution, demo role
  queries/updates, and demo permission queries/checks.
- **Unrelated anonymous usages:** registration and registration availability
  construct contexts for anonymous creation and public identifier validation.
  These do not authorize management through a session and retain their existing
  provisioning and rate-limit rules.
- **Existing browser SDK paths:** sandbox member deletion already uses the
  UltimateAuth browser client for the authenticated framework operation; it does
  not construct an authenticated flow in a component event.

## Endpoint responsibility audit (PR #154)

All Lab management routes below are application operations. None is a proxy for
an equivalent complete UltimateAuth client operation.

| Route (relative to `/api/lab`) | Responsibility retained in Identity Lab |
| --- | --- |
| `POST manage/users` | Owner membership, active sandbox capacity, account/role/membership provisioning and gated rollback. Standard user creation alone cannot complete this workflow. |
| `POST manage/owner-repair/status` | Determine recovery eligibility from persisted Owner identity and sandbox state. |
| `POST manage/owner-repair` | Reconcile an Owner's sandbox and membership under the recovery rules. |
| `POST manage/demo-roles/query` | Require active ownership and a regular member target in the same sandbox; return only the fixed demo role identities. Standard role queries do not enforce these application rules. |
| `POST manage/demo-roles` | Enforce the same sandbox/target rules and the fixed demo-role allowlist. Public framework role mutation is deliberately denied by `LabApplicationPolicy`. |
| `POST manage/demo-permissions` | Require active sandbox membership and derive only exact demo permissions from the fixed Manager/Customer roles. `GetMyPermissionsAsync` returns general framework permissions with different semantics. |
| `POST manage/demo-permissions/check` | Re-evaluate those application-specific permissions before a demo operation. |
| `POST admin/repair/scan` | Ladmin-only cross-sandbox reconciliation snapshot. |
| `POST admin/repair/owner/{userKey}` | Ladmin-only Owner/sandbox/membership reconciliation. |
| `POST admin/repair/sandbox/{sandboxId}/delete` | Ladmin-only deletion of an eligible empty sandbox. |
| `GET admin/sandboxes` (existing) | Ladmin-only application sandbox query. |
| `GET admin/statistics` (existing) | Ladmin-only application statistics. |

Standard framework operations continue to use their existing UltimateAuth client
APIs and endpoints (including member account deletion). No public client API
extension or server-to-server HTTP call is needed.

The HTTP filter keeps the validated `AuthFlowContext` alongside its `AccessContext`
in the current request. Member provisioning receives both explicitly and passes
that same flow to rollback. The context provider retains no last-flow field;
creating another context cannot change the flow used by an in-progress rollback.
