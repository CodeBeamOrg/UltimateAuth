# UltimateAuth Sessions InMemory

In-memory session persistence for UltimateAuth.

## Purpose

Provides lightweight session storage for:

- Active sessions
- Session lifecycle tracking
- Session validation

## When to use

- Development
- Testing
- Local environments

## ⚠️ Not for production

All session data is stored in memory and will be lost when the application restarts.

## Notes

- Zero configuration
- No external dependencies

## Use instead (production)

- CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
- Custom session persistence