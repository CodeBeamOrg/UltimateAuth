# UltimateAuth AI Guardrails

This document defines mandatory guardrails for AI-assisted tools operating on the UltimateAuth codebase.
These rules are NON-NEGOTIABLE.

---

## 1. Canonical Authority

- `.ultimateauth/architecture.md` is the single canonical source of architectural truth.
- AI tools MUST read and respect `architecture.md` before making any change.
- If a change conflicts with `architecture.md`, the change MUST NOT be made.

---

## 2. Forbidden Refactors

AI tools MUST NOT:

- Refactor or replace AuthFlowContext.
- Refactor or replace AccessContext.
- Change how AuthFlowContext or AccessContext is created.
- Introduce lazy, implicit, or on-demand context creation.
- Merge authentication and authorization logic.
- Move security logic into stores or domain models.
- Introduce client-side authority over identity or access.

---

## 3. Context Integrity Rules

AI tools MUST NOT:

- Mutate AuthFlowContext after creation.
- Create more than one AuthFlowContext per request.
- Create AccessContext independently of AuthFlowContext.
- Bypass context usage in application or domain code.

Context objects define security boundaries.

---

## 4. Orchestration and Authority Rules

AI tools MUST NOT:

- Allow security-relevant operations to bypass orchestrators.
- Call stores directly for security-sensitive operations.
- Embed policy or authorization logic in services or stores.
- Bypass authority evaluation for any operation that affects:
  - authentication state
  - authorization decisions
  - session validity
  - credentials
  - user security state

All such operations MUST pass through an orchestrator and an authority component.

---

## 5. Session and Token Rules

AI tools MUST NOT:

- Treat tokens as the primary source of identity.
- Introduce token-only authentication flows.
- Bypass server-side session validation.
- Weaken revocation or invalidation guarantees.
- Introduce eventual or best-effort revocation semantics.

Sessions are server-authoritative.

---

## 6. Domain Boundary Rules

AI tools MUST NOT:

- Merge UserLifecycle, UserProfile, or UserIdentifier domains.
- Introduce cross-domain state sharing.
- Allow domains to modify each other directly.
- Treat UserKey as a domain entity.

UserKey is an opaque cross-domain identity anchor.

---

## 7. Client and Runtime Rules

AI tools MUST NOT:

- Introduce runtime-specific authentication semantics.
- Allow clients or SDKs to create identity.
- Move authorization decisions to the client.
- Remove or weaken PKCE requirements for public clients.

Client SDKs are adapters, not security authorities.

---

## 8. Extensibility Safety Rules

AI tools MUST NOT:

- Introduce extension points that bypass security invariants.
- Allow overrides to weaken orchestration or authority rules.
- Alter server-authoritative behavior for convenience.

Extensibility MUST preserve all security guarantees.

---

## 9. Final Enforcement Rule

If an AI tool is uncertain whether a change violates these guardrails, the change MUST NOT be made.

Security correctness takes precedence over convenience,
performance, or refactoring elegance.
