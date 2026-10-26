---
title: Resource API Requests
order: 8
group: client
---

# Resource API Requests

**Authenticate once. Access your application resources through the same unified client.**

Authentication is only the beginning of an application.

Once a user signs in, the application still needs to retrieve data, create records, update resources, and communicate with protected APIs.

Traditionally, this introduces another layer of integration work.

Developers configure authentication through one library, then build a separate HTTP client to access their application APIs. They must handle credentials, browser restrictions, response parsing, errors, and differences between hosting environments.

The result is often two separate programming models: one for authentication and another for everything that comes after it.

**UltimateAuth brings these operations together through `IUAuthClient.Resource`.**

Instead of introducing another client abstraction, applications can use the same UltimateAuth entry point for authentication, identity management, and protected resource requests.

```csharp
var result = await UAuthClient.Resource
    .GetAsync<List<Product>>("/api/products");
```

No custom JavaScript wrapper. No application-specific response type. No separate HTTP client configuration for ordinary authenticated resource requests.

The goal is straightforward: **make accessing protected application resources feel like a natural continuation of authentication.**

---

## 1. The Problem: Authentication Ends, Integration Work Begins

Consider a typical application with three components:

```text
Blazor Application
        |
        +----> UAuthHub
        |       Authentication
        |       Sessions
        |       Identity
        |
        +----> Resource API
                Products
                Orders
                Application Data
```

UltimateAuth handles authentication and identity operations.

But the application also needs to communicate with its Resource API.

Without a unified resource client, developers typically need to:

1. Configure an additional HTTP client.
2. Determine how authentication credentials should accompany requests.
3. Handle browser credential policies and cross-origin restrictions.
4. Maintain device and client profile metadata where applicable.
5. Deserialize successful responses.
6. Translate unsuccessful HTTP responses into application-friendly results.
7. Implement similar request handling across multiple components.

These responsibilities are not necessarily complicated individually.

The problem is their repetition.

A framework that simplifies authentication should not require developers to rebuild the surrounding integration layer every time they need application data.

### A Unified Client Model

UltimateAuth already exposes specialized operations through `IUAuthClient`:

```csharp
UAuthClient.Flows
UAuthClient.Users
UAuthClient.Sessions
UAuthClient.Credentials
UAuthClient.Authorization
```

Resource requests follow the same model:

```csharp
UAuthClient.Resource
```

This preserves a consistent entry point without mixing application-specific business operations into UltimateAuth's authentication services.

UltimateAuth does not need to know what a `Product`, `Order`, or `Invoice` represents.

It only needs to provide a predictable way to communicate with the API that owns those resources.

---

## 2. Getting Started

The Resource API client is available through the UltimateAuth Client SDK.

For Blazor applications, register the Blazor client integration:

```csharp
builder.Services.AddUltimateAuthClientBlazor();
```

The registration includes the shared client services and the browser-specific resource transport.

You do not need to register a separate Resource Client.

### Inject IUAuthClient

In a Blazor component:

```razor
@using CodeBeam.UltimateAuth.Client
@inject IUAuthClient UAuthClient
```

Then call a protected application endpoint:

```csharp
var result = await UAuthClient.Resource.GetAsync<List<Product>>("/api/products");

if (result.IsSuccess && result.Value is not null)
{
    products = result.Value;
}
```

The response uses the same structured result model found throughout UltimateAuth.

### What Happens Behind the Call?

For the Blazor integration, the request follows this path:

```text
Blazor Component
       |
       v
IUAuthClient.Resource
       |
       v
Resource Client
       |
       v
UltimateAuth Browser Transport
       |
       v
Protected Resource API
       |
       v
UAuthResult<T>
```

The browser transport handles request construction and browser-specific behavior.

The Resource API remains responsible for validating authentication and enforcing authorization.

**A successful client request does not bypass server-side security.**

---

## 3. Supported HTTP Operations

The Resource Client provides five standard HTTP operations.

| Method | Purpose | Return type |
|---|---|---|
| `GetAsync<T>` | Retrieve a resource | `UAuthResult<T>` |
| `PostAsync<T>` | Create or submit a resource | `UAuthResult<T>` |
| `PutAsync<T>` | Replace or update a resource | `UAuthResult<T>` |
| `PatchAsync<T>` | Partially update a resource | `UAuthResult<T>` |
| `DeleteAsync` | Delete a resource | `UAuthResult` |

