# UltimateAuth InMemory Infrastructure

Shared in-memory infrastructure for UltimateAuth.

## Purpose

This package provides reusable in-memory building blocks for UltimateAuth modules:

- Thread-safe in-memory stores
- Base store implementations
- Development and testing utilities

## ⚠️ Not for production

Data is stored in memory and will be lost when the application restarts.

## Usage

This package is NOT intended to be installed directly.

Instead, use module-specific packages such as:

- CodeBeam.UltimateAuth.Users.InMemory
- CodeBeam.UltimateAuth.Credentials.InMemory
- CodeBeam.UltimateAuth.Authorization.InMemory
- CodeBeam.UltimateAuth.Authentication.InMemory
- CodeBeam.UltimateAuth.Sessions.InMemory
- CodeBeam.UltimateAuth.Tokens.InMemory

These packages include this one automatically.

## Advanced usage

You may use this package directly when implementing custom in-memory providers.