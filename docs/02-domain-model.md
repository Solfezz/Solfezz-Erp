# Domain Model

**Project:** ERP Platform
**Document:** Domain Model
**Version:** 0.1
**Status:** Draft
**Phase:** 00 — Architecture Foundation

---

# 1. Purpose

This document defines the initial business domain model for the ERP platform.

It establishes:

* business concepts,
* entity ownership,
* aggregate boundaries,
* relationships,
* value objects,
* lifecycle states,
* business invariants,
* important operations,
* domain events,
* cross-module rules.

This document is intentionally independent of database tables, EF Core configurations, controllers, pages, and external API implementations.

---

# 2. Domain Modeling Principles

## 2.1 Business Ownership

Every important business concept has one authoritative owner.

A module may read another module's information, but it should not silently change another module's state.

Example:

```text
GOOD

Manufacturing
    approves development

Product
    releases SKU

Warehouse
    records inventory


BAD

Warehouse directly changes:
    Product.ApprovalStatus
```

---

## 2.2 Explicit Business Operations

Important state transitions should occur through meaningful operations.

Prefer:

```text
ApproveManufacturingRevision()
ReleaseSku()
ReceiveInventory()
AdjustInventory()
PublishListing()
ImportOrder()
MarkPaymentPaid()
CreateShipment()
MarkShipmentShipped()
RecordDelivery()
ResolveConversation()
```

over generic updates such as:

```text
entity.Status = "Approved";
```

The operation is responsible for enforcing the business rules for that transition.

---

## 2.3 Historical Truth

The ERP must preserve important historical facts.

A later change to a product, SKU, kit composition, price, listing, or manufacturing revision must not make historical orders or inventory movements become misleading.

Historical records should use appropriate snapshots or immutable references where necessary.

---

# 3. Bounded Contexts

Initial contexts:

```text
Identity & Access
Manufacturing
Product & SKU
Warehouse
Listing
Orders
Shipping
Customer
Customer Service
Analytics
```

Customer is treated as a reusable business concept rather than making Customer Service the owner of every customer-related fact.

---

# 4. Domain Context Map

```text
Identity & Access
        │
        │ authorization
        ▼
 ┌───────────────────────────────────────────────┐
 │                                               │
 ▼                                               ▼
Manufacturing ───────► Product & SKU ───────► Warehouse
       │                    │
       │                    └──────────────────► Listing
       │                                             │
       │                                             ▼
       └────────────────────────────────────────► Orders
                                                     │
                                    ┌────────────────┴──────────────┐
                                    ▼                               ▼
                                Shipping                      Customer Service
                                    │                               │
                                    └───────────────┬───────────────┘
                                                    ▼
                                                Customer

All operational contexts
        │
        ▼
     Analytics
```

The arrows represent business dependencies and communication, not necessarily direct database relationships.

---

# 5. Aggregate Root Overview

Initial aggregate candidates:

| Context          | Aggregate Root        |
| ---------------- | --------------------- |
| Identity         | User                  |
| Identity         | Role                  |
| Manufacturing    | ManufacturingProject  |
| Manufacturing    | ManufacturingRevision |
| Product          | Product               |
| Product          | Sku                   |
| Product          | Kit                   |
| Warehouse        | Warehouse             |
| Warehouse        | InventoryPosition     |
| Listing          | Listing               |
| Orders           | SalesOrder            |
| Shipping         | Shipment              |
| Customer         | Customer              |
| Customer Service | Conversation          |

These boundaries are deliberate starting points and may be adjusted during implementation if actual invariants reveal a better boundary.

---

# 6. Shared Concepts

## 6.1 Identifier

Every major entity has a stable internal identifier.

External identifiers must not replace internal identifiers.

Example:

```text
Internal Order ID
    7f2...

External eBay Order ID
    123-456-789
```

The external identifier is data belonging to an integration boundary.

---

## 6.2 Audit Information

Important entities may carry or reference:

```text
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
```

Critical business events also require auditable history.

