---
name: remitee_dev_rules
description: Guidelines and patterns for the Remitee Treasury Transfers API challenge
---

# Technical Guidelines for Remitee Treasury Transfers API

These guidelines must be followed strictly during all phases of code development for this project.

## 1. Domain Modeling & DDD Constraints
- **Pure Domain**: The `Challenge.Domain` layer must have ZERO external dependencies (no Entity Framework Core, no MediatR, no third-party libraries).
- **Aggregates and Entities**: Define rich domain models. Domain state changes must only happen through methods on the entities (e.g. `Account.Debit(Money money)`, `Account.Credit(Money money)`), not by direct setters.
- **Value Objects**: Money representation, Currency, and Account Status should be modeled as Value Objects or strongly typed records/classes with proper validations to avoid primitive obsession.
- **Domain Events**: Changes in aggregates that trigger side-effects (e.g. publishing transaction history) should register Domain Events (e.g. `TransferCompletedDomainEvent`) in the entity itself, to be dispatched by the Unit of Work when saving.

## 2. Financial Logic & Money Handling
- **No Floating Point**: Never use `float` or `double` for currency amounts. Always use `decimal`.
- **Decimals per Currency**:
  - `USD`: 2 decimals.
  - `ARS`: 2 decimals.
  - `CLP`: 0 decimals.
- **Rounding Policy**:
  - **Banker's Rounding (MidpointRounding.ToEven)** is used to minimize statistical bias when converting currencies.
  - Rounding must occur strictly at the boundary of accreditation. The intermediate FX calculation `amount * fx` is rounded to the target currency's decimal precision.
- **FX Validation**:
  - Same currency: Reject `fx` if provided, or return a validation error.
  - Different currencies: `fx` is mandatory and must be a positive decimal.

## 3. Error Handling: Result Pattern
- **No Domain Exceptions**: Do not throw exceptions for business violations (e.g., insufficient funds, frozen accounts).
- **Result Object**: Use a generic `Result<T>` or `Result` pattern to return success or error states from the domain and application handlers back to the API layer.
- **HTTP Mapping**: The API layer maps the `Result` to the appropriate HTTP status code and response body `{ "code": "ErrorCode", "message": "User-friendly description" }`.

## 4. CQRS and MediatR
- **Separation of Concerns**: Keep Commands and Queries completely separate.
- **Handlers**: Application handlers should contain the business logic workflow, coordinating the Domain repositories, checking idempotency, and committing the Unit of Work.
- **Immutability**: All Command and Query request DTOs must be immutable (e.g., using C# positional `record`s).

## 5. Idempotency & Database Atomicity
- **Idempotency Key**: `operationId` (UUID) is the idempotency key.
- **Idempotency Verification**: Before processing a transfer, check if a transaction with the same `operationId` already exists.
  - If it exists and was successful, return the original transaction response.
  - If it exists but failed, decide whether it can be retried or rejected.
- **Atomic Unit of Work**: Debiting the source account, crediting the target account, and saving the `LedgerTransaction` must occur in a single database transaction. Use EF Core's Transaction behavior or save all changes in a single `SaveChangesAsync()` call, which EF Core wraps in a transaction implicitly.
