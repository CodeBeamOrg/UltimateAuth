---
title: Define Authorization Policies
order: 3
group: how-to-guides
---

# Define Authorization Policies

Learn how to separate authorization requirements from business workflows using reusable UltimateAuth policies.

Roles and permissions are essential building blocks of authorization, but **having permission to perform an action does not always mean an actor may perform it in every situation**.

In this guide, you'll learn how to:

- Recognize when role- and permission-based authorization needs contextual rules.
- Move authorization conditions out of application services.
- Write and register an `IAccessPolicy` using trusted `AccessContext` data.
- Combine multiple policies without expanding your business workflow.
- Keep domain validation separate from authorization.

## From Roles to Policies

Traditionally, applications manage authorization through **roles**. Users are assigned roles such as `Admin`, `Manager`, or `Member`, and access is granted based on those assignments.

This works well for simple applications, but roles alone become difficult to manage as requirements grow. Creating a new role for every possible combination of capabilities can quickly lead to **role explosion**.

**Permissions** solve this problem by separating capabilities from roles. Instead of creating a new role for every operation, applications define permissions such as `payments.create.admin` or `payments.approve.admin` and assign them to the appropriate roles.

But permissions answer only part of the question.

A user may have permission to approve payments without being allowed to approve **every payment**.

For example, a financial application may require that:

- Users cannot approve payments they created themselves.
- Payments above a certain amount require a `SeniorApprover` role.
- All applicable authorization requirements must be satisfied before approval.

These conditions depend on the actor and the specific operation being performed. Creating additional roles or permissions for every possible situation would make authorization increasingly difficult to maintain.

You could enforce these rules directly inside application services. However, doing so mixes authorization decisions with business operations and makes the same security requirements harder to reuse.

**Policy-based authorization provides a cleaner approach.**

With UltimateAuth, you can define application-specific authorization requirements as independent policies, evaluate them through a shared `AccessContext`, and compose multiple policies without embedding their implementation details in business services.

The result is a clear separation of responsibilities:

**Roles organize users. Permissions grant capabilities. Policies enforce contextual authorization rules.**

## 1. The Problem: Authorization Inside a Service

Imagine a payment application. Employees can create payments, and authorized employees can approve them. To reduce fraud, the application enforces **Separation of Duties**: an employee must not approve a payment they created.

For large payments, an additional requirement applies: payments above $10,000 require the `SeniorApprover` role.

An initial implementation might look like this:

```csharp
public async Task ApprovePaymentAsync(Guid paymentId, CancellationToken ct)
{
    var payment = await repository.GetAsync(paymentId, ct);

    if (!currentUser.HasPermission("payments.approve.admin"))
        throw new UnauthorizedAccessException();

    if (payment.CreatedBy == currentUser.UserKey)
        throw new UnauthorizedAccessException();

    if (payment.Amount > 10_000m && !currentUser.HasRole("SeniorApprover"))
        throw new UnauthorizedAccessException();

    if (payment.Status != PaymentStatus.Pending)
        throw new InvalidOperationException("Payment is not pending.");

    payment.Approve();
    await repository.SaveAsync(payment, ct);
}
```

This is **illustrative application code**, not an UltimateAuth API example. It can work, but it combines three responsibilities: permission checking, contextual authorization, and payment processing.

As the application grows, similar checks may appear in API endpoints, administrative workflows, and other application services. Repeating them makes authorization harder to review and maintain.

## 2. Why Permissions Are Not Enough

Roles organize users into responsibilities. Permissions describe the actions they are generally allowed to perform. Defining a separate role for every combination of circumstances leads to **role explosion**, while adding more permissions still does not answer questions about a particular payment.

Consider two employees who both have `payments.approve.admin`:

| Actor | Payment | Result | Reason |
|---|---|---|---|
| Approver | Created by someone else, $500 | Allow | Requirements satisfied |
| Approver | Created by themselves, $500 | Deny | Separation of Duties |
| Approver | Created by someone else, $15,000 | Deny | Missing `SeniorApprover` role |
| SeniorApprover | Created by someone else, $15,000 | Allow | Requirements satisfied |
| SeniorApprover | Created by themselves, $15,000 | Deny | Separation of Duties still applies |

**Permission asks:** *May this actor approve payments?*

**Policy asks:** *May this actor approve this payment under these conditions?*

You could implement the latter inside a service. UltimateAuth's recommended pattern is to keep **authorization decisions in policies** and **business operations in application services**. This is *Policy-Based Authorization*, applying the broader *Separation of Concerns* principle.

## 3. Define the Application Action

UltimateAuth actions use three or four segments and end with an explicit scope: `self`, `admin`, or `anonymous`.

