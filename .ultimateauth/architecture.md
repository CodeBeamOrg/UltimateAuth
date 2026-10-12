# UltimateAuth Architecture (Canonical)

This document defines the NON-NEGOTIABLE architectural principles of UltimateAuth.

If any code, documentation, or contribution conflicts with this document, the code is considered incorrect.

This document is intentionally concise. Explanations, examples, and extended discussions belong in separate architecture guides.

## 1. Architectural Scope

This document defines the canonical architectural boundaries and non-negotiable rules of the UltimateAuth framework.
The scope of this document is strictly limited to the authentication and authorization architecture of UltimateAuth.

### 1.1 What This Document Defines

This document defines:

- The core authentication model and its invariants.
- The request pipeline and context model used to derive authentication and authorization state.
- The separation of responsibilities between core layers (Store, Service, Authority, Issuer).
- The boundaries and responsibilities of plugin domains (Users, Credentials, Authorization).
- The session architecture and its role as the primary source of authentication truth.
- The client and runtime interaction model as it relates to authentication flows.
- Security invariants that MUST hold across all implementations.

These rules apply to all UltimateAuth components, including Core, Server, Client, plugin domains, and reference implementations.

---

### 1.2 What This Document Does NOT Define

This document explicitly does NOT define:

- User interface or user experience flows.
- Application-specific business rules.
- Hosting, deployment, or infrastructure concerns.
- Persistence technologies or storage engines.
- Network protocols or transport-level optimizations.
- Product roadmap, feature prioritization, or timelines.
- Documentation structure or tutorial content.

Any concern outside authentication and authorization architecture is considered out of scope.

---

### 1.3 Architectural Authority

This document is the single authoritative source for architectural decisions within UltimateAuth.

If any implementation, documentation, contribution, or automated refactoring conflicts with this document, the implementation is considered incorrect.

Convenience, performance optimizations, or stylistic preferences MUST NOT override the rules defined here.

---

### 1.4 Relationship to Other Documents

- The UltimateAuth Manifesto describes the philosophy, intent, and design values of the framework.
- This document translates those values into concrete, enforceable architectural rules.
- Architecture guides and design documents MAY provide explanations or examples, but MUST NOT redefine or contradict this document.

In case of conflict, this document takes precedence.

---

### 1.5 Intended Audience

This document is intended for:

- Core framework maintainers.
- Contributors modifying authentication-related code.
- Reviewers evaluating architectural correctness.
- Automated tools (including AI-assisted refactoring) operating on the UltimateAuth codebase.

This document is NOT intended as an onboarding guide or end-user documentation.

## 2. Core Authentication Model

UltimateAuth defines authentication as a server-authoritative process that establishes and validates the identity of the current actor.

Authentication determines *who* the current actor is and establishes a trustworthy identity context for the duration of an authenticated operation.

Authentication state MAY be represented through server-side sessions, cryptographically verifiable tokens, or a combination of both, depending on the effective authentication mode.

Authorization decisions are explicitly separate from authentication and are handled through dedicated authorization mechanisms.

---

### 2.1 Server Authority and Authentication State

UltimateAuth treats the server as the authoritative source of authentication rules and security decisions.

- Authentication authority MUST remain under server control.
- Client-provided identity information MUST NOT be trusted without validation.
- Authentication credentials MUST be validated according to the effective authentication mode.
- Session-backed modes MUST preserve the authority of server-side session state.
- Stateless modes MAY establish authenticated identity through cryptographically verifiable tokens without request-time session lookup.

Tokens, cookies, and other credentials represent evidence of authentication. Their presence alone MUST NOT establish authenticated identity without the validation required by the effective authentication mode.

No authentication mode may delegate security authority to an untrusted client.

---

### 2.2 Authentication Modes

UltimateAuth defines multiple authentication modes to support different security, lifecycle, and runtime requirements.

The following authentication modes are defined:

- **PureOpaque**  
  Fully stateful, session-based authentication. The server-side session is the primary authentication credential and source of authentication state. No separate access or refresh token is required.

- **Hybrid**  
  Stateful authentication combining server-side sessions, opaque access tokens, and refresh tokens. Authentication remains session-authoritative, and authenticated requests MUST enforce session validity.

