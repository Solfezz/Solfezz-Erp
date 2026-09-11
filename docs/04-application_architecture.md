# Application Architecture

**Project:** ERP Platform
**Document:** Application Architecture
**Version:** 0.1
**Status:** Draft
**Phase:** 00 — Architecture Foundation

---

# 1. Purpose

This document defines the technical architecture of the ERP application.

It establishes:

* project boundaries,
* dependency direction,
* application layering,
* module structure,
* dependency injection,
* persistence responsibilities,
* configuration,
* external integrations,
* authorization placement,
* testing boundaries,
* background processing,
* observability,
* failure handling.

The architecture must support the domain model without allowing infrastructure concerns to leak into business logic.

---

# 2. Architectural Style

The application will initially use a:

> **Modular Monolith with layered application architecture and domain-oriented module boundaries.**

The system is deployed as one application initially, while maintaining internal boundaries that allow future extraction if justified.

Conceptually:

```text
ERP Application
│
├── Identity
├── Manufacturing
├── Product
├── Warehouse
├── Listing
├── Orders
├── Shipping
├── Customer
├── Customer Service
└── Analytics
```

Each module owns its business rules and application use cases.

---

# 3. Primary Dependency Direction

The core dependency rule is:

```text
Web
 ↓
Application
 ↓
Domain

Infrastructure
 ↓
Application / Domain contracts
```

The domain must not depend on:

```text
ASP.NET Core
EF Core
PostgreSQL
HTTP
Marketplace SDKs
Shipping SDKs
UI frameworks
Logging implementations
```

---

# 4. Layer Responsibilities

## 4.1 Domain

The Domain layer contains business meaning.

Responsibilities:

* entities,
* aggregate roots,
* value objects,
* domain rules,
* domain invariants,
* domain operations,
* domain events.

The Domain layer should remain independent of external technical frameworks wherever practical.

---

## 4.2 Application

The Application layer contains application use cases.

Responsibilities:

* commands,
* queries,
* use-case orchestration,
* authorization requirements,
* transaction boundaries,
* DTOs,
* application services,
* integration contracts,
* domain event handling coordination.

Example:

```text
ApproveSku
AdjustInventory
ImportMarketplaceOrder
CreateShipment
ResolveConversation
```

The Application layer determines **how the system performs a use case**, while the Domain determines **what the business rules are**.

---

## 4.3 Infrastructure

Infrastructure implements technical concerns.

Responsibilities:

* EF Core,
* PostgreSQL,
* repositories where actually justified,
* external APIs,
* email,
* shipping providers,
* marketplace providers,
* file storage,
* background jobs,
* persistence of audit/events,
* technical logging.

Infrastructure is replaceable implementation detail.

---

## 4.4 Web / Presentation

The Web layer contains:

* HTTP endpoints,
* UI,
* authentication boundary,
* request/response models,
* presentation validation,
* authorization policy wiring,
* exception-to-response mapping.

Business rules should not live in controllers/pages.

---

# 5. Project Structure

Initial solution structure:

```text
src/
├── Erp.Web/
├── Erp.Application/
├── Erp.Domain/
├── Erp.Infrastructure/
│
└── Modules/
    ├── Identity/
    ├── Manufacturing/
    ├── Product/
    ├── Warehouse/
    ├── Listing/
    ├── Orders/
    ├── Shipping/
    ├── Customer/
    ├── CustomerService/
    └── Analytics/

tests/
├── Erp.Domain.Tests/
├── Erp.Application.Tests/
├── Erp.IntegrationTests/
└── Erp.ArchitectureTests/
```

The exact project layout may evolve after the first implementation slice.

---

# 6. Module Structure

Each business module should group its own concepts.

Example:

```text
Modules/
└── Product/
    ├── Domain/
    │   ├── Product.cs
    │   ├── Sku.cs
    │   ├── Kit.cs
    │   └── ...
    │
    ├── Application/
    │   ├── CreateProduct/
    │   ├── CreateSku/
    │   ├── ApproveSku/
    │   └── ...
    │
    ├── Infrastructure/
    │   ├── ProductConfiguration.cs
    │   └── ...
    │
    └── Presentation/
        └── ...
```

The module should make ownership visible from the file system.

---

# 7. Dependency Rules

The following dependency rules apply.

