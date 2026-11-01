# Approval API contract

## Current verified baseline

The repository currently exposes:

- `POST /api/approvals/{invoiceId}/approve`
- `POST /api/approvals/{invoiceId}/reject`

### Approve endpoint

Current contract:
- Request body: none
- Authenticated identity and role: read from `HttpContext.GetSession()`
- Controller method: `Approve(string invoiceId)`
- Domain call: `ApprovalService.Approve(invoiceId, session.Username, session.Role)`

The same endpoint is called for both approval steps. The domain service determines whether a call is the first or second approval from the invoice's persisted `ApprovalHistory`; the client does not provide an approval stage, username, or role.

For an invoice above `500,000`:
- The first call from an authenticated Manager or Director records that session's username, role, and timestamp as a provisional approval and returns the invoice as `PendingApproval`. `DecidedBy` and `DecidedAt` remain unset.
- A later call by a distinct authenticated Director records the second approval and transitions the invoice to `Approved`. Final-decision metadata identifies this second Director.
- Invalid second approvals return the existing business-rule error response and preserve invoice state and approval history.

For an invoice at or below `500,000`, a valid Manager or Director call remains an independent single approval under the existing limits and finalizes the invoice.

### Reject endpoint

Current contract:
- Request body: `{"reason":"string"}`
- Defined as: `public record RejectRequest(string Reason);`
- Authenticated identity: read from `HttpContext.GetSession().Username`
- Controller method: `Reject(string invoiceId, RejectRequest request)`
- Domain call: `ApprovalService.Reject(invoiceId, HttpContext.GetSession().Username, request.Reason)`

## HTTP semantics expected by this feature

- `200 OK` for valid approval or rejection
- `400 Bad Request` for invalid request values or malformed rejection reasons
- `404 Not Found` when an invoice does not exist
- `409 Conflict` for invalid invoice state or business-rule violations
- `401 Unauthorized` when bearer token/session is invalid or absent

These response mappings are already defined in `ApiControllerBase.Handle(...)` and must remain the API error contract baseline for this feature.
