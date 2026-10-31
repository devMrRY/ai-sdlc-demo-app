# Implementation Plan: Invoice approval role limits

**Branch**: `001-invoice-approval-limits` | **Date**: 2026-10-31 | **Spec**: `specs/001-invoice-approval-limits/spec.md`

**Input**: Approved feature specification from `/specs/001-invoice-approval-limits/spec.md`

## Summary

This feature extends the existing .NET 8 invoice approval demo with role-based approval limits and a two-approver rule for high-value invoices. The app currently supports single-approver approval and only `Approver`/`Admin` roles. The approved specification requires adding `Manager` and `Director`, enforcing threshold logic, preserving invoice state on rejected approvals, and honoring the repository’s layered architecture.

The implementation will be staged into three independent milestones, each mapped to one approved user story and committed separately. Business rules will remain in `backend/src/InvoiceApp.Domain`, the controllers will remain thin HTTP adapters, and the static UI will remain minimal.

## Technical Context

**Language/Version**: C#, .NET 8 ASP.NET Core Web API

**Primary Dependencies**:
- ASP.NET Core MVC / API
- In-memory repository abstraction (`IInvoiceRepository`)
- xUnit for backend tests
- Playwright for end-to-end browser validation
- Global `dotnet-stryker` version 4.13.0 already installed, to be used without reinstalling

**Storage**: In-memory repository in `backend/src/InvoiceApp.Api/InMemoryInvoiceRepository.cs`

**Testing**:
- Backend: `dotnet test backend/InvoiceApp.sln`
- E2E: `cd e2e && npm install && npx playwright install chromium && npm test`
- Mutation: use the installed global `dotnet-stryker` 4.13.0 and record results

**Target Platform**: Local ASP.NET Core app with a static web UI, validated in the current repository environment

**Project Type**: Web application with API, domain service, static UI, and test projects

**Performance Goals**: Not performance-sensitive; correctness, role validation, threshold logic, and state integrity are the priorities.

**Constraints**:
- Business rules must stay in `InvoiceApp.Domain` and not leak into the controller or UI.
- Approval identity must come from the authenticated session; no client-supplied username is authoritative.
- The three user stories must be implemented independently and committed separately.
- The approved specification must remain unchanged.
- `tasks.md` is not generated in this phase.

**Scale/Scope**:
- Small invoice approval demo with one invoice lifecycle and a lightweight role model.
- Scope limited to the approval-limit feature and required validation coverage.

## Constitution Check

The active repository guidance is defined by `AGENTS.md`, `ASSIGNMENT.md`, and the verified state in `PROJECT_CONTEXT.md`. The `.specify/memory/constitution.md` file in this repo is a template placeholder, not a project-specific governance document.

Gate assessment:
- Domain-layer business rules: pass
- Role-based authorization and authenticated identity: pass
- Boundary and negative-case coverage: pass
- Layering and architecture conventions: pass
- Independent milestone commit requirement: pass

No constitution-level exception is required. The remaining open points are implementation details that must be resolved using repository evidence and review behavior rather than guessing.

## Project Structure

```text
ai-sdlc-demo-app/
├── AGENTS.md
├── ASSIGNMENT.md
├── PROJECT_CONTEXT.md
├── README.md
├── specs/
│   └── 001-invoice-approval-limits/
│       ├── spec.md
│       ├── plan.md
│       ├── research.md
│       ├── data-model.md
│       ├── quickstart.md
│       └── contracts/
│           └── approval-api.md
├── backend/
│   ├── InvoiceApp.sln
│   ├── src/
│   │   ├── InvoiceApp.Domain/
│   │   │   ├── ApprovalService.cs
│   │   │   ├── DiscountCalculator.cs
│   │   │   ├── IInvoiceRepository.cs
│   │   │   ├── Invoice.cs
│   │   │   ├── InvoiceApp.Domain.csproj
│   │   │   └── InvoiceStatus.cs
│   │   └── InvoiceApp.Api/
│   │       ├── Program.cs
│   │       ├── InMemoryInvoiceRepository.cs
│   │       ├── appsettings.json
│   │       ├── Auth/
│   │       │   ├── RequireRoleAttribute.cs
│   │       │   ├── SessionAuthMiddleware.cs
│   │       │   ├── SessionStore.cs
│   │       │   └── UserSession.cs
│   │       ├── Controllers/
│   │       │   ├── ApiControllerBase.cs
│   │       │   ├── ApprovalsController.cs
│   │       │   ├── AuthController.cs
│   │       │   └── InvoicesController.cs
│   │       └── wwwroot/
│   │           ├── app.js
│   │           ├── index.html
│   │           └── styles.css
│   └── tests/
│       └── InvoiceApp.Tests/
│           ├── ApprovalServiceTests.cs
│           ├── FakeInvoiceRepository.cs
│           └── InvoiceApp.Tests.csproj
├── e2e/
│   ├── package.json
│   ├── playwright.config.ts
│   ├── pages/
│   └── tests/
├── .specify/
│   ├── integration.json
│   ├── init-options.json
│   ├── memory/
│   ├── scripts/
│   ├── templates/
│   └── workflows/
└── .github/
    └── templates/
        └── review-analysis-output.md
```