## Domain

May depend on:

```text
Domain primitives
Shared domain abstractions where necessary
```

Should not depend on:

```text
Web
Infrastructure
EF Core
database providers
external APIs
```

---

## Application

May depend on:

```text
Domain
Application abstractions
```

Should not depend directly on:

```text
Concrete database implementation
Concrete marketplace SDK
Concrete shipping provider
Web UI
```

---

## Infrastructure

May depend on:

```text
Domain
Application
Frameworks
External SDKs
Database provider
```

---

## Web

May depend on:

```text
Application
Infrastructure composition root
Presentation abstractions
```

The Web layer should not directly manipulate domain persistence.

---

# 8. Composition Root

Dependency injection configuration is centralized at the application composition root.

Conceptually:

```text
Program.cs
   ↓
Register Application
   ↓
Register Infrastructure
   ↓
Register Modules
   ↓
Build Application
```

Concrete implementations are selected there.

Example:

```text
IShippingProvider
        ↓
UspsShippingProvider
```

The application layer knows about the abstraction.

The composition root chooses the implementation.

---

# 9. Module Registration

Each module should expose a controlled registration entry point.

Conceptually:

```csharp
public static class ProductModule
{
    public static IServiceCollection AddProductModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // registrations
        return services;
    }
}
```

The goal is to prevent `Program.cs` from becoming a giant dependency-registration file.

---

# 10. Application Use Cases

Business behavior should be organized around use cases rather than generic CRUD services.

Prefer:

```text
Product
    CreateSku
    ApproveSku
    ActivateSku
    RetireSku
```

over:

```text
ProductService
    Create()
    Update()
    Delete()
    Get()
```

Use-case boundaries make authorization, validation, transactions, tests, and audit behavior explicit.

---

# 11. Commands

Commands represent operations that change application state.

Examples:

```text
CreateProduct
CreateSku
ApproveSku
ReceiveInventory
AdjustInventory
PublishListing
ImportOrder
RecordPayment
CreateShipment
ResolveConversation
```

A command should contain the information needed to perform its operation.

Commands should not contain persistence implementation details.

---

# 12. Queries

Queries retrieve information without changing business state.

Examples:

```text
GetProduct
GetSku
GetInventoryPosition
GetOrder
GetShipment
GetConversation
GetAnalyticsDashboard
```

The query side may use models optimized for reading rather than always loading full domain aggregates.

---

# 13. CQRS Position

The system will use **lightweight CQRS principles**, not full distributed CQRS/event sourcing.

We distinguish:

```text
Commands
    Change state

Queries
    Read state
```

However, we will not introduce separate databases or message brokers merely to follow a CQRS pattern.

---

# 14. Domain vs Application Validation

Validation has different responsibilities.

## Presentation Validation

Checks malformed user input.

Example:

```text
Quantity is numeric.
Required field is present.
```

## Application Validation

Checks use-case requirements.

Example:

```text
User provided required operation parameters.
```

## Domain Validation

Checks business invariants.

Example:

```text
Kit quantity must be greater than zero.
SKU cannot contain itself.
```

A client cannot bypass domain rules by calling the application directly.

---

# 15. Authorization Placement

Authorization is primarily an Application/Web concern.

Conceptually:

```text
Request
  ↓
Authentication
  ↓
Authorization
  ↓
Application Use Case
  ↓
Domain Rules
```

Sensitive operations should have explicit authorization requirements.

Example:

```text
AdjustInventory
    requires
Warehouse / Inventory / Edit
```

---

# 16. Domain Authorization Distinction

The domain should not know about:

```text
Role
Permission
HTTP
Cookies
Claims
ASP.NET policies
```

However, business rules may still determine whether a requested operation is valid.

This preserves separation between:

```text
Can this user perform the operation?
```

and:

```text
Is this operation valid?
```

---

# 17. Transaction Boundaries

Transactions belong to application use-case boundaries where multiple persistence operations must succeed or fail together.

Example:

```text
ReceiveInventory
    ↓
Validate
    ↓
Update inventory
    ↓
Create inventory transaction
    ↓
Commit
```

The exact EF Core transaction implementation belongs to Infrastructure.

The Application layer defines the logical boundary.

---

# 18. Persistence Strategy

The initial persistence technology is:

```text
Entity Framework Core
+
PostgreSQL
```

The domain model is not designed around database tables.

EF Core mappings should adapt persistence to the domain model.

---

# 19. DbContext Strategy

Initially prefer a small number of deliberate DbContext boundaries rather than one giant context containing every possible table.

The first implementation will determine the practical boundary.

The system must avoid uncontrolled cross-module entity manipulation.

---

# 20. EF Core Rule

Application/domain code should not perform arbitrary:

```csharp
_context.Products.Update(...)
```

operations throughout the codebase.

Persistence behavior should be centralized in Infrastructure and application use cases.

---

# 21. Repository Strategy

Do not create generic repositories automatically.

Avoid:

```csharp
IRepository<T>
```

unless a real abstraction is justified.

Use repositories where they provide a meaningful aggregate persistence boundary.

Example:

```text
ISkuRepository
IOrderRepository
IShipmentRepository
```

may be appropriate.

A repository should represent domain/application needs rather than simply wrapping every EF Core method.

---

# 22. Database Migrations

Database structure changes must use versioned EF Core migrations.

Migrations must be:

```text
Reviewable
Repeatable
Version controlled
Tested
```

No manual production database changes should become undocumented permanent schema changes.

---

# 23. Configuration

Configuration is divided into:

```text
Application configuration
Environment configuration
Secret configuration
```

Example:

```text
appsettings.json
appsettings.Development.json
Environment Variables
User Secrets
Production Secret Store
```

Secrets must not be committed to Git.

---

# 24. Strongly Typed Configuration

Important configuration groups should use typed options.

Conceptually:

```csharp
SkuGenerationOptions
MarketplaceOptions
ShippingOptions
AuthenticationOptions
```

Avoid scattering:

```csharp
configuration["SomeRandomKey"]
```

throughout business code.

---

# 25. SKU Configuration

SKU generation rules are business configuration and require controlled access.

They should be:

```text
Validated
Versioned where necessary
Auditable
Testable
```

Changes to production SKU rules should not silently make historical SKU interpretation impossible.

---

# 26. External Integration Architecture

External providers are represented by application contracts.

Example:

```text
Application
    IMarketplaceProvider
           ↑
Infrastructure
    EbayMarketplaceProvider
```

and:

```text
Application
    IShippingProvider
           ↑
Infrastructure
    UpsShippingProvider
```

External SDKs must not leak into domain objects.

---

# 27. Integration Adapter Pattern

Each external system should have an adapter.

Example:

```text
Orders
   ↓
Marketplace Integration Contract
   ↓
eBay Adapter
   ↓
eBay API
```

The adapter converts:

```text
External DTO
    ↓
Internal Application Model
```

The core domain should not know the external API schema.

---

# 28. External Identity

External identifiers belong at integration boundaries.

Examples:

```text
ExternalOrderId
ExternalListingId
ExternalMessageId
ExternalShipmentId
```

The internal entity keeps its own stable identifier.

---

# 29. Idempotency

External operations must define idempotency before implementation.

Examples:

```text
ImportOrder
ReceiveWebhook
PublishListing
CreateShipment
CreateLabel
ReceiveCustomerMessage
RecordPayment
```

The implementation should use an appropriate idempotency key or external identity.

---

# 30. Background Processing

Long-running or retryable operations should not block normal HTTP requests.

Potential background workloads:

```text
Marketplace order synchronization
Listing synchronization
Carrier tracking synchronization
Email processing
Analytics projection
Webhook processing
Retry queues
```

The initial architecture should support background processing without forcing every operation into an asynchronous job.

---

# 31. Background Job Rules

Background jobs must be:

```text
Idempotent
Retryable
Observable
Failure-aware
```

A failed job must not silently disappear.

---

# 32. Integration Failure

External provider failures are expected.

The system should distinguish:

```text
Business failure
Transient provider failure
Permanent provider failure
Unexpected application failure
```

Example:

```text
Marketplace temporarily unavailable
        ↓
Retry

Invalid marketplace listing data
        ↓
Do not blindly retry
        ↓
Record failure
        ↓
Require correction
```

---

# 33. Error Handling

Application use cases should return meaningful application outcomes rather than exposing infrastructure exceptions directly.

Conceptual categories:

