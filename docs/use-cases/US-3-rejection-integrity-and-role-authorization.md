# US-3: Rejection integrity and role authorization

Related: [US-1: Manager and Director approval limits](US-1-manager-and-director-approval-limits.md) · [US-2: Multi-approver approval above 500,000](US-2-multi-approver-approval-over-500000.md)

## Background

Invoices need a clear, auditable decision history. Invalid decisions must not partially change invoice state, and only authorized roles may approve or reject invoices.

## The change

Require a reason for rejection, preserve invoice state when a decision is invalid, and restrict both approval and rejection to Managers and Directors.

## What the business asked for

- A rejection requires a non-empty reason. A valid rejection sets the status to `Rejected` and records the authenticated actor, decision timestamp, and reason in `DecisionNote`.
- Only users with the `Manager` or `Director` role may approve or reject invoices. The actor and role come from the authenticated session.
- Approver, Admin, and unsupported roles receive HTTP 403 with: `A Manager or Director role is required to perform this operation.`
- An already-Rejected invoice cannot be approved, and an already-Approved invoice cannot be rejected.
- Invalid decisions leave invoice status, approval history, decision metadata, and rejection note unchanged.
- The UI allows other roles to sign in but does not show them approval or rejection controls.

## Boundary examples

- A Manager or Director rejects a pending invoice with a non-empty reason; the reason is returned as `decisionNote`.
- An empty or whitespace-only reason is rejected without changing the invoice.
- An approval attempt on a Rejected invoice and a rejection attempt on an Approved invoice fail without mutation.
- Approver, Admin, and unsupported roles cannot use either decision endpoint.

## Where it touches the code

Decision state and rejection validation are handled by `backend/src/InvoiceApp.Domain/ApprovalService.cs`. Session-based role authorization is applied by `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`; the static UI is in `backend/src/InvoiceApp.Api/wwwroot/app.js`.

Backend coverage is in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` and `backend/tests/InvoiceApp.Tests/ApprovalsControllerAuthorizationTests.cs`. Playwright coverage for Approver and Admin UI restrictions is in `e2e/tests/unsupported-roles-cannot-decide.spec.ts`.