All methods accept a relative endpoint and support cancellation.

### GET

Retrieve application data:

```csharp
var result = await UAuthClient.Resource.GetAsync<List<Product>>("/api/products");

if (result.IsSuccess && result.Value is not null)
{
    products = result.Value;
}
```

Retrieve a single resource:

```csharp
var result = await UAuthClient.Resource.GetAsync<Product>($"/api/products/{productId}");
```

### POST

Submit a new resource:

```csharp
var product = new Product
{
    Name = "Example Product",
    Price = 29.99m
};

var result = await UAuthClient.Resource.PostAsync<Product>("/api/products", product);

if (result.IsSuccess)
{
    // Product created successfully.
}
```

The request payload is serialized for transport.

### PUT

Update a resource:

```csharp
var result = await UAuthClient.Resource
    .PutAsync<Product>($"/api/products/{product.Id}", product);
```

### PATCH

Submit a partial update:

```csharp
var changes = new
{
    Price = 24.99m
};

var result = await UAuthClient.Resource
    .PatchAsync<Product>($"/api/products/{productId}", changes);
```

The Resource Client sends the request payload.

The receiving API determines which partial-update formats and operations it supports.

### DELETE

Delete a resource:

```csharp
var result = await UAuthClient.Resource.DeleteAsync($"/api/products/{productId}");

if (result.IsSuccess)
{
    // Resource deleted successfully.
}
```

Unlike the other methods, `DeleteAsync` returns the non-generic `UAuthResult` because the operation does not require a response value.

---

## 4. Consistent Results with UAuthResult

A common source of unnecessary complexity is response handling.

One API client returns `HttpResponseMessage`.

Another returns a custom response wrapper.

A third throws exceptions for ordinary HTTP failures.

Applications then need different handling logic for operations that are conceptually similar.

UltimateAuth uses `UAuthResult<T>` to provide a consistent response contract.

```csharp
var result = await UAuthClient.Resource.GetAsync<Product>("/api/products/42");

if (result.IsSuccess)
{
    var product = result.Value;
}
else
{
    var status = result.Status;
    var problem = result.Problem;
}
```

### Result Properties

| Property | Description |
|---|---|
| `IsSuccess` | Indicates whether the HTTP operation succeeded |
| `Status` | HTTP response status code |
| `Value` | Deserialized response value for successful generic operations |
| `Problem` | Structured problem information when available |

A failed HTTP response does not automatically mean a transport exception.

For example, an API may return:

- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`

These responses can be represented through `UAuthResult<T>`.

The application can inspect the status and decide how to respond.

### Structured Problems

When an unsuccessful response contains a compatible UltimateAuth problem payload, the Resource Client attempts to deserialize it into `UAuthProblem`.

```csharp
if (!result.IsSuccess)
{
    if (result.Problem is not null)
    {
        // Handle the structured API problem.
    }
    else
    {
        // Handle the HTTP status without a structured problem.
    }
}
```

Not every API returns a compatible problem body.

Therefore, `Problem` may be `null` even when the request failed.

### Transport Failures Are Different

Network failures, protocol errors, and request cancellation are different from ordinary unsuccessful HTTP responses.

For example, an unreachable API does not necessarily produce an HTTP status code.

Applications should distinguish transport exceptions from valid HTTP responses indicating failure.

**`UAuthResult<T>` standardizes HTTP response handling; it does not guarantee that every network or runtime failure becomes a result object.**

---

## 5. Resource API Configuration

Resource API configuration is part of `UAuthClientOptions`.

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Resource.BaseAddress = "https://api.example.com";
    options.Resource.Credentials = UAuthResourceCredentialsMode.Auto;
});
```

The `Resource` options are separate from the authentication endpoint configuration.

This distinction allows applications to authenticate against one server while accessing resources hosted elsewhere.

### BaseAddress

`BaseAddress` identifies the Resource API origin.

```csharp
options.Resource.BaseAddress = "https://api.example.com";
```

If `BaseAddress` is not configured, requests use the application's current origin.

