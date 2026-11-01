# Tasks: Invoice approval role limits

**Input**: Approved design docs from `/specs/001-invoice-approval-limits/`

**Prerequisites**: `spec.md` and `plan.md` are the authoritative sources. `research.md`, `data-model.md`, `quickstart.md`, and `contracts/approval-api.md` provide supporting implementation details.

**Scope**: This task list covers implementation for the approved approval-limit feature and does not modify the approved specification, implementation plan, source code, tests, or runtime configuration.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently. The three approved user stories remain separate milestones and separate commits.

**Commit rule**: Copilot must complete implementation, run the relevant tests, present a review report, and wait for explicit human approval before any git commit is created. No automatic commits are allowed.

## Phase 1: Setup (approval checkpoint)

**Purpose**: Require explicit human approval before implementation begins.

- [ ] T001 Human approval checkpoint before implementation begins in `specs/001-invoice-approval-limits/tasks.md`
- [ ] T002 Verify the approved feature context and confirm no application source, test, or runtime configuration files are changed before implementation in `specs/001-invoice-approval-limits/spec.md` and `specs/001-invoice-approval-limits/plan.md`
- [ ] T003 Confirm the current baseline in `backend/src/InvoiceApp.Domain/ApprovalService.cs`, `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`, `backend/src/InvoiceApp.Api/Auth/SessionAuthMiddleware.cs`, and `backend/src/InvoiceApp.Api/appsettings.json` before story work begins
- [ ] T004 Verify the required runtime demo users for `demo.manager` and `demo.director` are accounted for in the implementation checklist before any sign-in or approval tests are run

---

## Phase 2: Foundational (blocking prerequisites)

**Purpose**: Establish the shared domain, auth, repository, and test-harness baseline required before any user story can be implemented.

**ÃƒÂ¢Ã…Â¡Ã‚Â ÃƒÂ¯Ã‚Â¸Ã‚Â CRITICAL**: No story implementation may begin until this phase is complete.

- [ ] T005 Review the current approval lifecycle and invoice state model in `backend/src/InvoiceApp.Domain/Invoice.cs` and `backend/src/InvoiceApp.Domain/InvoiceStatus.cs` to confirm the baseline before story-specific changes
- [ ] T006 Confirm the session-based identity path used by the API in `backend/src/InvoiceApp.Api/Auth/UserSession.cs` and `backend/src/InvoiceApp.Api/Auth/SessionStore.cs` so approval identity always resolves from the authenticated session
- [ ] T007 Confirm the repository contract and error-mapping behavior in `backend/src/InvoiceApp.Domain/IInvoiceRepository.cs` and `backend/src/InvoiceApp.Api/Controllers/ApiControllerBase.cs` so invalid approvals fail cleanly without partial state mutation
- [ ] T008 Verify that the controller request/response model remains thin and that all business rules remain in the domain layer per `AGENTS.md`
- [ ] T009 Review the test harness in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` and `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs` so all story-specific tests can be added without broadening scope
- [ ] T010 Review error clarity requirements for invalid approvals and rejections without requiring exact error strings as a blocking foundational decision when the specification only requires clear reasons
- [x] T011 US2 design checkpoint: review the finalized >`500,000` approval requirements, including first approval by either `Manager` or `Director`, second approval by a distinct `Director`, same-user rejection, invalid second-role rejection, and the requirement that the invoice remains `PendingApproval` until the second valid approval is recorded; require explicit human review and approval before moving into US2 implementation
- [x] T012 Foundation checkpoint: confirm all role checks, session identity, state integrity rules, and finalized >`500,000` approval sequencing are ready before any user story implementation begins

---

## Phase 3: User Story 1 - Manager and Director approval authority and threshold validation (Priority: P1)

**Goal**: Add `Manager` and `Director` role authority with amount-based threshold checks and invalid-approval guardrails, while preserving the distinction between valid single approvals and the higher-value multi-approver flow.

**Independent Test**: A valid single approval succeeds within the applicable role limit, and invalid approval attempts fail without mutating the invoice state.

### Tests for User Story 1

- [x] T013 [US1] Add backend test coverage for `Manager` approval at `50,000` succeeding in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- [x] T014 [US1] Add backend test coverage for `Manager` approval at `50,001` failing without mutating invoice state in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- [x] T015 [US1] Add backend test coverage for `Director` approval below `50,000` and at `50,001` succeeding in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- [x] T016 [US1] Add backend test coverage for `Director` approval at `500,000` succeeding in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- [x] T017 [US1] Add backend test coverage for a `Manager` approval attempt at `500,000` and a single `Director` approval attempt at `500,001` failing without mutating invoice state, and for invalid role or unauthorized approval attempts with clear rejection reasons in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`