```text
ValidationFailure
Unauthorized
Forbidden
NotFound
Conflict
BusinessRuleViolation
IntegrationFailure
UnexpectedFailure
```

HTTP mapping belongs to the Web layer.

---

# 34. Exception Strategy

Exceptions should represent exceptional/unexpected conditions or be used consistently according to the chosen application error model.

The system must avoid giant controller blocks such as:

```csharp
try
{
    ...
}
catch (Exception ex)
{
    return BadRequest(ex.Message);
}
```

Unexpected errors must be logged safely and mapped to controlled responses.

---

# 35. Logging

Structured logging should be used.

Important fields may include:

```text
CorrelationId
UserId
Operation
EntityId
Module
Outcome
Duration
```

Do not log:

```text
Passwords
Tokens
API secrets
Sensitive authentication material
```

---

# 36. Correlation

Requests and asynchronous operations should have a correlation identifier.

This allows a production investigation to trace:

```text
HTTP Request
   ↓
Application Use Case
   ↓
Database Operation
   ↓
External API Call
   ↓
Background Job
```

where technically practical.

---

# 37. Observability

Initial observability consists of:

```text
Structured logs
Health checks
Application metrics
Database health
Integration failure visibility
```

Later phases may add distributed tracing and more advanced telemetry.

---

# 38. Audit vs Logging

Logging and auditing are different.

### Logging

Helps engineers diagnose system behavior.

### Audit

Records meaningful business/security actions.

Example:

```text
Log:
    HTTP request took 482ms.

Audit:
    User 123 approved SKU ABC-001.
```

Audit records must remain useful even if logs rotate.

---

# 39. Domain Events

Domain events represent important business facts.

Example:

```text
SkuApproved
InventoryReceived
OrderPaid
ShipmentDelivered
```

Events should be produced by domain/application behavior, not by arbitrary database changes.

---

# 40. Domain Event Handling

Initial event handling can occur inside the same application process.

Example:

```text
SkuApproved
    ↓
Application event dispatcher
    ↓
Analytics handler
```

No distributed broker is required initially.

---

# 41. Outbox Consideration

For events that must reliably communicate with external systems or asynchronous processing, an outbox pattern may be introduced.

Example:

```text
Business Transaction
       │
       ├── Business State
       │
       └── Outbox Event
                ↓
             Dispatcher
                ↓
        External / Async Handler
```

The outbox pattern should be introduced when actual integration/event reliability requirements justify it.

---

# 42. Caching

Caching is not part of the initial architecture unless demonstrated necessary.

Do not cache business state prematurely.

Potential future cache candidates:

```text
Reference data
Marketplace metadata
Read-heavy analytics
Configuration
```

Authoritative business state remains in the primary data store.

---

# 43. Concurrency

Operations involving shared mutable state must use appropriate concurrency protection.

Important areas:

```text
Inventory
Order fulfillment
SKU activation
Shipment creation
Listing publication
Payment processing
```

The implementation may use optimistic concurrency and database constraints where appropriate.

---

# 44. Database Constraints

Business-critical uniqueness rules should be enforced at the database level as well as application level.

Examples:

```text
SKU.Code
Marketplace + ExternalOrderId
Warehouse + Location + SKU
```

Application validation improves user experience.

Database constraints protect data integrity.

---

# 45. Security Architecture Boundary

Security implementation is distributed appropriately:

```text
Web
    Authentication

Web/Application
    Authorization

Domain
    Business invariants

Infrastructure
    Secrets/external security/persistence
```

No single layer is expected to solve all security concerns.

---

# 46. Testing Architecture

Tests are organized by purpose.

```text
Unit Tests
Integration Tests
Architecture Tests
End-to-End Tests
```

---

# 47. Unit Tests

Use for business logic that can be tested without infrastructure.

Examples:

```text
Kit cannot contain itself.
SKU generation produces valid code.
Approval rejects failed required test.
Order transition rejects invalid state.
```

These tests should be fast.

---

# 48. Integration Tests

Use to verify:

```text
EF Core
PostgreSQL
Authorization
Transactions
External integration adapters
```

Examples:

```text
Read-only user cannot edit inventory.
Duplicate order import is ignored.
Inventory transaction persists correctly.
```

---

# 49. Architecture Tests

Architecture tests should enforce rules such as:

```text
Domain cannot reference Infrastructure.
Domain cannot reference Web.
Modules cannot depend on unrelated internals.
```

This protects the architecture as the codebase grows.

---

# 50. End-to-End Tests

Use selectively for important business flows.

Examples:

```text
Login
Create SKU
Approve SKU
Receive inventory
Import order
Create shipment
Resolve customer conversation
```

Not every low-level behavior needs an end-to-end test.

---

# 51. API / Presentation Models

External request/response models should not automatically expose domain entities.

Example:

```text
HTTP Request DTO
      ↓
Application Command
      ↓
Domain
```

and:

```text
Domain/Application Result
      ↓
Response DTO
      ↓
HTTP Response
```

This prevents the public API from becoming coupled to internal domain structure.

---

# 52. Mapping

Mapping between:

```text
HTTP DTO
Application model
Domain model
Persistence model
External provider model
```

should be explicit where it improves clarity.

Do not introduce a mapping framework simply because mapping exists.

---

# 53. Shared Kernel

A small shared kernel may contain truly cross-cutting concepts such as:

```text
Result/Error primitives
Domain event abstractions
Common identifiers
Time abstraction
```

The shared kernel must remain small.

Business concepts should not be placed there merely for convenience.

---

# 54. Avoid the Common Shared "Everything"

Do not create:

```text
Shared/
    ProductStuff
    OrderStuff
    WarehouseStuff
    Helpers
    Misc
```

as a dumping ground.

If a concept belongs to Product, it should live with Product.

---

# 55. Module-to-Module Communication

A module should interact with another module through an explicit contract.

Preferred:

```text
Orders
    → Product application contract
```

or:

```text
SkuActivated event
    → Warehouse handler
```

Avoid:

```text
Orders
    directly updates Warehouse EF entity
```

---

# 56. Synchronous vs Event Communication

Use synchronous calls when immediate consistency is required.

Example:

```text
CreateShipment
    must immediately validate the order
```

Use events when reacting to a business fact can be asynchronous.

Example:

```text
ShipmentDelivered
    → update analytics
```

Do not use events merely to make simple method calls look sophisticated.

---

# 57. Module Boundary Example

For:

```text
ReceiveInventory()
```

the flow is:

```text
HTTP/UI
   ↓
Authorization
   ↓
ReceiveInventory Command
   ↓
Warehouse Application
   ↓
Warehouse Domain
   ↓
Inventory changes
   ↓
Inventory Transaction
   ↓
Commit
   ↓
InventoryReceived event
```

Product does not directly modify Warehouse state.

---

# 58. Module Boundary Example — Order Import

```text
Background Job
   ↓
Marketplace Provider
   ↓
External Order DTO
   ↓
Order Import Application Use Case
   ↓
Validate / Idempotency
   ↓
SalesOrder Domain
   ↓
Persist
   ↓
OrderImported event
```

The marketplace SDK remains in Infrastructure.

---

# 59. Module Boundary Example — Shipping

```text
CreateShipment
   ↓
Authorize
   ↓
Validate Order/Fulfillment state
   ↓
Create Shipment
   ↓
Commit shipment
   ↓
Shipping provider adapter
   ↓
Label / tracking
```

Where external side effects and database transactions interact, reliability/idempotency requirements must be explicitly designed.

---

# 60. Application Lifecycle

Startup should approximately follow:

```text
Load configuration
   ↓
Configure services
   ↓
Register modules
   ↓
Configure database
   ↓
Configure authentication
   ↓
Configure authorization
   ↓
Configure observability
   ↓
Configure Web
   ↓
Run application
```

---

# 61. Environment Strategy

Initial environments:

```text
Development
Test
Production
```

Additional staging environments may be added later.

Production configuration must not be assumed to be identical to local development configuration.

---

# 62. Local Development

Local development should be reproducible.

Preferred infrastructure:

```text
.NET SDK
PostgreSQL
Docker
Git
Required SDK/tooling versions
```

The repository should document how a new developer gets from:

```text
git clone
```

to:

```text
build
test
run
```

with minimal undocumented machine-specific configuration.

---

# 63. Build Reproducibility

The repository should pin or constrain important tool versions.

Examples:

```text
.NET SDK
NuGet dependencies
Container versions
Database compatibility
```

The exact versions will be finalized in Phase 00.

---

