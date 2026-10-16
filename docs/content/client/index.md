---
title: Client SDK Overview
order: 1
group: client
---

# Client SDK Overview

The UltimateAuth Client SDK provides a unified way to integrate authentication, session management, user operations, credentials, and authorization into .NET applications.

Instead of manually constructing requests for every authentication endpoint, applications can use dedicated client services that understand UltimateAuth's API contracts and authentication workflows.

The SDK is designed for applications that need to interact with an UltimateAuth server, including Blazor WebAssembly, Blazor Server, and other supported .NET client environments.

**This section is the practical developer guide to using UltimateAuth from application code.**

## What the Client SDK Provides

The Client SDK is more than a collection of HTTP methods.

It brings together several responsibilities:

- **Authentication workflows** — Start login, refresh, logout, and other supported authentication operations.
- **Session management** — Interact with available session and device management capabilities.
- **User management** — Retrieve and manage user information through the appropriate API contracts.
- **Credential operations** — Manage credentials and initiate supported recovery workflows.
- **Authorization management** — Work with roles, permissions, and other exposed authorization operations.
- **Structured results** — Receive operation results through the SDK's response contracts.
- **Client state integration** — Coordinate supported authentication state changes and notifications.

The SDK helps applications interact with UltimateAuth consistently while keeping the server responsible for authentication and authorization enforcement.

It does not replace server-side security checks.

## The Client Architecture

UltimateAuth separates client responsibilities into specialized services.

The main client entry point is `UAuthClient`, which provides access to the SDK's functional areas.

Conceptually:

```text
Application
    |
    v
UAuthClient
    |
    +-- Flows
    |     Login, logout, refresh, PKCE
    |
    +-- Sessions
    |     Sessions and device-associated chains
    |
    +-- Users
    |     User information and lifecycle
    |
    +-- Identifiers
    |     User identifiers and verification
    |
    +-- Credentials
    |     Credential management and recovery
    |
    +-- Authorization
          Roles, permissions, and access management
```

These represent functional responsibilities. The precise available members and operations depend on the installed packages and supported SDK contracts.

### Why Specialized Clients?

Authentication-related operations have different requirements.

A login flow may establish authentication state and involve redirects. A user management operation may retrieve or update account information. A credential recovery operation may require additional verification and security handling.

Keeping these responsibilities separate provides a clearer programming model and makes individual APIs easier to discover.

## Getting Started with UAuthClient

In a supported dependency injection environment, application components can consume the configured UltimateAuth client.

For example, a Blazor component can receive the client through dependency injection:

```razor
@inject UAuthClient UAuthClient
```

Once configured, the application uses the appropriate client service for the operation it needs.

The following illustrates the intended programming model:

```csharp
var user = await UAuthClient.Users.GetMeAsync();
```

The SDK handles the corresponding client operation and returns its structured result.

Before using the client, the application must register the appropriate UltimateAuth services and configure its connection to the authentication server.

For a complete setup, follow the Getting Started guide for your application environment.

## Choose Your Application Environment

UltimateAuth supports different application architectures, but their authentication and transport requirements are not identical.

### Blazor WebAssembly

Blazor WebAssembly executes application code in the browser.

The client communicates with the authentication server and protected APIs through browser-compatible HTTP requests.

Browser security rules, credential handling, CORS configuration, and authentication state synchronization are relevant to this environment.

### Blazor Server

Blazor Server executes application code on the server while maintaining an interactive connection to the browser.

Authentication state, cookie behavior, and server-side execution require different considerations from a browser-hosted WebAssembly application.

Server-side code may also invoke application services directly rather than making an HTTP request.

Such operations still require appropriate server-side authorization enforcement.

### Interactive Web Applications

Applications combining server rendering and WebAssembly interactivity may have multiple execution environments.

Developers should distinguish between operations performed on the server and operations performed in the browser.

Shared UI components do not automatically imply identical transport or credential handling.

### Other .NET Clients

Other supported .NET application environments can use the relevant client SDK capabilities according to their platform requirements.

Credential storage, HTTP transport, redirect handling, and authentication state integration may differ from browser applications.

The SDK's shared contracts help maintain consistency, while platform-specific integration handles the environment's requirements.

## Working with Authentication State

Authentication is not simply a method call that returns a token.

The client may need to coordinate several related concerns:

- Completing the selected authentication flow.
- Handling the resulting authentication state.
- Renewing authentication when continuation is supported.
- Reacting to logout or session invalidation.
- Updating application components when relevant state changes.

