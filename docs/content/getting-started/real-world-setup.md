---
title: Real World Setup
order: 4
group: getting-started
---

# Real-World Setup

**One authentication framework. Different deployment models.**

The [QuickStart](/docs/getting-started/quickstart) demonstrates UltimateAuth with Blazor Server and in-memory storage.

Real applications may require persistent data, a separate authentication server, browser-based clients, or independently deployed APIs.

UltimateAuth supports these scenarios through different service registrations and hosting integrations. You can choose the architecture that fits your application without configuring every available component.

This guide covers the setup patterns demonstrated by the UltimateAuth samples.

## 1. Choose Your Architecture

Start by identifying where authentication is hosted and how your application communicates with it.

| Application architecture | Authentication host | Application registration |
|---|---|---|
| Blazor Server, single project | Same application | Server + persistence + Blazor client |
| Blazor Server with EF Core | Same application | Server + EF Core + Blazor client |
| Dedicated UAuthHub | Separate authentication application | Server + persistence + Hub |
| Standalone Blazor WASM | Remote UAuthHub | Blazor client |
| Blazor Web App, Interactive WASM | Remote UAuthHub | Host integration + WASM client |
| Separate Resource API | Remote UAuthHub | Resource API integration |

These models can be combined.

For example, an application might use an EF Core-backed UAuthHub, a standalone Blazor WASM frontend, and a separate Resource API.

The frontend handles user interaction, UAuthHub manages authentication, and the Resource API enforces access to protected resources.

## 2. Persistent Storage with Entity Framework Core

For applications that need authentication state to survive restarts, use a persistent storage provider.

UltimateAuth provides an Entity Framework Core bundle that registers its reference domain implementations and EF Core-backed stores.

### Install packages

```bash
dotnet add package CodeBeam.UltimateAuth.EntityFrameworkCore.Bundle
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
```

The SQLite provider is used here for simplicity. You can use another compatible EF Core database provider.

When using the EF Core bundle, you do not need the in-memory bundle.

### Register the services

In `Program.cs`:

```csharp
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.EntityFrameworkCore;

builder.Services.AddUltimateAuthServer()
    .AddUltimateAuthEntityFrameworkCore(db =>
    {
        db.UseSqlite("Data Source=uauth.db", sqlite => sqlite.MigrationsAssembly(typeof(Program).Assembly.GetName().Name!));
    });
```

`AddUltimateAuthEntityFrameworkCore()` registers the EF Core stores and reference domain implementations.

The bundle provides a unified `UAuthDbContext` for the supported authentication domains.

You do not need to register each user, session, credential, token, or authorization store individually.

### Configure the database connection

For a deployed application, prefer configuration over hardcoded connection strings.

`appsettings.json`:

```json
{
  "ConnectionStrings": {
    "UltimateAuth": "Data Source=uauth.db"
  }
}
```

`Program.cs`:

```csharp
var connectionString = builder.Configuration.GetConnectionString("UltimateAuth")
    ?? throw new InvalidOperationException("UltimateAuth connection string is missing.");

builder.Services.AddUltimateAuthServer()
    .AddUltimateAuthEntityFrameworkCore(db =>
    {
        db.UseSqlite(connectionString);
    });
```

For production databases, use your deployment environment's secret management facilities rather than storing credentials in source control.

### Database migrations

UltimateAuth does not automatically create or apply migrations when the EF Core bundle is registered.

Your application owns the database migration lifecycle.

Install the EF Core CLI if necessary:

```bash
dotnet tool install --global dotnet-ef
```

Create the initial migration:

```bash
dotnet ef migrations add InitUAuth --context UAuthDbContext
```

Apply it:

```bash
dotnet ef database update --context UAuthDbContext
```

For Visual Studio Package Manager Console:

```powershell
Add-Migration InitUAuth -Context UAuthDbContext
Update-Database -Context UAuthDbContext
```

Install `Microsoft.EntityFrameworkCore.Tools` if required.

### Applying migrations programmatically