**Structure Decision**: The feature remains in the existing repository structure. No new architectural layer is introduced. Any new files must be minimal and directly support the approved feature requirements.

## Phase 0: Research and design decisions

### Research findings captured in `research.md`

- The app is a .NET 8 ASP.NET Core API with a static web UI and in-memory data store.
- The current app uses session-based authentication and controller-level `RequireRole` checks.
- The current role model contains `Approver` and `Admin`; `Manager` and `Director` are not yet present in runtime configuration.
- The current approval flow is single approver only.
- The repo has no current multi-approver API contract.
- `DecisionNote` is persisted for valid rejection reasons and returned in the invoice response payload.
- The feature must preserve server-authoritative identity checks for all approvers.

### Key decisions

1. Keep approval rules in `ApprovalService`.
2. Preserve authenticated session identity as the source of truth for approver identity.
3. Keep the invoice lifecycle and current domain model unless a minimal extension is required for multi-step approval state.
4. Fail closed on invalid approval paths so that invoice state remains unchanged.
5. Keep API and UI as thin adapters instead of business-rule owners.

## Phase 1: Design artifacts

The following artifacts were created in the approved feature directory and are the implementation baseline:

- `specs/001-invoice-approval-limits/research.md`
- `specs/001-invoice-approval-limits/data-model.md`
- `specs/001-invoice-approval-limits/quickstart.md`
- `specs/001-invoice-approval-limits/contracts/approval-api.md`

These support the approved specification rather than replacing it.

## Implementation Milestones

### Milestone 1 – User Story 1: Manager and Director approval authority and threshold validation

**Implementation scope**
- Add required `Manager` and `Director` runtime users to `appsettings.json`.
- Enforce thresholds in `ApprovalService`:
  - Manager allowed up to and including `50,000`
  - Director allowed up to and including `500,000`
- Reject invalid role/amount combinations with a clear reason.
- Preserve invoice state on all invalid approval attempts.

**Files to modify**
- `backend/src/InvoiceApp.Domain/ApprovalService.cs`
- `backend/src/InvoiceApp.Domain/Invoice.cs` only if a minimal extension is needed for approval metadata
- `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`
- `backend/src/InvoiceApp.Api/appsettings.json`
- `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`

**Files to create**
- No new architecture files are required during this milestone.

**Domain / API / UI responsibilities**
- Domain: threshold and role validation before any mutation.
- API: keep the session-driven controller contract and map service failures using `ApiControllerBase.Handle(...)`.
- UI: minimal error messaging only; no business rules in JavaScript.

**Required tests**
- Manager approval at `50,000` succeeds
- Manager approval at `50,001` fails
- Director approval at `500,000` succeeds
- Director approval above `500,000` without a valid second approver is rejected
- Wrong-role approval is rejected
- State-preservation assertions for invalid approval attempts

**Dependencies**
- This is the first milestone and establishes the threshold baseline required by the later multi-approver rules.

**Validation**
- `dotnet test backend/InvoiceApp.sln`
- Review against repository role and layering guidance

**Commit**
- `Add Manager and Director approval thresholds and role checks`

### Milestone 2 – User Story 2: Multi-approver enforcement for invoices above 500,000

**Implementation scope**
- Add server-side two-approver rules for invoices above `500,000`.
- Require two distinct approvers.
- Require the second approver to be a `Director`.
- Reject duplicate approvers and invalid approval order.
- Reject attempts on already-finalized invoices.
- Preserve invoice state on all invalid sequences.

**Files to modify**
- `backend/src/InvoiceApp.Domain/ApprovalService.cs`
- `backend/src/InvoiceApp.Domain/Invoice.cs` only if a minimal extension is needed for approval sequence tracking
- `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`
- `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- `backend/src/InvoiceApp.Api/wwwroot/app.js` only if the UI must surface the flow without moving business rules into the client

**Files to create**
- Only create minimal helper types if absolutely needed by the request contract or domain validation; otherwise keep the implementation in the existing files.

**Domain / API / UI responsibilities**
- Domain: all validation for duplicate approvers, wrong role, ordering, and finalized invoice checks.
- API: define the minimal multi-approver request contract if required; keep it thin and authenticated.
- UI: no business logic in the browser; any UI changes must mirror the backend contract.

**Required tests**
- Same user cannot approve twice
- Second approver must be a `Director`
- Invalid approval order is rejected
- A valid two-step approval succeeds for amounts above `500,000`
- Attempting approval on an already-finalized invoice fails without mutation

**Dependencies**
- Depends on Milestone 1 passing and remaining in place.

**Validation**
- `dotnet test backend/InvoiceApp.sln`
- Review risk-prone cases against the assignment: duplicate approvers and same-user rejection

**Commit**
- `Enforce two-approver Director approval rules above 500k`

### Milestone 3 – User Story 3: Rejection integrity and invoice state preservation

**Implementation scope**
- Require a rejection reason for invalid reject attempts.
- Ensure invalid approvals or rejections never partially update invoice state.
- Preserve `Status`, `DecidedBy`, `DecidedAt`, and `DecisionNote` on rejected attempts.

**Files to modify**
- `backend/src/InvoiceApp.Domain/ApprovalService.cs`
- `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`
- `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- `backend/src/InvoiceApp.Api/wwwroot/app.js` only for error presentation if needed