Audit information is not itself a substitute for domain history.

---

## 6.3 Business Date / Time

Business timestamps should be stored consistently.

The system should use UTC internally and convert to a user's display timezone at the presentation boundary.

---

# 7. Identity & Access Domain

Identity is a cross-cutting platform capability.

## 7.1 User

Represents an authenticated person or system user.

Conceptually:

```text
User
 ├── UserId
 ├── Username / Login Identity
 ├── Email
 ├── Status
 └── Roles
```

Possible lifecycle:

```text
Invited
   ↓
Active
   ↓
Suspended
   ↓
Deactivated
```

A deactivated user must not authenticate or perform protected business operations.

---

## 7.2 Role

Represents a named collection of permissions.

Examples:

```text
Administrator
WarehouseUser
ManufacturingUser
ListingManager
CSR
Manager
Executive
```

A role does not directly grant business authority merely by its name.

Its actual capabilities come from assigned permissions.

---

## 7.3 Permission

A permission is defined by:

```text
Module
Section
AccessLevel
```

Initial access levels:

```text
Read
Edit
```

Example:

```text
Warehouse / Inventory / Read
Warehouse / Inventory / Edit

CustomerService / Conversations / Read
CustomerService / Conversations / Edit
```

Future levels may include:

```text
Create
Delete
Approve
Publish
Export
Admin
```

but these are not required for the first implementation.

---

## 7.4 Role Assignment

```text
User
   │
   └── UserRole
           │
           ▼
          Role
           │
           └── Permissions
```

A user may have multiple roles.

Effective access is determined from the user's active role assignments.

---

## 7.5 Authorization Invariant

A user may perform an operation only when:

```text
Authenticated
AND
User Active
AND
Required Permission Exists
AND
Business Rule Allows Operation
```

Authorization is enforced server-side.

---

# 8. Manufacturing Domain

Manufacturing owns product development and manufacturing-side lifecycle.

---

# 9. ManufacturingProject

Represents a development effort.

```text
ManufacturingProject
 ├── ProjectId
 ├── Name
 ├── Description
 ├── Status
 ├── Manufacturing SKUs
 └── Revisions
```

Potential lifecycle:

```text
Draft
   ↓
Design
   ↓
Prototype
   ↓
Testing
   ↓
Approved
   ↓
Released
   ↓
Closed
```

---

# 10. ManufacturingRevision

Represents a specific revision of a design.

```text
ManufacturingRevision
 ├── RevisionId
 ├── ProjectId
 ├── RevisionNumber
 ├── ManufacturingSku
 ├── Specifications
 ├── Status
 └── Tests
```

Example:

```text
Product concept:
Curtain Rod 48"

Revision A
Revision B
Revision C
```

Revision history must remain recoverable.

A new revision should not silently mutate an already-approved historical revision.

---

# 11. ManufacturingSku

Represents an identifier used during manufacturing/development.

Example:

```text
MFG-CR-48-001
```

It may:

* remain a manufacturing-only identifier,
* map to a sellable Product SKU,
* correspond to a component,
* represent an intermediate manufacturing item.

There is no requirement that:

```text
ManufacturingSku == ProductSku
```

---

# 12. Manufacturing Test

A test verifies whether a revision satisfies a requirement.

```text
ManufacturingTest
 ├── TestId
 ├── RevisionId
 ├── TestType
 ├── Status
 ├── Result
 ├── StartedAt
 ├── CompletedAt
 └── Notes
```

Possible test results:

```text
Pending
Passed
Failed
Waived
```

---

# 13. Manufacturing Approval

Approval is a business event/state transition rather than a simple flag.

Approval records:

```text
ApprovedBy
ApprovedAt
ApprovedRevision
ApprovalReason
```

---

# 14. Manufacturing Invariants

1. A revision cannot be approved while required tests remain incomplete.
2. Required tests must pass unless an explicit waiver is allowed.
3. A failed required test blocks approval.
4. Approval references a specific revision.
5. Historical revisions cannot be silently rewritten.
6. Manufacturing does not directly create warehouse inventory.
7. Manufacturing approval does not itself equal marketplace availability.

