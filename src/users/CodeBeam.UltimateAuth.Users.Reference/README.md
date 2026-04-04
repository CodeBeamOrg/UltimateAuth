# UltimateAuth Users Reference

Default reference implementation for the UltimateAuth Users module.

## Purpose

This package provides a ready-to-use implementation of the Users domain, including:

- User management application services
- Default endpoint handlers
- Built-in behaviors and flows

It is intended as a starting point for most applications.

## Usage

Once installed, the Users module is automatically wired into the UltimateAuth server.

No additional setup is required.

## ⚠️ Important

This is a **reference implementation**, not a strict requirement.

You are free to:

- Replace it partially or completely
- Implement your own Users module
- Use only the Users.Contracts package

## Architecture Notes

This package currently depends on the server runtime.

In future versions, it will evolve into a fully decoupled plugin-based architecture.

## When to NOT use this package

- When building a fully custom Users domain
- When integrating with an external identity system