**Files to create**
- No new project-level files are required for this milestone.

**Domain / API / UI responsibilities**
- Domain: enforce rejection reason validity and no-partial-update semantics.
- API: preserve clear error mapping and session-based identity semantics.
- UI: surface clear backend feedback without implementing any business rules.

**Required tests**
- Blank rejection reason is rejected
- Invalid approval on a finalized invoice does not mutate state
- Duplicate approver invalid path leaves invoice unchanged
- Wrong-role invalid path leaves invoice unchanged
- Invalid order invalid path leaves invoice unchanged
- Full-state comparison for invalid approvals and invalid rejects

**Dependencies**
- Depends on Milestone 1 and Milestone 2.

**Validation**
- `dotnet test backend/InvoiceApp.sln`
- Confirm no partial approval writes occur on invalid paths

**Commit**
- `Preserve invoice state and rejection integrity on invalid approvals`

## Final integrated validation

### Backend validation

```bash
dotnet test backend/InvoiceApp.sln
```

Required checks:
- Manager threshold logic at `50,000`
- Director threshold logic at `500,000`
- Invalid approval attempts preserve state
- Duplicate approvers rejected
- Same-user rejection enforced
- Invalid order rejected
- Already-finalized invoice rejected
- Rejection reason required and persisted only on valid rejection

### Playwright E2E validation

The required browser test must be implemented in the final integration phase and must cover a rejected over-limit approval through the UI using the repository’s role-based locators.

Location:
- `e2e/tests/`
- `e2e/pages/`

Required scenario:
- Sign in with a valid role
- Attempt an over-limit approval in the browser UI
- Confirm the system rejects the request with a clear reason
- Confirm the invoice remains unchanged

Command:

```bash
cd e2e
npm install
npx playwright install chromium
npm test
```

### Mutation testing

The required version of `dotnet-stryker` is already installed globally and should not be reinstalled.

Run mutation testing and record:
- mutation score
- surviving mutants
- any uncovered negative paths requiring new tests

Do not weaken valid tests to obtain a pass.

## Review and evidence collection

- Use the repo’s review-analysis output template in `.github/templates/review-analysis-output.md`.
- Record findings tied to real repository evidence and assignment requirements.
- Mark each review finding as fixed, dismissed with reason, or deferred with reason.
- Ensure the PR description includes the feature context, decisions, and test evidence.

## Open questions and risk controls

### Open questions

1. Exact API contract for the second approver in a multi-step approval request.
   - Verified repo state has no second-approver contract today.
   - This must be defined as part of implementation without trusting client-supplied usernames.

2. Whether the invoice model needs minimal approval-sequence metadata beyond the existing decision fields.
   - Current domain model does not include approval-chain tracking.
   - It should only be extended if required by the implementation and the approved feature scope.

3. Exact user-facing error wording for invalid approvals.
   - The approved spec requires a clear reason but not a specific string.

### Risk controls

- Never use client-supplied username data as proof of approver identity.
- Do not allow invalid approvals to partially mutate invoice state.
- Keep the domain service as the single authority for business-rule enforcement.
- Ensure milestone commits remain independent and reviewable.

## Completion criteria

This plan is valid when:
- The approved spec remains unchanged.
- The implementation follows the repository architecture and layering rules.
- The three user stories are implemented in three independent commits.
- The required Playwright test is part of the implementation plan and not omitted.
- Validation includes backend tests, E2E tests, and mutation testing.
- No application code or tests are modified before human review and approval.

## Files created or modified in this plan step

Modified:
- `specs/001-invoice-approval-limits/plan.md`

Created:
- `specs/001-invoice-approval-limits/research.md`
- `specs/001-invoice-approval-limits/data-model.md`
- `specs/001-invoice-approval-limits/quickstart.md`
- `specs/001-invoice-approval-limits/contracts/approval-api.md`

No application code, tests, or approved specification files were modified as part of this planning step.
