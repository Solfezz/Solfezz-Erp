# Phase 00 — Foundation

**Project:** ERP Platform
**Phase:** 00
**Status:** Planned
**Purpose:** Establish a clean, secure, testable .NET application foundation before implementing business modules.

---

# 1. Phase Objective

Create the minimum production-quality application foundation required for the ERP.

At the end of Phase 00 we should have:

```text id="d7dr5s"
Repository
   ↓
.NET Solution
   ↓
Application Structure
   ↓
Build
   ↓
Automated Tests
   ↓
PostgreSQL Development Environment
   ↓
Configuration
   ↓
Logging
   ↓
Health Check
   ↓
CI
```

The phase does **not** implement Manufacturing, Product, Warehouse, Listing, Orders, Shipping, or Customer Service functionality.

---

# 2. Phase Principles

Phase 00 follows these rules:

1. Small slices.
2. One architectural concern at a time.
3. Keep the application buildable after every slice.
4. Tests accompany infrastructure where practical.
5. No speculative dependencies.
6. No premature business abstractions.
7. No generic framework layers without a demonstrated need.
8. Every slice ends with verification.

---

# 3. Definition of Done

Phase 00 is complete when:

* Solution builds successfully.
* Tests execute successfully.
* Repository structure is established.
* Local PostgreSQL can be started reproducibly.
* Application can connect to the development database.
* Configuration is environment-aware.
* Secrets are not committed.
* Structured logging is configured.
* Health checks exist.
* CI can restore, build, and test.
* Architecture rules are testable.
* Documentation reflects the actual implementation.
* Git history follows the project's commit standard.

---

# 4. Slice 00.1 — Repository Foundation

## Goal

Establish repository-level files and development conventions.

## Work

Create:

```text id="5krdk7"
.gitignore
README.md
.editorconfig
global.json
docs/
src/
tests/
```

Review existing README and architecture documents for consistency.

## Verification

```text id="6jig7g"
Git status
Expected:
only intentional files are tracked
```

No generated build artifacts should appear.

## Commit

```text id="c2i0dh"
chore(repo): establish repository foundation
```

---

# 5. Slice 00.2 — .NET SDK and Solution

## Goal

Create the solution and pin the intended .NET SDK.

## Work

Create:

```text id="j4awpe"
Erp.sln
```

and establish the initial source/test structure.

Initial projects:

```text id="42sh5x"
src/
    Erp.Web
    Erp.Application
    Erp.Domain
    Erp.Infrastructure

tests/
    Erp.Domain.Tests
    Erp.Application.Tests
    Erp.IntegrationTests
    Erp.ArchitectureTests
```

Do not add every future module as a project yet.

The folder/module structure can grow incrementally.

## Verification

```text id="a4a7lm"
dotnet restore
dotnet build
dotnet test
```

Expected:

```text id="8q1sjv"
Restore succeeds
Build succeeds
Tests execute successfully
```

## Commit

```text id="tcvqh8"
chore(build): create initial dotnet solution
```

---

# 6. Slice 00.3 — Nullable and Compiler Standards

## Goal

Make compiler behavior support reliable code from the beginning.

## Work

Establish:

```text id="gzo39u"
Nullable reference types
Implicit usings
Warnings policy
Language version
Target framework
```

Warnings that indicate genuine defects should not simply be hidden.

Avoid beginning the project with:

```text id="jjpl8f"
NoWarn = "*"
```

or broad warning suppression.

## Verification

Run:

```text id="kffy6m"
dotnet build
```

The build should have an intentionally controlled warning state.

## Commit

```text id="yzo8ul"
chore(build): establish csharp compiler standards
```

---

# 7. Slice 00.4 — Domain Project Foundation

## Goal

Create the clean Domain boundary.

## Work

Establish minimal domain primitives required by the architecture.

Do not create the complete ERP entity model yet.

Possible initial concepts:

```text id="a8x26y"
Entity identification strategy
Domain event abstraction
Business rule/error primitives
Time abstraction if justified
```

Avoid introducing:

```text id="rexeoj"
Product.cs
Order.cs
Warehouse.cs
```

at this stage.

## Verification

Architecture must confirm:

```text id="n2wt89"
Domain
    ↓
No Infrastructure reference
No Web reference
No EF Core dependency
```

## Commit

```text id="v4q6vf"
chore(domain): establish domain project foundation
```

---

# 8. Slice 00.5 — Application Project Foundation

## Goal

Establish the application/use-case boundary.

## Work

Create the Application project with minimal abstractions.

Initial concepts may include:

```text id="eg0e1r"
Command/use-case conventions
Application result/error conventions
Authorization abstraction
Integration contracts
```

Do not create generic:

```csharp id="flk3wq"
IService<T>
IRepository<T>
IHandler<T>
```

unless a concrete use case proves the abstraction useful.

## Verification

Application can depend on Domain.

Application must not depend on:

```text id="te2yff"
Web UI implementation
PostgreSQL provider
Concrete external marketplace SDK
```

## Commit

```text id="5w9u2b"
chore(application): establish application layer
```

---

# 9. Slice 00.6 — Web Project Foundation

## Goal

Create the ASP.NET Core application host.

## Work

Establish:

```text id="w1ofnt"
ASP.NET Core host
Dependency injection
Configuration
Environment handling
Basic routing/endpoints
Global error handling foundation
```

No ERP screens yet.

## Verification

Application starts successfully.

Expected:

```text id="y8j9b6"
dotnet run
```

and a basic health/application endpoint responds.

## Commit

```text id="9gagw7"
chore(web): establish aspnet core application host
```

---

# 10. Slice 00.7 — Infrastructure Foundation

## Goal

Create the infrastructure composition layer.

## Work

Establish:

```text id="kspj56"
Dependency injection registration
Persistence registration
Infrastructure options
External integration abstraction registration
```

The Infrastructure project may reference:

```text id="8w1y6u"
Domain
Application
```

and required technical frameworks.

## Verification

Application starts using the real Infrastructure registration path.

No manual service construction should be required inside business code.

## Commit

```text id="n4d07n"
chore(infrastructure): establish infrastructure composition
```

---

# 11. Slice 00.8 — PostgreSQL Development Environment

## Goal

Make the development database reproducible.

## Work

Establish Docker-based PostgreSQL configuration.

Example repository structure:

```text id="6t9qjg"
docker/
docker-compose.yml
```

or an equivalent documented development setup.

The repository must not contain real credentials.

Use safe development defaults or environment/user-secret configuration.

## Verification

Developer can:

```text id="cbw7bt"
start PostgreSQL
stop PostgreSQL
restart PostgreSQL
```

and the application can connect successfully.

## Commit

```text id="3ojtqc"
chore(dev): add local postgres environment
```

---

# 12. Slice 00.9 — EF Core Foundation

## Goal

Establish persistence infrastructure without designing the ERP schema.

## Work

Add EF Core and PostgreSQL provider.

Create the minimal persistence configuration.

Do not build all future tables.

Do not reverse-engineer the complete domain into database entities.

## Verification

Application can establish a database connection.

Migration infrastructure is functional.

## Commit

```text id="e6nr4x"
chore(data): establish ef core persistence
```

---

# 13. Slice 00.10 — Configuration

## Goal

Make environment-specific configuration predictable and safe.

## Work

Establish:

```text id="s84v2x"
appsettings.json
appsettings.Development.json
User Secrets / environment configuration
```

Typed options should be used for meaningful configuration groups.

Examples:

```text id="t6kbq2"
DatabaseOptions
AuthenticationOptions
ApplicationOptions
```

Do not create dozens of option classes without actual configuration needs.

## Verification

Application starts under different environments with expected configuration.

Secrets remain outside Git.

## Commit

```text id="z1s0nm"
chore(config): establish application configuration
```

---

# 14. Slice 00.11 — Logging

## Goal

Establish structured operational logging.

## Work

Configure application logging with:

```text id="ve22f3"
Timestamp
Log level
Message
Correlation context where available
```

Do not log secrets.

## Verification

Start the application and confirm useful structured logs are produced.

## Commit

```text id="2f3x3o"
chore(observability): configure structured logging
```

---

# 15. Slice 00.12 — Health Checks

## Goal

Provide a basic health model.

