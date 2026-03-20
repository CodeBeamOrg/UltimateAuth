# UltimateAuth Tokens InMemory

In-memory token persistence for UltimateAuth.

## Purpose

This package provides in-memory storage for token-related data:

- Refresh tokens
- Token metadata
- Token lifecycle state

## When to use

- Development
- Testing
- Local environments

## ⚠️ Not for production

All token data is stored in memory and will be lost when the application restarts.

## Notes

- No configuration required
- Lightweight and dependency-free

## Use instead (production)

- CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore
- Custom token persistence