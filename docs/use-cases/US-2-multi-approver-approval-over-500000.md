# US-2: Multi-approver approval above 500,000

Related: [US-1: Manager and Director approval limits](US-1-manager-and-director-approval-limits.md)

## Background

US-1 defines independent approval limits: Managers may approve up to and including 50,000, and Directors may approve up to and including 500,000. Invoices above 500,000 use a separate two-approver flow.

## The change

Require two distinct authenticated approvers for an invoice above 500,000. The first approval is provisional; a different Director must provide the second approval before the invoice is finalized.

## What the business asked for

- The first provisional approval above 500,000 may be made by either a Manager or a Director.
- The Manager's 50,000 limit applies to independent single approvals, not this first provisional approval.
- Record each approver's authenticated username, role, and approval timestamp in approval history, in order.
- After the first approval, the invoice remains `PendingApproval`; final-decision metadata remains unset.
- The second approval must be by a different user whose authenticated role is Director.
- Manager → Director and Director → Director succeed.
- Manager → Manager, Director → Manager, and a second approval by the same user fail without changing invoice state or history. A failed second attempt does not consume the opportunity for a valid Director.
- Finalize the invoice and set final-decision metadata only after the valid second approval.
- Reject approval attempts against already-finalized invoices without changing them.
- An approval request from a role other than Manager or Director returns HTTP 403 with: `A Manager or Director role is required to approve the invoice.`
- Approver identity and role come from the authenticated server session, not client-supplied request fields.
- The rejection endpoint retains its separate authorization behavior.

## Boundary examples

- An independent Director approval at 500,000 may finalize the invoice.
- An invoice at 500,001 requires two approvals; its first approval is provisional.
- For an invoice above 500,000, either a Manager or Director may provide the first provisional approval.
- The second approval for an invoice above 500,000 must be by a different Director.

## Where it touches the code

Approval sequencing is implemented in `backend/src/InvoiceApp.Domain/ApprovalService.cs`; atomic invoice operations are provided by `backend/src/InvoiceApp.Api/InMemoryInvoiceRepository.cs`. Session-based authorization is applied by `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs` and `backend/src/InvoiceApp.Api/Auth/RequireRoleAttribute.cs`.

Backend coverage is in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` and `backend/tests/InvoiceApp.Tests/ApprovalsControllerAuthorizationTests.cs`. The UI flow is covered by `e2e/tests/high-value-two-approver-approval.spec.ts`.

## Verification

- The full backend suite passed: 38 tests.
- The complete Playwright suite passed: 2 tests.
- The US2 implementation is complete through T041. T042 is a separate commit milestone and remains unchecked.