### Implementation for User Story 1

- [x] T018 [US1] Implement amount-based role authorization in `backend/src/InvoiceApp.Domain/ApprovalService.cs` for `Manager` up to and including `50,000` and `Director` for any valid amount up to and including `500,000`
- [x] T019 [US1] Ensure the valid single-approval role limits remain enforced in `backend/src/InvoiceApp.Domain/ApprovalService.cs`: `Manager` may approve up to and including `50,000`, `Director` may approve any valid amount up to and including `500,000`, and `Manager` approvals above `50,000` fail before any invoice mutation
- [x] T020 [US1] Preserve invoice status, approval history, decision metadata, and all invoice fields on rejected approval attempts in `backend/src/InvoiceApp.Domain/ApprovalService.cs`
- [x] T021 [US1] Keep the controller contract thin and map service failures through `backend/src/InvoiceApp.Api/Controllers/ApiControllerBase.cs` and `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`
- [x] T022 [US1] Update the runtime demo-user configuration in `backend/src/InvoiceApp.Api/appsettings.json` to include the required `Manager` and `Director` demo users before testing or sign-in
- [x] T023 [US1] Verify the HTTP and domain error behavior for invalid approvals remains consistent with the projectÃ¢â‚¬â„¢s 404/409/400 mapping conventions in `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`
- [x] T024 [US1] Add and execute a Playwright E2E test in `e2e/tests/` using a page object in `e2e/pages/`: sign in as `demo.manager` through the UI, create and submit an invoice with amount greater than `50,000` and no greater than `500,000`, attempt approval through the UI, assert a clear rejection message and `PendingApproval` status, and verify no final decision is recorded; depends on T018–T023
- [x] T025 [US1] Review the US1 boundary tests and state-preservation semantics before commit, ensuring the story remains limited to legitimate single-approval thresholds and does not duplicate the >`500,000` two-approver enforcement rules
- [x] T026 [US1] Copilot completes the US1 implementation, runs the relevant backend tests, presents the review report, and waits for explicit human approval before any commit is created
- [ ] T027 [US1] Commit milestone only after human approval: `Add Manager and Director approval thresholds and role checks`

**Checkpoint**: User Story 1 should be fully functional, independently testable, and ready only after explicit human approval of the US1 review report.

---

## Phase 4: User Story 2 - Multi-approver enforcement for invoices above 500,000 (Priority: P1)

**Goal**: Enforce the finalized high-value approval flow for invoices above `500,000`, including first-approval authority, second-approver requirements, duplicate-user rejection, invalid second-role rejection, and state preservation for invalid attempts.

**Independent Test**: An invoice above `500,000` can only be approved when two distinct users have valid approval records, and the second approval must come from a distinct `Director`; otherwise the invoice remains `PendingApproval` and unchanged.

### Tests for User Story 2

- [ ] T028 [US2] Add backend test for a `Director` first approval above `500,000`, followed by a failed `Manager` second approval with a clear API error and no mutation, then a different `Director` second approval succeeds and the invoice becomes `Approved` in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- [ ] T029 [US2] Add backend test for `Manager` Ã¢â€ â€™ `Director` approval above `500,000` succeeding in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- [ ] T030 [US2] Add backend test for `Director` Ã¢â€ â€™ `Director` approval above `500,000` succeeding when the users are distinct in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- [ ] T031 [US2] Add backend test for `Manager` Ã¢â€ â€™ `Manager` above `500,000` failing and leaving the invoice as `PendingApproval` in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- [ ] T032 [US2] Add backend test for `Director` Ã¢â€ â€™ `Manager` above `500,000` failing and leaving the invoice as `PendingApproval`, and add coverage for the same user attempting a second approval and for a single `Director` approval above `500,000` leaving the invoice as `PendingApproval` in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- [ ] T033 [US2] Add Playwright E2E coverage for the >`500,000` two-approver flow through the UI in `e2e/tests/` and `e2e/pages/` using accessible role-based locators; verify the invoice stays pending after the first approval and reaches `Approved` only after a distinct Director second approval

