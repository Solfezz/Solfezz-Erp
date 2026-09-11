# System Blueprint

**Project:** ERP Platform
**Status:** Draft v0.1
**Phase:** 00 — Architecture Foundation

---

# 1. Purpose

This system is an operations ERP connecting product development, SKU management, inventory, marketplace listings, orders, shipping, customer service, and analytics.

The platform is designed around a single principle:

> Each business concept has a clear owner, lifecycle, and set of rules.

Modules may consume information from other modules, but one module should remain the authoritative owner of a business concept.

---

# 2. Architectural Direction

## 2.1 Initial Architecture

The system will begin as a **modular monolith**.

We are intentionally not starting with microservices.

```text
                    ERP Application
                         │
        ┌────────────────┼────────────────┐
        │                │                │
        ▼                ▼                ▼
   Identity        Business Modules    Analytics
        │                │                │
        └────────────────┼────────────────┘
                         │
                      Database
```

Each module should have clear boundaries even though modules initially run in one application and may share one database.

The architecture must avoid creating a "single giant CRUD model" where every module directly modifies every other module's tables.

---

# 3. Domain Map

The initial bounded contexts are:

```text
Identity & Access
        │
        │ authorizes
        ▼

Manufacturing
        │
        │ approved manufacturing/product definition
        ▼

Product & SKU
        │
        ├──────────────► Warehouse
        │
        └──────────────► Listing
                             │
                             ▼
                          Orders
                             │
                             ▼
                         Shipping
                             │
                             ▼
                      Customer Service

All operational domains
        │
        ▼
     Analytics
```

This is a logical relationship map, not a statement that every module must directly call another module.

---

# 4. Domain Ownership

| Business Concept            | Owner                              |
| --------------------------- | ---------------------------------- |
| User                        | Identity                           |
| Role                        | Identity                           |
| Permission                  | Identity                           |
| Product development project | Manufacturing                      |
| Manufacturing SKU           | Manufacturing                      |
| Manufacturing revision      | Manufacturing                      |
| Test result                 | Manufacturing                      |
| Product                     | Product                            |
| Sellable SKU                | Product                            |
| Kit composition             | Product                            |
| SKU generation rule         | Product                            |
| Warehouse                   | Warehouse                          |
| Location                    | Warehouse                          |
| Inventory balance           | Warehouse                          |
| Inventory transaction       | Warehouse                          |
| Marketplace                 | Listing                            |
| Listing                     | Listing                            |
| Marketplace content         | Listing                            |
| Order                       | Orders                             |
| Payment state               | Orders                             |
| Fulfillment state           | Orders                             |
| Shipment                    | Shipping                           |
| Tracking                    | Shipping                           |
| Customer                    | Customer Service / Customer domain |
| Conversation                | Customer Service                   |
| Operational metrics         | Analytics                          |

The exact ownership of Customer may later become its own context if the platform grows.

---

# 5. Core Domain Language

The following terminology is intentional.

## Product

A business product definition.

Example:

```text
Curtain Rod 48"
```

A Product is not necessarily a warehouse item.

---

## SKU

A uniquely identified item definition used for inventory, listings, orders, or kits.

Examples:

```text
CR-48-BLK
CR-48-WHT
BRACKET-BLK
KIT-CR-48-BLK
```

---

## Basic SKU

A SKU representing a single physical item definition.

---

## Kit SKU

A SKU composed of one or more other SKUs.

Example:

```text
KIT-CR-48-BLK

1 × CR-48-BLK
2 × BRACKET-BLK
4 × SCREW-001
```

---

## Manufacturing SKU

A manufacturing-side identifier used during development and production.

A manufacturing SKU may:

1. map directly to a Product SKU,
2. become a Product SKU,
3. represent an intermediate manufacturing component,
4. remain separate from the sellable SKU.

The system must not assume these identifiers are always identical.

---

## Marketplace Listing

A channel-specific representation of a Product SKU.

Example:

```text
Product SKU
   CR-48-BLK

       ├── eBay Listing
       ├── Amazon Listing
       └── Walmart Listing
```

---

# 6. Product Lifecycle

The platform distinguishes **development state** from **commercial availability**.

A product should not become warehouse-eligible merely because it exists.

Initial lifecycle:

```text
Draft
  ↓
Development
  ↓
Prototype
  ↓
Testing
  ↓
Approved
  ↓
Released
  ↓
Retired
```

The exact transition rules will be defined by Manufacturing and Product.

---

# 7. Manufacturing Domain