Create `Authorization/PaymentActions.cs`:

```csharp
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;

public static class PaymentActions
{
    public static readonly string Approve = UAuthActions.Create("payments", "approve", ActionScope.Admin);
}
```

This produces:

```text
payments.approve.admin
```

For a subresource, the same API supports four segments:

```csharp
UAuthActions.Create("payments", "approve", ActionScope.Admin, "approvals");
// payments.approvals.approve.admin
```

Use the action consistently in permission assignments, authorization contexts, and policies. The `.admin` suffix participates in UltimateAuth's existing scope-based authorization rules; it does **not** mean that every application actor must literally have a role named `Admin`.

> **Important:** Application policies add requirements; they do not replace the permission checks or other mandatory policies configured by UltimateAuth. An `Allow` result from one policy does not override a denial from another.

## 4. Provide Trusted Payment Facts

A policy needs to know who created the payment and how much it is worth. Those facts belong to your application, not to an arbitrary client claim.

Create a small immutable snapshot:

```csharp
using CodeBeam.UltimateAuth.Core.Domain;

public sealed record PaymentApprovalFacts(UserKey CreatedBy, decimal Amount);
```

Load these facts from a **tenant-scoped server-side repository** before authorization, and attach them to the context with `WithAttribute`:

```csharp
var authorizationContext = context.WithAttribute(
    "payment.approval",
    new PaymentApprovalFacts(payment.CreatedBy, payment.Amount));
```

`WithAttribute` retains the existing actor, tenant, action, and trusted actor-role information. The new attribute provides operation-specific facts for your application policy.

Do not accept `CreatedBy`, `Amount`, or `ActorRoles` from the caller as authoritative authorization data.

## 5. Write the Separation of Duties Policy

Create `Authorization/PaymentSeparationOfDutiesPolicy.cs`:

```csharp
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

public sealed class PaymentSeparationOfDutiesPolicy : IAccessPolicy
{
    public bool AppliesTo(AccessContext context)
        => context.Action == PaymentActions.Approve;

    public ValueTask<AccessDecision> DecideAsync(AccessContext context, CancellationToken ct = default)
    {
        if (!context.IsAuthenticated || context.ActorUserKey is null)
            return ValueTask.FromResult(AccessDecision.Deny("payment_authentication_required"));

        if (!context.Attributes.TryGetValue("payment.approval", out var value) || value is not PaymentApprovalFacts payment)
        {
            return ValueTask.FromResult(AccessDecision.Deny("payment_context_missing"));
        }

        if (context.ActorUserKey.Value == payment.CreatedBy)
            return ValueTask.FromResult(AccessDecision.Deny("cannot_approve_own_payment"));

        return ValueTask.FromResult(AccessDecision.Allow());
    }
}
```

The policy is responsible for **one authorization requirement**: the payment creator cannot approve their own payment.

- `AppliesTo` limits the policy to the payment approval action.
- `ActorUserKey` identifies the authenticated actor.
- `PaymentApprovalFacts.CreatedBy` identifies the creator of the specific payment.
- Missing trusted payment facts cause a **deny**, not an accidental allow.

A policy's `Allow` means that **its own requirement** is satisfied. Other applicable requirements must still succeed.

## 6. Register the Policy

Register your application policy with the other UltimateAuth services in `Program.cs`:

```csharp
using CodeBeam.UltimateAuth.Core.Abstractions;

builder.Services.AddScoped<IAccessPolicy, PaymentSeparationOfDutiesPolicy>();
```

`IAccessPolicy` registrations participate in UltimateAuth's global policy evaluation. Because `AppliesTo` restricts this policy to `PaymentActions.Approve`, it does not reject unrelated operations.

The registration belongs in the **server-side authorization host**. Do not rely on a browser-side registration to secure a payment operation.

## 7. Simplify the Application Service

Now the service can focus on loading and approving the payment while the orchestrator evaluates authorization before executing the mutation.

```csharp
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class PaymentApplicationService(IPaymentRepository repository, IAccessOrchestrator accessOrchestrator)
{
    public async Task ApproveAsync(AccessContext context, Guid paymentId, CancellationToken ct = default)
    {
        // Load only from the tenant authorized for this operation.
        var payment = await repository.GetAsync(context.ResourceTenant, paymentId, ct);

        var authorizationContext = context.WithAttribute(
            "payment.approval",
            new PaymentApprovalFacts(payment.CreatedBy, payment.Amount));

        var command = new AccessCommand(async innerCt =>
        {
            // Domain validation stays in the business workflow.
            if (payment.Status != PaymentStatus.Pending)
                throw new InvalidOperationException("Payment is not pending.");

            payment.Approve();
            await repository.SaveAsync(payment, innerCt);
        });

        await accessOrchestrator.ExecuteAsync(authorizationContext, command, ct);
    }
}
```

