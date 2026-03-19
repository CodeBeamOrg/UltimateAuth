# UltimateAuth Users

User management module for UltimateAuth.

## Purpose

This package provides:

- Dependency injection setup
- User module orchestration
- Integration points for persistence providers

## Does NOT include

- Persistence (use EntityFrameworkCore or InMemory packages)
- Domain implementation (use Reference package if needed)

⚠️ This package is typically installed transitively via:

- CodeBeam.UltimateAuth.Server

In most cases, you do not need to install it directly unless you are building custom integrations or extensions.