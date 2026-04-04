# UltimateAuth Authorization InMemory

In-memory persistence implementation for the UltimateAuth Authorization module.

## Purpose

Provides lightweight storage for:

- Roles
- Claims
- Authorization rules

## When to use

- Development
- Testing
- Prototyping

## ⚠️ Not for production

Authorization data is stored in memory and will be lost when the application restarts.

## Notes

- No external dependencies
- Zero configuration required

## Use instead (production)

- CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore
- Custom authorization providers