The EF Core samples demonstrate initialization through a scoped `UAuthDbContext`.

For a controlled development environment:

```csharp
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UAuthDbContext>();
    await db.Database.MigrateAsync();
}
```

This applies pending migrations. It does not generate migration files.

**Production guidance:** Prefer applying reviewed migrations as a separate deployment step, particularly when multiple application instances share a database.

Do not copy the sample initializer's `reset: true` behavior into production. The samples use database reset functionality for repeatable demonstrations, not as a safe deployment strategy.

### Optional: sample accounts with EF Core

The sample seed package can also work with EF Core persistence.

```bash
dotnet add package CodeBeam.UltimateAuth.Sample.Seed
```

Register the scoped seed services:

```csharp
builder.Services.AddScopedUltimateAuthSampleSeed();
```

After preparing the database:

```csharp
if (app.Environment.IsDevelopment())
{
    await app.SeedUltimateAuthAsync();
}
```

The difference matters:

- `AddUltimateAuthSampleSeed()` is used by the in-memory samples.
- `AddScopedUltimateAuthSampleSeed()` is used by the EF Core samples.

Do not deploy the demonstration accounts and passwords to production.

## 3. Blazor Server — Integrated Authentication

**Best for:** A Blazor Server application that wants to host its own authentication system.

This is the simplest deployment model for a server-side Blazor application.

The application hosts UltimateAuth Server and uses the Blazor client adapter in the same project.

A separate UAuthHub is not required.

### Packages

```bash
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
dotnet add package CodeBeam.UltimateAuth.EntityFrameworkCore.Bundle
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

### Services

```csharp
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.EntityFrameworkCore;

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddUltimateAuthServer()
    .AddUltimateAuthEntityFrameworkCore(db =>
    {
        db.UseSqlite("Data Source=uauth.db");
    });

builder.Services.AddUltimateAuthClientBlazor();
```

### Pipeline

```csharp
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseUltimateAuthWithAspNetCore();
app.UseAntiforgery();

app.MapUltimateAuthEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddUltimateAuthRoutes(UAuthAssemblies.BlazorClient());
```

Also configure the `UAuthApp` root component, login page, and UltimateAuth JavaScript file as demonstrated in QuickStart.

### Authentication behavior

The default client profile resolution selects `PureOpaque` for Blazor Server.

This uses session-oriented authentication rather than requiring the application to manage access and refresh tokens manually.

You can configure session lifetimes, login policies, credential policies, and other behavior through `AddUltimateAuthServer()`.

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Login.MaxFailedAttempts = 5;
    options.Login.LockoutDuration = TimeSpan.FromMinutes(15);
    options.Session.Lifetime = TimeSpan.FromDays(7);
    options.Identifiers.Behavior.AllowMultipleUsernames = true;
});
```

The effective authentication mode is resolved at runtime. The application does not need to select a single mode globally.

## 4. UAuthHub — Dedicated Authentication Server

**Best for:** Applications where multiple clients share one authentication authority.

UAuthHub hosts UltimateAuth's authentication services separately from your frontend applications.

It is particularly relevant for standalone WASM applications, Interactive WASM applications, and systems with separate Resource APIs.

A typical architecture looks like this:

```text
       Standalone WASM       Interactive WASM
              │                    │
              └──────────┬─────────┘
                         │
                         ▼
                      UAuthHub
                  Authentication
                  Sessions & Users
                         │
                         ▼
                    Persistence

       Resource API ───► UAuthHub
         (validation)
```

### Create the Hub application

A UAuthHub can be hosted in an ASP.NET Core Blazor Server application.

The current samples use Blazor Server to provide the Hub's login UI.

### In-memory UAuthHub

For development:

```bash
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
dotnet add package CodeBeam.UltimateAuth.InMemory.Bundle
```

Register:

```csharp
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Server.Extensions;

builder.Services.AddUltimateAuthServer()
    .AddUltimateAuthInMemory()
    .AddUAuthHub(options =>
    {
        options.AllowedClientOrigins.Add("https://localhost:6130");
    });

builder.Services.AddUltimateAuthClientBlazor();
```

