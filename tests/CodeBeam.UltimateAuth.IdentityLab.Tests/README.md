# Identity Lab security suite

Requires .NET 10 SDK and Docker with permission to start containers. Run from the repository root:

```sh
dotnet test tests/CodeBeam.UltimateAuth.IdentityLab.Tests -c Release --logger trx
```

By default, each database test class starts PostgreSQL 17 through Testcontainers. Every test gets a new database, applies the actual LabDbContext migrations, and drops its database on disposal. Tests never use EF InMemory. Nothing connects to application production databases.

Alternatively set `IDENTITYLAB_TEST_POSTGRES` to a connection string for a **dedicated test PostgreSQL instance**. Its login needs CREATE DATABASE and permission to drop the databases it creates. Only databases named `identitylab_test_<generated GUID>` are created/dropped. This setting bypasses Docker; it does not bypass relational tests. Missing infrastructure fails fixture initialization rather than silently skipping tests.

Tests replace authenticated-context sources and selected framework store/client contracts with Moq. The actual Lab services, policies, relational model, migrations and transaction boundaries execute normally. The focused mock tests do not exercise framework persistence. Integration/RegistrationIntegrationTests additionally composes the real framework services, both PostgreSQL DbContexts, migrations, seed contributors, registration, member provisioning, demo assignment and rollback deletion. Contexts.cs uses a small reflection adapter because AccessContext construction is internal; production visibility and authorization remain unchanged.

`Regression/SystemRoleIdentityTests.cs` records an expected rejection that the inspected implementation does not enforce. It is intentionally neither skipped nor inverted to accept unsafe behavior. The `KnownDefect=SystemRoleIdentity` trait identifies it; normal local and CI runs include it. Expect that regression to fail until separately approved production work addresses the issue. Do not enable this suite as a required green check before defect triage.

See [coverage matrix](docs/security-coverage.md) and [execution report](docs/execution-report.md). The project is included in the solution. Repository CI excludes it from the general matrix test step and runs it in a dedicated .NET 10 PostgreSQL job; the dedicated job uploads TRX results even on failure.
