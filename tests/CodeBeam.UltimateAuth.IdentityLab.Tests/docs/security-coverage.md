# Inspection and security coverage matrix

## Architecture and trust boundaries

Identity Lab is a .NET 10 interactive Blazor Server application. Framework entities use UAuthDbContext; sandbox, membership, settings and activity entities use LabDbContext, on PostgreSQL. These are separate persistence units. Registration is a multi-step orchestration, not one transaction across both contexts.

`SandboxUAuthAccessPolicy` constrains framework users, credentials, sessions, roles and flows. It queries membership/status and validates single and batch targets. `LabApplicationPolicy` blocks public role mutation even for Ladmin. Policies compose with framework policies: an Allow from one policy is not evidence that the entire operation succeeds.

`LabDemoRoleService` resolves authenticated context internally, requires persisted active ownership, restricts targets to regular members in the same sandbox, and pins Manager/Customer IDs. `LabUserProvisioningService` additionally compares its actor argument to the authenticated actor. Internal reference tokens protect account creation and narrowly scoped rollback deletion. `LabRoleService` is a trusted store helper; it is not registered as a public mutation endpoint.

`LabAdminGuard` rechecks fixed-ID roles and assignments in stores. `/api/lab/admin` applies an authorization filter and its services also use the guard. `LabRootResolver`, unlike the guard, consumes typed role IDs from server-generated context attributes. Context production and transport tamper resistance therefore need separate integration coverage.

`SandboxService`, repair services and some other application methods accept user keys as trusted caller inputs. Direct invocation tests establish their database behavior; they do not establish that every Blazor or HTTP caller supplies the authenticated identity. UI visibility was not counted as authorization.

Admin seeding verifies fixed system role IDs, prevents identifier takeover and assigns Ladmin to the configured fixed identity. Demo seeding pins IDs and reconciles permission sets. The database has unique owner and membership indexes, a filtered unique owner-membership index, foreign keys and cascading membership/settings deletion. Membership capacity changes use serializable transactions.

Existing tests use xUnit 2.9.3, Test SDK 17.14.1 and net10.0 for EF integration; those tests use SQLite. This project follows their xUnit/SDK conventions but uses PostgreSQL for provider-specific constraints, migrations, scope queries and transaction behavior. It adds no duplicate framework CRUD test suite.

## Matrix

The implemented tests have been compiled and exercised against PostgreSQL 17. The execution report records final counts and the deliberately preserved failing regression.

| Priority | Boundary | Test area | Status and limits |
|---|---|---|---|
| P0 | Public Owner/User/Ladmin role mutation | Security/RolePolicyTests | Implemented policy and actual framework role-service denials, stored assignments unchanged, authenticated reads and system positive control; no HTTP dispatch |
| P0 | Demo operations cannot assign system roles | Services/DemoRoleServiceTests | Implemented hostile names, owner/member scope, inactive status, wrong fixed ID, legitimate assignment; focused role stores mocked; full-persistence integration also covers legitimate Manager assignment and denied Ladmin assignment |
| P0 | Forged admin role attributes | Security/AdminGuardTests, RolePolicyTests; Integration/RegistrationIntegrationTests | Implemented persisted-assignment guard and typed-ID root behavior, plus real context-factory replacement of forged typed Ladmin attributes with stored Owner identity; HTTP/cookie tampering untested |
| P0 | Foreign/missing targets and sessions | Authorization/SandboxPolicyTests | Implemented read/update/delete/session/role decisions and positive controls, both owners/members; no real session rows |
| P0 | Batch targets, owner deletion, inactive sandbox | Authorization/SandboxPolicyTests | Implemented policy checks against PostgreSQL |
| P0 | Spoofed actor during provisioning | Provisioning/MemberProvisioningTests | Implemented mismatched actor and regular-member denials before framework calls |
| P0 | Provisioning token forgery | Provisioning/ProvisioningPolicyTests | Implemented bool/string impostors rejected by actual policy |
| P0 | Fault rollback and token reuse | Provisioning/MemberProvisioningTests | Implemented injected assignment failure, exact target, unrelated target, one-shot consumption and post-operation revocation; focused framework deletion mocked; real-account hard deletion after injected role-store failure also covered |
| P0 | Registration relationships | Provisioning/RegistrationTests | Implemented trusted-context handoff, PostgreSQL account/Owner-role/sandbox/membership creation, and absence of Ladmin assignment; both real-persistence and focused mock cases |
| P0 | Relational atomicity/ownership | Integration/SandboxPersistenceTests | Implemented duplicate-owner rollback, concurrent owner uniqueness, no cross-sandbox membership |
| P0 | User capacity contention | Integration/SandboxPersistenceTests | Implemented two independent serializable operations competing for one slot; any scheduling allowed, no sleeps |
| P0 | Deletion membership lifecycle | Services/AccountDeletionTests, Integration/SandboxPersistenceTests | Implemented success/failure ordering, owner restrictions and cross-sandbox preservation; actual framework cleanup untested |
| P0 | Repair scope | Services/RepairTests | Implemented stale-member cleanup restricted to one sandbox, owners preserved; owner/admin repair paths untested |
| P0 | Wrong system role ID | Regression/SystemRoleIdentityTests | Executed and reproduced failing rejection; corrupted store scenario, not demonstrated external exploit |
| P1 | Demo store permissions | Services/DemoStoreTests | Implemented Manager/Customer controls, wildcard/admin filtering, suspended denial; role persistence mocked |
| P1 | Status transitions | Authorization/SandboxPolicyTests | Active/passive/suspended behavior covered; authorized transition APIs untested |
| P1 | Identifier availability/duplicate registration | Integration/RegistrationIntegrationTests | Duplicate registration covered with real framework PostgreSQL composition; standalone availability interface not yet covered |
| P1 | Diagnostics authorization | Authorization/SandboxPolicyTests | Session read policy only; diagnostics UI/endpoint transport untested |
| P1 | Account management | Services/AccountDeletionTests | Partial: deletion ordering only; credentials/status/profile endpoint composition untested |
| P2 | Seed contributor end-to-end repeatability | Integration/RegistrationIntegrationTests | Implemented real PostgreSQL seeding, repeat execution, fixed admin assignment count and exact Manager permissions |
| P2 | Full HTTP/cookie/CSRF/context trust | — | Not implemented |
| P2 | Faults across both real DbContexts and cancellation/restart | Integration/RegistrationIntegrationTests | Real member-account hard-delete rollback after role-store fault covered; registration faults, cancellation and process restart remain untested |

Passing the implemented tests cannot prove that Ladmin is unreachable through every Identity Lab operation. Highest-priority remaining work is authenticated transport tests for caller-supplied identities, registration failure recovery, and full cleanup/cancellation/restart fault coverage. Both DbContexts and production migrations are exercised, but no test runs the complete hosted HTTP application.
