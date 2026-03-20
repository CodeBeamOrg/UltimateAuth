# UltimateAuth Credentials InMemory

In-memory persistence implementation for the UltimateAuth Credentials module.

## Purpose

Provides lightweight credential storage for:

- Password-based authentication
- Credential validation
- Credential lifecycle operations

## When to use

- Development
- Testing
- Prototyping

## ⚠️ Not for production

Credentials are stored in memory and will be lost when the application restarts.

## Notes

- No external dependencies
- Zero configuration required

## Use instead (production)

- CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore
- Custom credential persistence