---

# 15. Manufacturing Operations

Important operations:

```text
CreateProject()
CreateRevision()
StartPrototype()
StartTesting()
RecordTestResult()
ApproveRevision()
ReleaseRevision()
CloseProject()
```

---

# 16. Manufacturing Domain Events

Potential events:

```text
ManufacturingProjectCreated
ManufacturingRevisionCreated
PrototypeStarted
TestCompleted
ManufacturingRevisionApproved
ManufacturingRevisionReleased
```

---

# 17. Product & SKU Domain

Product owns commercially meaningful product identity.

---

# 18. Product

Represents the commercial product definition.

Example:

```text
Product
 ├── ProductId
 ├── Name
 ├── Category
 ├── Brand
 └── SKUs
```

A Product can have multiple sellable SKUs.

Example:

```text
Curtain Rod 48"

CR-48-BLK
CR-48-WHT
CR-48-BRN
```

---

# 19. SKU

Represents a uniquely identifiable commercial item.

```text
Sku
 ├── SkuId
 ├── Code
 ├── ProductId
 ├── SkuType
 ├── LifecycleState
 └── Attributes
```

Initial types:

```text
Basic
Kit
```

Potential future types:

```text
Assembly
MadeToOrder
Service
Virtual
```

These are not required initially.

---

# 20. SKU Lifecycle

Initial lifecycle:

```text
Draft
   ↓
PendingApproval
   ↓
Approved
   ↓
Active
   ↓
Retired
```

Not every implementation needs every state immediately, but the distinction is useful.

---

# 21. SKU Approval

A SKU may not become operationally available merely because its database record exists.

Approval should capture:

```text
ApprovedBy
ApprovedAt
ApprovalReason
```

The system should distinguish:

```text
SKU exists
```

from:

```text
SKU is approved
```

and:

```text
SKU is warehouse eligible
```

---

# 22. Warehouse Eligibility

A Product/SKU may become warehouse-eligible only when required commercial/manufacturing conditions are satisfied.

Example:

```text
Manufacturing Approved
        +
Product SKU Approved
        =
Warehouse Eligible
```

The exact eligibility policy belongs to the Product/Warehouse boundary and should be explicitly modeled.

---

# 23. Kit SKU

A Kit SKU represents a sellable composition of other SKUs.

Example:

```text
KIT-CR-48-BLK

1 × CR-48-BLK
2 × BRACKET-BLK
4 × SCREW-001
2 × END-CAP-BLK
```

---

# 24. Kit Component

```text
KitComponent
 ├── ParentSkuId
 ├── ChildSkuId
 └── Quantity
```

The parent must be a Kit SKU.

The child may initially be a Basic SKU or another valid component type according to business policy.

---

# 25. Kit Invariants

1. Parent must be a Kit SKU.
2. Quantity must be greater than zero.
3. Parent cannot equal Child.
4. Circular dependency must be prevented.
5. A kit must contain at least one component.
6. Components must be valid active/approved SKUs according to policy.
7. Historical kit composition must remain available when required to interpret past orders.

---

# 26. SKU Generation

SKU code generation is configuration-driven.

Example:

```yaml
product:
  pattern: "{CATEGORY}-{PRODUCT}-{VARIANT}"

manufacturing:
  pattern: "MFG-{PRODUCT}-{REVISION}"

kit:
  pattern: "KIT-{PRODUCT}-{SIZE}-{COLOR}"
```

The domain concept is:

```text
SkuGenerationRule
```

The generation process is:

```text
Input Attributes
      ↓
Apply Rule
      ↓
Generate Candidate
      ↓
Validate Candidate
      ↓
Uniqueness Check
      ↓
Create SKU
```

Generation rules must not be embedded throughout the UI.

---

# 27. Product Operations

