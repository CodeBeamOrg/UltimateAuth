# Service regression coverage addition

## Assessment

Reviewed SandboxService, SandboxUserService, LabDashboardService, SandboxAdminService,
LabAdminService, SandboxRepairService, LabOwnerRepairService and LabRepairService,
alongside existing provisioning, registration, deletion, role, authorization and
PostgreSQL integration tests. Existing tests already protect creation relationships,
duplicate owners, membership isolation, capacity contention, member provisioning,
rollback, owner deletion denial and basic stale membership cleanup. Those scenarios
were retained without duplication.

The selected gaps are dashboard membership aggregation and graceful degradation,
capacity-setting effects on subsequent operations, PostgreSQL literal search,
statistics after mutations, and repeatable repairs that preserve valid relationships.

## Added tests (12 cases across 7 methods)

| File / group | Observable behavior and regression detected | Priority rationale |
| --- | --- | --- |
| Services/DashboardTests.cs (4 cases) | Own sandbox's persisted membership count; owner/member distinction; configured and absolute capacity limits; remaining capacity floors at zero; counts update after removal; optional profile/session/role failures do not lose the overview; absent membership avoids client calls | Incorrect counts and optional-service outages affect the main application overview |
| Services/SandboxAdminTests.cs (5 cases) | Reject capacity below member count without writes; full sandbox rejects additions and increased capacity allows them; foreign sandbox preserved; `%`, `_` and backslash searches are literal under PostgreSQL; rename trims; status filtering and statistics reflect persisted transitions/removal | Protects application settings and real provider query behavior |
| Services/RepairTests.cs (1 additional case) | Preview is read-only and predicts cleanup; valid members survive alongside owners and foreign members; repeat repair removes nothing and leaves state identical | Detects overbroad cleanup and non-repeatable repair |
| Integration/SandboxPersistenceTests.cs (2 additional cases) | Ensure restores missing or misclassified owner membership in the original sandbox, retains settings/name and other members, and is repeatable | Protects membership recovery against duplicates and replacement of existing sandbox data |

All database scenarios reuse the existing isolated PostgreSQL fixture and actual
migrations. Framework clients/stores are mocked only at the existing boundary;
no production code, package dependencies or infrastructure abstractions changed.
Dashboard counts intentionally represent membership rows, as the current service
specifies; these tests do not claim that dashboard reads filter deleted accounts.

## Validation in this workspace

- `dotnet test tests/CodeBeam.UltimateAuth.IdentityLab.Tests -c Release --logger trx`
  could not start: `dotnet: command not found`.
- SDK bootstrap attempted through the inherited proxy; download failed because
  `proxy:8080` was unreachable. TLS verification was not disabled.
- The managed local Docker socket health check returned permission denied.
- Tests executed: 0; passed: 0; failed test cases: 0. All 12 added cases remain
  uncompiled/unexecuted; no pass claim is made. Broader suite likewise blocked.
- Static review checked client signatures, DTO property names, existing fixture
  conventions and expected service behavior. `git diff --check` passed.
- Existing README documents an intentionally failing
  `KnownDefect=SystemRoleIdentity` test. It was preserved; this run cannot confirm
  its current outcome. Do not interpret historical execution reports as this run.

Run the command above with .NET 10 and Docker, or a dedicated PostgreSQL instance
via `IDENTITYLAB_TEST_POSTGRES`. Then run the broader solution suite as appropriate.

## Remaining risks

- HIGH: SandboxUserService owner-initiated deletion success/failure and membership
  cleanup with framework persistence; self-deletion tests cover a different path.
- HIGH: LabOwnerRepairService eligibility/capacity and LabRepairService owner repair,
  scan completeness and activity-history-protected empty sandbox deletion.
- HIGH: Existing system-role identity rejection regression documented by the suite;
  outside the permitted testability-only production changes.
- MEDIUM: SandboxUserService missing/deleted summaries; real deletion to repair to
  dashboard workflow; dashboard successful optional data and cancellation.
- MEDIUM: LabAdminService's separate update/query/statistics workflows, activity
  visibility, standalone availability and diagnostics policy composition.
- LOW: Additional query paging/sort combinations and validation message mappings.

## Changed files

Added Services/DashboardTests.cs and Services/SandboxAdminTests.cs. Extended
Services/RepairTests.cs and Integration/SandboxPersistenceTests.cs. Added this
report. No production changes. Work was performed on the supplied `work` checkout
at 45dcfcd (which differs from the locally recorded origin/dev); branch history was
not rewritten or reset.

## Continuation: critical workflows

The previous 12 cases were preserved. Static source review found their client
signatures, DTO properties and expected membership-based counts consistent with
current services. Their intentionally inconsistent capacity fixtures test defensive
dashboard calculations, rather than claiming those settings can be applied through
the admin UI. They still require compilation and execution.

Added Integration/UserWorkflowTests.cs: four cases across two parameterized methods.
The deletion variants compose actual registration, provisioning, SandboxUserService,
summary queries, framework lifecycle/deletion services and both PostgreSQL contexts.
They verify creation visibility, membership-based capacity rejection with no rejected
account (including deleted summaries), foreign deletion denial before client access,
owner hard deletion or member soft deletion, membership cleanup, dashboard count,
replacement provisioning and preservation of the foreign sandbox. The IUAuthClient
boundary is adapted with Moq to invoke the real framework application service;
authenticated contexts use the existing test adapter. These are service integration
tests, not hosted HTTP or cookie authentication tests. Optional dashboard calls use
its existing graceful failure path.

The repair variants invoke LabOwnerRepairService and LabRepairService with actual
lifecycle, summary, role and assignment stores. Admin recovery uses the real
LabAdminGuard with the seeded administrator. They verify missing-owner detection,
persisted restoration alongside valid members, repeat rejection without mutations,
and preservation of another sandbox. Existing Ensure and stale-member repair tests
exercise different repair entry points and were not duplicated.

Validation command attempted:
`dotnet test tests/CodeBeam.UltimateAuth.IdentityLab.Tests -c Release --logger trx`.
Result: shell exit 127 (`dotnet: command not found`), no tests executed. No blocked
SDK downloads were retried. Focused and broader execution and compilation remain
unavailable, not failed test cases. `git diff --check` passed. Static review checked
constructors, dependency registrations in LabRuntime, async mock return types,
UserQuery/summary APIs, lifecycle IsDeleted and client deletion result types against
source. No production changes were made. A build command for a capable environment
is `dotnet build tests/CodeBeam.UltimateAuth.IdentityLab.Tests -c Release`.

Before merging, compile and run the entire Identity Lab project with .NET 10 and
PostgreSQL, including these four cases and the original 12. Triage the existing
KnownDefect=SystemRoleIdentity failure separately; it has not been removed or run.
The most important workflows now have test implementations, but reliable regression
protection cannot be claimed until execution succeeds. The additional suite remains
small and reuses existing fixtures without new production abstractions.

Material remaining gaps: framework deletion failure followed by facade membership
preservation; inactive accounts retaining membership and capacity; repair capacity
rejection/new sandbox recovery and activity-history deletion safeguards. Transport
identity validation remains outside these service integration tests.
