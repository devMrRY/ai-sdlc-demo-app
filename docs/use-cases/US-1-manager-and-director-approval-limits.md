# US-1: Manager and Director approval limits

## Background

Invoices need approval from users with authority appropriate to the invoice amount. Managers and Directors have different approval limits, and an invalid approval must not finalize or otherwise change an invoice.

## The change

Allow Managers and Directors to approve pending invoices within their single-approval limits, and clearly reject over-limit or unauthorized approval attempts.

## What the business asked for

- A Manager may independently approve an invoice for up to and including 50,000.
- A Director may independently approve a valid invoice for up to and including 500,000, including amounts at or below 50,000.
- An approval above the caller's single-approval limit is rejected with a clear reason.
- Invalid approval attempts leave the invoice unchanged, including its status and decision metadata.
- Approval authorization and identity come from the authenticated session; the client cannot supply a trusted role or approver identity.
- Rejection remains a separate operation with its existing role authorization.
- Through the UI, a Manager's attempt to approve an invoice for more than 50,000 and no more than 500,000 shows a clear rejection, leaves the invoice pending, and records no final decision.

## Boundary examples

- Manager approves 50,000; Manager cannot approve 50,001.
- Director can approve amounts below 50,000 and at 50,000, 50,001, and 500,000.
- A single approval above 500,000 is not part of US-1; it requires the separate multi-approver workflow.

## Where it touches the code

The approval service in `backend/src/InvoiceApp.Domain/ApprovalService.cs`, session-driven authorization in `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`, backend tests in `backend/tests/InvoiceApp.Tests/`, and the Manager over-limit browser test in `e2e/tests/`.