Manufacturing owns product development.

## 7.1 Manufacturing Project

Represents development of a product or manufacturing item.

Example:

```text
Project
    Product concept
    Revision
    Manufacturing SKU
    Development state
    Tests
    Approval
```

---

## 7.2 Manufacturing Revision

Designs change over time.

We therefore model revisions explicitly.

Example:

```text
CR-48

Revision A
Revision B
Revision C
```

A later revision must not silently overwrite historical information.

---

## 7.3 Testing

Testing produces explicit results.

```text
Test
 ├── Test Type
 ├── Revision
 ├── Started
 ├── Completed
 ├── Result
 └── Notes
```

Possible results:

```text
Pending
Passed
Failed
Waived
```

---

## 7.4 Manufacturing Approval

Approval must be a business operation, not merely a boolean field.

Approval should record:

```text
Approved By
Approved At
Approved Revision
Approval Reason
```

A product cannot transition to a released/eligible state without satisfying required approval rules.

---

# 8. Product / SKU Domain

Product owns sellable identity.

## 8.1 Product

Example:

```text
Product
    Name
    Category
    Brand
    Description
```

---

## 8.2 SKU

```text
SKU
    Id
    Code
    Product
    Type
    Lifecycle State
```

Initial SKU types:

```text
Basic
Kit
```

Future types may be added without rewriting the entire system.

---

## 8.3 SKU Composition

Kit composition is a relationship between SKUs.

```text
SkuComponent
    ParentSku
    ChildSku
    Quantity
```

Rules:

* Quantity must be greater than zero.
* A SKU cannot contain itself.
* Circular composition must be prevented.
* A kit must contain at least one valid component.
* Component SKUs must be valid for the kit's lifecycle.
* Historical composition changes must be preserved where operational history depends on the previous definition.

---

# 9. SKU Generation

SKU generation is configuration-driven.

Example:

```yaml
skuRules:
  product:
    pattern: "{CATEGORY}-{PRODUCT}-{VARIANT}"

  manufacturing:
    pattern: "MFG-{PRODUCT}-{REVISION}"

  kit:
    pattern: "KIT-{PRODUCT}-{SIZE}-{COLOR}"
```

The application should not scatter SKU formatting rules across controllers and UI code.

The SKU generator should provide:

```text
Validate Rule
Generate Candidate
Validate Candidate
Check Uniqueness
Reserve / Create
```

The exact reservation behavior will be designed before implementation.

---

# 10. Manufacturing → Product Boundary

Manufacturing can signal:

```text
Development completed
Testing passed
Approved
Released
```

Product owns the creation/activation of the sellable SKU.

Conceptually:

```text
Manufacturing
      │
      │ Product Release
      ▼
Product
      │
      │ SKU becomes eligible
      ▼
Warehouse
```

Manufacturing does not directly create warehouse inventory.

Warehouse consumes Product/SKU eligibility.

---

# 11. Warehouse Domain

Warehouse owns physical inventory.

## 11.1 Warehouse

```text
Warehouse
    Id
    Name
    Code
```

---

## 11.2 Location

```text
Warehouse
   └── Location
```

Examples:

```text
WH1-A-01
WH1-A-02
WH1-B-01
```

---

## 11.3 Inventory

Inventory represents the current quantity associated with:

```text
Warehouse
Location
SKU
```

---

## 11.4 Inventory Transaction

Inventory changes must be auditable.

Examples:

```text
Receipt
Adjustment
Transfer
Pick
Return
Shipment
Damage
Correction
```

Each transaction records:

```text
SKU
Quantity
From Location
To Location
Reason
Performed By
Performed At
Reference
```

The system must be able to explain how an inventory balance was produced.

---

# 12. Inventory Invariants

Initial rules:

1. Only eligible SKUs may enter normal warehouse inventory.
2. Every inventory-changing operation creates an auditable transaction.
3. Inventory operations must be safe under concurrent access.
4. Duplicate external operations must not double-apply inventory.
5. Transfers must not lose or duplicate inventory.
6. Negative inventory must be explicitly allowed by business policy rather than occurring accidentally.

---

# 13. Listing Domain

Listing owns marketplace representation.

```text
Listing
    SKU
    Marketplace
    External Listing ID
    Status
    Marketplace-specific Content
```

Content may include:

```text
Title
Description
Bullets
Specifications
Images
Brand
Attributes
```

The internal SKU remains authoritative for product identity.

Marketplace identifiers remain external identifiers.

---

# 14. Listing Lifecycle

Initial state model:

