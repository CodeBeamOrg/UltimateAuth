# UltimateAuth Server

The main backend package for UltimateAuth.

## What this package includes

- Authentication core
- Users module
- Credentials module
- Authorization (roles & permissions)
- Policies (authorization logic)

## Notes

This package automatically includes all required core modules.

You do NOT need to install individual packages like:

- Core
- Users
- Credentials
- Authorization
- Policies

unless you are building custom integrations. (But still need reference and persistence packages)