```text
CreateProduct()
CreateSku()
GenerateSkuCode()
ApproveSku()
ActivateSku()
RetireSku()
CreateKit()
AddKitComponent()
RemoveKitComponent()
```

---

# 28. Product Domain Events

Potential events:

```text
ProductCreated
SkuCreated
SkuApproved
SkuActivated
KitCreated
KitCompositionChanged
SkuRetired
```

---

# 29. Warehouse Domain

Warehouse owns physical stock.

---

# 30. Warehouse

```text
Warehouse
 ├── WarehouseId
 ├── Code
 ├── Name
 └── Locations
```

Example:

```text
WH1
WH2
```

---

# 31. Location

A physical storage location inside a warehouse.

```text
Location
 ├── LocationId
 ├── WarehouseId
 ├── Code
 └── Status
```

Example:

```text
WH1-A-01
WH1-A-02
WH1-B-01
```

Possible location state:

```text
Active
Inactive
Blocked
```

---

# 32. InventoryPosition

Represents the current quantity of a SKU at a location.

Conceptually:

```text
InventoryPosition
 ├── Warehouse
 ├── Location
 ├── SKU
 └── Quantity
```

A unique inventory position exists for the applicable:

```text
Warehouse + Location + SKU
```

combination.

---

# 33. InventoryTransaction

Represents a change to inventory.

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

Conceptually:

```text
InventoryTransaction
 ├── TransactionId
 ├── SKU
 ├── Quantity
 ├── FromLocation
 ├── ToLocation
 ├── Type
 ├── Reason
 ├── Reference
 ├── PerformedBy
 └── PerformedAt
```

Transactions should be immutable after posting except through explicit correction procedures.

---

# 34. Inventory Invariants

1. Only eligible SKUs may enter normal inventory.
2. Every stock-changing operation creates an inventory transaction.
3. Inventory changes are atomic.
4. Concurrent changes must not produce lost updates.
5. A transfer must remove stock from the source and add it to the destination atomically.
6. Duplicate receipt/import operations must not double-count inventory.
7. Negative inventory is prohibited unless explicitly enabled by business policy.
8. Posted inventory history cannot be silently rewritten.

---

# 35. Inventory Operations

```text
ReceiveInventory()
AdjustInventory()
TransferInventory()
PickInventory()
ReturnInventory()
RecordDamage()
CorrectInventory()
```

---

# 36. Warehouse Domain Events

```text
InventoryReceived
InventoryAdjusted
InventoryTransferred
InventoryPicked
InventoryReturned
InventoryDamaged
```

---

# 37. Listing Domain

Listing owns marketplace representation.

---

# 38. Marketplace

Represents an external sales channel.

Examples:

```text
eBay
Amazon
Walmart
HomeDepot
Lowe's
```

The marketplace is a configured integration/channel, not the Product itself.

---

# 39. Listing

Represents the external sales representation of a Product SKU.

```text
Listing
 ├── ListingId
 ├── SkuId
 ├── MarketplaceId
 ├── ExternalListingId
 ├── Status
 ├── Content
 └── SynchronizationState
```

One SKU may have multiple listings.

---

# 40. Listing Content

Marketplace-specific content may include:

```text
Title
Description
BulletPoints
Specifications
Images
Brand
Attributes
```

Different marketplaces may have different required formats.

The internal Product/SKU remains authoritative for product identity.

---

# 41. Listing Lifecycle

Initial lifecycle:

```text
Draft
   ↓
Ready
   ↓
Publishing
   ↓
Published
```

Failure path:

```text
Publishing
   ↓
Failed
   ↓
Ready
```

Retirement:

```text
Published
   ↓
Retired
```

---

# 42. Listing Invariants

1. A listing must reference a valid Product SKU.
2. A marketplace listing has its own external identity.
3. External listing IDs must not replace internal SKU identity.
4. Publishing a listing must not modify Product identity.
5. Integration failures must not corrupt Product data.
6. Repeated publish/update operations must have defined idempotency behavior.

---

# 43. Listing Operations