- **SemiHybrid**  
  Authentication combining stateless JWT access-token validation with server-side session lifecycle management. Sessions support operations such as refresh, logout, revocation, and device management. Normal JWT validation does not require a request-time session lookup.

- **PureJwt**  
  Stateless JWT-based authentication without mandatory server-side session persistence or request-time session lookup. Token validity is established through cryptographic verification and applicable token validation rules.

Authentication modes define different validation and lifecycle strategies, not merely different credential transport formats.

Each mode MUST preserve the framework's common authentication and authorization boundaries while enforcing its own explicitly defined security guarantees.

Defining an authentication mode does not imply that its implementation is complete or available in every release. Supported modes MUST be documented separately.

---

### 2.3 Client Profiles and Runtime Awareness

UltimateAuth operates across multiple client runtimes (Blazor Server, Blazor WebAssembly, MAUI, MVC, APIs).

Authentication behavior is adapted at runtime using Client Profiles.

- Client Profiles define runtime-specific defaults.
- Client Profiles are automatically detected when possible.
- Runtime-specific behavior MUST NOT require separate authentication models.

All clients participate in the same core authentication model regardless of runtime or platform.

---

### 2.4 Request-Scoped Authentication Evaluation

Authentication state is evaluated per request.

- Each request derives its authentication context independently.
- Authentication evaluation is deterministic and repeatable.
- No implicit or hidden authentication state is allowed.

Request-based evaluation ensures consistent behavior across distributed systems, retries, and concurrent requests.

---

### 2.5 Separation of Authentication and Authorization

Authentication and authorization are strictly separated.

- Authentication establishes identity.
- Authorization evaluates permissions and access decisions.

Authentication MUST NOT embed authorization logic, permission checks, or policy decisions.

Authorization systems MUST rely on authenticated identity provided by the authentication model.

---

### 2.6 Extensibility Without Semantic Drift

The core authentication model is extensible through plugin domains and explicit extension points.

Extensibility MUST preserve the security guarantees applicable to the effective authentication mode.

Custom implementations, alternative storage mechanisms, and runtime-specific optimizations MUST NOT change:

- The server-authoritative nature of authentication.
- The separation between authentication and authorization.
- The integrity of authenticated identity and security contexts.
- The validation and revocation guarantees defined for the selected authentication mode.
- The requirement that security-sensitive decisions remain under trusted server-side control.

Extensions MUST NOT silently weaken or redefine the security semantics of an authentication mode.

## 3. Request Pipeline & Context Model

UltimateAuth evaluates authentication and authorization state within a well-defined, deterministic request pipeline.
This pipeline is responsible for producing immutable context objects that represent the authentication and authorization boundaries of a request.

---

### 3.1 AuthFlowContext

AuthFlowContext represents the effective authentication flow context established for an authentication operation.

- Within an HTTP authentication pipeline, AuthFlowContext MUST be established through the designated context creation mechanism.
- Once established, the effective AuthFlowContext MUST remain stable throughout the operation.
- AuthFlowContext MUST NOT be arbitrarily mutated, replaced, or reconstructed by application or domain code.
- Non-HTTP execution environments MAY use explicitly defined context creation mechanisms, provided equivalent security guarantees are preserved.

AuthFlowContext encapsulates the authentication-related information required by the operation, including effective authentication mode, client characteristics, tenant context, and applicable runtime signals.

Context creation and ownership MUST remain under framework-controlled boundaries.

---

### 3.2 AccessContext

AccessContext represents the authorization boundary derived from authentication state.

- AccessContext is derived from AuthFlowContext.
- AccessContext defines *who* the current actor is and *what context* the request is operating under.
- AccessContext is used exclusively for authorization, policy evaluation, and permission checks.

AccessContext MUST NOT contain authentication logic.
Authentication decisions MUST be resolved before AccessContext is created.

---

### 3.3 Context Creation Boundaries

Context creation is a controlled operation within the authentication and authorization pipeline.

- Authentication context creation MUST occur through designated framework mechanisms.
- AccessContext MUST be derived from trusted authentication state and the applicable authorization request context.
- Context creation MUST NOT be delegated to arbitrary application or domain code.
- Services and stores MUST NOT independently reconstruct or override established security contexts.