# 64. Dependency Management

Dependencies should be added only when they solve an actual problem.

Every significant dependency should answer:

```text
Why do we need it?
What problem does it solve?
What is the maintenance risk?
Can we remove it later?
```

Avoid package accumulation.

---

# 65. Frontend Architecture

The initial frontend technology will be selected according to ERP usability requirements and the final hosting model.

The frontend must consume application functionality through defined presentation/application boundaries.

The domain must remain completely independent of frontend technology.

---

# 66. API Style

When HTTP APIs are introduced, use resource-oriented endpoints combined with explicit business operations where appropriate.

For example:

```http
POST /api/skus
POST /api/skus/{id}/approve
POST /api/inventory/receipts
POST /api/orders/{id}/cancel
POST /api/shipments
```

Avoid turning the entire system into generic:

```text
PUT /entities/{id}
```

endpoints that allow clients to bypass domain rules.

---

# 67. Database/API Independence

The API should not mirror database tables automatically.

A table represents persistence.

An API represents application capabilities.

A domain operation may touch multiple persistence structures while still being one application operation.

---

# 68. Performance Principles

Do not optimize without measurement.

Initial priorities:

```text
Correctness
Security
Maintainability
Observability
```

Then optimize demonstrated bottlenecks.

Potential future concerns:

```text
Indexes
Pagination
Query projections
Bulk imports
Caching
Batch processing
Async jobs
```

---

# 69. Data Access Performance

Read-heavy screens should avoid loading entire aggregates unnecessarily.

Queries should project only required data.

Example:

```text
Inventory screen
    ↓
Inventory projection
```

rather than loading:

```text
Warehouse
 → Locations
 → Products
 → SKUs
 → Listings
 → Orders
```

unless actually required.

---

# 70. Failure Recovery

Every important operation should consider:

```text
What can fail?
When can it fail?
Has anything already changed?
Can it safely retry?
How does the user recover?
```

This becomes a required design question for integration-heavy features.

---

# 71. Architecture Rules

The following rules are considered architectural constraints:

```text
1. Domain does not depend on Infrastructure.

2. Domain does not depend on Web.

3. Business rules do not live in controllers/pages.

4. External provider SDKs remain behind integration boundaries.

5. UI visibility is not authorization.

6. Application use cases enforce authorization and orchestration.

7. Domain operations enforce business invariants.

8. Cross-module state changes use explicit contracts/events.

9. External operations define idempotency.

10. Persistence does not become the domain model.

11. Database integrity rules are reinforced with database constraints.

12. Tests protect architecture as well as behavior.
```

---

# 72. Architecture Definition of Done

The architecture is ready for implementation when:

* dependency direction is agreed,
* module ownership is clear,
* application/domain/infrastructure responsibilities are understood,
* persistence boundary is established,
* authorization placement is established,
* integration boundaries are established,
* testing strategy is established,
* configuration strategy is established,
* logging/audit distinction is established,
* concurrency/idempotency requirements are identified,
* local development path is known,
* Phase 00 solution structure can be created without architectural invention during implementation.

---

# 73. Phase 00 Implementation Target

The first implementation should establish only the foundation:

```text
Repository
   ↓
.NET solution
   ↓
Project/module structure
   ↓
Build
   ↓
Test
   ↓
Database connection foundation
   ↓
Authentication foundation
   ↓
Authorization foundation
   ↓
CI
```

No full business module should be implemented during this foundation slice.

---

# 74. First Vertical Slice After Foundation

The first meaningful application slice is:

```text
Login
   ↓
Authenticated User
   ↓
Role
   ↓
Permission
   ↓
Section
   ↓
Read Access
   ↓
Edit Access
   ↓
Server-side enforcement
   ↓
Automated test
```

This establishes the security mechanism that all later modules use.

---

# 75. Future Architecture Documents

After this document:

```text
docs/
├── 01-system-blueprint.md
├── 02-domain-model.md
├── 03-security-and-rbac.md
├── 04-application-architecture.md
│
└── phases/
    ├── phase-00-foundation.md
    └── phase-01-identity-rbac.md
```

Further architecture documents should be introduced only when the system needs them.

Possible future documents:

```text
Integration Architecture
Observability Architecture
Deployment Architecture
Data/Reporting Architecture
Security Hardening
```