```text
CreateListing()
EditListingContent()
ValidateListing()
PublishListing()
UpdateListing()
RetryPublish()
RetireListing()
```

---

# 44. Listing Domain Events

```text
ListingCreated
ListingValidated
ListingPublishingStarted
ListingPublished
ListingPublishFailed
ListingRetired
```

---

# 45. Customer Domain

Customer is a reusable business concept referenced by Orders and Customer Service.

---

# 46. Customer

```text
Customer
 ├── CustomerId
 ├── Name
 ├── Contact Information
 ├── Addresses
 └── External Identities
```

A customer may have multiple external identities.

Example:

```text
Internal Customer
    │
    ├── eBay Customer ID
    ├── Amazon Customer ID
    └── Website Customer ID
```

External identities should not be assumed to be globally unique across all marketplaces.

---

# 47. Orders Domain

Orders owns the commercial transaction.

---

# 48. SalesOrder

```text
SalesOrder
 ├── OrderId
 ├── Channel
 ├── ExternalOrderId
 ├── CustomerId
 ├── Items
 ├── Payment
 ├── OrderState
 └── FulfillmentState
```

---

# 49. Order Identity

The external order identity is:

```text
Marketplace + ExternalOrderId
```

This combination must be unique.

This is essential for idempotent order imports.

---

# 50. OrderItem

An order item represents what the customer purchased.

```text
OrderItem
 ├── SKU
 ├── Quantity
 ├── UnitPrice
 ├── Product/SKU snapshot
 └── Listing context
```

The order should retain enough historical information to explain what was purchased even if the Product/SKU changes later.

---

# 51. Payment

Payment is a distinct state concept.

Initial lifecycle:

```text
Pending
   ↓
Authorized
   ↓
Paid
```

Failure paths:

```text
Failed
Refunded
PartiallyRefunded
```

Exact payment transitions will depend on integration requirements.

---

# 52. Order State

Order state is separate from payment.

Initial order lifecycle:

```text
New
   ↓
Confirmed
   ↓
Completed
```

Cancellation may occur where business rules permit:

```text
New ─────────► Cancelled
Confirmed ───► Cancelled
```

---

# 53. Fulfillment State

Fulfillment is separate from payment and shipping.

Initial lifecycle:

```text
Unfulfilled
   ↓
Allocated
   ↓
Picking
   ↓
Packed
   ↓
Fulfilled
```

Cancellation/exception paths may be introduced later.

---

# 54. Order Invariants

1. External order identity is unique per marketplace.
2. Duplicate marketplace imports do not create duplicate orders.
3. Order items reference valid commercial SKUs.
4. Historical order information remains understandable if product data changes.
5. An unpaid order cannot enter a fulfillment path requiring payment.
6. Cancellation rules depend on current order/payment/fulfillment state.
7. Order state must not be used as a substitute for payment or shipment state.

---

# 55. Order Operations

```text
ImportOrder()
ConfirmOrder()
AddOrderItem()
AuthorizePayment()
RecordPayment()
CancelOrder()
AllocateOrder()
StartPicking()
MarkPacked()
MarkFulfilled()
CompleteOrder()
```

---

# 56. Order Domain Events

```text
OrderImported
OrderConfirmed
OrderPaymentAuthorized
OrderPaid
OrderCancelled
OrderAllocated
OrderPickingStarted
OrderPacked
OrderFulfilled
OrderCompleted
```

---

# 57. Shipping Domain

Shipping owns shipment execution.

---

# 58. Shipment

A shipment represents a physical shipment created to fulfill one or more order items.

```text
Shipment
 ├── ShipmentId
 ├── OrderId
 ├── ShipmentItems
 ├── Carrier
 ├── ShippingMethod
 ├── Label
 ├── Tracking
 └── Status
```

One order may potentially have multiple shipments.

---

# 59. ShipmentItem

```text
ShipmentItem
 ├── OrderItemId
 ├── SKU
 └── Quantity
```

This allows partial shipments.

---

# 60. Shipping Method

Represents the requested service.

