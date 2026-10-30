# Execution report — 2026-10-30

Branch: `test/identity-lab-security`, based on `dev` at `64d1dc5` (Identity Lab: UI Polish, Authorization Playground & Diagnostics, #118). Remote `dev` was fetched and confirmed to match the supplied checkout. No production source was changed. No merge or push was performed.

## Implemented and executed

.NET SDK 10.0.403; Release configuration; PostgreSQL 17 (Testcontainers 4.16.0); actual Lab and framework migrations. Every relational test uses an isolated database. The real integration runtime composes framework services, both PostgreSQL contexts, seed contributors, and Identity Lab policies; focused tests mock selected stores/clients and authenticated context sources.

| Identity Lab area | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Security: admin guard, role policies, root resolution | 15 | 0 | 0 |
| Authorization: two-sandbox targets, status, owner restrictions | 10 | 0 | 0 |
| Services: demo roles/store, repair, account deletion | 20 | 0 | 0 |
| Provisioning: forged actors/tokens, rollback replay, registration | 7 | 0 | 0 |
| Integration: real registration/seeding/provisioning, context attribute forgery, role denials, fault cleanup, relational invariants | 14 | 0 | 0 |
| Regression: mismatched system role identity | 0 | 1 | 0 |
| **Total** | **66** | **1** | **0** |

The enabled failing regression expresses required rejection. It was not skipped, inverted or excluded from CI. Final Identity Lab execution took 33 seconds after build. All 67 discovered cases executed.

Existing EF integration tests: **3 passed, 0 failed, 0 skipped**. Existing unit tests: **2,754 passed, 0 failed, 0 skipped**. Existing broader integration tests: **375 passed, 0 failed, 0 skipped** (4 minutes 44 seconds). Across existing projects: **3,132 passed, 0 failed, 0 skipped**.

## Confirmed integrity defect: system-role ID validation gap

Runtime reproduction is encoded in `Regression/SystemRoleIdentityTests.cs`:

1. Configure the role-store contract to resolve normalized `OWNER` to a Role whose ID is `LabRoleIds.Admin`.
2. Return no assignments for a regular target user.
3. Invoke `LabRoleService.AssignAsync(singleTenant, target, "Owner")`.
4. The implementation calls AssignAsync with the returned Admin ID. The regression requires InvalidOperationException before assignment and fails because no exception is thrown.

This requires a malformed role definition or compromised store; no public operation that creates that definition was established. Admin seed validation normally rejects mismatched system IDs. This is a reproduced defense-in-depth integrity defect, **not a demonstrated ordinary-user escalation**. The helper is not registered as a public role-mutation endpoint. Production code remains unchanged.

The real-persistence tests separately confirm that ordinary role-service attempts to acquire Owner/Ladmin or mutate the admin role are denied without altering stored role state. The real context factory replaces forged typed Ladmin attributes with stored Owner role IDs, and the sandbox policy still denies the foreign target. Legitimate registration creates Owner rather than Ladmin, trusted seeding assigns Ladmin only to the fixed administrator, and member provisioning/demo assignment preserves regular user boundaries.

Other risks, not confirmed exploits: registration permits partial account creation and returns RequiresRepair after some failures; multiple services trust actor-key arguments supplied by their callers. Authenticated transport tests are required before classifying those as exploitable defects.

## Infrastructure and verification

Initial shell attempts could not find dotnet or reach the proxy/Docker socket. Enabling network access for the shell command made the configured infrastructure accessible; a local .NET 10 SDK was installed under work/. These initial limitations were resolved. No relational tests were substituted or disabled. The test project explicitly pins EF Core 10.0.12 to match the application and avoid transitive 10.0.5 assembly conflicts. Testcontainers was updated to 4.16.0 after restore reported advisories in an older transitive SSH.NET dependency.

`git diff --check` passes. Application builds emit an existing MudBlazor analyzer warning about Disabled on MudSelectItemGroupExtended; production code was left unchanged. CI itself has not been run remotely, and the external-PostgreSQL connection-string alternative was not separately exercised.

## CI readiness and remaining boundaries

The project is in UltimateAuth.slnx. Existing CI excludes it from the general matrix test step and runs it in a dedicated .NET 10 job using Docker/Testcontainers, with TRX uploaded even on failure. No continue-on-error or defect exclusion was added.

**Not ready to require as a green CI gate:** the preserved integrity regression fails and needs separate triage/approved production work. Full hosted HTTP/cookie/context authenticity, diagnostics transport, standalone identifier availability, complete deletion cleanup, owner/admin repair, registration fault recovery, and cancellation/restart boundaries remain partial or untested. See the coverage matrix for exact boundaries. Passing the other tests is not a claim of complete security coverage.
