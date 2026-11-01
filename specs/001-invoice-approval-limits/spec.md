# Feature Specification: Invoice approval role limits

**Feature Branch**: `001-invoice-approval-limits`

**Created**: 2026-10-28

**Status**: Draft

**Input**: User description: "Create the Phase 1 specification for my AI-in-SDLC assignment using GitHub Spec Kit. Understand the project first..."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manager and Director approval authority (Priority: P1)

A manager or director must be able to approve an invoice only when the invoice amount is within the authorized threshold for their role, and any invalid approval attempt must be rejected with a clear reason without changing the invoice state.

**Why this priority**: This is the primary business workflow and the baseline rule for approval authority by role and amount.

**Independent Test**: A manager can approve exactly 50,000, and a director can independently approve any valid invoice amount up to and including 500,000, including amounts below 50,000. Attempts above the applicable single-approval limit are rejected without changing invoice state.

**Acceptance Scenarios**:

1. **Given** an invoice amount of 50,000 and a logged-in user with the Manager role, **When** the user approves the invoice, **Then** the approval succeeds and the invoice is marked approved.
2. **Given** an invoice amount of 500,000 or less and a logged-in user with the Director role, **When** the user approves the invoice, **Then** the approval succeeds and the invoice is marked approved.
3. **Given** an invoice amount above 50,000 and a logged-in user with the Manager role, **When** the user approves the invoice, **Then** the system rejects the request because Manager approvals are valid only up to and including 50,000 and the invoice remains unchanged.
4. **Given** an invoice amount above 500,000 and a logged-in user with the Director role, **When** the user attempts approval without a valid second approver, **Then** the system rejects the attempt and the invoice remains unchanged.
5. **Given** a logged-in user without a valid approval role, **When** the user attempts to approve an invoice, **Then** the system rejects the request with a clear authorization message and does not mutate the invoice.
6. **Given** an invoice amount greater than 50,000 and no greater than 500,000, and a logged-in Manager, **When** the Manager attempts approval through the actual user interface, **Then** the interface displays a clear rejection message, the invoice remains `PendingApproval`, and no final approval decision is recorded; this behavior MUST be verified by a Playwright end-to-end test.

---

### User Story 2 - Multi-approver enforcement for invoices above 500,000 (Priority: P1)

Any invoice above 500,000 must have a valid two-step approval flow where the first approver is valid and the second approver is a distinct Director. The same person cannot approve twice, and invalid approval order must be rejected.

**Why this priority**: This covers the highest-risk logic: two-approver enforcement, same-user rejection, and approval-order validation.

**Independent Test**: An invoice above 500,000 can only be approved through a valid two-step flow where the second approver is a distinct Director.

**Acceptance Scenarios**:

1. **Given** an invoice amount above 500,000 and a valid first approver, **When** the second approver is the same user, **Then** the system rejects the request and preserves the original invoice state.
2. **Given** an invoice amount above 500,000 and a valid first approver, **When** the second approver is not a Director, **Then** the system rejects the request and preserves the original invoice state.
3. **Given** an invoice amount above 500,000 and a valid first approver, **When** the approval order is invalid or the invoice is already finalized, **Then** the system rejects the request, returns a clear reason, and leaves the invoice unchanged.
4. **Given** an invoice amount above 500,000 and two valid approvers, **When** the request is processed successfully, **Then** the invoice is approved only after both approvers are distinct and the second approver is a Director.

---

### User Story 3 - Rejection integrity and state preservation (Priority: P1)

When an approval or rejection request is invalid, the system must return a clear reason and must not partially update the invoice. A rejection requires a message, and the invoice must retain its previous state before the attempted approval decision.

**Why this priority**: The assignment explicitly requires state integrity and reason-based rejection handling to prevent incorrect partial updates.

**Independent Test**: A rejected approval or invalid rejection attempt leaves all invoice fields, status, and decision data unchanged, and a rejection without a message is rejected.

**Acceptance Scenarios**:

1. **Given** an invoice awaiting decision and a user submits a rejection, **When** the rejection reason is blank, **Then** the system rejects the rejection and preserves the invoice state.
2. **Given** a valid invoice and an invalid approval attempt, **When** the approval is rejected, **Then** the invoice remains exactly in its previous state and no partial decision data is written.
3. **Given** a valid invoice before an attempted decision, **When** the user attempts approval or rejection with an invalid role, invalid order, duplicate approver, or already-finalized invoice, **Then** the system rejects the request and leaves the invoice unchanged.

---

### Edge Cases

