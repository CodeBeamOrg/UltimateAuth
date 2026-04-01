# 🔐 Credentials Domain

Credentials in UltimateAuth define how a user proves their identity.

## 🧠 Core Concept

Authentication is not tied to users directly.

👉 It is performed through credentials.

<br>

## 🔑 What is a Credential?

A credential represents a secret or factor used for authentication.

Examples:

- Password
- OTP (future)
- External providers (future)

<br>

## 🔒 Password Credential

The default credential type is password.

A password credential contains:

- Hashed secret (never raw password)
- Security state (active, revoked, expired)
- Metadata (last used, source, etc.)

👉 Credentials are always stored securely and validated through hashing.

<br>

## ⚙️ Credential Validation

Credential validation is handled by a validator:

- Verifies secret using hashing
- Checks credential usability (revoked, expired, etc.)

👉 Validation is isolated from business logic.

<br>

## 🔗 Integration with Users

Credentials are NOT created directly inside user logic.

Instead:

👉 They are integrated via lifecycle hooks

Example:

- When a user is created → password credential may be created
- When a user is deleted → credentials are removed

👉 This keeps domains decoupled.

<br>

## 🔄 Credential Lifecycle

Credentials support:

- Creation
- Secret change
- Revocation
- Expiration
- Deletion

<br>

## 🔁 Security Behavior

Credential changes trigger security actions:

- Changing password revokes sessions
- Reset flows require verification tokens
- Invalid attempts are tracked

👉 Credentials are tightly coupled with security.

<br>

## 🔑 Reset Flow

Password reset is a multi-step process:

1. Begin reset (generate token or code)
2. Validate token
3. Apply new secret

👉 Reset flow is protected against enumeration and abuse.

<br>

## 🧠 Mental Model

Users define identity.

Credentials define authentication.

## 🎯 Summary

- Credentials handle authentication secrets
- Password is default but extensible
- Integrated via lifecycle hooks
- Strong security guarantees
- Fully extensible for new credential types