The Hub's Blazor client registration is needed when the Hub itself hosts UltimateAuth-powered Blazor pages and login UI.

### EF Core UAuthHub

For persistent storage:

```csharp
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

builder.Services.AddUltimateAuthServer()
    .AddUltimateAuthEntityFrameworkCore(db =>
    {
        db.UseSqlite("Data Source=uauthhub.db");
    })
    .AddUAuthHub(options =>
    {
        options.AllowedClientOrigins.Add("https://localhost:6132");
    });

builder.Services.AddUltimateAuthClientBlazor();
```

The `AllowedClientOrigins` collection identifies browser origins permitted to interact with the Hub.

Use exact origins, including scheme and port. Do not include paths such as `/home`.

### Hub pipeline

```csharp
app.UseHttpsRedirection();

app.UseUltimateAuthWithAspNetCore();
app.UseAntiforgery();

app.MapUltimateAuthEndpoints();
app.MapUAuthHub();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddUltimateAuthRoutes(UAuthAssemblies.BlazorClient());
```

`MapUltimateAuthEndpoints()` maps the authentication endpoints.

`MapUAuthHub()` maps the Hub-specific entry endpoint.

Both registrations are used by the current UAuthHub samples.

The Hub also needs its login page and Blazor application lifecycle components.

### Choosing origins

For example:

| Application | URL |
|---|---|
| UAuthHub | `https://localhost:6112` |
| Standalone WASM | `https://localhost:6130` |
| Interactive WASM | `https://localhost:6132` |

Configure the Hub's allowed origins for the actual frontend applications it serves.

These localhost addresses are illustrative and must match your deployment.

## 5. Blazor Standalone WebAssembly

**Best for:** A browser-hosted application with a separate UAuthHub.

Standalone Blazor WASM runs in the browser. It does not host UltimateAuth Server or database persistence.

Authentication is handled by the remote UAuthHub.

### Client packages

```bash
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
```

### Configure services

In the standalone WASM application's `Program.cs`:

```csharp
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.Core.Domain;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Endpoints.BasePath = "https://localhost:6110/auth";
    options.Pkce.ReturnUrl = "https://localhost:6130/home";
    options.Login.AllowCredentialPost = true;
    options.Reauth.Behavior = ReauthBehavior.RaiseEvent;
});

await builder.Build().RunAsync();
```

The current standalone WASM sample explicitly configures `Login.AllowCredentialPost` and the PKCE return URL.

These are client-side settings. The UAuthHub must be configured independently.

### What the URLs mean

`Endpoints.BasePath` points to the remote authentication endpoints.

`Pkce.ReturnUrl` identifies the frontend destination used by the relevant PKCE flow.

The Hub must recognize the frontend's origin, and any return URL used in an authentication flow must be validated against the application's security policy.

### Root component

A standalone WASM application should initialize UltimateAuth through `UAuthApp`.

For example, its root application or routing component can contain:

```razor
@using CodeBeam.UltimateAuth.Client.Blazor

<UAuthApp UseBuiltInRouter="true" AppAssembly="typeof(Program).Assembly" DefaultLayout="typeof(Layout.MainLayout)" />
```

Include the UltimateAuth JavaScript asset in the application's HTML host page.

### Authentication modes

The default `BlazorWasm` profile resolves to `Hybrid` in the current mode resolver.

Do not manually force `PureOpaque` for a standalone browser client simply because a Blazor Server application uses it.

The transport and authentication lifecycle must match the client environment.

## 6. Blazor Web App — Interactive WebAssembly

**Best for:** A Blazor Web App with an ASP.NET Core host and a separate `.Client` project.

This scenario differs from standalone WASM because components may participate in both server-side rendering and browser-side interactivity.

The current UltimateAuth sample registers the Blazor client integration in **both projects**.

It also adds a separate ASP.NET Core compatibility registration in the host.

### Project structure