- What happens when the invoice amount is exactly 50,000 for a Manager approval?
- What happens when the invoice amount is exactly 500,000 for a Director approval?
- What happens when a Director approves an invoice amount below 50,000?
- What happens when the invoice amount is just above 50,000 for a Manager approval?
- What happens when the invoice amount is just above 500,000 for a Director approval?
- What happens when the same user is listed twice in a two-approver approval sequence?
- What happens when the second approver is not a Director?
- What happens when approval is attempted on an already-approved or rejected invoice?
- What happens when the rejection request includes no explanation message?
- What happens when a failed approval occurs after the first approver is validated but before the invoice is persisted?
## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow a Manager to approve an invoice amount up to and including 50,000.
- **FR-002**: The system MUST reject a Manager approval attempt for an invoice amount above 50,000 with a clear reason.
- **FR-003**: The system MUST allow a Director to approve any valid invoice amount up to and including 500,000, including amounts below 50,000.
- **FR-004**: The system MUST reject a Director approval attempt for an invoice amount above 500,000 unless the approval is part of a valid two-approver flow.
- **FR-005**: The system MUST require invoices above 500,000 to have two distinct approvers.
- **FR-006**: The second approver for invoices above 500,000 MUST be a Director.
- **FR-007**: The system MUST reject any approval sequence where the same user appears as both approvers.
- **FR-008**: The system MUST reject invalid approval order, unsupported roles, and unauthorized attempts with a clear reason.
- **FR-009**: The system MUST leave the invoice unchanged when any approval attempt is rejected, with no partial approval or state mutation.
- **FR-010**: The system MUST reject attempts to approve already-finalized invoices.
- **FR-011**: The system MUST verify the runtime demo-user configuration and include the required Manager and Director users before testing or sign-in.
- **FR-012**: The approval rules MUST be implemented in the Domain layer, not in controllers, startup code, or the UI.
- **FR-013**: The project MUST have automated tests covering threshold values, just-above thresholds, duplicate approvers, wrong role attempts, invalid order, already-finalized invoices, and invoice state integrity after rejected approvals.
- **FR-014**: The project MUST include a Playwright E2E test using the actual user interface and Manager demo-user sign-in to verify that an approval attempt for an invoice amount greater than 50,000 and no greater than 500,000 is rejected with a clear message, leaves the invoice `PendingApproval`, and records no final decision.
- **FR-015**: The project MUST use the repository conventions for role-based checks, domain validation, and test structure defined by the current project guidance.

### Key Entities

- **Invoice**: The approval target, including amount, status, actor metadata, and decision state.
- **Approver**: A signed-in user who is evaluated against the applicable role and approval threshold.
- **Approval Decision**: A request to approve or reject an invoice, including approver identity, sequence, and validation result.
- **Role Model**: The authorization model that defines which users can approve invoices and what amount each role is allowed to approve.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All approval-limit boundary scenarios are validated for amounts below 50,000 for Directors, at 50,000, just above 50,000, 500,000, and just above 500,000.
- **SC-002**: Every negative approval scenario produces a clear rejection reason and leaves the invoice state unchanged.
- **SC-003**: The project demonstrates that the approval authority rules are enforced by role and amount without bypassing the Domain layer.
- **SC-004**: Automated test coverage includes valid and invalid approval path validation, including duplicate approvers and invalid order.
- **SC-005**: A Playwright test signs in as a Manager through the UI, attempts to approve an invoice greater than 50,000 and no greater than 500,000, observes a clear rejection message, and verifies that the invoice remains `PendingApproval` with no final decision recorded.
- **SC-006**: The project is ready for implementation planning and later execution of validation commands, mutation testing, and review evidence collection.

## Assumptions

- The existing application uses a role-based authorization model and will continue to do so for this feature.
- The assignment-defined Manager and Director credentials are required for the feature even if the repo baseline currently contains only Approver and Admin roles.
- The business logic must rely on the existing invoice lifecycle and state model rather than introducing a new invoice approval workflow.
- Invoice amounts follow the app’s current monetary conventions and decimal handling.
- UI changes are optional only to the extent required to expose the workflow under the existing app design.

## Constraints and Dependencies

- The feature must respect the repository layering: business rules in the Domain project, API controllers as request handlers, and UI as a thin client.
- The assignment requires verification against boundaried negative cases and the same-user rule, which carry the highest risk of incorrect implementation.
- The runtime role model must be verified and updated before end-to-end sign-in or approval testing.
- Validation must include backend tests, Playwright E2E coverage, and mutation testing as part of the later implementation phase.

## Out of Scope

- Changing the underlying invoice data model beyond what is required for approval validation and state integrity.
- Introducing new persistence or database requirements for this assignment.
- Reworking the whole application architecture to support a different approval model.
- Implementing unrelated invoice workflows outside the approval-limit feature.

## Phase 1 and Phase 2 Planning Summary

### Phase 1 — Complete in this specification

- Document the assignment requirements and current-state findings.
- Define measurable acceptance criteria and risk-based negative cases.
- Prepare the approval-limit specification and identify required validation routes.
- Confirm architecture constraints, security requirements, and unresolved assumptions before feature implementation.

### Phase 2 — Planned future work

- Implement domain-level approval rules for Manager and Director thresholds.
- Update runtime demo-user configuration and role authorization checks.
- Add or revise API error mapping and any required UI behavior.
- Create unit and integration tests covering valid and invalid approval flows.
- Add the Playwright E2E rejection scenario.
- Run backend validation, Playwright tests, mutation testing, and review evidence collection.
- Prepare the pull request and demonstration summary.

## Open Questions / Unresolved Points

- The assignment defines the approval semantics clearly, but the specific UI transaction flow for multi-approver submission must still be validated during implementation against the current web client and controller contracts.
- The current repo baseline includes `Approver` and `Admin` roles; the actual runtime configuration for `Manager` and `Director` must be confirmed before testing sign-in and authorization.
- The exact user-facing error text for invalid approval attempts is not mandated in the assignment beyond requiring a clear reason; wording can be finalized during implementation based on project conventions.
