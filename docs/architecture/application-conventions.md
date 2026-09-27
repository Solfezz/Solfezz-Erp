# Application Conventions

## Purpose

`Erp.Application` coordinates ERP use cases.

The Application layer sits between external entry points and the Domain.

Its responsibilities include:

- Orchestrating use cases
- Loading required domain state
- Calling domain behavior
- Coordinating persistence
- Performing application-level authorization
- Managing transaction boundaries
- Handling idempotency at appropriate workflow boundaries
- Calling abstractions for external capabilities
- Returning application results to callers

## Dependency Rule

Application may depend on:

- Erp.Domain
- .NET runtime libraries

Application must not depend on:

- Erp.Infrastructure
- Erp.Web
- Entity Framework implementation details
- PostgreSQL-specific APIs
- ASP.NET controllers or HTTP request types
- Marketplace SDK implementations
- Shipping SDK implementations

Intended direction:

Erp.Web
    ↓
Erp.Application
    ↓
Erp.Domain

Erp.Infrastructure
    ↓
Erp.Application / Erp.Domain

## Application Use Cases

Application code should be organized around business operations rather than generic CRUD services.

Future examples:

- CreateSku
- ApproveSku
- ReleaseSku
- ReceiveInventory
- AdjustInventory
- PublishListing
- ImportOrder
- MarkOrderPaid
- CreateShipment
- ResolveConversation

Prefer an explicit use case such as:

ApproveSku

over a generic service such as:

SkuService.UpdateStatus(...)

## Domain vs Application Responsibility

The Application layer coordinates.

The Domain layer decides business validity.

Example:

Application:
- verify caller may approve SKUs
- load SKU
- begin transaction
- call Sku.Approve()
- persist changes
- commit transaction

Domain:
- determine whether the SKU is actually eligible for approval
- enforce valid state transitions
- preserve business invariants

Application authorization does not replace domain validity.

A user may have permission to approve a SKU while the SKU itself is still ineligible for approval.

## External Capabilities

When an application use case requires infrastructure, Application may define an abstraction describing what it needs.

Examples may eventually include:

- IOrderRepository
- IInventoryRepository
- IShippingProvider
- IMarketplaceOrderSource
- IClock

Infrastructure provides implementations.

Example:

Application:
IShippingProvider

Infrastructure:
UpsShippingProvider

Do not create abstractions before a concrete use case requires them.

Avoid generic abstractions such as:

- IRepository<T>
- IService<T>
- IHandler<T>

unless a demonstrated requirement justifies them.

## Commands and Queries

The ERP may use lightweight command/query separation.

Examples:

Commands:
- ApproveSku
- ReceiveInventory
- CreateShipment

Queries:
- GetSkuDetails
- GetInventoryPosition
- GetOrderDetails

This does not require full CQRS infrastructure.

Do not introduce MediatR or a messaging framework simply to implement command/query separation.

## DTOs and Contracts

Application-facing request and result models may live in Application when they represent use-case inputs or outputs.

They should not replace domain entities.

Avoid returning mutable domain entities directly across external boundaries without considering the contract being exposed.

## Transactions

Application normally owns use-case transaction boundaries.

Example:

ImportOrder
    ↓
detect duplicate external identity
    ↓
create Order
    ↓
create OrderItems
    ↓
persist
    ↓
commit

The Domain should not open database transactions.

## Idempotency

External workflows such as:

- marketplace order import
- shipment creation
- label purchase
- webhook processing

must eventually consider idempotency.

Application is a natural coordination layer for those workflows.

Implementation will be added only when required by a concrete use case.

## Review Question

Before placing code in `Erp.Application`, ask:

> Is this code coordinating a business operation, or is it defining the business rule itself?

If it defines business truth, it probably belongs in Domain.

If it coordinates persistence, authorization, external capabilities, or a multi-step use case, it likely belongs in Application.