```text
MyApplication/
├── MyApplication/
│   ├── Program.cs
│   └── Components/
│       └── App.razor
│
└── MyApplication.Client/
    ├── Program.cs
    └── Pages/
```

The host serves the application.

The `.Client` project contains browser-executed components and services.

A remote UAuthHub manages authentication.

### 6.1 Host project packages

Install the Blazor client integration and ASP.NET Core compatibility package in the host:

```bash
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
dotnet add package CodeBeam.UltimateAuth.Client.AspNetCore
```

### 6.2 Host project services

In the host `Program.cs`:

```csharp
using CodeBeam.UltimateAuth.Client.AspNetCore;
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.Core.Domain;

builder.Services
    .AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Endpoints.BasePath = "https://localhost:6112/auth";
    options.Pkce.ReturnUrl = "https://localhost:6132/home";
    options.Login.AllowCredentialPost = true;
    options.Reauth.Behavior = ReauthBehavior.RaiseEvent;
});

builder.Services.AddUltimateAuthAspNetCoreCompatibility();
```

`AddUltimateAuthAspNetCoreCompatibility()` registers the compatibility authentication scheme and ASP.NET Core authorization services used by this hosting pattern.

It does **not** turn the host into an UltimateAuth authentication server.

It is also not equivalent to `AddUltimateAuthResourceApi()`.

### 6.3 Host project pipeline

```csharp
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MyApplication.Client._Imports).Assembly, UAuthAssemblies.BlazorClient().First());
```

Replace `MyApplication.Client._Imports` with the corresponding type from your client project.

The additional assemblies allow the host to discover routable components from the WASM client project and UltimateAuth's Blazor client assembly.

The sample does not register `AddUltimateAuthServer()` in this host because authentication is delegated to a separate UAuthHub.

### 6.4 Client project packages

Install the Blazor client adapter in the `.Client` project:

```bash
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
```

### 6.5 Client project services

In `MyApplication.Client/Program.cs`:

```csharp
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.Core.Domain;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Endpoints.BasePath = "https://localhost:6112/auth";
    options.Pkce.ReturnUrl = "https://localhost:6132/home";
    options.Login.AllowCredentialPost = true;
    options.Reauth.Behavior = ReauthBehavior.RaiseEvent;
});

await builder.Build().RunAsync();
```

The host and client configurations should agree on the remote UAuthHub URL and authentication flow settings.

### Why both registrations are necessary

The host and browser client have separate dependency injection containers.

Registering `AddUltimateAuthClientBlazor()` in the host does not automatically register those services inside the WebAssembly runtime.

Likewise, registering the client adapter only in the `.Client` project does not supply the services required during server-side integration.

The additional ASP.NET Core compatibility registration addresses the host's ASP.NET Core authentication and authorization integration requirements.

**Important:** This compatibility layer is not a replacement for server-side authorization of your protected business APIs.

## 7. Resource API — Separate Protected Backend

**Best for:** An API that needs UltimateAuth authentication validation but does not host the authentication system itself.

A Resource API can be an ASP.NET Core Web API, MVC application, or another supported server-side application.

It does not need to be a Blazor application.

### Architecture

```text
        Blazor Client
             │
       ┌─────┴─────┐
       ▼           ▼
    UAuthHub    Resource API
       ▲           │
       └───────────┘
       Authentication
         validation
```

The Resource API uses the configured UAuthHub for remote authentication and session validation behavior.

### Install the server integration package

```bash
dotnet add package CodeBeam.UltimateAuth.Server
```

### Configure services

In the Resource API's `Program.cs`:

```csharp
using CodeBeam.UltimateAuth.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddUltimateAuthResourceApi(options =>
{
    options.UAuthHubBaseUrl = "https://localhost:6112";
    options.AllowedClientOrigins.Add("https://localhost:6132");
    options.CorsPolicyName = "resourceApi";
});
```

`UAuthHubBaseUrl` is the base address of the authentication server.

Notice that this example uses the Hub origin without the `/auth` suffix, matching the current Resource API samples.

