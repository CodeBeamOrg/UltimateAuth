# UltimateAuth AI Guardrails

This document defines mandatory guardrails for AI-assisted tools operating on the UltimateAuth codebase.
These rules are NON-NEGOTIABLE.

---

## 1. Canonical Authority

- `.ultimateauth/architecture.md` is the single canonical source of architectural truth.
- AI tools MUST read and respect `architecture.md` before making any change.
- If a change conflicts with `architecture.md`, the change MUST NOT be made.

---

## 2. Protected Architectural Boundaries

AI tools MUST NOT independently introduce changes that alter established architectural or security boundaries.

The following components and mechanisms are considered security-critical:

- AuthFlowContext and its creation lifecycle.
- AccessContext and its derivation rules.
- Authentication and authorization separation.
- Security-sensitive orchestration and authority evaluation.
- Session and token validation mechanisms.
- Cross-domain identity and security boundaries.

AI tools MUST NOT:

- Replace or redesign these mechanisms without explicit maintainer approval.
- Introduce lazy, implicit, or uncontrolled security context creation.
- Merge authentication and authorization responsibilities.
- Move authorization decisions into stores or domain models.
- Introduce client-side authority over identity or access.
- Remove or bypass established security validation mechanisms.

Internal refactoring MAY be performed when explicitly authorized, provided all applicable architectural invariants remain preserved.

A change that modifies a security boundary MUST be treated as an architectural change, not as a routine refactor.

---

## 3. Context Integrity Rules

AI tools MUST NOT:

- Arbitrarily mutate an established AuthFlowContext.
- Replace or reconstruct security contexts outside designated framework mechanisms.
- Create AccessContext independently of trusted authentication state.
- Allow application or domain code to override established authentication or authorization context.
- Introduce hidden, implicit, or uncontrolled security context creation.

Within an HTTP authentication pipeline, the effective authentication context MUST remain stable throughout its execution scope.

Non-HTTP execution environments MAY use explicitly defined context creation mechanisms, provided equivalent security guarantees are preserved.

Any proposed change to context ownership, creation, or derivation MUST receive explicit maintainer approval.

---

## 4. Orchestration and Authority Rules

AI tools MUST NOT:

- Bypass required orchestrators in security-sensitive workflows.
- Bypass applicable authority decisions.
- Embed authorization policies or permission decisions in stores.
- Introduce direct persistence operations that circumvent required security checks.
- Move cross-domain security coordination into unrelated components.

AI tools MUST preserve the designated execution path for each security-sensitive operation.

- Operations requiring authorization decisions MUST use the applicable authority.
- Complex security-sensitive workflows MUST use their designated orchestrators.
- Cryptographic verification, protocol validation, and deterministic security checks MAY be performed by dedicated validators or security primitives.
- Stores MUST remain persistence-focused and MUST NOT independently make authorization decisions.

AI tools MUST NOT introduce unnecessary authority or orchestration dependencies for operations that do not require them.

---

## 5. Session and Token Rules

AI tools MUST preserve the authentication, validation, and revocation semantics defined for each authentication mode.

AI tools MUST NOT:

- Treat unvalidated client-provided credentials as trusted identity.
- Introduce token validation paths that bypass the requirements of the effective authentication mode.
- Remove required session validation from stateful authentication modes.
- Introduce mandatory request-time session lookups into stateless modes without explicit architectural approval.
- Weaken session or token revocation guarantees without explicit maintainer approval.
- Claim immediate revocation when the implementation cannot enforce it.
- Change token issuance or validation semantics as an incidental refactor.

The following mode-specific boundaries MUST be preserved:

- **PureOpaque:** Server-side session authentication.
- **Hybrid:** Stateful authentication with opaque access tokens, refresh tokens, and server-side session validation.
- **SemiHybrid:** Stateless JWT access-token validation with server-side session lifecycle management.
- **PureJwt:** Stateless JWT authentication without mandatory session persistence.

AI tools MUST NOT change the authentication mode contract without explicit maintainer approval.

Revocation behavior MUST remain consistent with the documented guarantees of the effective authentication mode.

---

## 6. Domain Boundary Rules

AI tools MUST NOT:

- Merge UserLifecycle, UserProfile, or UserIdentifier domains.
- Introduce unauthorized cross-domain mutable state sharing.
- Allow domains to modify each other's state directly.
- Treat UserKey as a domain entity or persistence entity.
- Introduce mandatory UltimateAuth inheritance requirements for host user entities.
- Require host application user models to implement UltimateAuth-specific interfaces.
- Couple framework-owned runtime identity records directly to host persistence entities.

UserKey MUST remain an opaque cross-domain identity anchor.

Host user models MUST remain independent of UltimateAuth framework types.

Integration with host user entities MUST occur through explicit adapters, providers, or controlled boundary contracts.

Cross-domain security operations MUST be coordinated through designated services and orchestrators.

---

