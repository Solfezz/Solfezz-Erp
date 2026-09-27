# Web Conventions

## Purpose

`Erp.Web` is the ERP HTTP host, presentation boundary, and composition root.

It translates external requests into Application use cases and translates application results into HTTP responses.

## Responsibilities

Web may contain:

- HTTP endpoints
- Controllers or endpoint definitions
- HTTP request and response models when appropriate
- Authentication middleware
- Authorization policies
- Dependency injection composition
- Middleware
- API versioning when required
- HTTP-specific validation
- HTTP error mapping
- Host configuration

## Dependency Direction

Web may depend on:

- Erp.Application
- Erp.Infrastructure

Web should normally access domain behavior through Application rather than directly manipulating domain entities.

## Web Is Not the Business Layer

Do not place business invariants in controllers or endpoints.

Bad:

```text
Controller:
if manufacturing tests passed
    sku.Status = Approved
    