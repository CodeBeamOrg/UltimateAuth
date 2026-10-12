# UltimateAuth Governance

## 1. Project Philosophy

UltimateAuth is an open-source, community-first authentication and authorization framework for modern .NET applications.

The project was initiated by CodeBeam and is developed with the goal of creating a secure, unified, extensible, and developer-friendly authentication ecosystem.

We believe that sustainable open-source software requires both community participation and architectural consistency.

Our governance philosophy is simple:

**Community-driven development. Maintainer-led decisions. Shared progress.**

## 2. Governance Model

UltimateAuth follows a maintainer-led governance model.

Project maintainers are responsible for:

- Protecting the project's security and architectural integrity.
- Reviewing and merging contributions.
- Managing releases and compatibility expectations.
- Maintaining the project roadmap.
- Coordinating security vulnerability handling.
- Supporting healthy community collaboration.

Maintainers make final decisions on technical direction and project scope.

However, these decisions should be informed by community feedback, technical evidence, and open discussion whenever practical.

This governance model may evolve as the contributor community grows.

## 3. Community Participation

Everyone is welcome to participate in UltimateAuth.

Community members may:

- Report bugs and unexpected behavior.
- Propose features or improvements.
- Submit pull requests.
- Contribute documentation, examples, and tests.
- Participate in technical discussions.
- Suggest architectural improvements.
- Help other developers.

Participation does not require formal membership or prior approval.

We value the quality of ideas and contributions, not organizational affiliation or contributor status.

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidance.

## 4. Decision-Making

### 4.1 Routine Changes

Routine changes, including documentation improvements, minor bug fixes, tests, and non-breaking implementation improvements, may be reviewed and merged by authorized maintainers.

Formal voting or prior architectural approval is not required when established project contracts and security guarantees remain unchanged.

### 4.2 Significant Changes

Significant changes should be discussed before implementation when practical.

Examples include:

- Changes to public APIs.
- Authentication or authorization behavior changes.
- New authentication modes or security mechanisms.
- Major architectural changes.
- Breaking changes.
- New extension mechanisms affecting security boundaries.

These proposals may be discussed through GitHub issues, discussions, or pull requests.

Maintainers are responsible for evaluating technical implications, compatibility, security, and alignment with the project's direction.

### 4.3 Final Decisions

Maintainers have final decision-making authority over changes accepted into the official UltimateAuth repository.

Decisions should prioritize:

1. Security and correctness.
2. Architectural consistency.
3. Developer experience.
4. Maintainability and long-term sustainability.
5. Community needs and feedback.

When a proposal is declined, maintainers should provide a brief explanation whenever practical.

Consensus is preferred, but unanimous agreement is not required.

## 5. Architecture and Security

UltimateAuth maintains a canonical architectural specification:

[.ultimateauth/architecture.md](./.ultimateauth/architecture.md)

Architectural and security-sensitive changes must preserve the project's established invariants or be accompanied by an explicitly reviewed architectural amendment.

Changes to the canonical architecture require maintainer approval.

AI-assisted development is additionally governed by:

[.ultimateauth/ai-guardrails.md](./.ultimateauth/ai-guardrails.md)

These documents guide implementation and review. They do not prevent contributors from questioning existing decisions or proposing architectural improvements.

Security vulnerabilities must be reported according to [SECURITY.md](SECURITY.md).

## 6. Maintainers and Contributors

### 6.1 Contributors

A contributor is anyone who helps improve UltimateAuth through code, documentation, testing, feedback, discussions, or community support.

Contributors do not need special status to participate.

### 6.2 Maintainers

Maintainers are trusted contributors responsible for the project's technical and community stewardship.

Maintainer responsibilities may include:

- Reviewing contributions.
- Managing issues and pull requests.
- Making architectural decisions.
- Managing releases.
- Coordinating security responses.
- Supporting contributors.

Maintainer permissions are granted by the existing project maintainers based on demonstrated trust, sustained contributions, technical judgment, and alignment with the project's values.

Maintainer status is not automatically granted based on contribution count.

### 6.3 Maintainer Changes

New maintainers may be invited as the project grows.

Maintainers who become inactive may voluntarily step back or have their responsibilities adjusted.

Maintainer permissions may be revoked when necessary to protect project security, integrity, or community trust.

Such decisions should be handled responsibly and with appropriate discretion.

## 7. Releases and Roadmap

Maintainers coordinate official releases and determine release readiness.

The project roadmap reflects current intentions rather than binding delivery commitments.

Community feedback may influence priorities, but inclusion in the roadmap does not guarantee implementation or a specific release date.

Breaking changes, security considerations, compatibility, and maintenance capacity should be considered before releases.

## 8. Transparency and Communication

UltimateAuth aims to keep development decisions transparent whenever practical.

Public GitHub discussions, issues, and pull requests are preferred for ordinary technical collaboration.

The [UltimateAuth Discord community](https://discord.gg/QscA86dXSR) provides an additional space for questions, feedback, and collaboration.

Security reports, sensitive moderation matters, and confidential information may require private communication.

Maintainers are encouraged to explain significant decisions and maintain clear project direction.

## 9. Future Evolution

This governance model is intentionally lightweight.

As UltimateAuth grows, the project may introduce:

- Additional maintainers.
- Defined maintainer responsibilities.
- Working groups for specific technical areas.
- More structured architectural proposal processes.
- Broader community participation in project governance.

Such changes should serve the project's needs rather than introduce unnecessary bureaucracy.

Governance amendments require maintainer approval and should be communicated transparently.

## 10. Our Commitment

UltimateAuth is built for developers and strengthened by its community.

We aim to create an environment where contributions are welcomed, technical decisions are thoughtful, and security is never compromised for convenience.

**Open to everyone. Guided by principles. Built together.**
