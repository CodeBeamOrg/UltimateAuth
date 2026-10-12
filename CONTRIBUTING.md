# Contributing to UltimateAuth

Thank you for your interest in UltimateAuth! 💜

UltimateAuth is a community-first, open-source authentication and authorization framework for modern .NET applications. Contributions of every size are welcome: questions, bug reports, ideas, documentation, examples, tests, code, and thoughtful feedback.

**You do not need to be an expert to contribute.** Our goal is to make collaboration approachable, constructive, and enjoyable.

## Join the community

- [GitHub Issues](https://github.com/CodeBeamOrg/UltimateAuth/issues) — report bugs and suggest improvements.
- [GitHub Discussions](https://github.com/CodeBeamOrg/UltimateAuth/discussions) — ask questions and explore ideas, when discussions are enabled.
- [Discord](https://discord.gg/QscA86dXSR) — meet contributors, ask for help, and share feedback.

If you are unsure where to start, open an issue or say hello on Discord.

## Reporting bugs and suggesting features

A good title and a short, clear description are usually enough to get started. Templates are helpful, but not mandatory.

For bug reports, please share whatever you can:

- What you expected to happen and what actually happened.
- The UltimateAuth version, .NET version, and relevant client/runtime, if known.
- A small reproduction, code snippet, logs, or steps to reproduce, if available.

For feature ideas, describe the problem you want to solve and how you imagine the feature helping users.

**Please do not post passwords, tokens, private keys, personal data, or sensitive logs.** If you believe you found a security vulnerability, use the private process in [SECURITY.md](SECURITY.md) rather than opening a public issue.

## Pull requests

We welcome pull requests, including small fixes and improvements.

1. Fork the repository and create a branch for your work.
2. Make a focused change, with tests when practical.
3. Open a pull request targeting the **`dev`** branch.
4. Use a descriptive title and explain what changed and why.

There is no strict pull request format. If a change fixes an issue, feel free to link it. Draft pull requests and work-in-progress discussions are welcome.

For substantial architectural changes or new authentication behavior, opening an issue first is encouraged so we can align on the approach and avoid wasted effort. This is a recommendation, not a barrier to proposing ideas.

Maintainers may suggest revisions, request tests, or help refine the approach. Reviews are intended to improve the contribution, not discourage contributors.

## Building and testing

The repository contains the `UltimateAuth.slnx` solution and unit/integration test projects.

From the repository root, you can try:

```bash
dotnet restore UltimateAuth.slnx
dotnet build UltimateAuth.slnx
dotnet test UltimateAuth.slnx
```

Some integration tests or samples may require additional configuration or infrastructure. If a command fails or you cannot run a test, simply mention it in your pull request. You do not need a perfect local environment to start contributing.

## Architecture and security

UltimateAuth separates authentication, authorization, sessions, credentials, domain responsibilities, and client/runtime adaptation.

Before making a substantial architectural or security-sensitive change, please read:

- [Canonical architecture](.ultimateauth/architecture.md)
- [AI-assisted development guardrails](.ultimateauth/ai-guardrails.md), particularly if you use coding agents

These documents guide architectural decisions; they are not intended to prevent questions, alternative proposals, or constructive discussion. If you believe an architectural rule should change, raise the proposal for maintainer review rather than silently bypassing it.

Security-sensitive changes may need more thorough review and regression tests than routine changes.

## Using AI-assisted tools

AI tools are welcome as development aids. Contributors remain responsible for understanding their changes, reviewing generated code, and accurately describing what was tested.

Please do not submit unreviewed generated changes or claim that tests were run when they were not.

## Licensing

UltimateAuth is licensed under the [Apache License 2.0](LICENSE.txt). By contributing, you agree that your submitted contributions may be incorporated into the project under its applicable contribution and licensing terms.

If a separate contributor agreement is introduced, its requirements will be documented before enforcement.

## A welcoming community

Be respectful, assume good intentions, and help others learn. Different experience levels and perspectives make the project better.

**Thank you for helping build UltimateAuth.**