`AllowedClientOrigins` controls the browser origins permitted by the Resource API's CORS configuration.

### Configure the pipeline

```csharp
var app = builder.Build();

app.UseHttpsRedirection();

app.UseUltimateAuthResourceApiWithAspNetCore();

app.MapControllers();

app.Run();
```

`UseUltimateAuthResourceApiWithAspNetCore()` installs the Resource API integration middleware together with ASP.NET Core authentication and authorization middleware.

You do not need to register a local UltimateAuth Server or its persistence bundle in the Resource API.

### Protecting endpoints

Use ASP.NET Core authorization attributes for endpoints that require authentication:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult GetProducts()
    {
        return Ok(new[]
        {
            new { Id = 1, Name = "Example product" }
        });
    }
}
```

The authorization attribute is enforced by ASP.NET Core on the server.

For permission-specific rules, configure and apply the corresponding authorization policies.

Do not rely on Blazor UI authorization checks to protect Resource API endpoints.

### CORS configuration

The current Resource API integration registers CORS from the configured allowed origins.

For example:

```csharp
options.AllowedClientOrigins.Add("https://localhost:6132");
```

Only add origins that should be allowed to call your API from a browser.

CORS is a browser access control mechanism, not an authentication or authorization mechanism.

A request must still pass the applicable authentication and authorization checks.

### Resource API vs UAuthHub

These are different responsibilities.

| UAuthHub | Resource API |
|---|---|
| Hosts authentication flows | Hosts application resources |
| Manages authentication sessions | Validates remote authentication context |
| Uses authentication persistence | Does not require local authentication stores |
| Provides login and related endpoints | Provides application endpoints |
| Serves authentication clients | Serves authorized API consumers |

Use a Resource API when you want independent deployment of business endpoints without duplicating the authentication server.

## 8. Resource API with an EF Core-Backed UAuthHub

The Resource API does not need to use EF Core just because UAuthHub does.

For example:

```text
Interactive WASM
      │
      ├──────────────► Resource API
      │
      ▼
   UAuthHub
      │
      ▼
 EF Core Database
```

The Hub stores users, credentials, sessions, and related security state in its database.

The Resource API communicates with the Hub through the configured remote validation integration.

Its registration remains:

```csharp
builder.Services.AddUltimateAuthResourceApi(options =>
{
    options.UAuthHubBaseUrl = "https://localhost:6112";
    options.AllowedClientOrigins.Add("https://localhost:6132");
    options.CorsPolicyName = "resourceApi";
});
```

This separation allows the API and authentication infrastructure to be deployed, scaled, and maintained independently.

## 9. Calling a Resource API from Blazor

UltimateAuth provides a unified Resource Client for accessing protected application APIs.

Instead of configuring a separate HTTP client and implementing browser credential handling for ordinary resource requests, applications can use `IUAuthClient.Resource`.

This keeps authentication, identity operations, and application resource requests accessible through the same client SDK.

### Configure the Resource API

For a separate Resource API, configure its origin through the Blazor client registration:

```csharp
using CodeBeam.UltimateAuth.Client;

builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Endpoints.BasePath = "https://localhost:6112/auth";

    options.Resource.BaseAddress = "https://localhost:6122";
    options.Resource.Credentials = UAuthResourceCredentialsMode.Auto;
});
```

`Endpoints.BasePath` identifies the authentication server.

`Resource.BaseAddress` identifies the application Resource API.

These addresses can point to different applications.

If the Resource API shares the application's origin, `Resource.BaseAddress` can remain `null`.

### Call Protected Resources

Inject `IUAuthClient` into a Blazor component:

```razor
@using CodeBeam.UltimateAuth.Client
@inject IUAuthClient UAuthClient
```

Then retrieve application data:

```csharp
var result = await UAuthClient.Resource.GetAsync<List<Product>>("/api/products");

