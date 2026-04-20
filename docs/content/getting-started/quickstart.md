---
title: QuickStart
order: 2
group: getting-started
---

# ⚡ Quick Start

In this guide, you will set up UltimateAuth in a few minutes and perform your **first login**.

---

## 1. Create a Project

Start by creating a new Blazor app:

```bash
dotnet new blazorserver -n UltimateAuthDemo
cd UltimateAuthDemo
```

## 2. Install Packages

Install the required UltimateAuth packages:

```csharp
dotnet add package CodeBeam.UltimateAuth.Server
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
dotnet add package CodeBeam.UltimateAuth.InMemory.Bundle
```

## 3. Configure Services

Update your Program.cs:
```csharp
builder.Services
    .AddUltimateAuthServer()
    .AddUltimateAuthInMemory();

builder.Services
    .AddUltimateAuthClientBlazor();
```

## 4. Configure Middleware
In `Program.cs`
```csharp
app.UseUltimateAuthWithAspNetCore();
app.MapUltimateAuthEndpoints();
```

## 5. Enable Blazor Integration
In `Program.cs`
```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode() // or webassembly (depends on your application type)
    .AddUltimateAuthRoutes(UAuthAssemblies.BlazorClient());
```

## 6. Add UAuth Script
Add this to `App.razor` or `index.html`:

```csharp
<script src="_content/CodeBeam.UltimateAuth.Client.Blazor/uauth.min.js"></script>
```

## 7. Configure Application Lifecycle
Replace `Routes.razor` with this code:
```csharp
<UAuthApp UseBuiltInRouter="true" AppAssembly="typeof(Program).Assembly" DefaultLayout="typeof(Layout.MainLayout)" />
```

## 8. Recommended Setup (Optional)
Add these for better experience:

For login page (Use this only once in your application)
```csharp
@attribute [UAuthLoginPage]
```

For protected pages
```csharp
@attribute [UAuthAuthorize]
```

For any page that you use UltimateAuth features like AuthState etc.
```csharp
@inherits UAuthFlowPageBase
```

## 9. Seed Data For QuickStart (Optional)
This code creates admin and user users with same password and admin role.

For in memory
```csharp
builder.Services.AddUltimateAuthSampleSeed();
```

For entity framework core:
```csharp
builder.Services.AddScopedUltimateAuthSampleSeed();
```

In pipeline configuration
```csharp
if (app.Environment.IsDevelopment())
{
    await app.SeedUltimateAuthAsync();
}
```

## 10. Perform Your First Login
Example using IUAuthClient:
```csharp
[Inject] IUAuthClient UAuthClient { get; set; } = null!;

private async Task Login()
{
    await UAuthClient.Flows.LoginAsync(new LoginRequest
    {
        Identifier = "admin",
        Secret = "admin"
    });
}
```

## 🎉 That’s It
You now have a working authentication system with:

- Session-based authentication
- Automatic client detection
- Built-in login flow
- Secure session handling

## What Just Happened?

When you logged in:

- A session (with root and chain) was created on the server,
- Your client received an authentication grant (cookie or token),
- UltimateAuth established your auth state automatically.

👉 You didn’t manage cookies, tokens, or redirects manually.

## Next Steps
Discover the setup for real world applications with entity framework core.
