# UltimateAuth Credentials

Credential management module for UltimateAuth.

## Purpose

This package provides:

- Dependency injection setup
- Credential module orchestration
- Integration points for credential providers

## Does NOT include

- Persistence (use EntityFrameworkCore or InMemory packages)
- Domain implementation (use Reference package if needed)

⚠️ This package is typically installed transitively via:

- CodeBeam.UltimateAuth.Server

In most cases, you do not need to install it directly unless you are building custom integrations.