`IPaymentRepository`, `PaymentStatus`, and `Payment` are **application-defined types**. This example assumes the caller supplies a trusted, server-constructed `AccessContext` whose `Action` is `PaymentActions.Approve` and whose tenant has been resolved. Never construct the actor or action from untrusted request fields.

The service still explicitly requests authorization. What disappears is the duplicated knowledge of *which actors may approve which payments*.

The orchestrator evaluates applicable requirements before executing `AccessCommand`. If authorization fails, the payment mutation is not executed.

> **Data integrity:** The example keeps the focus on authorization. In production, protect approval against concurrent changes with an appropriate transaction, optimistic concurrency check, or equivalent mechanism. A policy is not a substitute for domain invariants or atomic persistence.

## 8. Add Another Policy Without Expanding the Workflow

A new requirement arrives: **payments over $10,000 require the `SeniorApprover` role**.

Instead of adding another conditional to `PaymentApplicationService`, define another policy:

```csharp
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

public sealed class HighValuePaymentPolicy : IAccessPolicy
{
    public bool AppliesTo(AccessContext context)
        => context.Action == PaymentActions.Approve;

    public ValueTask<AccessDecision> DecideAsync(AccessContext context, CancellationToken ct = default)
    {
        if (!context.Attributes.TryGetValue("payment.approval", out var value) || value is not PaymentApprovalFacts payment)
        {
            return ValueTask.FromResult(AccessDecision.Deny("payment_context_missing"));
        }

        if (payment.Amount <= 10_000m)
            return ValueTask.FromResult(AccessDecision.Allow());

        var isSeniorApprover = context.ActorRoles.Any(role => role.Name == "SeniorApprover");

        return ValueTask.FromResult(isSeniorApprover
            ? AccessDecision.Allow()
            : AccessDecision.Deny("senior_approver_required"));
    }
}
```

Register it alongside the first policy:

```csharp
builder.Services.AddScoped<IAccessPolicy, PaymentSeparationOfDutiesPolicy>();
builder.Services.AddScoped<IAccessPolicy, HighValuePaymentPolicy>();
```

The second policy uses `AccessContext.ActorRoles`, populated from UltimateAuth's trusted role resolution or validated resource-API claims. No caller-provided role list is necessary.

Both policies apply to the same action. **Both requirements must succeed.** The `SeniorApprover` role never bypasses the prohibition on approving one's own payment.

The application service remains unchanged.

## 9. Why This Pattern Scales

| Concern | Permission + service conditionals | Reusable application policies |
|---|---|---|
| General capability | Permission check | Permission check remains |
| Contextual restrictions | Embedded in each workflow | Named, focused policy |
| Reuse across operations | Extract shared checks manually | Apply the same policy to relevant actions |
| New authorization rule | Often edit service code | Add or extend a policy |
| Multiple requirements | Larger conditional blocks | Independently evaluated policies |
| Testing | Authorization mixed with service tests | Policies can be tested in isolation |
| Security review | Rules may be scattered | Requirements are easier to locate |
| Business workflow | Contains access rules and mutations | Focuses on domain operations |

These are architectural benefits, not capabilities exclusive to policies: a dedicated authorization service can also separate concerns. UltimateAuth provides a common `AccessContext` and evaluation pipeline so applications do not have to build that coordination themselves.

## 10. Recommended Pattern

**Keep authorization decisions in policies and business workflows in application services.**

```text
Authenticated actor + trusted resource facts
                    |
                    v
        UltimateAuth AccessContext
                    |
                    v
          Authorization pipeline
                    |
        +-----------+-----------+
        |           |           |
    Permission   Separation   High-value
      rules      of Duties     payment
        |           |           |
        +-----------+-----------+
                    |
             All must allow
                    |
                    v
          Application service
                    |
                    v
       Domain validation + mutation
```

Use policies for **authorization questions**: *Who may do this, to which resource, and under what security conditions?*

Keep **domain questions** in the business layer: *Is this payment pending? Is the transition valid? Can the transaction be committed safely?*

This separation is called **Policy-Based Authorization**. The payment example demonstrates **Separation of Duties**, while adding independent requirements demonstrates **Policy Composition**.

**Roles organize users. Permissions grant capabilities. Policies enforce contextual authorization rules.**

For the underlying concepts and evaluation order, see [Policies & Access Control](/docs/plugin-domains/policies) and [Authorization](/docs/plugin-domains/authorization).
