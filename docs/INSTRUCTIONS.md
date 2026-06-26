# Treasury Transfers API - Technical & Business Specifications

This document outlines the business rules, architectural guidelines, financial calculations, domain invariants, and data models for the Remitee Treasury Transfers API challenge.

---

## 1. Business Understanding & Goal
The Treasury Transfers API simulates a core ledger system for moving money between internal accounts of the Remitee platform. Its primary goal is to expose an endpoint `POST /transfers` that processes transfers atomically and idempotently.

A ledger transaction is **immutable and auditable**. Once created, it cannot be modified or deleted. 

Every transfer consists of three concurrent operations:
1. **Debit**: Subtract the transfer amount from the source account balance.
2. **Credit**: Add the converted amount (using FX rate if applicable) to the target account balance.
3. **Audit**: Record a `LedgerTransaction` with the status `COMPLETED`.

These three operations must succeed or fail together as a single **atomic unit of work**.

---

## 2. Financial Logic & Decimal Precision
To ensure financial accuracy and avoid floating-point issues, all balance and transaction calculations must strictly adhere to the following rules:

### 2.1 Currency Configurations
Three specific currencies are supported, each with different decimal requirements:
- **USD** (United States Dollar): 2 decimal places (e.g., `100.00`).
- **ARS** (Argentine Peso): 2 decimal places (e.g., `1000000.00`).
- **CLP** (Chilean Peso): 0 decimal places (integers only, e.g., `1500`).

### 2.2 Avoidance of Floating-Point Drift
- **Data Type**: All currency values must be stored and processed using the `decimal` type in C# and the `decimal(18, 4)` (or similar high-precision scale) in SQL Server.
- **Floating-point types** (`float`, `double`) are strictly forbidden for balance storage or calculation.

### 2.3 Exchange Rates (FX) and Conversions
- **FX Definition**: The exchange rate `fx` is the price of 1 unit of the source currency expressed in the target currency.
- **Conversion Formula**:
  $$\text{AmountCredited} = \text{AmountDebited} \times \text{fx}$$
- **Applicability**:
  - **Cross-Currency Transfer**: If the source account currency differs from the target account currency, `fx` is **mandatory** and must be positive (`fx > 0`).
  - **Same-Currency Transfer**: If the source and target currencies are identical, `fx` is **not allowed** and must be rejected to prevent inconsistencies.

### 2.4 Rounding Policy: Banker's Rounding (Half-Even)
To prevent statistical drift and rounding bias across millions of transactions, **Banker's Rounding (Midpoint Rounding to Even)** is applied. 
- **Method**: Calculations use `Math.Round(amount, decimals, MidpointRounding.ToEven)`.
- **Timing**: Rounding is applied immediately before crediting the target account balance and writing the transaction log.
- **Examples**:
  - Converting to USD (2 decimals): `10.005 USD` rounds to `10.00 USD`, whereas `10.015 USD` rounds to `10.02 USD`.
  - Converting to CLP (0 decimals): `100.5 CLP` rounds to `100 CLP`, whereas `101.5 CLP` rounds to `102 CLP`.

---

## 3. Domain Invariants & Validations

The following business rules must be verified in the domain layer prior to executing a transfer:

| Invariant | Description | Failure Status | Error Response Code |
| :--- | :--- | :--- | :--- |
| **Account Existence** | Both source and target accounts must exist in the system. | `404 Not Found` | `ACCOUNT_NOT_FOUND` |
| **Account Status** | Both source and target accounts must have an `ACTIVE` status. `FROZEN` accounts cannot send or receive money. | `422 Unprocessable` | `ACCOUNT_IS_FROZEN` |
| **Account Disjointness**| Source and target accounts must be different (`sourceAccountId != targetAccountId`). | `400 Bad Request` | `IDENTICAL_ACCOUNTS` |
| **Currency Match** | The request `currency` parameter must match the source account's currency. | `400 Bad Request` | `CURRENCY_MISMATCH` |
| **Sufficient Funds** | The source account must have enough balance to cover the debit: `Balance - Amount >= 0`. (No negative balances allowed). | `422 Unprocessable` | `INSUFFICIENT_FUNDS` |
| **FX Mandatoriness** | If currencies differ, `fx` must be provided and must be $> 0$. | `400 Bad Request` | `FX_REQUIRED` |
| **FX Redundancy** | If currencies are identical, `fx` must not be provided (should be null or omitted). | `400 Bad Request` | `FX_NOT_ALLOWED` |
| **Amount Validity** | The transfer `amount` must be strictly positive ($> 0$). | `400 Bad Request` | `INVALID_AMOUNT` |

---

## 4. Idempotency Specification

The system guarantees idempotency using the client-provided `operationId` (UUID):
1. **Idempotency Key Verification**: When a request to `POST /transfers` is received, the system queries the database for an existing `LedgerTransaction` with the same `operationId`.
2. **Duplicate Request Handling**:
   - If a matching transaction is found and its status is `COMPLETED`, the system returns the **original response details** immediately, without modifying any balances.
   - If a matching transaction is found but it is failed or in progress, the system responds with the appropriate state or error.
   - If no matching transaction is found, the system proceeds with the transfer transaction.

---

## 5. Proposed Data Model

### 5.1 Account Entity
Represents an internal account holding funds.

| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| **Id** | `VARCHAR(50)` | Primary Key | Custom format, e.g. `ACC-USD-1` |
| **Currency** | `VARCHAR(3)` | Not Null | ISO 4217 Currency Code (`USD`, `ARS`, `CLP`) |
| **Balance** | `DECIMAL(18, 4)` | Not Null | Current balance, must respect decimal precision of its currency |
| **Status** | `VARCHAR(20)` | Not Null | Account state: `ACTIVE` or `FROZEN` |
| **Version** | `INT` | Not Null | Optimistic locking token to prevent double-spend concurrency |

### 5.2 LedgerTransaction Entity
Represents the audit log of a transfer.

| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| **Id** | `UNIQUEIDENTIFIER` | Primary Key | Unique transaction identifier |
| **OperationId** | `UNIQUEIDENTIFIER` | Unique Index, Not Null | Idempotency Key provided by client |
| **SourceAccountId** | `VARCHAR(50)` | Foreign Key | Reference to the originating Account |
| **TargetAccountId** | `VARCHAR(50)` | Foreign Key | Reference to the receiving Account |
| **AmountDebited** | `DECIMAL(18, 4)` | Not Null | Amount debited from the source account (source currency) |
| **AmountCredited** | `DECIMAL(18, 4)` | Not Null | Amount credited to the target account (target currency) |
| **FxRate** | `DECIMAL(18, 6)` | Nullable | Exchange rate applied (if cross-currency) |
| **Status** | `VARCHAR(20)` | Not Null | Transaction status: `COMPLETED` |
| **CreatedAt** | `DATETIMEOFFSET` | Not Null | Date and time the transaction was processed |

### 5.3 Initial Seed Data
The following seed records must be loaded into the database during the migration process:

```sql
INSERT INTO Accounts (Id, Currency, Balance, Status, Version) VALUES
('ACC-USD-1', 'USD', 10000.00, 'ACTIVE', 0),
('ACC-USD-2', 'USD', 500.00, 'ACTIVE', 0),
('ACC-ARS-1', 'ARS', 1000000.00, 'ACTIVE', 0),
('ACC-CLP-1', 'CLP', 0.00, 'ACTIVE', 0),
('ACC-FROZEN', 'USD', 9999.00, 'FROZEN', 0);
```
