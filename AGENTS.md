# UltimateAuth — Agent Instructions

## Project Guidance

UltimateAuth is a security-sensitive authentication and authorization framework for .NET.

Before making changes, review the relevant project guidance:

- `.ultimateauth/architecture.md` — canonical architecture
- `.ultimateauth/ai-guardrails.md` — mandatory AI security and architectural guardrails
- `CONTRIBUTING.md` — contribution conventions

These documents must not be silently bypassed or modified to justify an implementation.

## Scope and Changes

- Follow the explicitly requested scope.
- Preserve public contracts and existing authentication guarantees unless a change is explicitly authorized.
- Prefer focused changes over speculative refactoring.
- Do not implement unrelated TODOs or roadmap features.
- Report unrelated findings separately.
- Do not create commits, branches, pull requests, or modify repository settings unless requested.

## Implementation Quality

- Follow existing naming, formatting, and architectural conventions.
- Prefer existing abstractions over introducing unnecessary new ones.
- Keep security-sensitive operations within their established execution boundaries.
- Preserve compatibility across supported target frameworks.

## Testing

- Add focused regression tests for behavior changes and bug fixes.
- For security defects, demonstrate the original failure before implementing the fix when practical.
- Preserve existing negative security tests.
- Run relevant tests and report their actual results.
- Never claim that tests passed unless they were executed.

## Pull Requests

- Keep pull requests focused on a single purpose.
- Follow the pull request conventions in `CONTRIBUTING.md`.
- Explain the motivation, changes, relevant tests, and compatibility impact.
- Do not claim an issue is fully resolved when part of the finding remains outside the change.

## Final Report

Summarize the changes, tests performed, unresolved concerns, and any relevant compatibility or security impact.
