# UltimateAuth Credentials Reference

Default reference implementation for the UltimateAuth Credentials module.

## Purpose

This package provides a ready-to-use implementation of credential management:

- Password-based authentication
- Secret rotation
- Credential lifecycle operations

## Usage

This package is automatically integrated when used with:

- CodeBeam.UltimateAuth.Server

No additional configuration is required.

## ⚠️ Important

This is a reference implementation.

You are free to:

- Replace it partially or completely
- Implement your own credential system
- Integrate external identity providers

## Architecture Notes

This package currently depends on the server runtime.

Future versions will move towards a fully decoupled plugin architecture.

## When to NOT use this package

- When integrating external auth providers (Auth0, Azure AD, etc.)
- When building a custom credential system