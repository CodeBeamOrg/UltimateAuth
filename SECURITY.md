# Security Policy

Security is a core concern for UltimateAuth. We appreciate responsible reports from researchers, users, and contributors, and we aim to handle vulnerabilities carefully and transparently.

## Supported versions

UltimateAuth is in active pre-1.0 development. Security fixes are generally prioritized for the latest maintained release line.

| Version | Security support |
| --- | --- |
| Latest maintained 0.x release | Supported for security review and fixes |
| Older previews and superseded 0.x releases | Not guaranteed |

This policy does not promise long-term support for every preview or pre-1.0 version. Please update to the latest available maintained version when possible.

## Reporting a vulnerability

**Please do not disclose suspected vulnerabilities in public GitHub issues, pull requests, discussions, or public Discord channels.**

### Preferred: GitHub private vulnerability reporting

If private vulnerability reporting is enabled for this repository, use GitHub's **Report a vulnerability** option under the repository's **Security** tab:

[Privately report a vulnerability](https://github.com/CodeBeamOrg/UltimateAuth/security/advisories/new)

This lets repository maintainers review the report privately before a coordinated disclosure.

If the reporting option is unavailable, use the alternative contact method below. Do not publish exploit details to work around an unavailable private reporting feature.

### Alternative: contact maintainers through Discord

Join the [UltimateAuth Discord community](https://discord.gg/QscA86dXSR) and contact a project maintainer or authorized moderator **by direct message** to request a private reporting channel.

Do not share exploit steps, secrets, proof-of-concept code, or sensitive technical details in a public Discord channel. Discord direct messages are an initial contact option, not a guarantee of encrypted or confidential vulnerability handling. Maintainers may direct you to GitHub's private reporting workflow for the full report.

## What to include

Please share as much of the following as you reasonably can:

- A clear summary of the suspected vulnerability and its potential impact.
- Affected UltimateAuth package(s), version(s), and authentication mode(s), if known.
- Steps to reproduce or a minimal proof of concept.
- Any relevant configuration or runtime details.
- Whether the issue is already public or has been exploited, if known.

You do not need a complete exploit or a perfect report to contact us. Please redact credentials, tokens, private keys, and personal information.

## What happens next

Maintainers will aim to:

1. Acknowledge and assess the report as capacity allows.
2. Work with the reporter to clarify reproduction steps and impact.
3. Prepare and validate a fix when the issue is confirmed.
4. Coordinate a security advisory and disclosure when appropriate.
5. Credit the reporter if they wish to be acknowledged.

We do not promise fixed response or remediation deadlines at this stage. Response time depends on severity, reproducibility, and maintainer availability.

## Responsible research

We welcome good-faith research performed on systems and accounts you own or are explicitly authorized to test.

Please avoid accessing others' data, disrupting services, degrading availability, or disclosing a vulnerability before a reasonable opportunity for coordinated remediation.

## Scope

This policy covers security issues in UltimateAuth's maintained source code and official packages. Issues in third-party dependencies, hosting environments, or applications integrating UltimateAuth may require coordination with their respective maintainers.

For ordinary bugs, feature requests, and non-sensitive questions, please use [GitHub Issues](https://github.com/CodeBeamOrg/UltimateAuth/issues) or [Discord](https://discord.gg/QscA86dXSR).

Thank you for helping keep the UltimateAuth ecosystem safer.
