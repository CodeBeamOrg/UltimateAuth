---
title: Users
order: 2
group: plugin-domains
---


# 👤 Users Domain

Users in UltimateAuth are not a single entity.

Instead, they are composed of multiple parts that together define identity.

## 🧠 Core Concept

A user is represented by three main components:

- Lifecycle (security & state)
- Identifiers (login surface)
- Profile (user data)

<br>

## 🔐 Lifecycle (Security Anchor)

Lifecycle defines:

- When a user created
- Whether a user is active, suspended, or deleted
- Security version (used for invalidating sessions/tokens)

👉 This is the core of authentication security.

<br>

## 🔑 Identifiers (Login System)

Identifiers represent how a user logs in:

- Email
- Username
- Phone

Each identifier has:

- Normalized value
- Primary flag
- Verification state

### ⭐ Login Identifiers

Not all identifiers are used for login.

👉 Only **primary identifiers** are considered login identifiers.

#### ⚙️ Configurable Behavior

Developers can control:

- Which identifier types are allowed for login
- Whether email/phone must be verified
- Whether multiple identifiers are allowed
- Global uniqueness rules

#### 🔌 Custom Login Logic

UltimateAuth allows custom login identifier resolution.

You can:

- Add custom identifier types
- Override resolution logic
- Implement your own resolver

👉 This means login is fully extensible.

<br>

## 🧾 Profile (User Data)

Profile contains non-auth data:

- Name
- Bio
- Localization
- Metadata

👉 This is not used for authentication.

<br>

## ⚙️ Application Service

User operations are handled by an orchestration layer.

It is responsible for:

- Creating users
- Managing identifiers
- Applying validation rules
- Triggering integrations
- Revoking sessions when needed

<br>

## 🔁 Mental Model

Authentication answers:

→ Who are you?

Users domain answers:

→ What is this user?

## 🎯 Summary

- Users are composed, not singular
- Login is based on identifiers
- Login identifiers are configurable
- System is extensible by design