if (result.IsSuccess && result.Value is not null)
{
    products = result.Value;
}
```

The Resource Client also supports POST, PUT, PATCH, and DELETE operations.

### Application-Specific API Services

Applications can organize resource operations into small domain-specific services.

For example:

```csharp
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;

public sealed class ProductApiService(IUAuthClient client)
{
    public Task<UAuthResult<List<Product>>> GetAllAsync()
        => client.Resource.GetAsync<List<Product>>("/api/products");

    public Task<UAuthResult<Product>> CreateAsync(Product product)
        => client.Resource.PostAsync<Product>("/api/products",product);

    public Task<UAuthResult> DeleteAsync(int id)
        => client.Resource.DeleteAsync($"/api/products/{id}");
}
```

Register the application service:

```csharp
builder.Services.AddScoped<ProductApiService>();
```

The service uses UltimateAuth's transport and result contracts rather than implementing another HTTP abstraction.

### Interactive WebAssembly

In an Interactive WebAssembly application, configure the UltimateAuth Blazor client in each project where the Resource Client will be used.

The host and `.Client` project have separate dependency injection containers.

A shared application API service may be registered in both projects when components execute in both environments.

The Resource Client uses browser-based transport, including when invoked from an interactive Blazor Server component.

Browser interop must be available before making resource requests. Do not initiate browser-dependent resource requests during static prerendering.

### Standalone WebAssembly

Standalone Blazor WebAssembly applications use the same Resource Client API.

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Endpoints.BasePath = "https://localhost:6112/auth";
    options.Resource.BaseAddress = "https://localhost:6122";
});
```

Once configured, application components can call protected resources through `IUAuthClient.Resource`.

A separate application-specific `HttpClient` is not required for these requests.

### Browser Credentials and CORS

For cross-origin requests, the browser and receiving API must agree on credential handling.

The Resource Client supports `Auto`, `SameOrigin`, `Include`, and `Omit` credential policies.

For cross-origin cookie-based authentication, configure the appropriate credential policy and ensure that the Resource API supports the required CORS and cookie settings.

The Resource Client does not automatically make an API authenticated merely because the user has signed in through UAuthHub.

The receiving API must validate the authentication material associated with the configured authentication mode and enforce its authorization policies.

### Blazor Server and Direct Service Calls

Blazor Server applications can often access application services directly when those services are hosted in the same process.

For example:

```csharp
var products = await productService.GetAllAsync();
```

There is no need to introduce an HTTP request solely to use the Resource Client.

Use `IUAuthClient.Resource` when the application actually needs to communicate with a protected Resource API through the browser.

For server-to-server requests, use an appropriate server-side HTTP client and authentication mechanism.

### Learn More

See [Resource API Requests](/docs/client-sdk/resource-api-requests) for supported operations, result handling, credential configuration, security considerations, and request cancellation.

## 10. Authentication Modes and Client Profiles

UltimateAuth separates **client profile detection** from **authentication mode enforcement**.

The default resolver maps client profiles to authentication modes.

| Client profile | Default resolved mode |
|---|---|
| Blazor Server | `PureOpaque` |
| Blazor WASM | `Hybrid` |
| Web Server | `Hybrid` |
| MAUI | `Hybrid` |
| API | `PureJwt` |

This mapping describes the resolver's behavior, not a guarantee that every mode is currently fully supported by the server.

In particular, `PureJwt` and `SemiHybrid` have implementation limitations in the current release.

### Restrict allowed modes

On the authentication server:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.AllowedModes =
    [
        UAuthMode.PureOpaque,
        UAuthMode.Hybrid
    ];
});
```

`AllowedModes` is a guardrail.

It does not select a mode for every request.

The effective mode is resolved from the client profile and then checked against the server's allowed-mode policy.

For most applications, use the default mode resolution unless you have a specific integration requirement.

## 11. Server Options for Real Applications

Production configuration should reflect your application's security requirements.

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Login.MaxFailedAttempts = 5;
    options.Login.LockoutDuration = TimeSpan.FromMinutes(15);
    options.Identifiers.Behavior.AllowMultipleUsernames = true;
    options.UserProfile.EnableMultiProfile = false;
});
```

