# UltimateAuth Authorization Reference

Default reference implementation for the UltimateAuth Authorization module.

## Purpose

This package provides a ready-to-use authorization system including:

- Role-based access control (RBAC)
- Claims-based authorization
- Policy evaluation

## Usage

This package is automatically wired when used with:

- CodeBeam.UltimateAuth.Server

## Features

- Claims resolution
- Role mapping
- Access policy evaluation

## ⚠️ Important

This is a reference implementation.

You can:

- Replace it partially or completely
- Integrate external authorization systems
- Define custom policies

## Architecture Notes

This package currently depends on the server runtime.

Future versions will move towards a fully decoupled plugin model.

## When to NOT use this package

- When using external identity providers with built-in authorization
- When implementing a fully custom authorization system