## 7. Client and Runtime Rules

AI tools MUST NOT:

- Introduce runtime-specific security authority.
- Allow client SDKs to establish trusted identity without server-side validation.
- Move authorization decisions to untrusted clients.
- Weaken PKCE requirements for authorization-code flows involving public clients.
- Introduce inconsistent security guarantees for the same authentication mode across different runtimes.
- Duplicate core authentication logic solely to accommodate a client platform.

Client runtimes MAY use different transport mechanisms, credential delivery strategies, and authentication flow adaptations.

Runtime-specific adaptations MUST preserve the security guarantees of the effective authentication mode.

Client SDKs are adapters, not security authorities.

---

## 8. Extensibility Safety Rules

AI tools MUST NOT:

- Introduce extension points that bypass mandatory security controls.
- Allow overrides to circumvent applicable authority decisions.
- Allow plugins to weaken authentication mode guarantees.
- Introduce hidden security behavior through extension mechanisms.
- Alter server-authoritative security boundaries for convenience or performance.
- Couple host application domain models directly to framework-owned persistence or runtime models.

Extensions MUST preserve the security invariants applicable to the effective authentication mode.

New extension points affecting authentication, authorization, credential handling, or session lifecycle MUST receive explicit maintainer approval before implementation.

---

## 9. Change Authorization and Review

AI tools MUST classify proposed changes before implementation.

### 9.1 Routine Changes

Routine changes MAY proceed within the explicitly authorized task scope when they preserve all applicable architectural and security invariants.

Examples include:

- Documentation corrections.
- Non-behavioral code cleanup.
- Focused bug fixes that preserve established contracts.
- Additional tests for existing behavior.

### 9.2 Security-Critical Changes

The following changes REQUIRE explicit maintainer approval:

- Authentication mode behavior changes.
- Token issuance, validation, or revocation changes.
- Security context creation or lifecycle changes.
- Authorization or authority evaluation changes.
- Session lifecycle or persistence semantics changes.
- Credential handling or verification changes.
- New security-sensitive extension points.
- Changes to public security contracts.

Approval for one change MUST NOT be interpreted as approval for unrelated changes.

### 9.3 Architectural Conflicts

If a requested change conflicts with `.ultimateauth/architecture.md`, AI tools MUST:

1. Identify the conflicting architectural rule.
2. Explain why the proposed change conflicts with that rule.
3. Present a compliant alternative when possible.
4. Request explicit maintainer review if an architectural amendment is necessary.
5. Refrain from implementing the conflicting change until the canonical architecture has been explicitly updated.

AI tools MUST NOT silently modify architectural rules to justify an implementation.

---

## 10. Verification Requirements

AI tools MUST verify that their changes preserve applicable architectural and security guarantees.

For security-relevant changes, AI tools MUST:

- Identify affected authentication modes and client profiles.
- Review relevant issuance, validation, authorization, and revocation paths.
- Preserve existing negative security tests.
- Add or update focused regression tests when behavior changes.
- Check for unintended changes to public contracts.
- Report any validation or testing that could not be completed.

AI tools MUST NOT:

- Remove or weaken security tests merely to make a change pass.
- Modify unrelated security behavior without explicit authorization.
- Claim that a change is secure solely because the project builds successfully.
- Claim tests passed when they were not executed.
- Conceal unresolved security concerns or architectural conflicts.

If the required verification cannot be completed, the AI tool MUST clearly report the limitation.

---

## 11. Final Enforcement Rule

If an AI tool cannot determine whether a proposed change preserves the applicable architectural and security invariants, it MUST NOT proceed with the uncertain change without explicit maintainer review.

AI tools MUST distinguish between:

- A confirmed architectural violation.
- A potential security or architectural concern.
- An implementation detail that does not alter architectural guarantees.

AI tools MUST NOT treat uncertainty as evidence of a confirmed vulnerability.

Explicit maintainer approval is required for architectural changes, but approval alone does not override the canonical architecture.

When an architectural change is necessary, `.ultimateauth/architecture.md` MUST be intentionally reviewed and updated before implementing the conflicting behavior.

Security correctness, architectural consistency, and verifiable behavior take precedence over convenience, performance, or refactoring elegance.

---

## 12. Scope Discipline

AI tools MUST remain within the scope of the explicitly authorized task.

AI tools MUST NOT:

- Expand a focused bug fix into an unrelated architectural refactor.
- Implement roadmap features without explicit authorization.
- Change authentication mode semantics while fixing unrelated issues.
- Introduce new abstractions solely for speculative future requirements.
- Remove public contracts without reviewing their intended purpose and compatibility impact.
- Treat TODO comments as authorization to implement unfinished features.
- Modify unrelated files merely for stylistic consistency.

When an unrelated issue is discovered, AI tools SHOULD report it separately rather than modifying it without authorization.

Deferred work MUST remain deferred unless explicitly reopened by the maintainer.