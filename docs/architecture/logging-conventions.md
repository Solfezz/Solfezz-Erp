# Logging Conventions

## Purpose

Logging provides operational evidence about what the ERP is doing without becoming a substitute for business state or audit history.

## Logging Framework

Solfezz ERP initially uses the built-in Microsoft.Extensions.Logging abstractions.

Additional logging providers may be introduced later if operational requirements justify them.

## Structured Logging

Prefer message templates with named properties.

Preferred:

logger.LogInformation(
    "Imported order {ExternalOrderId} from {Marketplace}",
    externalOrderId,
    marketplace);

Avoid:

logger.LogInformation(
    $"Imported order {externalOrderId} from {marketplace}");

Named properties allow log systems to index and query important values.

## Log Levels

Trace:
Highly detailed diagnostic information.

Debug:
Developer-oriented diagnostic information.

Information:
Expected important application events.

Warning:
Unexpected conditions from which the application can recover.

Error:
An operation failed.

Critical:
The application or a major subsystem cannot continue safely.

Do not use Error for normal business outcomes.

Example:

A SKU being ineligible for approval is normally a business result, not an infrastructure error.

## Sensitive Information

Never log:

- passwords
- connection strings containing credentials
- API keys
- access tokens
- authentication cookies
- private certificates
- full payment credentials

Be cautious with customer personally identifiable information.

## Identifiers

Where useful, include stable identifiers as structured properties.

Examples:

- OrderId
- ExternalOrderId
- SkuId
- ShipmentId
- CustomerId
- Marketplace

Do not concatenate identifiers into arbitrary message strings when structured properties can be used.

## Logging vs Audit History

Application logs and business audit history are different.

Logs answer operational questions such as:

- Did a provider call fail?
- Why did this request return an error?
- How long did an operation take?

Audit history answers business questions such as:

- Who approved this SKU?
- When was inventory adjusted?
- What was the previous state?
- Who changed the order?

Logs must not become the authoritative ERP audit trail.

## Exceptions

When logging an exception, pass the exception object to ILogger.

Preferred:

logger.LogError(
    exception,
    "Failed to import order {ExternalOrderId}",
    externalOrderId);

Do not discard the exception stack trace by logging only exception.Message.
