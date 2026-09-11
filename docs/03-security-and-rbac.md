# Security and RBAC

**Project:** ERP Platform
**Document:** Security and Role-Based Access Control
**Version:** 0.1
**Status:** Draft
**Phase:** 00 — Architecture Foundation

---

# 1. Purpose

This document defines the initial authentication, authorization, role, permission, and security model for the ERP platform.

The objectives are:

* authenticate users securely,
* control access through roles,
* grant permissions at module and section level,
* distinguish read and edit authority,
* enforce authorization server-side,
* provide auditable security operations,
* provide a foundation for future permission types without overengineering the initial system.

---

# 2. Security Principles

## 2.1 Deny by Default

A user has no access unless access is explicitly granted.

```text
No Permission
      ↓
Access Denied
```

The application must not infer access merely because a user is authenticated.

---

## 2.2 Server-Side Enforcement

The browser/UI is never the security boundary.

The following must all enforce authorization where applicable:

```text
UI
 ↓
Application Service / Use Case
 ↓
Domain Operation
 ↓
Persistence
```

Hiding an Edit button does not prevent an unauthorized HTTP request.

---

## 2.3 Authentication ≠ Authorization

Authentication answers:

> Who are you?

Authorization answers:

> Are you allowed to perform this operation?

They must remain separate concepts.

---

## 2.4 Business Rules ≠ Permissions

Having permission to edit a record does not mean the operation is automatically valid.

Example:

```text
User:
    Inventory / Edit

Request:
    AdjustInventory(-500)
```

Authorization may pass while the business rule fails because the adjustment is invalid.

Therefore:

```text
Authorization
+
Business Rules
=
Valid Operation
```

---

# 3. Security Model

The initial model is:

```text
User
  ↓
Role Assignment
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

---

# 4. User

The User represents an authenticated human or application identity.

Conceptually:

```text
User
 ├── UserId
 ├── Login Identity
 ├── Email
 ├── Status
 ├── Roles
 └── Security Metadata
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

---

# 5. User Security Rules

A user must be:

```text
Authenticated
AND
Active
```

before protected application operations can proceed.

Suspended and deactivated users must not be permitted to access protected functionality.

---

# 6. Role

A Role is a named collection of permissions.

Examples:

```text
Administrator
ManufacturingUser
WarehouseUser
ListingManager
OrderManager
CSR
Manager
Executive
```

Roles should describe a business responsibility rather than becoming arbitrary collections of individual users.

---

# 7. Role Assignment

A user may have multiple roles.

Example:

```text
User: John

Roles:
    WarehouseUser
    InventoryManager
```

Effective permissions are calculated from all active roles assigned to the user.

---

# 8. Permission

A Permission identifies what part of the system a role may access.

Initial structure:

```text
Permission
 ├── Module
 ├── Section
 └── AccessLevel
```

Example:

```text
Warehouse
    Inventory
        Read

Warehouse
    Inventory
        Edit
```

---

# 9. Access Levels

Initial access levels:

```text
Read
Edit
```

### Read

Allows viewing permitted information.

### Edit

Allows permitted modifications and normally implies Read.

Therefore:

```text
Edit ≥ Read
```

The system should not require a user to have two separate permissions for ordinary viewing and editing of the same section.

---

# 10. Future Access Levels

The design should permit future levels such as:

```text
Create
Delete
Approve
Publish
Export
Admin
```

However, they should not be implemented until a real requirement exists.

Avoid building an authorization system with dozens of unused permission types.

---

# 11. Module Catalog

The initial module catalog is:

```text
Identity
Manufacturing
Product
Warehouse
Listing
Orders
Shipping
Customer
CustomerService
Analytics
```

Each module may expose one or more sections.

---

# 12. Example Section Catalog

## Manufacturing

```text
Projects
Revisions
Prototypes
Testing
Approvals
```

## Product

```text
Products
SKUs
Kits
SKU Rules
Approvals
```

## Warehouse

```text
Warehouses
Locations
Inventory
Receiving
Transfers
Adjustments
```

## Listing

```text
Marketplaces
Listings
Content
Publishing
Synchronization
```

## Orders

```text
Orders
Order Items
Payments
Fulfillment
```

## Shipping

```text
Shipments
Labels
Tracking
Carriers
```

## Customer

```text
Customers
Addresses
External Identities
```

## Customer Service

```text
Conversations
Messages
Assignments
```

## Analytics

```text
Dashboards
Reports
Statistics
```

The section list is a starting catalog and may change as domain requirements become more precise.

---

# 13. Example Permission Set

WarehouseUser:

```text
Warehouse / Warehouses / Read
Warehouse / Locations / Read + Edit
Warehouse / Inventory / Read + Edit
Warehouse / Receiving / Read + Edit
Warehouse / Transfers / Read + Edit

Product / Products / Read
Product / SKUs / Read
```

CSR:

```text
Customer / Customers / Read + Edit

CustomerService / Conversations / Read + Edit
CustomerService / Messages / Read + Edit

Orders / Orders / Read
Orders / Fulfillment / Read

Shipping / Shipments / Read
Shipping / Tracking / Read
```

Executive:

```text
Analytics / Dashboards / Read
Analytics / Reports / Read

Manufacturing / Projects / Read
Product / Products / Read
Product / SKUs / Read
Warehouse / Inventory / Read
Orders / Orders / Read
Shipping / Shipments / Read
```

---

# 14. Permission Resolution

Given:

```text
User
Action
Module
Section
```

the system evaluates:

```text
User authenticated?
        ↓
User active?
        ↓
Applicable role?
        ↓
Role contains required permission?
        ↓
Requested access level sufficient?
        ↓
Allow
```

Otherwise:

```text
Deny
```

---

# 15. Multiple Roles

If a user has multiple roles, effective permissions are the union of granted permissions.

Example:

```text
Role A:
Warehouse / Inventory / Read

Role B:
Warehouse / Inventory / Edit
```

Effective permission:

```text
Warehouse / Inventory / Edit
```

---

# 16. No Implicit Cross-Module Permissions

Permission inheritance between unrelated modules should not be automatic.

Example:

```text
Product / SKUs / Edit
```

does not imply:

```text
Warehouse / Inventory / Edit
```

The user must be explicitly granted the Warehouse permission.

This prevents accidental privilege escalation.

---

# 17. Edit Implies Read

For initial implementation:

```text
Edit
```

is considered sufficient for:

```text
Read
```

Therefore:

```text
HasEditPermission()
```

should satisfy a normal read requirement.

---

# 18. Permission Checking

The application should provide a consistent authorization mechanism.

Conceptual API:

```csharp
CanAccess(
    user,
    module,
    section,
    accessLevel
)
```

Possible result:

```text
Allowed
Denied
```

Actual implementation should use ASP.NET Core authorization policies rather than allowing controllers/pages to implement ad-hoc permission logic.

---

# 19. Policy-Based Authorization

The preferred implementation direction is policy-based authorization.

Conceptually:

```text
Policy
   ↓
Permission Requirement
   ↓
Current User
   ↓
Role/Permission Evaluation
```

This keeps authorization logic centralized.

Avoid repeatedly writing:

```csharp
if (user.Role == "Admin")
```

throughout the application.

---

# 20. Resource Authorization

Module/section permission is not always sufficient.

Some operations may require access to a specific resource.

Example:

```text
Warehouse / Inventory / Edit
```

may authorize inventory editing generally, but future business rules may restrict a user to:

```text
Warehouse A
```

while another user may access:

```text
Warehouse B
```

Resource-level authorization should therefore remain possible without being required in the first slice.

---

# 21. Approval Permissions

Approval should be treated differently from ordinary editing when the business domain requires it.

Example:

```text
Product / SKUs / Edit
```

does not automatically imply:

```text
Product / SKUs / Approve
```

For Phase 01, approval-specific permission types may remain deferred.

When introduced, approval should require both:

```text
Approval Permission
+
Business Approval Preconditions
```

---

# 22. Authentication

Initial authentication should use established ASP.NET Core identity/security mechanisms rather than a custom password system.

Responsibilities include:

* user authentication,
* password hashing,
* password validation,
* account lockout/rate limiting where appropriate,
* password reset,
* session/cookie security,
* account status enforcement.

Passwords must never be stored in plaintext or with custom hashing code.

---

# 23. Authentication Identity

The application needs a stable internal user identity.

External identity providers may be added later.

The design should not assume that:

```text
Email == UserId
```

or that email addresses can never change.

---

# 24. Password Security

Do not implement:

```text
SHA256(password)
MD5(password)
CustomHash(password)
```

The application should rely on a maintained identity framework and secure password-hashing implementation.

Application code must not manage password hashes manually.

---

# 25. Session Security

Authenticated sessions must use secure platform mechanisms.

Initial requirements:

* Secure cookies
* HttpOnly cookies
* Appropriate SameSite behavior
* HTTPS in deployed environments
* Session expiration
* Reauthentication where sensitive operations require it
* Protection against session fixation

Exact configuration belongs in the implementation/security hardening phase.

---

# 26. Login Protection

Login endpoints should be protected against abuse.

The implementation should consider:

```text
Failed login attempts
Rate limiting
Account lockout or temporary blocking
Suspicious activity logging
```

Exact thresholds should be configuration-driven and validated before production.

---