```text
Draft
  ↓
Ready
  ↓
Publishing
  ↓
Published
  ↓
Failed
  ↓
Retired
```

A listing integration failure must not corrupt the underlying Product/SKU.

---

# 15. Marketplace Integration Boundary

Each marketplace should be treated as an external system.

Conceptual abstraction:

```csharp
IMarketplaceProvider
```

The provider handles external concerns such as:

```text
Publish Listing
Update Listing
Pull Orders
Send Messages
```

The core domain should not contain Amazon/eBay-specific HTTP logic.

---

# 16. Orders Domain

Orders own the commercial transaction.

An order contains:

```text
Order
 ├── Channel
 ├── External Order ID
 ├── Customer
 ├── Items
 ├── Payment
 └── Fulfillment
```

---

# 17. Order State Separation

Do not create one giant status enum such as:

```text
OrderStatus = PaidShippedDeliveredCompleted...
```

Instead separate concerns.

### Order lifecycle

```text
New
Confirmed
Cancelled
Completed
```

### Payment lifecycle

```text
Pending
Authorized
Paid
Failed
Refunded
```

### Fulfillment lifecycle

```text
Unfulfilled
Allocated
Picking
Packed
Fulfilled
Cancelled
```

### Shipping lifecycle

Owned by Shipping.

This prevents unrelated states from becoming coupled.

---

# 18. Order Import

Marketplace order imports must be idempotent.

The system must recognize:

```text
Marketplace
+
External Order ID
```

as the external identity of an order.

Running the importer twice must not create duplicate orders.

---

# 19. Shipping Domain

Shipping owns physical shipment execution.

```text
Order
  ↓
Shipment
  ↓
Shipping Provider
  ↓
Label
  ↓
Tracking
  ↓
Delivery
```

Shipping should be provider-neutral internally.

---

# 20. Shipment Lifecycle

Initial state:

```text
Pending
   ↓
LabelCreated
   ↓
Shipped
   ↓
InTransit
   ↓
Delivered
```

Possible failure/correction paths:

```text
LabelFailed
ShipmentCancelled
DeliveryException
Returned
```

---

# 21. Shipping Invariants

Initial rules:

1. A shipment cannot be created for an invalid order.
2. Normal fulfillment requires payment eligibility.
3. A shipping label belongs to a shipment.
4. Tracking belongs to the shipment/carrier relationship.
5. Repeated label requests must be handled safely.
6. Delivery updates from carriers must be idempotent.

---

# 22. Customer Service Domain

Customer Service provides a unified operational workspace.

Core concepts:

```text
Customer
Conversation
Message
Channel
Case / Resolution
```

Channels may include:

```text
Marketplace
Email
Phone
```

A conversation should be capable of referencing:

```text
Customer
Order
Listing
SKU
Shipment
```

This allows a CSR to resolve a problem without manually searching multiple modules.

---

# 23. Customer Service Integration

External communications are treated as integrations.

Examples:

```text
Marketplace Messaging Provider
Email Provider
Phone Provider
```

Incoming communications must be idempotent.

The same external message received twice must not create two customer-service messages.

---

# 24. Identity and Authorization

Identity is a cross-cutting platform capability.

The authorization model is:

```text
User
  ↓
Role
  ↓
Permission
  ↓
Module
  ↓
Section
  ↓
Access Level
```

Initial access levels:

```text
Read
Edit
```

---

# 25. Example Authorization

```text
Role: WarehouseUser

Warehouse
    Inventory        Read + Edit
    Locations        Read + Edit
    Receiving        Read + Edit

Product
    SKU              Read

Orders
    Orders           Read
```

Another role:

```text
Role: CSR

Customer Service
    Conversations    Read + Edit
    Customers        Read + Edit

Orders
    Orders           Read

Warehouse
    Inventory        Read
```

Authorization must be enforced server-side.

UI visibility is only a convenience layer.

---

# 26. Authorization Granularity

The system should support:

```text
Module-wide access
Section-specific access
Read access
Edit access
```

Future expansion may include:

```text
Create
Delete
Approve
Publish
Export
Administer
```

We should not implement these additional permissions until there is a real business requirement, but the design should not make them impossible.

---

# 27. Audit

Security-sensitive and business-critical operations should generate audit information.

Examples:

```text
User Role Changed
Permission Changed
Product Approved
SKU Activated
Inventory Adjusted
Order Cancelled
Shipment Created
Listing Published
```

Audit records should include:

```text
Actor
Timestamp
Action
Entity
Entity ID
Result
Relevant context
```

---

# 28. Domain Events

Modules may publish internal domain/application events.

Examples:

```text
ManufacturingApproved
SkuReleased
InventoryReceived
InventoryAdjusted
ListingPublished
ListingPublishFailed
OrderImported
OrderPaid
ShipmentCreated
ShipmentShipped
ShipmentDelivered
CustomerMessageReceived
```

Events should communicate meaningful business facts.

They should not become a substitute for normal domain modeling.

---

# 29. Analytics

Analytics consumes operational facts.

Analytics should not become the authoritative owner of operational state.

Conceptually:

```text
Operational Modules
       │
       │ Events / Facts
       ▼
 Analytics
       │
       ├── Metrics
       ├── Statistics
       └── Reporting
```

Initial metrics may include:

```text
Manufacturing
    Development cycle time
    Test pass/fail rate

Product
    Active SKUs
    New SKUs

Warehouse
    Inventory on hand
    Inventory movements
    Stockouts

Listing
    Active listings
    Publish failures

Orders
    Orders/day
    Units/day
    Revenue

Shipping
    Ship time
    Delivery time
    Exceptions

Customer Service
    Open conversations
    Response time
    Resolution time
```

---

# 30. Cross-Module Rules

The following relationships are particularly important.

## Manufacturing → Product

Manufacturing establishes when a product/revision is approved for release.

Product owns the sellable SKU.

---

## Product → Warehouse

Warehouse may only manage SKUs that are warehouse-eligible.

Warehouse does not approve products.

---

## Product → Listing

Listings reference Product SKUs.

Listing does not redefine SKU identity.

---

## Listing → Orders

Orders originate from marketplace listings/orders.

Orders retain external marketplace identity.

---

## Orders → Shipping

Shipping fulfills eligible orders.

Orders remain the owner of commercial order state.

Shipping owns shipment execution.

---

## Orders / Shipping → Customer Service

Customer Service may reference operational records but does not own those records.

---

## All Modules → Analytics

Analytics reads operational facts and produces statistics.

---

# 31. Critical Invariants

The following invariants are foundational.

### Product

```text
SKU code is unique.
SKU cannot be activated without required approval.
```

### Manufacturing

```text
Required testing must pass before approval.
Historical revisions cannot be silently overwritten.
```

### Kits

```text
Quantity > 0.
No self-reference.
No circular composition.
```

### Warehouse

```text
Inventory changes are transactional and auditable.
Duplicate operations cannot double-apply inventory.
```

### Listings

```text
Marketplace listing identity is separate from internal SKU identity.
```

### Orders

```text
External order identity is unique per marketplace.
Duplicate imports are ignored safely.
```

### Shipping

```text
Shipment cannot bypass required order/payment conditions.
External shipping updates are idempotent.
```

### Authorization

```text
Every protected operation is authorized server-side.
```

---

# 32. Data Ownership Rule

A module may consume another module's information, but should not casually update another module's state.

Example:

```text
GOOD

Product tells Warehouse:
    SKU-123 is eligible.

Warehouse decides:
    inventory = 50


BAD

Warehouse directly modifies:
    Product.IsApproved = true
```

The owner of a state transition must perform that transition.

---

# 33. Integration Rule

External systems are untrusted boundaries.

Examples:

```text
eBay
Amazon
Walmart
Email Provider
Phone Provider
UPS
FedEx
USPS
```

External data should enter through an integration boundary, be validated, and then be converted to internal domain/application models.

---

# 34. Error Handling

Errors should be treated as part of normal system design.

We distinguish:

```text
Validation Error
Authorization Error
Business Rule Violation
Integration Failure
Transient Failure
Unexpected System Failure
```

The user-facing message should not expose internal implementation details.

---

# 35. Concurrency

Concurrency must be considered for operations involving:

```text
Inventory
Orders
Payments
Shipment creation
Listing synchronization
External imports
```

The design must prevent two workers/users from successfully performing an operation that should only happen once.

---

# 36. Idempotency

Any operation capable of retrying must define its idempotency strategy.

Examples:

```text
Import marketplace order
Create shipment
Create shipping label
Receive inventory
Process webhook
Process customer message
Publish listing
```

The blueprint requires every external integration operation to answer:

> What happens if this exact request arrives twice?

---

# 37. Security Baseline

Initial security requirements:

* Secure authentication
* Password hashing through established .NET identity mechanisms
* Role-based and policy-based authorization
* Server-side permission enforcement
* Secure password recovery
* Session management
* CSRF protection where applicable
* Input validation
* Output encoding
* Secure secrets/configuration handling
* Audit of important security operations
* Protection against unauthorized direct API access

Security implementation details belong in the security architecture document.

---

# 38. Initial Application Layers

The application should separate:

```text
Presentation
     ↓
Application
     ↓
Domain
     ↓
Infrastructure
```

### Presentation

HTTP/UI concerns.

### Application

Use cases, orchestration, authorization decisions, transaction boundaries.

### Domain

Business rules, entities, value objects, domain events.

### Infrastructure

Database, external APIs, messaging, files, email, carrier/marketplace integrations.

---

# 39. Database Strategy

The initial database strategy is:

```text
PostgreSQL
+
Entity Framework Core
```

The database is an implementation detail of the domain model, not the definition of the domain.

We will design tables from validated domain concepts rather than starting with dozens of generic CRUD tables.

---

# 40. API Strategy

Use explicit application operations rather than generic CRUD whenever a business operation has meaningful rules.

Prefer:

```http
POST /products/{id}/approve
```

over:

```http
PUT /products/{id}
{
    "status": "Approved"
}
```

The former expresses a business operation and gives us a natural place to enforce approval rules, authorization, audit, and events.

---

# 41. Initial Aggregate Candidates

These are working candidates, not final decisions.

```text
ManufacturingProject
ManufacturingRevision
Product
Sku
Kit
Warehouse
Inventory
Order
Shipment
Listing
Conversation
Role
```

Aggregate boundaries must be confirmed during detailed design before implementation.

---

# 42. State Changes

Important state changes should normally happen through explicit domain/application operations.

Example:

```text
ApproveProduct()
ReleaseSku()
ReceiveInventory()
AdjustInventory()
PublishListing()
ImportOrder()
MarkOrderPaid()
CreateShipment()
MarkShipmentShipped()
RecordDelivery()
ResolveConversation()
```

This keeps business rules visible and testable.

---

# 43. Non-Functional Requirements

The system should be:

### Reliable

Operations fail predictably and recoverably.

### Secure

Unauthorized actions are rejected regardless of UI behavior.

### Auditable

Important business changes can be traced.

### Testable

Business rules can be tested without requiring the entire application.

### Maintainable

Modules have clear ownership and limited coupling.

### Observable

Failures and important operations are diagnosable.

### Evolvable

External providers can change without rewriting core business logic.

---

# 44. Architecture Decision: Modular Monolith

### Decision

Start with a modular monolith.

### Reason

The system has substantial domain complexity, but the initial engineering team does not need the operational complexity of distributed microservices.

A modular monolith gives us:

```text
Clear boundaries
+
Simple deployment
+
Single transactional database
+
Easy local development
+
Strong domain separation
```

A module may later be extracted into a service if scale or organizational boundaries justify it.

---

# 45. What We Are Explicitly NOT Designing Yet

Do not prematurely build:

* Microservices
* Event sourcing everywhere
* CQRS everywhere
* Distributed message brokers
* Complex workflow engines
* AI agents
* Advanced analytics warehouse
* Dozens of permission types
* Every possible marketplace integration

Those can be introduced when a real requirement justifies them.

---

# 46. Phase 00 Acceptance Criteria

The blueprint is considered ready for implementation when:

* Domain terminology is agreed upon.
* Module ownership is agreed upon.
* Major lifecycle states are defined.
* Critical invariants are identified.
* Authorization model is defined.
* Integration boundaries are defined.
* Data ownership rules are defined.
* Initial architecture is agreed upon.
* Testing strategy is defined.
* Phase 01 can be implemented without inventing architecture while coding.

---

# 47. First Implementation Target

The first production slice is:

```text
Identity
    ↓
User
    ↓
Login
    ↓
Role
    ↓
Permission
    ↓
Module
    ↓
Section
    ↓
Read/Edit authorization
    ↓
Protected application operation
```

This slice establishes the security foundation that every subsequent module will depend on.

---

# 48. Next Design Documents

After approval of this blueprint, create:

```text
docs/
├── 01-system-blueprint.md
├── 02-domain-model.md
├── 03-security-and-rbac.md
├── 04-data-model.md
├── 05-application-architecture.md
├── 06-integration-architecture.md
└── phases/
    ├── phase-00.md
    └── phase-01-identity.md
```

The next document should be **02-domain-model.md**, where we turn these concepts into precise entities, value objects, relationships, state transitions, and invariants without yet writing implementation code.
