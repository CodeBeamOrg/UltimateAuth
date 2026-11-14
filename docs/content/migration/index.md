---
title: Migration Guides
order: 1
group: migration
---

# Migration Guides

Keep your UltimateAuth applications up to date with clear, version-specific upgrade guidance.

This page provides an overview of stable UltimateAuth releases, their migration impact, and any actions required when upgrading.

**Most updates require little or no application code changes.** When an update requires attention, a dedicated migration guide explains what changed and how to adapt your application.

## Version Migration Overview

The table below lists stable releases in descending order, with the newest version first.

Preview and release candidate versions are excluded.

| Version | Migration Impact | What's Changed | Migration Guide |
|---|---|---|---|
| **v0.2.0 (In Progress)** | **Medium (Provisional)** | Authorization changes, native login forms, authentication security defaults, concurrency controls, and persistence updates | [View Guide](/docs/migration/v0.2.0) |
| **v0.1.4** | **Low** | CSRF protection, antiforgery integration, and interface changes | [View Guide](/docs/migration/v0.1.4) |
| **v0.1.3** | **None** | Session deletion, revocation, and refresh consistency fixes | Not required |
| **v0.1.2** | **None** | Password change transaction and session revocation fixes | Not required |
| **v0.1.1** | **Low** | Blazor route authorization and component lifecycle changes | [View Guide](/docs/migration/v0.1.1) |
| **v0.1.0** | **N/A** | Initial stable release | Not applicable |

**Migration impact describes the expected developer effort and affected integration surface, not the size of the code change or the significance of the release.** A small change to a widely used authentication API may have a higher migration impact than a large internal refactor.

> Individual preview and release candidate versions are not listed separately. Migration guidance for an in-progress release may change before its stable release.

Security and reliability fixes may have an impact of **None** while still being strongly recommended.

## Understanding Migration Impact

| Impact | Meaning | Expected Action |
|---|---|---|
| **None** | No application code or configuration changes are required for standard integrations. | Update packages and run existing tests. |
| **Low** | Small, localized changes may be required, typically for specific integrations or customizations. | Review the migration guide and update affected code. |
| **Medium** | Changes affect commonly used APIs, core integration points, or multiple application areas. Even a small code change is classified as Medium when developers must update functionality they use directly and extensively. | Review the migration guide, update affected integrations, and perform targeted regression testing. |
| **High** | Significant architectural, API, persistence, or behavioral changes require a planned migration. | Review all migration steps, test thoroughly, and plan deployment carefully. |

Migration impact is evaluated using both **the effort required and how directly the changes affect application developers**.

A change does not qualify as Low simply because it requires only a few lines of code. Breaking changes to frequently used authentication, authorization, session management, or client integration APIs may be classified as Medium even when the implementation changes are relatively small.

Applications using only unaffected functionality may require fewer changes than the stated impact level suggests.

## How to Upgrade

1. **Identify your current version.** Check the installed `CodeBeam.UltimateAuth.*` NuGet packages.
2. **Review every version between your current and target versions.** Migration requirements are cumulative when skipping releases.
3. **Follow the linked migration guides.** Apply any required API, configuration, or behavioral changes in version order.
4. **Update your UltimateAuth packages together.** Keep related packages on compatible versions.
5. **Build and test your application.** Verify login, logout, refresh, authorization, and session management flows relevant to your integration.

For releases marked **None**, no dedicated migration guide is necessary. You should still review the release notes and run your application tests.

## Available Migration Guides

### [Migrating to v0.1.4](/docs/migration/v0.1.4)

**Impact: Low**

Introduces antiforgery protection for cookie-based authentication flows, updates browser client integration, and changes selected session resolution and browser bridge interfaces.

Applications using the standard UltimateAuth browser client generally receive antiforgery handling automatically.

Custom HTTP clients, manually implemented browser bridges, and custom session resolvers should review the guide.

### [Migrating to v0.1.1](/docs/migration/v0.1.1)

**Impact: Low**

Introduces routing-level enforcement for `[UAuthAuthorize]` and updates the `UAuthComponentBase` lifecycle.

Most applications using the built-in `UAuthApp` router require no changes. Applications with custom routers, component lifecycle overrides, or specialized session requirements should review the guide.

## Release History

For complete release notes, bug fixes, and security updates, visit the [UltimateAuth GitHub Releases](https://github.com/CodeBeamOrg/UltimateAuth/releases) page.

Migration guides focus on changes developers must make to their applications. Release notes provide the complete history of changes in each version.

---

**Upgrading across multiple versions?** Start with the earliest version after your currently installed release and review each migration entry through your target version.