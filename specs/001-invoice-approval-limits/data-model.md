# Data model

## Overview

The invoice approval feature reuses the existing domain model and extends validation logic rather than changing the underlying invoice lifecycle model. The repository currently models invoices and approval decisions with a small state-driven design and a single authenticated user session.

## Entities

### Invoice

Source: `backend/src/InvoiceApp.Domain/Invoice.cs`

Fields:
- `Id` (`string`): unique invoice identifier
- `CustomerName` (`string`): customer or invoice owner label
- `Amount` (`decimal`): invoice amount using the existing monetary conventions
- `Status` (`InvoiceStatus`): current lifecycle status
- `CreatedAt` (`DateTime`): invoice creation timestamp
- `DecidedBy` (`string?`): current approver/decision actor
- `DecidedAt` (`DateTime?`): timestamp when decision was finally recorded
- `DecisionNote` (`string?`): reason for rejection or related decision note
- `ApprovalHistory` (`List<ApprovalRecord>`): authenticated approver records, in approval order

State transitions:
- `Draft` -> `PendingApproval` via submit flow
- `PendingApproval` -> `PendingApproval` when the first provisional approval is recorded for an invoice above `500,000`
- `PendingApproval` -> `Approved` after the valid second approval, or after a valid independent approval at or below `500,000`
- `PendingApproval` -> `Rejected` via the separate rejection flow
- Invalid transitions are rejected by domain rule checks

### ApprovalRecord

Each approval record contains:
- `Approver` (`string`): username from the authenticated session
- `Role` (`string`): role from the authenticated session
- `ApprovedAt` (`DateTime`): time the approval was recorded

For invoices above `500,000`, the first record is provisional: the invoice remains `PendingApproval`, and `DecidedBy`/`DecidedAt` remain unset. The first approver may be a Manager or Director; the Manager's `50,000` limit applies to independent single approvals, not this provisional step. A distinct Director must add the second record. Only then does the invoice become `Approved`, with final-decision metadata set to that second Director.

For invoices at or below `500,000`, a valid independent approval adds one approval record and finalizes the invoice under the existing role limits.

### InvoiceStatus

Source: `backend/src/InvoiceApp.Domain/InvoiceStatus.cs`

Current values:
- `Draft`
- `PendingApproval`
- `Approved`
- `Rejected`

The feature does not change this status model unless required by implementation-specific design; the approved spec expects state-preserving invalid-path behavior without introducing a new invoice workflow.

### UserSession

Source: `backend/src/InvoiceApp.Api/Auth/UserSession.cs`

Fields:
- `Username` (`string`)
- `Role` (`string`)

This is the authenticated identity used by the API. The multi-approver flow must use server-side validation of roles and signed-in session identity, not client-supplied values.

### DemoUser / DemoUsersOptions

Source: `backend/src/InvoiceApp.Api/Auth/UserSession.cs`

Fields:
- `Username`
- `Password`
- `Role`

The assignment requires that runtime demo users include:
- `demo.manager` / `ChangeMe123!` / `Manager`
- `demo.director` / `ChangeMe123!` / `Director`

## Validation rules captured from approved spec

### User Story 1: Manager / Director authorization

- Manager may approve `<= 50,000`
- Director may approve `<= 500,000`
- Values above these thresholds require valid higher-level rules and fail closed otherwise
- Unauthorized or invalid approval attempts must return clear reasons and preserve invoice state

### User Story 2: multi-approver enforcement

- Invoices above `500,000` require two distinct approvers
- The second approver must be a Director
- The same user cannot serve as both approvers
- Invalid approval order is rejected
- Already-finalized invoices are rejected

### User Story 3: rejection integrity

- Rejection must provide a reason
- Invalid approval or rejection attempts must not partially update invoice state
- `DecisionNote` is retained only for valid rejection outcomes
- `DecidedBy`, `DecidedAt`, and status remain unchanged when validation fails

## Data integrity expectations

- Invalid approval attempts must not mutate invoice state.
- Invalid second approvals must preserve the complete existing `ApprovalHistory` and must not consume the second-approval opportunity.
- Approval history checks and writes for an invoice must be serialized so concurrent requests cannot record multiple provisional first approvals.
- Server-side validation must derive the approver identity from authenticated session data.
- The domain service must perform validations before writing updates to the repository.
- The API contract must not trust client-provided usernames for approver identity.