### Implementation for User Story 2

- [ ] T034 [US2] Review the finalized pending-first-approval model and API contract for invoices above `500,000` in `backend/src/InvoiceApp.Domain/Invoice.cs` and the approval controller contract; keep the design minimal, domain-owned, and aligned with the server-authenticated user identity
- [ ] T035 [US2] Implement the multi-approver validation in `backend/src/InvoiceApp.Domain/ApprovalService.cs` for invoices above `500,000`, ensuring the first approval can be from either `Manager` or `Director`, the second approver is a distinct `Director`, and the same user cannot approve twice
- [ ] T036 [US2] Reject invalid second-approver role attempts and same-user approval attempts before any status or decision mutation in `backend/src/InvoiceApp.Domain/ApprovalService.cs`
- [ ] T037 [US2] Reject attempts on already-finalized invoices and preserve the original invoice state in `backend/src/InvoiceApp.Domain/ApprovalService.cs`
- [ ] T038 [US2] Keep controller logic thin and pass server-authenticated approver identity through `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs` without trusting client-supplied usernames
- [ ] T039 [US2] Keep UI updates minimal and non-authoritative in `backend/src/InvoiceApp.Api/wwwroot/app.js` or related UI assets; no business logic is allowed in the browser
- [ ] T040 [US2] Validate the full high-value approval sequence and the explicit rejection paths before commit, including same-user, wrong-role, invalid ordering, and unchanged state assertions, and review the API and UI error clarity
- [ ] T041 [US2] Copilot completes the US2 implementation, runs the relevant backend and E2E tests, presents the review report, and waits for explicit human approval before any commit is created
- [ ] T042 [US2] Commit milestone only after human approval: `Enforce two-approver Director approval rules above 500k`

**Checkpoint**: User Story 2 should be independently testable and committed only after explicit human approval of the US2 design and implementation review report.

---
## Phase 5: User Story 3 - Rejection integrity and invoice state preservation (Priority: P1)

**Goal**: Ensure invalid approvals and rejections never partially update invoice data, that approval history remains unchanged on invalid attempts, and that explicit invoice rejection remains a separate behavior from invalid approval failures.

**Independent Test**: An invalid approval attempt preserves invoice status, approval history, decision metadata, and other relevant state, while an explicit rejection still transitions the invoice to `Rejected` according to the existing rejection rules.

### Tests for User Story 3

