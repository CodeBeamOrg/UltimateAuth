# UltimateAuth EntityFrameworkCore Infrastructure

Shared EF Core infrastructure for UltimateAuth.

## Purpose

This package contains common EF Core building blocks used by
UltimateAuth persistence providers:

- Base DbContext implementations
- Entity configuration helpers
- Common mapping conventions
- Shared persistence utilities

## ⚠️ Important

This package is NOT intended to be used directly.

Instead, use one of the following:

- CodeBeam.UltimateAuth.Users.EntityFrameworkCore
- CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore
- CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore
- CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore
- CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
- CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore

These packages include this one automatically.

## When to use directly?

Only if you are building a custom persistence provider.