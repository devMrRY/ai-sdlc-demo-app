# Research and design decisions

## Research summary

This feature is an extension of the existing ASP.NET Core 8 invoice approval demo app already present in this repository. The verified baseline shows a single-approver workflow, role-based authorization through `RequireRoleAttribute`, and an in-memory invoice repository. The assignment adds approval limits by role and a two-approver flow for high-value invoices.

## Verified repository facts

- Language/runtime: .NET 8 ASP.NET Core Web API
- UI: static web client under `backend/src/InvoiceApp.Api/wwwroot/`
- Domain layer: `backend/src/InvoiceApp.Domain`
- API layer: `backend/src/InvoiceApp.Api`
- Test project: `backend/tests/InvoiceApp.Tests`
- E2E project: `e2e/`
- Initial baseline roles before the approval-limit work: `Approver`, `Admin`; the configured roles now include `Manager` and `Director`, and only Manager/Director may approve or reject invoices.
- Current baseline invoice statuses: `Draft`, `PendingApproval`, `Approved`, `Rejected`
- Current approval API contract: single approver only, derived from authenticated session

## Key decisions

### Decision: keep business rules in the Domain layer

Rationale:
- `AGENTS.md` and `PROJECT_CONTEXT.md` explicitly require domain business rules to stay in `InvoiceApp.Domain` and not in controllers or startup code.
- `ApprovalService` is the existing domain owner for lifecycle checks and state transitions.

Outcome:
- The plan keeps threshold validation, approver role validation, multi-approver logic, and invalid-state checks in `ApprovalService`.
- Controllers remain request/response adapters and mapping logic only.

### Decision: preserve the current authenticated-session pattern for approver identity

Rationale:
- The current API contract identifies the approver through `HttpContext.GetSession().Username`.
- `SessionAuthMiddleware` validates bearer tokens and attaches the authenticated `UserSession` to `HttpContext.Items`.

Outcome:
- The multi-approver flow must explicitly resolve each approver from authenticated, validated session state rather than trusting any client-provided username.
- This requirement is captured in the design and contracts for the feature so the plan does not rely on a client-supplied identity as proof of authorization.

### Decision: use the existing invoice state model unless a project-level requirement forces a model extension

Rationale:
- `Invoice` already contains the approval state fields needed for the feature.
- The assignment states that the business rules should not introduce unrelated data model changes.

Outcome:
- Plan assumes the current `Invoice` model remains the basis for state integrity checks unless the implementation identifies a minimal extension required for multi-step approval metadata.
- Any new fields must be justified and kept minimal.

### Decision: keep validation order deterministic and fail closed

Rationale:
- The approved spec explicitly calls out the need for invalid attempts to leave the invoice unchanged and to reject unsupported or invalid patterns.
- The repo does not currently define the validation order for multiple invalid conditions.

Outcome:
- The implementation plan requires a deterministic validation order in the domain service and treats multiple-invalid-condition cases as a fail-closed path that preserves invoice state.
- This is intentionally planned as part of implementation and review rather than guessed in code.

## Research items and open questions

### Open question: exact request contract for the two-approver flow

Verified status:
- The current repo has no second-approver API contract.
- The approved feature specification calls for multi-approver enforcement but leaves the exact UI/API transaction contract to implementation validation.

Status:
- This remains an open design item that must be resolved as part of the implementation, but it must not rely on client-supplied username identity.

### Open question: whether current `Invoice` model needs explicit approval-chain metadata

Verified status:
- The repository currently tracks only a single decision actor and decision note.
- For two-approver logic, the implementation may need to record the approval sequence or preserve the first approver identity.

Status:
- The design plan keeps this as a minimal extension only if required by the implementation; otherwise state checks can be performed against the existing fields and validation rules.

### Open question: final API error wording

Verified status:
- The approved spec requires a clear rejection reason, not a mandated exact phrase.

Status:
- Error text policy is left for implementation to define in a consistent and clear way, while ensuring the cause is explicit and state remains unchanged.

## Scope constraints from assignment and repo guidance

- Implement in three independent commits, each aligned to one user story.
- Do not modify the approved specification.
- Keep domain logic in `ApprovalService`.
- Add Manager and Director runtime users before testing or sign-in.
- Run backend tests, Playwright E2E tests, and mutation testing before final PR readiness.
- Use the installed global `dotnet-stryker` version 4.13.0; do not reinstall.

## Design implications

1. User Story 1 will be implemented with a domain service method that validates role and threshold before any mutation.
2. User Story 2 will extend the domain service to enforce the two-approver rule and preserve invoice state on invalid sequences.
3. User Story 3 will enforce immutable state semantics on rejects and invalid approval attempts, ensuring no partial updates.
4. The UI and API are request adapters only; they cannot contain business rules.
5. The multi-approver flow must be anchored to authenticated session identity and server-side validation, not to client-provided values.