Examples:

```text
Ground
2Day
Overnight
Standard
```

The domain should not depend directly on a particular carrier implementation.

---

# 61. Shipping Provider

External carrier systems are represented through an integration abstraction.

Conceptual interface:

```csharp
IShippingProvider
```

Potential implementations:

```text
UPS
USPS
FedEx
Other Provider
```

---

# 62. Shipping Label

```text
ShippingLabel
 ├── LabelId
 ├── ShipmentId
 ├── Provider
 ├── LabelFormat
 ├── ExternalLabelId
 └── CreatedAt
```

The actual label file/storage mechanism is an infrastructure concern.

---

# 63. Tracking

Tracking belongs to the shipment/provider relationship.

```text
Tracking
 ├── TrackingNumber
 ├── Carrier
 ├── CurrentStatus
 └── Events
```

Tracking events may include:

```text
LabelCreated
PickedUp
InTransit
OutForDelivery
Delivered
Exception
Returned
```

---

# 64. Shipment Lifecycle

Initial lifecycle:

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

Exceptional states:

```text
LabelFailed
ShipmentCancelled
DeliveryException
Returned
```

---

# 65. Shipping Invariants

1. Shipment must reference a valid order.
2. Shipment quantity cannot exceed fulfillable quantity.
3. A normal shipment requires the appropriate payment/fulfillment condition.
4. Duplicate shipment creation must be prevented.
5. Duplicate label requests must have defined idempotency behavior.
6. Carrier updates must be idempotent.
7. Delivery cannot be recorded against a nonexistent shipment.

---

# 66. Shipping Operations

```text
CreateShipment()
AddShipmentItem()
CreateLabel()
RetryLabel()
RecordShipment()
RecordTrackingUpdate()
RecordDelivery()
CancelShipment()
RecordReturn()
```

---

# 67. Shipping Domain Events

```text
ShipmentCreated
ShippingLabelCreated
ShippingLabelFailed
ShipmentShipped
ShipmentTrackingUpdated
ShipmentDelivered
ShipmentReturned
```

---

# 68. Customer Service Domain

Customer Service owns communications and support cases.

---

# 69. Conversation

A conversation represents a customer-service thread.

```text
Conversation
 ├── ConversationId
 ├── CustomerId
 ├── Channel
 ├── RelatedOrder
 ├── RelatedListing
 ├── RelatedShipment
 ├── Status
 └── Messages
```

---

# 70. Message

```text
Message
 ├── MessageId
 ├── ConversationId
 ├── Direction
 ├── ExternalMessageId
 ├── ReceivedAt
 ├── SentAt
 └── Content
```

Direction:

```text
Inbound
Outbound
```

---

# 71. Communication Channels

Initial channels:

```text
Marketplace
Email
Phone
```

The system should be able to add future channels without redesigning the conversation model.

---

# 72. Conversation Lifecycle

Initial lifecycle:

```text
Open
   ↓
InProgress
   ↓
Waiting
   ↓
Resolved
   ↓
Closed
```

A conversation may be reopened.

---

# 73. Customer Service Invariants

1. External messages must be idempotent.
2. Duplicate inbound messages must not create duplicate conversation entries.
3. A response must be associated with a valid conversation.
4. Customer Service may reference Orders, Listings, Shipments, and SKUs but does not own their state.
5. Closing a conversation does not change the underlying Order or Shipment automatically unless an explicit business operation performs that action.

---

# 74. Customer Service Operations

```text
CreateConversation()
ReceiveMessage()
ReplyToConversation()
AssignConversation()
ChangeConversationStatus()
ResolveConversation()
ReopenConversation()
```

---

# 75. Customer Service Domain Events

```text
ConversationCreated
CustomerMessageReceived
CustomerMessageSent
ConversationAssigned
ConversationResolved
ConversationReopened
```

---

# 76. Analytics Domain

Analytics is not the authoritative owner of operational business state.

Analytics consumes facts from operational contexts and creates projections/statistics.

---