Server options also compose the Core-defined login, session, token, PKCE, and multi-tenant options.

Configure those through `AddUltimateAuthServer()`.

Do not independently configure Core options and assume the server will automatically use them.

### Configuration files

UltimateAuth also supports configuration through `appsettings.json`.

```json
{
  "UltimateAuth": {
    "Server": {
      "Login": {
        "MaxFailedAttempts": 5,
        "LockoutDuration": "00:15:00"
      }
    }
  }
}
```

The server options registration binds the `UltimateAuth:Server` configuration section.

Remember that effective mode defaults and mode-specific overrides are applied after the base options have been configured.

For detailed configuration behavior, see Server Options and Advanced Configuration.

## 12. Client IP Address & Reverse Proxy

UltimateAuth uses ASP.NET Core's resolved client IP address for device information and IP-based rate limiting. It does not directly trust `X-Forwarded-For` or `CF-Connecting-IP` headers.

If your application runs behind Caddy, Nginx, or another reverse proxy, configure ASP.NET Core to trust that proxy.

In `Program.cs`, before `builder.Build()`:

```csharp
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    // Replace with your actual Caddy or Nginx proxy IP.
    options.KnownProxies.Add(IPAddress.Parse("172.20.0.5"));
});
```

Then enable the middleware **before** UltimateAuth and HTTPS redirection:

```csharp
app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseUltimateAuthWithAspNetCore();
```

The example IP `172.20.0.5` represents a trusted reverse proxy, not the visitor's IP address. Replace it with your actual proxy IP. For Docker deployments, ensure the proxy address is stable or configure a narrowly scoped trusted network.

**Important:** Never trust arbitrary forwarded headers or clear the trusted proxy lists to accept all sources. If your application connects directly to Kestrel without a reverse proxy, no forwarded headers configuration is required.

For multi-proxy deployments, configure the complete trusted proxy chain and forwarding behavior according to your infrastructure.

## 13. Development and Production Differences

The samples intentionally prioritize visibility and repeatability.

A production application needs additional safeguards.

| Area | Sample or development | Production |
|---|---|---|
| Persistence | InMemory or local SQLite | Durable database |
| Seed accounts | Example users and passwords | Real provisioning |
| Database initialization | May reset sample data | Reviewed migrations |
| HTTPS | Local development certificate | Proper TLS termination |
| CORS | Localhost origins | Exact trusted origins |
| Signing keys | Development defaults | Managed, persistent keys |
| Diagnostics | Detailed debugging | Restrict sensitive diagnostics |
| Credential recovery | Development notifier | Real secure notification channel |
| Identifier verification | Development bypass | Proof-based verification |
| Secrets | Local settings | Secure configuration provider |

The sample projects include development-only implementations for credential reset notifications and identifier verification.

Those implementations demonstrate extension points and should not be treated as production verification systems.

### Credential recovery

If your application exposes credential recovery, configure a real implementation of the credential reset notifier.

Recovery secrets must be delivered through an appropriate channel and must not be exposed directly through anonymous API responses.

### Identifier verification

If your application supports verified email addresses, phone numbers, or other identifiers, use an implementation that verifies ownership through suitable proof.

Do not use a development bypass verifier in production.

### Signing keys

Configure persistent signing keys for authentication modes that issue JWTs.

Development signing key providers are not a substitute for production key management.

## 14. Deployment Checklist

Before deploying, verify the following:

- [ ] The selected architecture matches the actual client hosting model.
- [ ] The authentication server uses appropriate persistent storage.
- [ ] Database migrations have been reviewed and applied.
- [ ] Sample users and development-only verification services are disabled.
- [ ] Production signing keys and secrets are configured.
- [ ] HTTPS is enforced at the appropriate boundary.
- [ ] UAuthHub and Resource API origins are explicitly configured.
- [ ] Authentication endpoints and application endpoints are protected appropriately.
- [ ] Client and server endpoint URLs match the deployment.
- [ ] Session expiration, refresh, logout, and revocation have been tested.
- [ ] Protected resources enforce server-side authorization.
- [ ] Reverse proxy headers are configured correctly if applicable.
- [ ] Authentication state and persistence survive the expected application lifecycle.