UltimateAuth provides client-side abstractions and notifications for supported operations.

For example, user or credential changes may publish SDK events after a successful operation.

Applications can use the available state integration mechanisms to update their UI without treating every API response as a complete authentication state transition.

The exact behavior depends on the client environment and configured integration.

## Understanding SDK Results

UltimateAuth client operations use structured response contracts.

Applications should inspect the result of an operation rather than assuming that completing an asynchronous call means the requested action succeeded.

A typical application workflow is:

```text
Call SDK operation
        |
        v
Receive structured result
        |
        +-- Success
        |      |
        |      v
        |   Continue application workflow
        |
        +-- Failure
               |
               v
           Handle error
```

Different operations may return different result types.

The relevant guide for each client service explains its request and response contracts, including how to handle failures.

## Client Events and UI Updates

Some SDK operations can publish notifications when a successful operation changes application-relevant state.

For example, credential or user management operations may notify subscribers that related information has changed.

These notifications help decouple an operation from the UI components that react to it.

A component can respond by refreshing its data rather than requiring the caller to manually update every affected view.

However, client events are not a security authority.

An event does not replace server-side validation, and the absence of an event does not prove that the underlying security state has remained unchanged.

Applications should continue to treat the server as authoritative wherever the selected authentication mode requires it.

## Calling Protected Resource APIs

Authentication is often only the first step.

Applications also need to call protected business APIs after the user has authenticated.

UltimateAuth supports integration with protected resource requests, allowing the client application to use the authentication mechanisms appropriate to its configured environment.

A protected resource request involves two distinct responsibilities:

1. The client sends the request with the appropriate authentication material.
2. The receiving server validates authentication and enforces authorization.

The Client SDK can help with the client-side responsibility.

It cannot grant access to an operation that the receiving server does not authorize.

When building applications with separate resource APIs, developers must configure the relevant API integration and server-side authentication enforcement.

## Client SDK Guides

The following guides cover the major client responsibilities with practical C# examples.

| Guide | What you will learn |
|---|---|
| **Authentication** | Login, logout, refresh, PKCE, and authentication state handling |
| **Session Management** | Retrieve, inspect, and manage sessions and device-associated chains |
| **User Management** | Retrieve user information and perform supported user lifecycle operations |
| **Identifiers** | Manage email addresses, usernames, phone numbers, and verification workflows |
| **Credentials** | Add, change, revoke, and recover credentials |
| **Authorization** | Work with roles, permissions, and exposed authorization management operations |
| **Resource API Requests** | Call protected application APIs using the appropriate authentication integration |

Each guide focuses on a specific developer task and includes the relevant SDK methods, request models, response handling, and implementation considerations.

### Recommended Learning Path

If you are integrating UltimateAuth for the first time, start with **Authentication**.

Once login and authentication state handling work correctly, continue with **Resource API Requests** to connect authentication to your application's protected functionality.

After that, use the domain-specific guides for the features your application needs.

For example, an account settings page may require the User Management, Identifiers, and Credentials guides, while an administration panel may additionally require Authorization and Session Management.

## Client SDK and Server Responsibilities

The Client SDK simplifies integration, but it does not independently enforce the security of server-side operations.

| Client responsibility | Server responsibility |
|---|---|
| Submit authentication requests | Validate authentication requirements |
| Manage supported client state | Establish and validate authoritative session state |
| Send protected resource requests | Authenticate and authorize protected operations |
| Handle structured responses | Enforce domain rules and security policies |
| React to state notifications | Apply revocation and account security controls |

This separation is particularly important for applications that share UI components across different hosting models.

A shared component can use the same SDK abstractions, while the hosting environment and server determine how authentication material is transported and validated.

## When to Use the Client SDK

Use the Client SDK when building an application that interacts with UltimateAuth's exposed authentication and management APIs.

It is generally preferable to manually reproducing the corresponding HTTP contracts because it provides a consistent integration surface and reduces duplicated request-handling code.

Direct endpoint integration may still be appropriate for clients that cannot use the .NET SDK or for specialized interoperability requirements.

Regardless of the client implementation, the same server-side authentication and authorization requirements apply.

## Next Steps

Continue with the Client SDK guides to implement real application workflows.

Start with **Authentication** to configure and use login, refresh, and logout operations.

Then explore session management, user operations, credentials, authorization, and protected resource API integration according to your application's requirements.

For the concepts behind these operations, refer to [Authentication Modes](/docs/fundamentals/authentication-modes) and [Plugin Domains](/docs/plugin-domains).