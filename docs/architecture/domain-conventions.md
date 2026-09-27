# Domain Conventions

## Purpose

`Erp.Domain` contains the core business model and business rules for the ERP.

The Domain must remain independent of technical infrastructure.

## Allowed in Domain

Examples:

- Entities
- Value objects
- Business invariants
- Domain services when behavior does not naturally belong to one entity
- Domain-specific exceptions when justified
- Domain events when justified by a concrete workflow
- Business state transitions
- Domain interfaces only when the abstraction represents a business need

## Not Allowed in Domain

Do not add:

- ASP.NET Core types
- Controllers or HTTP concerns
- Entity Framework Core
- PostgreSQL-specific code
- Database connections
- Marketplace SDKs
- Shipping SDKs
- Logging frameworks
- Dependency injection registration
- Configuration loading
- UI concerns
- Serialization-specific behavior unless explicitly required by the domain

## Dependency Rule

The Domain project must not reference:

- Erp.Application
- Erp.Infrastructure
- Erp.Web

The intended direction is:

Erp.Web
    ↓
Erp.Application
    ↓
Erp.Domain

Erp.Infrastructure
    ↓
Erp.Application / Erp.Domain

## Module Organization

Domain types are grouped by business capability.

Examples:

- Erp.Domain.Manufacturing
- Erp.Domain.Products
- Erp.Domain.Warehouse
- Erp.Domain.Listings
- Erp.Domain.Orders
- Erp.Domain.Shipping
- Erp.Domain.Customers
- Erp.Domain.CustomerService

Folders should be created only when actual domain types are introduced.

Do not create empty folders or placeholder classes.

## Domain Design Rules

Prefer explicit business operations.

Examples:

- Approve()
- Release()
- Activate()
- ReceiveInventory()
- AdjustInventory()

Avoid exposing state changes as unrestricted public setters.

Prefer:

```csharp
sku.Approve();
