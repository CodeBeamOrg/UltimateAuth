# UltimateAuth Authorization

Authorization module for UltimateAuth.

## Purpose

This package provides:

- Dependency injection setup
- Role and permission orchestration
- Integration points for authorization providers

## Does NOT include

- Persistence (use EntityFrameworkCore or InMemory packages)
- Domain implementation (use Reference package if needed)
- Policy enforcement integrations

⚠️ This package is typically installed transitively via:

- CodeBeam.UltimateAuth.Server

In most cases, you do not need to install it directly unless you are building custom integrations.