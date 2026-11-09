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