Application code MUST treat established security contexts as trusted, read-only inputs.

---

### 3.4 Request Determinism

Authentication evaluation in UltimateAuth is deterministic and request-based.

- Each request independently derives its authentication and authorization context.
- No implicit cross-request authentication state is allowed.
- Authentication evaluation MUST be consistent for equivalent inputs and equivalent authoritative security state. State transitions, replay protection, token rotation, and revocation MAY legitimately produce different outcomes for otherwise identical requests.

Deterministic evaluation ensures predictable behavior across distributed systems and asynchronous execution.

---

### 3.5 Pipeline Integration Model

UltimateAuth integrates with host frameworks through explicit pipeline extension points.

- Authentication state is established before endpoint execution.
- Context creation occurs as part of the request pipeline, not within application logic.
- Application endpoints MUST NOT be responsible for constructing or mutating authentication context.

The exact integration mechanism (e.g. middleware, endpoint filters, or framework-specific hooks) is an implementation detail and MUST preserve the semantic guarantees defined in this section.

---

### 3.6 Architectural Invariants

The following invariants MUST hold:

- The effective authentication context remains stable throughout its execution scope.
- AccessContext is derived from trusted authentication state, never the inverse.
- Security contexts MUST NOT be arbitrarily modified or replaced by application code.
- Context creation MUST occur through explicit, controlled boundaries.
- Context integrity MUST be preserved across supported execution environments.

Any implementation that bypasses these boundaries is architecturally incorrect.

## 4. Domain Boundaries

UltimateAuth is composed of clearly separated domains with explicit responsibilities and non-overlapping concerns.
Domains define behavioral and security boundaries. They MUST NOT be merged, partially implemented, or implicitly coupled.

---

### 4.1 User-Centric Domains

User-related concerns are intentionally split into multiple independent domains.

The following domains exist:

- **UserLifecycle**  
  Represents user existence and security-relevant state (e.g. active, disabled, deleted).

- **UserProfile**  
  Represents user-facing profile and presentation data. This domain MUST NOT affect authentication decisions.

- **UserIdentifier**  
  Represents login identifiers (e.g. email, phone, username) and their verification lifecycle.
  This domain does NOT contain secrets or credentials.

Each domain has its own lifecycle, persistence model and invariants.

No domain may directly modify the state of another domain.

---

### 4.2 Credentials Domain

The Credentials domain is responsible for secret material used to prove identity.

- Credentials are not user profiles.
- Credentials are not identifiers.
- Credentials are security-critical and isolated by design.

Credential types (e.g. password, passkey, OTP) are modeled as distinct domain concepts, independent of storage layout.

---

### 4.3 Authorization Domain

The Authorization domain evaluates permissions and access decisions based on authenticated identity.

- Authorization depends on authentication state.
- Authentication MUST NOT depend on authorization.
- Authorization logic MUST NOT leak into other domains.

---

### 4.4 UserKey as a Cross-Domain Identity Anchor

All user-related domains are linked through a shared UserKey.

UserKey is a value object that represents a stable, opaque identity anchor across domains.

- UserKey is NOT a domain itself.
- UserKey does NOT represent persistence identity.
- UserKey does NOT expose internal structure to domains.
- Domains MUST treat UserKey as an opaque identifier.

Mapping between UserKey and application-specific user identifiers occurs exclusively at system boundaries.

The internal representation of UserKey MUST remain flexible and replaceable without affecting domain logic.

---

### 4.5 Host User Model Independence

UltimateAuth MUST remain independent of host application user entity models.

- Host applications MUST NOT be required to inherit from framework-owned user entities.
- Host user entities MUST NOT be required to implement UltimateAuth-specific interfaces.
- Integration with host user models MUST occur through explicit adapters, providers, or boundary contracts.
- Framework-owned runtime records and identity snapshots MUST remain independent of host persistence entities.
- Mapping between host user identities and UltimateAuth UserKey values MUST occur at controlled integration boundaries.

Reference implementations MAY provide ready-to-use adapters or providers, but MUST NOT impose a mandatory host user model.

UltimateAuth MUST preserve its authentication, authorization, and domain invariants regardless of the host application's user entity structure.

