# Approval API contract

## Current verified baseline

The repository currently exposes:

- `POST /api/approvals/{invoiceId}/approve`
- `POST /api/approvals/{invoiceId}/reject`

### Approve endpoint

Current contract:
- Request body: none
- Authenticated identity: read from `HttpContext.GetSession().Username`
- Controller method: `Approve(string invoiceId)`
- Domain call: `ApprovalService.Approve(invoiceId, HttpContext.GetSession().Username)`

### Reject endpoint

Current contract:
- Request body: `{"reason":"string"}`
- Defined as: `public record RejectRequest(string Reason);`
- Authenticated identity: read from `HttpContext.GetSession().Username`
- Controller method: `Reject(string invoiceId, RejectRequest request)`
- Domain call: `ApprovalService.Reject(invoiceId, HttpContext.GetSession().Username, request.Reason)`

## Planned evolution for this feature

The feature adds a two-approver approval flow for invoices above `500,000`. The API contract must be extended in a server-authoritative and authenticated manner without trusting client-supplied usernames as identity proofs.

Required design principle:
- The authenticated session must be the source of truth for the current approver’s identity.
- Any second approver must be resolved from an authenticated, validated identity and not from a raw client input alone.

## Proposed contract changes

If the implementation requires a multi-approver request body, it should use a typed request object that includes only the required approval metadata and never assumes client trust for identity. The request should at minimum support:

- `firstApprover` or equivalent internal validation source derived from authenticated session
- `secondApprover` identity validated by the server using a trusted session or user registry
- approval order metadata for sequence validation
- invalid or unauthorized attempt handling with clear reasons and no invoice mutation

There is currently no verified second-approver API contract in the repo. The implementation must explicitly define it as part of the feature work and preserve the existing controller layering.

## HTTP semantics expected by this feature

- `200 OK` for valid approval or rejection
- `400 Bad Request` for invalid request values or malformed rejection reasons
- `404 Not Found` when an invoice does not exist
- `409 Conflict` for invalid invoice state or business-rule violations
- `401 Unauthorized` when bearer token/session is invalid or absent

These response mappings are already defined in `ApiControllerBase.Handle(...)` and must remain the API error contract baseline for this feature.