- [ ] T043 [US3] Add backend test for blank rejection reason rejection in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- [ ] T044 [US3] Add backend test for invalid approval attempts preserving invoice status, approval history, decision metadata, and other relevant state in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- [ ] T045 [US3] Add backend test asserting invalid approval attempts do not add approval records or change decision metadata in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- [ ] T046 [US3] Add backend test verifying explicit invoice rejection still transitions to `Rejected` according to the existing rejection rules in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`

### Implementation for User Story 3

- [ ] T047 [US3] Ensure invalid approval or rejection requests return clear reasons and never partially update invoice state in `backend/src/InvoiceApp.Domain/ApprovalService.cs`
- [ ] T048 [US3] Preserve invoice status, approval history, actor metadata, timestamps, and decision-note data on invalid approval attempts, while keeping rejection logic separate from approval validation in `backend/src/InvoiceApp.Domain/ApprovalService.cs`
- [ ] T049 [US3] Enforce rejection-message validation and invalid rejection path checks in the domain layer before any write operation, without conflating them with the >`500,000` approval-order rules in `backend/src/InvoiceApp.Domain/ApprovalService.cs`
- [ ] T050 [US3] Keep API behavior aligned with controller mapping and session-based auth in `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`
- [ ] T051 [US3] Review the state-preservation assertions and confirm they are limited to invalid approval and rejection integrity without duplicating US1 or US2 role, threshold, or duplicate-approver coverage
- [ ] T052 [US3] Copilot completes the US3 implementation, runs the relevant backend tests, presents the review report, and waits for explicit human approval before any commit is created
- [ ] T053 [US3] Commit milestone only after human approval: `Preserve invoice state and rejection integrity on invalid approvals`

**Checkpoint**: User Story 3 should be complete, independently validated, and committed only after explicit human approval of the US3 review report.

---

## Phase 6: Final validation and review

**Purpose**: Run the required backend, UI, and mutation validation only after all three stories are complete and individually approved.

- [ ] T054 Run the backend suite with `dotnet test backend/InvoiceApp.sln` and confirm all approval-limit, negative-path, and invalid-state tests pass
- [ ] T055 Run the Playwright E2E suite with `cd e2e && npm install && npx playwright install chromium && npm test` and confirm the rejected over-limit UI scenario passes while checking both the displayed rejection reason and unchanged invoice state
- [ ] T056 Run mutation testing with the installed global Stryker 4.13.0 against the domain approval logic and matching test project, without reinstalling or altering project dependencies
- [ ] T057 Review mutation survivors, uncovered negative paths, and story-specific test gaps before final sign-off
- [ ] T058 Final review checkpoint: verify all three user stories remain in three separate commits, the approved specification and plan remain unchanged, and the implementation is ready for final human approval

---

## Dependencies & execution order

### Phase dependencies

- **Phase 1: Setup**: must occur before implementation starts, with human approval required
- **Phase 2: Foundational**: must complete before any story work starts
- **Phase 3: User Story 1**: depends on Phase 2 completion
- **Phase 4: User Story 2**: depends on the Phase 2 foundation and explicit human approval of the US2 design checkpoint
- **Phase 5: User Story 3**: depends on Phase 2, the US1 commit approval, and the US2 commit approval
- **Phase 6: Final validation**: depends on all three story milestones being complete and committed

### User story dependency map

- **User Story 1 (US1)**: no dependency on other stories; establishes the threshold baseline and valid single-approval rules within each role limit
- **User Story 2 (US2)**: depends on US1 being complete and approved; it adds the final >`500,000` sequence enforcement and invalid second-approver cases
- **User Story 3 (US3)**: depends on US1 and US2 being complete and approved; it validates rejection integrity, invalid approval preservation, and state immutability

### Parallel opportunities

- Setup tasks T002ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“T004 can proceed in parallel once the approval checkpoint is complete.
- Foundational tasks T006ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“T009 can proceed in parallel after the baseline review is complete.
- Story tests within the same user story may be drafted in parallel, but task labels remain single-story only when they touch the same file and could conflict.
- Final validation tasks T055-T056 are independent and can proceed in parallel after all stories are approved and committed.

---

## Implementation strategy

### MVP first

1. Complete the approval checkpoint and baseline review.
2. Complete Phase 2: foundational domain, auth, and repository validation.
3. Implement User Story 1 and obtain explicit human approval before commit.
4. Complete the US2 design checkpoint and obtain explicit human approval before US2 implementation begins.
5. Implement User Story 2 and obtain explicit human approval before commit.
6. Implement User Story 3 and obtain explicit human approval before commit.
7. Run the final validation and mutation testing tasks.

### Separate-commit expectation

- Commit 1: `Add Manager and Director approval thresholds and role checks`
- Commit 2: `Enforce two-approver Director approval rules above 500k`
- Commit 3: `Preserve invoice state and rejection integrity on invalid approvals`

### Review checkpoints required before each story commit

- US1 review checkpoint: confirm threshold logic, valid single-approval behavior, and invalid approval state-preservation without multi-approver scope
- US2 design checkpoint: confirm the finalized >`500,000` approval flow before implementation begins, including first approver by either `Manager` or `Director`, second approver as distinct `Director`, same-user rejection, and invalid second-role rejection
- US2 review checkpoint: confirm duplicate approvers, wrong second-approver role, invalid order, state immutability, and the over-limit UI rejection flow
- US3 review checkpoint: confirm rejection reasons, invalid approval state preservation, and explicit rejection semantics without duplicating US1/US2 logic

---

## Notes

- All tasks follow the required checklist format: `- [ ] T### ...`
- Story-scoped tasks are labeled with `[US1]`, `[US2]`, or `[US3]` as required.
- This task list intentionally does not modify application source code, tests, runtime configuration, or approved repository documents before approval.
- The task list preserves the approved architecture, the three user stories, and the Stryker 4.13.0 final validation requirement.
- No automatic commit step is allowed; all commits require explicit human approval after the implementation and review report are complete.
- Approval attempts must respect the corrected business rules: valid single approvals within role limits, a valid multi-approver flow above `500,000`, no duplicate approvers, no invalid second approver role, and no partial mutation on invalid approval or rejection attempts.