---

### 4.6 Domain Independence Guarantees

The following guarantees MUST hold:

- Domains share UserKey but do NOT share state.
- Domain lifecycles are independent.
- Persistence concerns MUST NOT redefine domain boundaries.
- Cross-domain operations MUST be coordinated at the service or orchestration layer.

Violating domain boundaries is considered an architectural error.

## 5. Store, Service, Orchestrator, and Authority Separation

UltimateAuth enforces a strict separation of responsibilities between persistence, application coordination, orchestration and security decision-making.
Each layer has explicit constraints and MUST NOT assume responsibilities belonging to another layer.

---

### 5.1 Stores

Stores are persistence-only components.

- Stores handle data access and persistence.
- Stores MUST NOT contain authorization logic.
- Stores MUST NOT evaluate policies or permissions.
- Stores MUST NOT create or modify authentication or authorization context.
- Stores MUST NOT depend on AccessContext or AuthFlowContext.

Stores are deterministic and side-effect free beyond their persistence responsibility.

---

### 5.2 Services

Services represent application-level use cases.

- Services define *what* operation is being performed.
- Services coordinate high-level workflows.
- Services invoke orchestrators to execute security-sensitive operations.

Services MUST NOT bypass orchestrators or authorities.

Services are not security boundaries.

---

### 5.3 Orchestrators

Orchestrators coordinate complex, security-critical flows
across multiple domains and subsystems.

- Orchestrators are policy-aware.
- Orchestrators enforce sequencing, invariants and cross-domain consistency.
- Orchestrators interact with Authority components to evaluate security decisions.

UltimateAuth defines multiple orchestrators, including but not limited to:

- Session Orchestrator
- Access Orchestrator
- Login Orchestrator

The existence of multiple orchestrators is intentional.
New orchestrators MAY be introduced as the system evolves.

---

### 5.4 Authority Components

Authority components are responsible for security and authorization decisions within their explicitly defined responsibility boundaries.

- Authorities evaluate applicable security policies and permissions.
- Authorities determine whether protected operations are permitted.
- Authorities MUST remain independent of persistence implementation details.
- Authority decisions MUST NOT be bypassed by services or orchestrators.

Not every security-related validation requires an Authority component.

Cryptographic verification, token parsing, protocol validation, and other deterministic validation operations MAY be performed by dedicated validators or security primitives.

---

### 5.5 Mandatory Security Coordination Rule

Security-sensitive operations MUST follow their designated framework-controlled execution paths.

- Operations requiring authorization decisions MUST invoke the applicable authority.
- Complex security-sensitive workflows MUST use their designated orchestrators.
- Services MUST NOT bypass required authorization or security checks.
- Stores MUST NOT independently make authorization decisions.
- Cryptographic and protocol validation MAY be performed by dedicated components without introducing unnecessary orchestration layers.

No implementation may bypass a security boundary required by the operation being performed.

---

### 5.6 Architectural Guarantees

The following guarantees MUST hold:

- Stores are never policy-aware.
- Services MUST NOT independently override required authority decisions.
- Orchestrators always coordinate security-sensitive flows.
- Authorities are the single source of truth for authorization decisions.
- No execution path may bypass the security controls required by its designated execution model.

Violations of these guarantees compromise system security and are not permitted.

## 6. Session Architecture

UltimateAuth provides a server-controlled session architecture for authentication modes that require stateful identity management.

Sessions establish authentication continuity, lifecycle management, revocation capabilities, and server-side control over authenticated identity.

The role of sessions depends on the effective authentication mode.

Session-backed modes MUST enforce their documented session validation guarantees. Stateless modes MUST NOT be required to introduce server-side session persistence solely to conform to the session architecture.

---

### 6.1 Session as an Authentication Primitive

A session represents server-managed authenticated identity state and its associated security lifecycle.

- Sessions are server-owned and server-validated.
- Sessions provide authentication continuity and lifecycle control.
- In stateful authentication modes, sessions are authoritative for authentication validity.
- In modes using stateless access tokens, sessions MAY remain authoritative for lifecycle operations without participating in every access-token validation.

Client-held credentials MUST be validated according to the effective authentication mode.

