# UltimateAuth Authentication InMemory

In-memory authentication persistence for UltimateAuth.

## Purpose

Provides lightweight storage for:

- Authentication state
- Session validation context
- Identity resolution data

## When to use

- Development
- Testing
- Local environments

## ⚠️ Not for production

All authentication state is stored in memory and will be lost when the application restarts.

## Notes

- No external dependencies
- Zero configuration required

## Use instead (production)

- CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore
- Custom authentication persistence