## 15. Choosing Your Architecture

**Start with one server and one client. Connect more clients whenever you need them.**

UltimateAuth is built around two fundamental responsibilities:

- **UltimateAuth Server** — Manages authentication, users, credentials, sessions, authorization, and the security lifecycle.
- **UltimateAuth Client** — Integrates an application with the authentication system using the appropriate client profile and authentication flows.

Every application needs access to an authentication server, but that server does not have to be a separate application. It can be hosted within your existing ASP.NET Core application or deployed independently as UAuthHub.

### Single Application — The Simplest Setup

For a traditional ASP.NET Core or Blazor Server application, UltimateAuth Server and Client can run together.

```text
┌──────────────────────────────┐
│       Your Application       │
│                              │
│    UltimateAuth Client       │
│             │                │
│    UltimateAuth Server       │
│             │                │
│          Database            │
└──────────────────────────────┘
```

This approach is ideal when you want authentication without introducing another service or deployment.

### Multiple Applications — One Authentication Server

As your system grows, you can connect multiple client applications to the same UltimateAuth Server.

```text
     Blazor Server       Blazor WASM
           │                  │
           ├────────┬─────────┤
           │        │         │
       Razor Pages  MAUI   Other .NET Clients
           │        │         │
           └────────┼─────────┘
                    │
                    ▼
           UltimateAuth Server
                 UAuthHub
                    │
          ┌─────────┼─────────┐
          ▼         ▼         ▼
        Users    Sessions   Authorization
                    │
                    ▼
              Persistence
```

Each client can have its own application structure, hosting model, and authentication requirements while sharing the same authentication authority.

For example, a business platform might use Blazor Server for administration, Blazor WASM for its customer portal, MAUI for mobile access, and Razor Pages for an internal application.

**Adding another client does not require replacing the existing clients or creating another UltimateAuth Server.**

The same users, credentials, security policies, and session infrastructure can serve multiple applications, subject to the authentication modes and client integrations supported by the installed version.

### Distributed Applications — Separate Resource APIs

When your architecture includes independently deployed backend services, Resource APIs can integrate with the same authentication authority.

```text
         Multiple Client Applications
                     │
             ┌───────┴───────┐
             ▼               ▼
          UAuthHub       Resource APIs
             ▲               │
             └───────────────┘
             Authentication
               Validation
```

Resource APIs protect application resources without becoming additional authentication servers.

This lets you separate business services from authentication infrastructure while maintaining a shared security model.

### One Foundation, Different Scales

| Your scenario | Recommended approach |
|---|---|
| A simple Blazor Server application | Integrated Server + Client |
| A traditional ASP.NET Core application | Server hosted within the application |
| A standalone Blazor WASM application | Client + remote UAuthHub |
| An Interactive WASM application | Host + WASM Client + remote UAuthHub |
| Multiple web, desktop, or mobile applications | Multiple Clients + shared UAuthHub |
| Multiple backend services | Shared UAuthHub + Resource APIs |

**UltimateAuth does not require a complex architecture to get started, and it does not restrict you to a single client as your application grows.**

Start with the simplest deployment that meets your needs. Add new client applications, persistent storage, or separate Resource APIs as your requirements evolve.

The authentication foundation remains consistent.

## Next Steps

- [QuickStart](/docs/getting-started/quickstart) — Build the minimal in-memory application.
- [Ecosystem](/docs/getting-started/ecosystem) — Explore packages and supported integrations.
- [Server Options](/docs/configuration/server-options) — Configure server authentication and security behavior.
- [Client Options](/docs/configuration/client-options) — Configure client profiles, endpoints, refresh, and reauthentication.
- [Advanced Configuration](/docs/configuration/advanced-configuration) — Explore effective options, dependency injection, and extension points.