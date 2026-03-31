# 🔄 Request Lifecycle
This section explains what happens when a request enters UltimateAuth.

👉 From the moment an HTTP request arrives  
👉 Until authentication state is established or a flow is executed  

<br>

## 🧠 Two Types of Requests
UltimateAuth processes requests in two different ways:

### 1. Passive Requests
Regular application requests (page load, API call)

### 2. Active Flow Requests
Authentication flows (login, refresh, logout)

👉 Both share the same foundation, but diverge at the flow level.

<br>

## 🧩 Middleware Pipeline
Every request passes through the UltimateAuth middleware pipeline:

```
Tenant → Session Resolution → (Validation) → User Resolution
```

### 🏢 Tenant Resolution
The system determines the tenant:

- Multi-tenant → resolved via `ITenantResolver`
- Single-tenant → default context applied

👉 If tenant cannot be resolved → request fails early

### 🔐 Session Resolution
The system attempts to extract a session:

- From headers, cookies, or tokens  
- Converted into a `SessionContext`  

```
SessionId → SessionContext
```

👉 If no session is found → request becomes anonymous

### ✔ Session Validation (Resource APIs)
For API scenarios:

- Session is validated immediately  
- Device context is considered  
- A validation result is attached to the request  

👉 This enables stateless or semi-stateful validation

### 👤 User Resolution
The system resolves the current user:

- Based on validated session  
- Using `IUserAccessor`  

👉 This produces the final user context

<br>

## 🔄 Passive Request Flow
For normal requests:

```
Request  
→ Middleware pipeline  
→ Session resolved  
→ User resolved  
→ Application executes  
```

👉 No flow execution happens

<br>

## 🔐 Active Flow Requests
For auth endpoints (login, refresh, etc.):

The lifecycle continues beyond middleware.

### Step 1: Flow Detection
```
Endpoint → FlowType
```

### Step 2: Context Creation
An `AuthFlowContext` is created.

It includes:

- Client profile  
- Effective mode  
- Tenant  
- Device  
- Session  
- Response configuration  

👉 This defines the execution environment

### Step 3: Flow Execution
```
AuthFlowContext → Flow Service → Orchestrator → Authority
```

### Step 4: State Mutation
Depending on the flow:

- Session may be created, updated, or revoked  
- Tokens may be issued  
- Security state may change  

### Step 5: Response Generation
The system writes the response:

- SessionId  
- Access token  
- Refresh token  
- Redirect (if needed)

<br>

## 🔁 Example: Login Request
```
HTTP Request  
→ Tenant resolved  
→ Session resolved (anonymous)  
→ Flow detected (Login)  
→ AuthFlowContext created  
→ Credentials validated  
→ Session created  
→ Tokens issued  
→ Response returned  
```

<br>

## 🔐 Flow Execution Boundary
Authentication flows are only executed for endpoints explicitly marked with flow metadata.

- Regular requests do not create an AuthFlowContext  
- AuthFlowContext is only created during flow execution  

👉 This ensures that authentication logic does not interfere with normal application behavior.

<br>

## 🧠 Mental Model
👉 Middleware prepares the request  
👉 Flows change the state