# 77. Analytics Model

Potential projections:

```text
SalesMetrics
InventoryMetrics
ManufacturingMetrics
ListingMetrics
ShippingMetrics
CustomerServiceMetrics
```

Analytics may contain denormalized read models optimized for reporting.

It should not directly modify operational aggregates.

---

# 78. Analytics Events

Analytics may consume:

```text
ManufacturingRevisionApproved
SkuActivated
InventoryReceived
InventoryAdjusted
ListingPublished
OrderImported
OrderPaid
OrderCompleted
ShipmentShipped
ShipmentDelivered
ConversationResolved
```

---

# 79. Cross-Domain Rules

## 79.1 Manufacturing → Product

Manufacturing establishes whether a design/revision has completed the required development and approval process.

Product owns commercial SKU identity.

---

## 79.2 Product → Warehouse

Product/SKU provides the business identity and eligibility required for warehouse operations.

Warehouse owns actual physical quantity.

---

## 79.3 Product → Listing

Listing references Product SKUs.

Listing owns marketplace-specific representation.

---

## 79.4 Listing → Orders

Marketplace orders identify the external commercial transaction.

Orders retain marketplace identity and order history.

---

## 79.5 Orders → Warehouse

Orders create fulfillment demand.

Warehouse determines physical inventory availability.

Neither domain should silently overwrite the other's state.

---

## 79.6 Orders → Shipping

Orders determine what needs to be fulfilled.

Shipping determines the physical shipment lifecycle.

---

## 79.7 Shipping → Customer Service

Customer Service can use shipment information to answer customer questions.

Customer Service does not directly change shipment status.

---

# 80. Domain Commands

The initial business command vocabulary is:

```text
Manufacturing
    CreateProject
    CreateRevision
    RecordTestResult
    ApproveRevision
    ReleaseRevision

Product
    CreateProduct
    CreateSku
    ApproveSku
    ActivateSku
    RetireSku
    CreateKit
    AddKitComponent
    RemoveKitComponent

Warehouse
    ReceiveInventory
    AdjustInventory
    TransferInventory
    PickInventory
    ReturnInventory

Listing
    CreateListing
    ValidateListing
    PublishListing
    UpdateListing
    RetireListing

Orders
    ImportOrder
    ConfirmOrder
    RecordPayment
    CancelOrder
    AllocateOrder
    StartPicking
    MarkPacked
    MarkFulfilled
    CompleteOrder

Shipping
    CreateShipment
    CreateLabel
    Ship
    RecordTrackingUpdate
    RecordDelivery

Customer Service
    CreateConversation
    ReceiveMessage
    Reply
    ResolveConversation
    ReopenConversation
```

These commands are domain vocabulary, not yet API endpoint definitions.

---

# 81. Domain Events

Domain events represent facts that have already occurred.

Examples:

```text
ManufacturingRevisionApproved
SkuApproved
SkuActivated
KitCompositionChanged

InventoryReceived
InventoryAdjusted
InventoryTransferred

ListingPublished
ListingPublishFailed

OrderImported
OrderPaid
OrderFulfilled

ShipmentCreated
ShipmentShipped
ShipmentDelivered

CustomerMessageReceived
ConversationResolved
```

Events should communicate meaningful business facts.

They should not simply mirror every database CRUD operation.

---

# 82. Aggregate Rule

An aggregate is a consistency boundary.

Rules that must remain atomically consistent should generally be inside the same aggregate.

Examples:

```text
Kit
    Parent SKU
    + Components

SalesOrder
    Order
    + Order Items
    + Relevant Order State

Shipment
    Shipment
    + Shipment Items
    + Shipment State
```

Operations spanning multiple aggregates should be coordinated at the application layer or through domain events rather than forcing every entity into one giant aggregate.

---

# 83. Important Aggregate Boundary Decision

Do not create one giant aggregate containing:

```text
Product
Manufacturing
Inventory
Listing
Order
Shipment
Customer
```

These are separate business lifecycles.