```csharp
builder.Services.AddUltimateAuthClientBlazor();
```

A call such as:

```csharp
await UAuthClient.Resource.GetAsync<List<Product>>("/api/products");
```

then targets the application's origin.

### BaseAddress Validation

UltimateAuth validates the configured Resource API address.

The address must:

- Be an absolute HTTP or HTTPS URL.
- Use HTTPS, except for loopback development addresses.
- Represent an origin without an additional path.
- Exclude embedded credentials, query strings, and fragments.

Valid examples:

```text
https://api.example.com
https://localhost:7001
```

Invalid examples:

```text
https://api.example.com/api
https://api.example.com?version=1
http://api.example.com
```

Keep endpoint paths in individual Resource Client calls:

```csharp
options.Resource.BaseAddress = "https://api.example.com";
```

```csharp
await UAuthClient.Resource.GetAsync<List<Product>>("/api/products");
```

This separates deployment configuration from application resource paths.

---

## 6. Browser Credential Handling

Browser-based applications do not have unrestricted control over authentication credentials.

Cookies, origins, cross-origin requests, and browser security policies all influence how requests are sent.

UltimateAuth exposes credential handling through `UAuthResourceCredentialsMode`.

| Mode | Behavior |
|---|---|
| `Auto` | Let UltimateAuth select the appropriate browser credential policy |
| `SameOrigin` | Send browser credentials only for same-origin requests |
| `Include` | Include browser credentials for the configured Resource API origin |
| `Omit` | Do not send browser credentials |

The default is:

```csharp
options.Resource.Credentials = UAuthResourceCredentialsMode.Auto;
```

### Same-Origin Resource API

When the application and Resource API share an origin:

```text
https://app.example.com
https://app.example.com/api/products
```

you can use:

```csharp
options.Resource.Credentials = UAuthResourceCredentialsMode.SameOrigin;
```

This is appropriate when the Resource API relies on browser credentials available to the application origin.

### Separate Resource API

Consider:

```text
Application:  https://app.example.com
Resource API: https://api.example.com
```

For a cross-origin API that requires browser cookies, configure:

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Resource.BaseAddress = "https://api.example.com";
    options.Resource.Credentials = UAuthResourceCredentialsMode.Include;
});
```

The receiving API must also support the relevant browser security requirements.

For credentialed cross-origin requests, this typically includes:

- Explicitly permitted origins.
- Appropriate CORS configuration.
- Compatible cookie attributes and browser policies.
- Server-side authentication and authorization.

Configuring `Include` does not override browser restrictions or guarantee that a cookie is available for the destination.

### When to Use Omit

Some resource endpoints do not require browser cookies.

For those cases:

```csharp
options.Resource.Credentials = UAuthResourceCredentialsMode.Omit;
```

This prevents browser credentials from being included in the resource request.

The API must still enforce whichever authentication mechanism it requires.

**Credential transmission is not the same as authorization.**

---

## 7. Separate UAuthHub and Resource API

UltimateAuth supports architectures where authentication and application resources are hosted independently.

For example:

```text
                   Blazor Application
                          |
             +------------+------------+
             |                         |
             v                         v
          UAuthHub                Resource API
     auth.example.com             api.example.com
             |                         |
             v                         v
      Authentication             Application Data
      Sessions                   Business Operations
      Identity                   Authorization
```

The client can configure both destinations independently:

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Endpoints.BasePath = "https://auth.example.com/auth";

    options.Resource.BaseAddress = "https://api.example.com";

    options.Resource.Credentials = UAuthResourceCredentialsMode.Auto;
});
```

Authentication operations use the configured authentication endpoints.

Resource operations use the configured Resource API origin.

```csharp
var profile = await UAuthClient.Users.GetMeAsync();

var products = await UAuthClient.Resource.GetAsync<List<Product>>("/api/products");
```

Both operations are accessed through `IUAuthClient`, but they serve different purposes.

### Authentication Does Not Automatically Protect an API

Configuring a Resource API address does not automatically enable authentication or authorization on that API.

The Resource API must independently validate incoming requests and enforce its access rules.

For separate deployments, the Resource API must also be configured to accept and validate the authentication material associated with the selected UltimateAuth authentication mode.