# 27. Authorization Failure

When the user is authenticated but lacks permission:

```text
Authorization denied
```

The system must not expose internal permission configuration or sensitive business details.

For a web request, the response should use the appropriate HTTP/application behavior for an authenticated but unauthorized request.

---

# 28. Authentication Failure

When the user is not authenticated:

```text
Authentication required
```

The application should use the appropriate authentication challenge behavior.

---

# 29. UI Behavior

The UI may hide controls the user cannot use.

Example:

```text
User has Read only

[View Product]
[Edit] ← hidden
```

However, the application must still enforce:

```text
PUT /product/123
```

server-side.

UI hiding is a usability feature, not a security mechanism.

---

# 30. API/Application Security

Every protected use case must have authorization.

Examples:

```text
ApproveSku()
AdjustInventory()
CancelOrder()
PublishListing()
CreateShipment()
ResolveConversation()
```

A request cannot bypass authorization merely because it reaches a different application service.

---

# 31. Audit Requirements

Security-sensitive operations must be auditable.

Examples:

```text
Login success
Login failure
Logout where useful
Role assigned
Role removed
Permission changed
User activated
User suspended
User deactivated
Password/security event
```

Important business authorization events should also be auditable:

```text
SKU approved
Inventory adjusted
Order cancelled
Listing published
Shipment created
```

---

# 32. Audit Record

Conceptually:

```text
AuditRecord
 ├── AuditId
 ├── Timestamp
 ├── ActorUserId
 ├── Action
 ├── EntityType
 ├── EntityId
 ├── Result
 └── Context
```

The audit system must not store credentials, raw passwords, tokens, or other secrets.

---

# 33. Sensitive Data

Never log:

```text
Passwords
Password hashes
Authentication tokens
Session cookies
API secrets
Private keys
Full payment credentials
```

Logs must be designed under the assumption that operational logs may eventually be accessible to administrators or support personnel.

---

# 34. Secret Management

Secrets must not be committed to Git.

Examples:

```text
Database passwords
Marketplace API keys
Shipping provider credentials
Email credentials
Encryption keys
OAuth client secrets
```

Local development should use appropriate local secret storage.

Production should use environment/secret-management mechanisms appropriate for the deployment environment.

---

# 35. Configuration

Non-secret application configuration may be stored in version-controlled configuration files.

Sensitive values must remain outside source control.

Example:

```text
appsettings.json
    Safe defaults

appsettings.Development.json
    Development-only safe settings

User Secrets / Environment / Secret Store
    Sensitive values
```

---

# 36. Authorization Architecture

Conceptual architecture:

```text
HTTP Request
     ↓
Authentication
     ↓
Authenticated User
     ↓
Authorization Policy
     ↓
Permission Evaluation
     ↓
Application Use Case
     ↓
Business Rules
     ↓
Domain Operation
```

Authorization should not be embedded only at the UI layer or only at the database layer.

---

# 37. Business Operation Security

Sensitive operations should require explicit permissions.

Examples:

```text
ApproveManufacturingRevision
ApproveSku
AdjustInventory
CancelOrder
RecordPayment
PublishListing
CreateShipment
ChangeRole
ChangePermission
```

The required permission should be documented alongside the use case.

---

# 38. Example Secure Use Case

Operation:

```text
AdjustInventory
```

Required:

```text
Warehouse / Inventory / Edit
```

Then application logic evaluates:

```text
Authenticated?
       ↓
Active?
       ↓
Permission granted?
       ↓
SKU eligible?
       ↓
Location valid?
       ↓
Quantity valid?
       ↓
Concurrency valid?
       ↓
Commit transaction
       ↓
Audit
       ↓
Publish event
```

Authorization is only one part of the complete operation.

---

# 39. Administrative Security

Administrative privileges are highly sensitive.

Administrator capabilities may include:

```text
Manage Users
Manage Roles
Manage Permissions
View Security Audit
Change System Configuration
```

Administrative actions should be auditable.

Future hardening may require elevated reauthentication for especially sensitive operations.

---

# 40. Principle of Least Privilege

Users receive the minimum permissions required for their job.

Example:

A CSR who only needs to respond to customers should not automatically receive:

```text
Inventory Edit
SKU Approval
Payment Administration
Role Administration
```

Permissions should be intentionally assigned.

---

# 41. Permission Changes

Changes to roles and permissions take effect according to the application's session/security model.

The implementation must define how authorization changes propagate to already-authenticated sessions.

This should be tested explicitly.

---

# 42. External Integration Security

External marketplace, shipping, email, and payment systems are untrusted boundaries.

Requirements include:

* authenticate outbound requests,
* securely store credentials,
* validate inbound data,
* verify webhook authenticity where supported,
* prevent replay/duplicate processing,
* avoid logging secrets,
* use HTTPS,
* apply timeouts,
* handle provider failures safely.

---

# 43. Webhook Security

External webhooks must not be trusted merely because they target a known endpoint.

Where supported, verify:

```text
Signature
Timestamp
Provider identity
Expected event format
```

Webhook processing must also be idempotent.

---

# 44. Input Validation

All external/user input must be validated.

Sources include:

```text
Forms
HTTP requests
Marketplace APIs
Shipping APIs
Email systems
Phone systems
Webhooks
Imported files
```

Validation failures must not produce partial business state changes.

---

# 45. Authorization and Transactions

Authorization should occur before performing the protected business operation.

Business state changes must occur within appropriate transaction boundaries.

Example:

```text
Authorize
   ↓
Validate
   ↓
Execute
   ↓
Persist
   ↓
Audit/Event
```

The implementation must ensure an operation cannot partially succeed and leave invalid state.

---

# 46. Security Testing

Security tests should exist at multiple levels.

## Unit Tests

Permission evaluation.

## Integration Tests

Authenticated requests with different roles.

## End-to-End Tests

Actual UI/application flows.

Examples:

```text
Read-only user cannot edit SKU.
Warehouse user cannot approve SKU.
CSR cannot edit inventory.
Unauthenticated user cannot access protected page.
Deactivated user cannot authenticate.
```

---

# 47. Initial Security Roles

These are example roles, not fixed system constants:

```text
Administrator
ManufacturingUser
ProductManager
WarehouseUser
ListingManager
OrderManager
ShippingUser
CSR
Manager
Executive
```

Roles are configuration/data, not hard-coded business logic.

---

# 48. Initial Permission Model

```text
Permission
 ├── ModuleId
 ├── SectionId
 └── AccessLevel
```

Role assignments:

```text
Role
 └── RolePermission
```

User assignments:

```text
User
 └── UserRole
```

---

# 49. Security Decision

We will use:

```text
ASP.NET Core Identity
+
ASP.NET Core Authorization
+
Role-based permissions
+
Policy-based authorization
+
Server-side enforcement
+
Audit
```

We will not create a custom authentication system unless a future requirement explicitly requires one.

---

# 50. What We Are NOT Implementing Yet

Do not prematurely build:

* SSO
* Multi-factor authentication
* External identity providers
* Fine-grained resource ACLs
* Attribute-based access control
* Complex policy languages
* Delegated administration
* Zero-trust service mesh
* Distributed identity services

These may become valuable later, but they are not prerequisites for the first ERP slice.

---

# 51. Phase 01 Security Acceptance Criteria

Phase 01 will be considered complete when the system can demonstrate:

### Authentication

```text
User can authenticate.
Invalid credentials are rejected.
Deactivated users cannot authenticate.
```

### Roles

```text
User can be assigned a role.
User can have multiple roles.
Role permissions are evaluated correctly.
```

### Module permissions

```text
Read permission allows viewing.
Missing permission denies access.
```

### Section permissions

```text
Permission can be restricted to a section.
Access to another section remains denied.
```

### Edit permission

```text
Read-only user cannot perform edit operation.
Edit user can perform permitted edit operation.
```

### Server-side enforcement

```text
Direct request cannot bypass permission checks.
```

### Audit

```text
Role/permission changes are auditable.
Important authorization-sensitive operations are auditable.
```

---

# 52. First Security Vertical Slice

The first implementation slice should be intentionally tiny:

```text
Create User
     ↓
Authenticate
     ↓
Assign Role
     ↓
Assign Permission
     ↓
Protect One Test Section
     ↓
Read Allowed
     ↓
Edit Denied
     ↓
Edit Allowed
```

The first protected business feature does not need to be Manufacturing or Product.

We can create a minimal test/proof section specifically to validate the authorization architecture.

---

# 53. Security Definition of Done

Security infrastructure is not complete when login works.

It is complete when:

* authentication works,
* inactive users are blocked,
* roles work,
* module permissions work,
* section permissions work,
* read/edit behavior works,
* server-side enforcement works,
* unauthorized operations are rejected,
* authorization behavior is tested,
* security-sensitive actions are audited,
* secrets remain outside source control,
* the security model is documented.

---

# 54. Next Step

After this document is accepted, Phase 00 should define the concrete .NET solution structure and implementation conventions.

The first implementation should then be:

```text
Phase 00
    Solution Foundation
       ↓
Phase 01
    Identity/RBAC Slice 01.1
       ↓
    User
       ↓
    Login
       ↓
    Role
       ↓
    Permission
       ↓
    Protected Test Operation
```

The implementation must remain small enough that every security rule can be understood and tested before the next slice is added.