---

### 6.2 Session Types and Composition

UltimateAuth supports structured session composition.

- **Root Sessions** represent the primary authenticated identity.
- **Chained Sessions** represent derived or delegated authentication contexts.

Chained sessions MUST be traceable to a root session and MUST NOT exist independently.

Session composition enables controlled delegation, refresh flows, and security isolation without duplicating identity state.

---

### 6.3 Session Validation and Resolution

Session validation is required whenever the effective authentication mode or operation depends on server-side session state.

- Stateful authentication modes MUST validate session validity for authenticated access.
- Session resolution MUST follow explicit and deterministic rules.
- Stateless access-token validation MAY operate without request-time session lookup.
- Operations that require session state, including session refresh and session revocation, MUST validate the applicable session.

Session validation requirements MUST be explicitly defined for each authentication mode.

---

### 6.4 Revocation and Invalidation Semantics

Revocation is a first-class security operation in UltimateAuth.

- Stateful authentication modes MUST reject revoked sessions during subsequent authentication validation.
- Session-dependent operations MUST reject revoked or invalid sessions.
- Stateless access tokens MAY remain valid until expiration unless an additional revocation mechanism is explicitly supported.
- Revocation guarantees MUST be documented separately for each authentication mode.
- No implementation may claim immediate access-token revocation without an enforcement mechanism capable of providing that guarantee.

Revocation behavior MUST be explicit, predictable, and consistent with the selected authentication mode.

---

### 6.5 Session Refresh and Continuity

Session refresh preserves authentication continuity without re-authentication.

- Refresh operations MUST validate the underlying session.
- Refresh MUST NOT silently elevate privileges.
- Refresh behavior MUST respect the active authentication mode.

Refresh mechanisms MUST NOT bypass session validation or authority evaluation.

---

### 6.6 Relationship Between Sessions and Authentication Modes

Authentication modes determine how authentication credentials are validated and how session state participates in authentication.

- **PureOpaque:** Session state is authoritative for authenticated access.
- **Hybrid:** Opaque access tokens are validated through server-controlled, session-backed authentication.
- **SemiHybrid:** JWT access tokens are validated without mandatory request-time session lookup; sessions govern applicable lifecycle operations.
- **PureJwt:** Authentication is based on stateless JWT validation without mandatory session persistence.

Switching authentication modes MAY change validation and revocation behavior, but MUST NOT weaken the common security boundaries of the framework.

---

### 6.7 Security Invariants

The following invariants MUST hold:

- Authentication authority remains under server control.
- Stateful authentication modes MUST enforce session validity.
- Stateless authentication modes MUST validate token authenticity, integrity, expiration, and applicable security constraints.
- Session-dependent operations MUST NOT bypass required session validation.
- Revocation guarantees MUST match the actual enforcement capabilities of the selected authentication mode.
- Client-held credentials MUST NOT be trusted without appropriate validation.

Any implementation violating these invariants is architecturally incorrect.

## 7. Client & Runtime Model

UltimateAuth defines a single, unified authentication model that operates consistently across multiple client runtimes.
Client runtimes influence *how* authentication flows are executed, but MUST NOT redefine authentication semantics.

---

### 7.1 Runtime-Agnostic Core

The UltimateAuth core authentication model is runtime-agnostic.

- Authentication semantics are defined on the server.
- Client runtimes do not own identity or authentication state.
- Runtime differences MUST NOT result in divergent authentication models.

All clients participate in the same authentication and session architecture regardless of platform.

---

### 7.2 Supported Client Runtimes

UltimateAuth supports multiple client runtimes, including
but not limited to:

- Blazor Server
- Blazor WebAssembly
- MAUI
- MVC applications
- API and headless clients

Support for multiple runtimes is achieved through adaptation, not duplication of authentication logic.

---

### 7.3 Client Profiles

Runtime-specific behavior is expressed through Client Profiles.

- Client Profiles define runtime-appropriate defaults.
- Client Profiles are automatically detected when possible.
- Client Profiles MAY be explicitly configured when required.

Client Profiles influence transport mechanisms, flow selection, and security constraints, but MUST NOT change core authentication semantics.

---

### 7.4 Request-Based Client Participation