The Resource Client simplifies communication.

It does not replace the receiving server's security responsibilities.

---

## 8. Device and Client Profile Metadata

UltimateAuth maintains a distinction between user identity, device identity, and client environment.

For browser resource requests, the transport can propagate UltimateAuth metadata, including:

```text
X-UDID
X-UAuth-ClientProfile
X-Requested-With
```

This keeps resource requests consistent with the UltimateAuth client environment without requiring every application component to construct these headers manually.

For example:

```csharp
await UAuthClient.Resource.GetAsync<List<Product>>("/api/products");
```

does not require the developer to manually retrieve and attach the device identifier.

### Metadata Is Not Proof of Identity

Device identifiers and client profile values describe request context.

They are not independent authentication credentials.

A receiving API must not grant access solely because a request declares a particular device identifier or client profile.

The server remains responsible for validating the user's authentication state and permissions.

For more information, see [Client Profiles](/docs/fundamentals/client-profiles).

---

## 9. Browser Transport and Security

The Blazor Resource Client uses UltimateAuth's browser transport.

This allows UltimateAuth to apply consistent request behavior without requiring developers to write a separate JavaScript module for their application.

### Origin Validation

Resource requests are constrained by the configured Resource API origin.

The transport validates request destinations rather than allowing arbitrary URLs to bypass the configured resource boundary.

This helps prevent application code from unintentionally sending credentials or UltimateAuth request metadata to unrelated destinations.

### Redirect Protection

Resource requests use redirect protection.

A request should not silently follow a redirect to an unexpected destination while carrying authentication-related context.

Applications should handle unexpected redirects as transport failures rather than assuming the request succeeded.

### Request Cancellation

Resource operations support `CancellationToken`.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

var result = await UAuthClient.Resource.GetAsync<List<Product>>("/api/products", cts.Token);
```

The Blazor transport coordinates cancellation with the underlying browser request.

Cancellation is particularly useful when a user navigates away from a page or starts another operation before the previous request completes.

### Browser Security Still Applies

The Resource Client does not disable browser security mechanisms.

Cross-origin requests remain subject to CORS and browser credential policies.

Likewise, origin validation and redirect protection do not replace:

- Authentication validation.
- Authorization policies.
- CSRF protection where applicable.
- Input validation.
- Server-side resource ownership checks.

These responsibilities remain part of the receiving API's security model.

---

## 10. Blazor Server vs. Blazor WebAssembly

One of UltimateAuth's design goals is to provide a consistent client API across supported application environments.

However, different hosting models have different execution characteristics.

### Blazor WebAssembly

Blazor WebAssembly executes application code in the browser.

It cannot directly invoke server-side application services or access a server database.

A typical application architecture is:

```text
Blazor WebAssembly
        |
        v
IUAuthClient.Resource
        |
        v
Resource API
        |
        v
Application Services
        |
        v
Database
```

The Resource Client provides a natural way to access protected application data from this environment.

### Blazor Server

Blazor Server executes component logic on the server.

When the application service is available in the same process, a direct service call is often more appropriate.

```text
Blazor Server Component
        |
        v
Application Service
        |
        v
Database
```

For example:

```csharp
var products = await productService.GetAllAsync();
```

There is no requirement to introduce an HTTP request simply to use the Resource Client.

However, when a Blazor Server application needs to communicate with a separate protected Resource API, it can use:

```csharp
var result = await UAuthClient.Resource.GetAsync<List<Product>>("/api/products");
```

**Important:** The current Blazor Resource Client uses browser-based transport, including in Blazor Server applications.

The HTTP request is initiated by the user's browser, not by a server-side `HttpClient`.

For server-to-server communication, use an appropriate server-side HTTP integration and authentication mechanism.

### The Same Client API, Different Application Architectures

UltimateAuth aims to make client integration consistent without forcing every application into the same deployment model.

The goal is not to replace direct application service calls.

The goal is to remove unnecessary integration work whenever an HTTP resource request is actually needed.

---

## 11. Handling Unauthorized Responses

An authenticated session may expire or become invalid while an application is running.

When a Resource API returns `401 Unauthorized`, the Resource Client exposes the unsuccessful HTTP response through its standard result contract.

```csharp
var result = await UAuthClient.Resource.GetAsync<List<Product>>("/api/products");

