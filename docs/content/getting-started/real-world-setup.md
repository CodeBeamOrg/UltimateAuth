---
title: Real World Setup
order: 4
group: getting-started
---

# 🏗 Real-World Setup

The Quick Start uses an in-memory setup for simplicity.
In real-world applications, you should replace it with a persistent configuration as shown below.

In real applications, you will typically configure:

- A persistent database
- An appropriate client profile
- A suitable authentication mode

This guide shows how to set up UltimateAuth for real-world scenarios.

## 🗄️ Using Entity Framework Core
For production, you should use a persistent store. (In-memory provider is volatile and automatically resets on each restart.)

In this setup, you no longer need the `CodeBeam.UltimateAuth.InMemory.Bundle` package.

### Install Packages

```bash
dotnet add package CodeBeam.UltimateAuth.EntityFrameworkCore.Bundle
```

### Configure Services
```csharp
builder.Services
    .AddUltimateAuthServer()
    .AddUltimateAuthEntityFrameworkCore(db =>
    {
        db.UseSqlite("Data Source=uauth.db");
        // or UseSqlServer(...) / UseNpgsql(...) / UseMySql(...) etc.
    });

builder.Services
    .AddUltimateAuthClientBlazor();
```

### Database Migrations

UltimateAuth integrates with Entity Framework Core, but database migrations belong to your application.

UltimateAuth does not automatically create or apply migrations on your behalf. This keeps your database schema lifecycle under your control and allows migrations to follow the same deployment and review process as the rest of your application.

After configuring the Entity Framework Core provider, create the initial migration and update the database using either the .NET CLI or Visual Studio Package Manager Console.

#### Option A — .NET CLI

If you use the .NET CLI:

```bash
dotnet ef migrations add InitUAuth --context UAuthDbContext
dotnet ef database update --context UAuthDbContext
```

If the dotnet ef command is not available, install the EF Core CLI tool:

```bash
dotnet tool install --global dotnet-ef
```

Your project also needs the Entity Framework Core design package:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Design
```

#### Option B — Visual Studio Package Manager Console

If you use Visual Studio, you can perform the same operation from Tools → NuGet Package Manager → Package Manager Console:

```bash
Add-Migration InitUAuth -Context UAuthDbContext
Update-Database -Context UAuthDbContext
```

For Package Manager Console tooling, make sure the required EF Core tooling package is available:

```bash
Install-Package Microsoft.EntityFrameworkCore.Tools
```

### Who Owns the Migrations?

Your application does.

This is intentional. UltimateAuth provides the authentication and identity model through its Entity Framework Core integration, while your application remains responsible for managing the resulting database schema.

This means you can:

- review migrations before applying them,
- include UltimateAuth schema changes in your normal deployment process,
- control when database changes are applied,
- maintain migration history alongside your application,
- use the database provider and deployment strategy appropriate for your environment.

When upgrading UltimateAuth, review the release notes for persistence-related schema changes and create a new migration when required.

> **Tip:** Treat UltimateAuth model changes like any other Entity Framework Core model change: upgrade the package, create a migration, review the generated migration, and apply it through your normal deployment process.

## Configure Services With Options
UltimateAuth provides rich options for server and client service registration.
```csharp
builder.Services.AddUltimateAuthServer(o => {
    o.Diagnostics.EnableRefreshDetails = true;
    o.Login.MaxFailedAttempts = 4;
    o.Identifiers.AllowMultipleUsernames = true;
});
```

## Blazor Standalone WASM Setup
Blazor WASM applications run entirely on the client and cannot securely handle credentials.
For this reason, UltimateAuth uses a dedicated Auth server called **UAuthHub**.

WASM `Program.cs`:
```csharp
builder.Services.AddUltimateAuthClientBlazor(o =>
{
    o.Endpoints.BasePath = "https://localhost:6110/auth"; // UAuthHub URL
    o.Pkce.ReturnUrl = "https://localhost:6130/home"; // Your (WASM) application domain + return path
});
```

UAuthHub `Program.cs`:
```csharp
builder.Services.AddUltimateAuthServer()
    .AddUltimateAuthInMemory()
    .AddUAuthHub(o => o.AllowedClientOrigins.Add("https://localhost:6130")); // WASM application's URL
```

UAuthHub Pipeline Configuration
```csharp
app.MapUltimateAuthEndpoints();
app.MapUAuthHub();
```

## Blazor Web App Setup
A blazor web app contains two projects that includes host and client. You need to arrange them both.

In the host project:
```csharp
builder.Services.AddUltimateAuthClientBlazor(o =>
{
    o.Endpoints.BasePath = "https://localhost:6112/auth"; // UAuthHub URL
    o.Pkce.ReturnUrl = "https://localhost:6132/home"; // Current application domain + path
});

// In pipeline configuration
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(UAuthAssemblies.BlazorClient().First());
```

In the client project:
```csharp
builder.Services.AddUltimateAuthClientBlazor(o =>
{
    o.Endpoints.BasePath = "https://localhost:6112/auth"; // UAuthHub URL
    o.Pkce.ReturnUrl = "https://localhost:6132/home"; // Current application domain + path
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Optional if you use external API calls in your client project.
builder.Services.AddHttpClient("resourceApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:6122");
});
```

> If you want to use embedded UAuthHub in host project, you can register server services as shown in quickstart.

> ℹ️ UltimateAuth automatically selects the appropriate authentication mode (PureOpaque, Hybrid, etc.) based on the client type.

## ResourceApi Setup
You may want to secure your custom API with UltimateAuth. UltimateAuth provides a lightweight option for this case. (ResourceApi doesn't have to be a blazor application, it can be any server-side project like MVC.)

ResourceApi's `Program.cs`
```csharp
builder.Services.AddUltimateAuthResourceApi(o =>
    {
        o.UAuthHubBaseUrl = "https://localhost:6110";
        o.AllowedClientOrigins.Add("https://localhost:6130");
    });
```

Configure pipeline:
```csharp
app.UseUltimateAuthResourceApiWithAspNetCore();
```

Notes:
- ResourceApi should connect with an UAuthHub, not a pure-server. Make sure `.AddUAuthHub()` after calling `builder.Services.AddUltimateAuthServer()`.
- UltimateAuth automatically configures CORS based on the provided origins.

Use ResourceApi when:

- You have a separate backend API
- You want to validate sessions or tokens externally
- Your API is not hosting UltimateAuth directly

## 🧠 How to Think About Setup

In UltimateAuth:

- The **Server** manages authentication flows and sessions  
- The **Client** interacts through flows (not tokens directly)  
- The **Storage layer** (InMemory / EF Core) defines persistence  
- The **Application type** determines runtime behavior  

👉 You configure the system once, and UltimateAuth adapts automatically.
