# Identity Lab route smoke coverage

## Baseline

The initial ten render cases are preserved. The checkout was fast-forwarded from
`45dcfcd` to the latest available local `origin/dev`, `b886b94` (critical service
workflow regressions, #132). `HEAD...origin/dev` reports `0 0`. The merged service
regression tests are unchanged by this implementation.

`git fetch origin dev` still fails because the configured proxy at port 8080 is
unreachable. The live remote tip could not be verified; this is based on the
available local dev ref, not a claim of live synchronization.

## Inventory

There are **24 routable page components with 24 route attributes** in the supplied
application source. All 24 have an actual render test mapping; there are no
routable exclusions. Counts are statically checked, not executed test results.

| Component | Route | Render scenario |
| --- | --- | --- |
| LandingPage | `/` | Existing public registration entry |
| LoginPage | `/login` | Existing anonymous form and authenticated entry |
| RegisterPage | `/register` | Existing anonymous form |
| DashboardPage | `/dashboard` | Existing Owner and User navigation |
| MySessionsPage | `/dashboard/sessions` | Existing empty, loading and failure states |
| AuthorizationPage | `/dashboard/authorization` | Existing empty and failure states |
| UsersPage | `/dashboard/users` | Owner, active sandbox, one member summary |
| MyIdentityPage | `/dashboard/identity` | User, populated profile, empty identifiers, credential section |
| MyAccessPage | `/dashboard/access` | User, sandbox membership, empty roles and permissions |
| SettingsPage | `/dashboard/settings` | Owner, sandbox settings and resolved owner name |
| ActivityPage | `/dashboard/activity` | User, active sandbox, no activity |
| AdminPage | `/admin` | Ladmin with matching server role assignment, empty platform, real repair panel |
| SessionDiagnostics | `/dashboard/session-diagnostics` | User, current chain and active session details |
| DemoStorePage | `/dashboard/demo-store` | User, active sandbox, no demo role grants |
| AuthorizationLab | `/dashboard/authorization-lab` | User, real navigation index |
| Error | `/Error` | No HTTP request context |
| PublicPage | `/dashboard/auth/public` | Anonymous sample |
| AuthenticatedPage | `/dashboard/auth/authenticated` | User sample |
| AdminRolePage | `/dashboard/auth/admin` | Admin sample role |
| AdminPermissionPage | `/dashboard/auth/permission-admin` | Sample Admin plus query permission |
| AspNetAuthorizePage | `/dashboard/auth/aspnet` | User, ASP.NET authentication cascade |
| CombinedAuthorizationPage | `/dashboard/auth/combined` | Sample Admin plus query permission |
| StateViewPage | `/dashboard/auth/state-view` | Real shared state-view examples |
| MatchModesPage | `/dashboard/auth/match-modes` | Real shared match-mode examples |

Credentials have no separate route; their section belongs to MyIdentityPage.
AdminRepairPanel has no route and is exercised as an actual AdminPage child.
Its scan/repair actions are outside smoke scope. Other non-routable children,
layouts and dialogs are not independently inventoried.

## Tests and route enforcement

The original eight test methods / ten cases retain their bodies and InlineData;
only six SmokeTestFor annotations were added to connect their pages to inventory.
Added 18 one-scenario page tests and one route coverage guard: **28 page-render
cases plus one inventory test**, 29 total cases under `Category=Render`.

SmokeTestFor attributes live on actual enabled xUnit render methods. The guard
reflects only the Identity Lab application assembly for RouteAttribute-bearing
IComponent types. It checks every component, reports all route aliases for missing
components, rejects mappings to non-routable/external types, and verifies mappings
point to enabled Fact/Theory methods. Non-routable children and test/library
assemblies are not route candidates. A page with multiple aliases needs one real
component render test. There are no exception lists or arbitrary dynamic renders.

The small generic assertion helper renders a statically selected real component.
It checks stable page text and rejects error alerts. StateView/MatchModes samples
are the narrow exception to the error-alert check because denied examples are
intentional content. Application pages additionally assert loaded content where
needed: member display name, profile/credentials, owner name, empty access/activity,
admin table/repair panel, active session and evaluated demo access.

## Setup

Reuse the original bUnit/MudBlazor/MudExtensions fixture, real UltimateAuth
authorization evaluator and uniquely named EF InMemory LabDbContext store.
Additional registrations are grouped for management, admin, demo and diagnostics;
external client/store responses are explicit. Real page and Lab service lifecycle
methods remain active. No application host or child stubs are installed.

The renderer now supplies ASP.NET AuthenticationState as well as UAuthState.
The existing internal snapshot adapter accepts optional permission claims.
Regular application Users do not receive admin permission claims. The sample
routes use their declared sample Admin role/permission as appropriate.
Diagnostics renders a real current-chain/detail response and uses async fixture
disposal for its timer/subscription lifecycle. No new packages or database
dependencies were added in this completion phase.

## Validation

Attempted from repository root:

```sh
git fetch origin dev
dotnet build tests/CodeBeam.UltimateAuth.IdentityLab.Tests/CodeBeam.UltimateAuth.IdentityLab.Tests.csproj -c Release
dotnet test tests/CodeBeam.UltimateAuth.IdentityLab.Tests/CodeBeam.UltimateAuth.IdentityLab.Tests.csproj -c Release --filter Category=Render
```

Fetch failed: configured proxy unreachable. Both .NET commands failed before
execution: `dotnet: command not found`. **No build, render test, route guard or
existing service regression test has executed here.** No measured coverage is
reported.

Completed static checks:

- Compared and fast-forwarded to available local dev; no divergent commits.
- Matched all 24 source page types/routes to the annotated actual render methods.
- Reviewed component namespaces, cascades, injected service constructors, client
  and store method signatures, result constructors and bUnit API usage against
  existing repository conventions. This cannot substitute for compilation.
- Parsed project XML and checked added-source whitespace.
- `git diff --check` passed.
- Full patch, including new files, passes `git apply --cached --check` against
  an isolated index initialized from `b886b94`.

On a .NET 10 CI runner, the filtered command above runs the existing behavioral
render cases, new page smoke cases and route guard without Docker, PostgreSQL or a
browser. Run the full Identity Lab project separately with its documented
PostgreSQL/Docker requirements. Existing known-defect tests remain unchanged.

## Limits

Compilation and successful rendering remain unverified until .NET execution.
These tests bypass routing, so they do not validate authorization enforcement by
the router. Layout composition, dialogs, mutations, repair scans, populated large
data sets and detailed interaction states remain outside this smoke phase. EF
InMemory does not validate relational constraints, SQL translation, migrations or
transactions. No percentage coverage target or estimate is used.

## Files

Completion phase:

- Rendering/PageRenderContext.cs: identifier client, ASP.NET cascade, optional claims.
- Rendering/PageRenderContext.ApplicationPages.cs: focused dependency/response groups.
- Rendering/PageRenderTests.cs: mappings only; original cases preserved.
- Rendering/RemainingPageRenderTests.cs: 18 additional real page smoke tests.
- Rendering/RouteSmokeCoverageTests.cs: metadata mapping and route guard.
- README.md and this report: inventory, commands and validation limits.

The prior phase's project dependencies remain bunit 2.11.3 and EF InMemory 10.0.12.
No production code, migration, CI configuration or service test was edited.