if (!result.IsSuccess && result.Status == 401)
{
    // Authentication may need to be renewed.
}
```

### Automatic Refresh and Retry

The current Resource Client does not automatically refresh authentication and retry a request after receiving a `401` response.

This behavior is intentionally outside the initial Resource API implementation.

Applications should not assume that an unsuccessful request has been retried.

Authentication renewal and retry policies require careful handling, particularly for requests that modify application state.

Future improvements may provide additional coordination between resource requests and authentication lifecycle management.

Until then, applications should handle unauthorized responses according to their authentication flow and UI requirements.

---

## 12. Organizing Application Resource Services

Although Resource Client operations can be called directly from components, larger applications often benefit from small, domain-specific services.

For example:

```csharp
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;

public sealed class ProductApiService(IUAuthClient client)
{
    public Task<UAuthResult<List<Product>>> GetAllAsync()
        => client.Resource.GetAsync<List<Product>>("/api/products");

    public Task<UAuthResult<Product>> GetAsync(int id)
        => client.Resource.GetAsync<Product>($"/api/products/{id}");

    public Task<UAuthResult<Product>> CreateAsync(Product product)
        => client.Resource.PostAsync<Product>("/api/products", product);

    public Task<UAuthResult<Product>> UpdateAsync(int id, Product product)
        => client.Resource.PutAsync<Product>($"/api/products/{id}", product);

    public Task<UAuthResult> DeleteAsync(int id)
        => client.Resource.DeleteAsync($"/api/products/{id}");
}
```

This approach keeps application-specific endpoints and operations in one place.

The service does not need to implement:

- A custom HTTP transport.
- Browser credential handling.
- Device metadata propagation.
- JSON response deserialization.
- A separate response wrapper.

Those responsibilities remain with UltimateAuth.

### Keep the Standard Result

Notice that `ProductApiService` returns `UAuthResult<T>` directly.

There is no need to create another response type merely to wrap the same status, value, and problem information.

This keeps the application aligned with the UltimateAuth SDK.

### Application Services Still Own Business Logic

The Resource Client does not attempt to understand application-specific business rules.

A `ProductApiService` can organize product-related operations, but the Resource API remains responsible for validating product data, enforcing ownership, and authorizing changes.

This separation keeps UltimateAuth focused on the infrastructure it can provide consistently.

---

## 13. Design Philosophy

The Resource Client follows the same principle as the rest of UltimateAuth:

**Complexity should be handled by the framework when it is common, predictable, and independent of application business logic.**

Developers should not need to repeatedly solve browser transport, credential configuration, device metadata, and response handling simply because their application has its own API.

At the same time, a framework should not hide important security boundaries or take ownership of application-specific decisions.

UltimateAuth therefore provides:

- A unified client entry point.
- Familiar HTTP operations.
- Consistent result contracts.
- Configurable resource origins and credential policies.
- Browser-aware transport behavior.
- Explicit server-side security responsibilities.

The result is a simpler developer experience without requiring applications to abandon their existing architecture.

```csharp
// Authentication
await UAuthClient.Flows.RefreshAsync();

// Identity
var user = await UAuthClient.Users.GetMeAsync();

// Sessions
var sessions = await UAuthClient.Sessions.GetMyChainsAsync(
    new PageRequest
    {
        PageNumber = 1,
        PageSize = 10
    });

// Application resources
var products = await UAuthClient.Resource.GetAsync<List<Product>>("/api/products");
```

Different responsibilities. One consistent client model.

**UltimateAuth does not stop at authentication. It helps applications use authentication without rebuilding the integration layer around it.**

---

## Related Documentation

- [Client SDK Overview](/docs/client-sdk/overview) — Understand the unified client architecture.
- [Client Options](/docs/configuration/client-options) — Configure client profiles, authentication endpoints, and resource options.
- [Authentication](/docs/client-sdk/authentication) — Work with authentication flows and session renewal.
- [Client Profiles](/docs/fundamentals/client-profiles) — Understand runtime-specific authentication behavior.
- [Real-World Setup](/docs/getting-started/real-world-setup) — Configure Blazor applications, UAuthHub, and separate Resource APIs.