# UltimateAuth Users InMemory

In-memory persistence implementation for the UltimateAuth Users module.

## Purpose

This package provides a lightweight in-memory storage implementation for:

- User data
- User identifiers
- Basic user lifecycle operations

## When to use

- Development environments
- Testing scenarios
- Prototyping

## ⚠️ Not for production

Data is stored in memory and will be lost when the application restarts.

## Usage

Once installed, this package provides default in-memory implementations.

## Notes

- No external dependencies
- Zero configuration required

## Use instead (production)

- CodeBeam.UltimateAuth.Users.EntityFrameworkCore
- Custom persistence providers