## Work

Create health checks for:

```text id="uixb5b"
Application
Database
```

Potential future checks:

```text id="x4d3kj"
Marketplace providers
Shipping providers
Email providers
```

Do not make external services mandatory dependencies for the first health endpoint.

## Verification

Health endpoint reports application/database state correctly.

## Commit

```text id="fd4t9j"
feat(health): add application health checks
```

---

# 16. Slice 00.13 — Architecture Tests

## Goal

Make architectural rules executable.

## Work

Add tests verifying rules such as:

```text id="k66b1w"
Domain cannot reference Web.
Domain cannot reference Infrastructure.
Application does not reference Web.
```

As module projects are introduced, module dependency rules should also be tested.

## Verification

Intentionally violating an architecture rule should cause the architecture test to fail.

## Commit

```text id="48rxx7"
test(architecture): enforce dependency boundaries
```

---

# 17. Slice 00.14 — CI Foundation

## Goal

Prevent broken code from being merged.

## Work

Create GitHub Actions workflow.

Initial pipeline:

```text id="eym3vu"
Checkout
   ↓
Setup .NET
   ↓
Restore
   ↓
Build
   ↓
Test
```

Later CI stages may add:

```text id="1qfizk"
Formatting
Static analysis
Security scanning
Container build
Integration tests
```

Do not overload the first workflow.

## Verification

Push a branch and confirm GitHub Actions succeeds.

## Commit

```text id="x0ilp5"
chore(ci): add dotnet build and test workflow
```

---

# 18. Slice 00.15 — Foundation Smoke Test

## Goal

Prove a fresh environment can reproduce the project.

Starting from a clean checkout:

```text id="db5l7p"
Clone
 ↓
Install required SDK
 ↓
Start PostgreSQL
 ↓
Restore
 ↓
Build
 ↓
Test
 ↓
Run application
 ↓
Health check
```

The process should be documented in the README.

This is the first true developer-experience test.

## Commit

```text id="h2v9w3"
test(repo): verify clean environment setup
```

---

# 19. Phase 00 Final Review

Before moving to Phase 01, review:

### Architecture

```text id="dtc0mx"
Are dependency directions clean?
Are project references intentional?
Are boundaries enforceable?
```

### Build

```text id="3u7b2d"
Can the application restore/build/test?
```

### Database

```text id="nh1qy9"
Can PostgreSQL be reproduced locally?
Can the application connect?
```

### Configuration

```text id="8o2wvf"
Are secrets excluded?
Are environments predictable?
```

### Testing

```text id="vl5kl8"
Do unit/integration/architecture test foundations work?
```

### CI

```text id="m22f3t"
Does a clean push produce a passing pipeline?
```

---

# 20. Phase 00 Final Commit

Only after the phase passes review:

```text id="q8gmh9"
chore(phase-00): complete application foundation
```

The phase completion commit is optional if the individual commits already tell the complete story.

Do not create meaningless "final" commits simply to mark a phase.

---

# 21. Phase 00 Exit Criteria

Phase 00 can move to Phase 01 only when:

```text id="q6j6j4"
✓ Repository clean
✓ Solution builds
✓ Tests pass
✓ Architecture tests pass
✓ PostgreSQL reproducible
✓ Database connection verified
✓ Configuration verified
✓ Secrets protected
✓ Logging operational
✓ Health checks operational
✓ CI passing
✓ Fresh-checkout setup verified
✓ Documentation updated
```

---

# 22. Phase 01 Entry Condition

Phase 01 begins only after Phase 00 is explicitly accepted.

Phase 01 target:

```text id="tpm6ue"
Identity
   ↓
Authentication
   ↓
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
Read/Edit
   ↓
Server-side authorization
```

The first Identity/RBAC slice must be designed separately before implementation.

---

# 23. Senior Tech Lead Gate

At the end of every slice:

```text id="wqg1cs"
Implementation
      ↓
Build
      ↓
Tests
      ↓
Manual verification
      ↓
Diff review
      ↓
Documentation
      ↓
Commit
      ↓
STOP
```

No automatic progression to the next slice.

The next slice begins only after the previous slice has been reviewed and accepted.

