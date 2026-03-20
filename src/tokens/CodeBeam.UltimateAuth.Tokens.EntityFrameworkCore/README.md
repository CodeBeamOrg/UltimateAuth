# UltimateAuth Tokens EntityFrameworkCore

Entity Framework Core persistence for UltimateAuth tokens.

## Purpose

Provides durable storage for token-related data:

- Refresh tokens
- Token rotation state
- Token revocation tracking

## Features

- Persistent token storage
- Refresh token rotation support
- Revocation tracking

## Notes

- Requires EF Core configuration
- Migrations must be handled by the application

## When to use

- Production environments
- Distributed systems
- Scalable architectures

## Alternatives

- CodeBeam.UltimateAuth.Tokens.InMemory (development only)
- Custom packages