They communicate through explicit application operations and events.

---

# 84. State Machine Principles

State values alone are not sufficient.

Every state transition needs:

```text
Current State
+
Command
+
Authorization
+
Business Preconditions
=
New State
```

Example:

```text
Sku.PendingApproval
       │
       │ ApproveSku()
       ▼
Sku.Approved
```

The command may fail if:

```text
User unauthorized
OR
Required manufacturing approval missing
OR
Required product information missing
```

---

# 85. Historical Data Rules

The following records should generally preserve historical meaning:

```text
Manufacturing revisions
Manufacturing test results
Inventory transactions
Orders
Order items
Payments
Shipments
Tracking events
Customer messages
Audit events
```

A current Product/SKU definition must not be the only source of truth for explaining a past order.

---

# 86. Integration Identity

External systems use their own identifiers.

Examples:

```text
Marketplace Order ID
Marketplace Listing ID
Marketplace Message ID
Carrier Tracking Number
Carrier Label ID
```

For each external integration, define:

```text
External System
External Identifier
Internal Entity
Idempotency Strategy
```

---

# 87. Idempotency Requirements

The following operations require explicit retry behavior:

```text
Marketplace order import
Marketplace listing publish
Marketplace message receive
Inventory receipt
Shipment creation
Shipping label creation
Carrier webhook processing
Payment webhook processing
```

The design must answer:

> What happens if the exact same operation is received twice?

before integration code is written.

---

# 88. Security Boundary

Domain rules and authorization are different.

Example:

```text
User has:
    Warehouse / Inventory / Edit
```

does not mean:

```text
Inventory adjustment is automatically valid.
```

The system must check both:

```text
Authorization
+
Business Invariant
```

Example:

```text
CanEditInventory(user)
        AND
AdjustmentIsValid(command)
```

---

# 89. Audit Boundary

Not every field change requires a full audit event.

High-value business actions do.

Examples:

```text
ApproveRevision
ApproveSku
ActivateSku
AdjustInventory
CancelOrder
RecordPayment
CreateShipment
PublishListing
ChangeRole
ChangePermission
```

---

# 90. Domain Model Decisions Deferred

The following decisions remain intentionally open:

* Exact EF Core persistence model
* Exact database schema
* Strongly typed IDs vs primitive IDs
* Detailed Product attribute model
* Product versioning strategy
* Pricing domain
* Procurement/purchasing
* Returns/RMA domain
* Accounting integration
* Tax model
* Multi-company support
* Multi-currency support
* Multi-language catalog
* Advanced warehouse reservations
* Marketplace-specific schemas
* Event transport mechanism
* Background job framework
* Distributed messaging
* Reporting database strategy

These should be added only when justified by actual requirements.

---

# 91. First Domain Implementation Priorities

The domain should be implemented in this order:

```text
1. Identity / Authorization

2. Manufacturing
   Project
   Revision
   Test
   Approval

3. Product / SKU
   Product
   SKU
   Kit
   SKU generation

4. Warehouse
   Warehouse
   Location
   Inventory
   Inventory transactions

5. Listing

6. Orders

7. Shipping

8. Customer

9. Customer Service

10. Analytics
```

---

# 92. Domain Definition of Done

The domain model is ready for implementation when:

* Every major business concept has an owner.
* Aggregate boundaries are understood.
* Important lifecycle states are defined.
* Important state transitions are named.
* Critical invariants are documented.
* Cross-module responsibilities are clear.
* External identities are separated from internal identities.
* Idempotency requirements are identified.
* Historical data requirements are understood.
* Authorization is separated from business rules.
* No module depends on another module's internal implementation details.

---

# 93. Next Design Step

The next document is:

```text
docs/03-security-and-rbac.md
```

It should convert the high-level authorization model into a precise security design covering:

```text
User
Role
Permission
Module
Section
Access Level
Policy
Resource authorization
Authentication
Session/security behavior
Audit
```

Only after the security model is complete should Phase 00 define the concrete .NET solution and persistence structure.