Clients participate in authentication on a per-request basis.

- Each request is evaluated independently.
- Client-provided data is treated as input, not authority.
- Authentication state is resolved server-side for every request.

No client runtime is permitted to cache or infer authentication authority outside server validation.

---

### 7.5 Public Clients and PKCE Requirements

Public clients, including browser-based and mobile applications, operate in environments that cannot be assumed to protect long-lived client authentication secrets.

- Public clients MUST NOT be trusted to securely maintain confidential client credentials.
- Authorization-code flows involving public clients MUST use PKCE.
- PKCE validation MUST follow the supported protocol requirements and security invariants.
- Client-provided metadata MUST NOT independently establish trusted client identity.
- Authentication flows MUST account for potentially compromised or malicious clients.

User credentials, short-lived authorization artifacts, and PKCE code verifiers MUST NOT be confused with confidential client authentication secrets.

Security guarantees MUST remain enforceable at trusted server-side boundaries.

---

### 7.6 Client SDK Responsibilities

Client SDKs and libraries provide convenience and integration support only.

- Client SDKs MUST NOT create identity.
- Client SDKs MUST NOT evaluate authorization decisions.
- Client SDKs MUST NOT bypass authentication or session validation.

Client SDKs are adapters, not security authorities.

---

### 7.7 Cross-Runtime Consistency Guarantees

The following guarantees MUST hold across all runtimes:

- Authentication semantics are identical across clients.
- Session validation remains server-authoritative.
- Revocation behavior is consistent across runtimes.
- Runtime-specific optimizations MUST NOT weaken security.

Any implementation that introduces runtime-specific authentication semantics is considered architecturally incorrect.

## 8. Security Invariants

The following security invariants define the non-negotiable security guarantees of UltimateAuth.
These invariants apply across all authentication modes, client runtimes, domains and implementations.
Violating any invariant compromises system security and is considered architecturally incorrect.

---

### 8.1 Server Authority Invariant

The server is the sole authority for authentication and authorization decisions.

- Clients are never trusted authorities.
- Client-provided data is always treated as untrusted input.
- Authentication state MUST be validated server-side.

No client runtime, SDK, or application code may assume authority over identity or access decisions.

---

### 8.2 Authentication State Validation Invariant

Authentication state MUST be validated according to the effective authentication mode.

- Stateful authentication modes MUST validate the authoritative session state.
- Stateless authentication modes MUST validate cryptographic token integrity and applicable token constraints.
- Client-held credentials MUST NOT independently establish authentication authority.
- Authentication validation MUST NOT bypass security checks required by the effective mode.

The framework MUST NOT assume that all authentication modes require request-time session validation.

---

### 8.3 Context Integrity Invariant

Authentication and authorization context objects define security boundaries.

- AuthFlowContext is immutable after creation.
- Exactly one AuthFlowContext exists per request.
- AccessContext is derived from AuthFlowContext.
- Context objects MUST NOT be mutated, recreated or bypassed.

Context integrity is mandatory for deterministic and secure request processing.

---

### 8.4 Security Coordination Invariant

Security-sensitive operations MUST follow explicitly defined execution and validation boundaries.

- Operations requiring authorization decisions MUST use the applicable authority components.
- Complex security-sensitive workflows MUST use their designated orchestrators.
- Security validation primitives MAY operate independently when orchestration or authority evaluation is not required.
- Services, stores, and extensions MUST NOT bypass mandatory security controls.
- Cross-domain security operations MUST preserve coordination and consistency guarantees.

No implementation may weaken required security controls by bypassing the designated execution path.

---

### 8.5 Domain Isolation Invariant

Domains represent isolated security and responsibility boundaries.

- Domains MUST NOT share mutable state.
- Domains MUST NOT directly modify other domains.
- Cross-domain operations MUST be coordinated through services and orchestrators.

Domain isolation MUST NOT be weakened by persistence or implementation convenience.

---

### 8.6 Credential Protection Invariant

Credential material is security-critical and MUST be handled through controlled security boundaries.

- Credential secrets MUST NOT be persisted, logged, or exposed outside authorized credential-handling mechanisms.
- Credential validation MUST occur through trusted server-side components.
- Credentials MUST NOT be treated as user identifiers or profile data.
- Services and transport layers MAY temporarily handle credential material only as required to execute authorized authentication flows.
- Credential material MUST NOT be propagated to unrelated domains.

Credential protection MUST remain independent of the host application's persistence model.

---

### 8.7 Deterministic Evaluation Invariant

Authentication and authorization evaluation MUST be deterministic for equivalent inputs and equivalent authoritative security state.

- Security decisions MUST follow explicit and predictable rules.
- Hidden or uncontrolled security authority is not permitted.
- State transitions, token rotation, replay prevention, expiration, and revocation MAY legitimately change evaluation outcomes.
- Concurrent operations MUST preserve applicable security invariants.

Determinism MUST NOT prevent legitimate stateful security transitions.

---

### 8.8 Revocation Invariant

Revocation guarantees MUST be explicit and consistent with the effective authentication mode.

- Stateful authentication modes MUST enforce session revocation during subsequent authentication validation.
- Session-dependent operations MUST reject revoked sessions.
- Stateless tokens MAY remain valid until expiration unless additional revocation enforcement is provided.
- Revocation behavior MUST NOT be represented as immediate when the implementation cannot enforce immediate invalidation.
- Runtime-specific implementations MUST preserve the revocation guarantees defined for their authentication mode.

Security documentation MUST accurately describe the revocation capabilities and limitations of each supported mode.

---

### 8.9 Extensibility Safety Invariant

UltimateAuth is extensible, but extensibility MUST NOT alter security semantics.

- Extensions MUST preserve all security invariants.
- Overrides MUST NOT bypass orchestration, authority or session validation.
- Custom implementations MUST remain server-authoritative.

Extensibility that compromises security guarantees is not supported.

## 9. What This Document Does NOT Define

This document intentionally limits its scope to architectural rules and security invariants.
The following concerns are explicitly out of scope and MUST NOT be inferred from this document.

---

### 9.1 User Experience and Application Flow

This document does NOT define:

- User interface design or layout.
- User experience flows.
- Screen navigation or interaction patterns.
- Application-specific onboarding or registration flows.

Such concerns are application responsibilities and vary by use case.

---

### 9.2 API Shapes and Public Contracts

This document does NOT define:

- Public API method signatures.
- DTO shapes or transport contracts.
- Client SDK APIs or surface area.
- HTTP endpoint structures or routing conventions.

API design MAY evolve as long as architectural and security rules are preserved.

---

### 9.3 Persistence and Infrastructure

This document does NOT define:

- Database technologies or providers.
- Schema designs or table layouts.
- Caching strategies.
- Replication, sharding, or scaling approaches.
- Hosting or deployment topology.

Persistence and infrastructure choices MUST NOT alter architectural or security guarantees.

---

### 9.4 Performance Optimizations

This document does NOT define:

- Performance tuning strategies.
- Caching heuristics or TTL policies.
- Latency optimizations.
- Resource allocation strategies.

Performance improvements MUST preserve all architectural and security invariants.

---

### 9.5 Feature Set and Product Roadmap

This document does NOT define:

- Feature completeness.
- Supported scenarios.
- Roadmap priorities or timelines.
- Backward compatibility guarantees.

Product direction is defined separately and MUST NOT override architectural constraints.

---

### 9.6 Implementation Techniques

This document does NOT define:

- Framework-specific implementation patterns.
- Language-level constructs or idioms.
- Code organization or folder structure.
- Testing strategies or tooling.

Implementation techniques are free to evolve within the boundaries defined by this document.

---

### 9.7 Documentation and Educational Content

This document does NOT define:

- Tutorials or onboarding materials.
- Example applications.
- Reference guides or walkthroughs.

Educational content MUST explain the architecture but MUST NOT redefine it.

---

### 9.8 Final Authority Statement

If a concern is not explicitly defined in this document, it is considered an implementation or product decision, not an architectural rule.

This document exists to constrain behavior, not to describe every possible behavior.


---

## Change Policy

This document is expected to change rarely.
Any change to this document MUST be intentional, explicit, and reviewed with extreme care.
Incremental refactors, convenience changes, or stylistic improvements MUST NOT modify the architectural rules defined here.
