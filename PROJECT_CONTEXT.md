# Project Context

This file is a snapshot of repository facts verified on 2026-11-01.

## Repository state

- Repository: `devMrRY/ai-sdlc-demo-app`
- Branch: `feature/us2-multi-approval-flow`
- HEAD: `21d6b8c44e859ecffb34152ccaef986e3d1b2338`
- Recent commits include `21d6b8c` (`fix: reviewed the PR and fixed findings, created docs`) and `d8de64f` (`US1: enforce Manager and Director approval limits`).
- The US2 implementation is present as uncommitted worktree changes; no US2 commit is claimed.
- The worktree also contains modified `.github/logs/ai-interactions.jsonl` and the untracked `.github/logs/sessions/b30ed110-1dbc-49a1-a969-c64a9e8daad1/`. Preserve these unrelated log changes.
- T042, the US2 commit milestone, remains unchecked.

## Application and repository structure

This is an ASP.NET Core 8 Web API with a static web UI and an in-memory invoice repository.

- `backend/src/InvoiceApp.Domain/`
  - `Invoice.cs`: invoice state, decision fields, and approval history.
  - `ApprovalRecord.cs`: persisted approver identity, role, and timestamp.
  - `InvoiceStatus.cs`: `Draft`, `PendingApproval`, `Approved`, and `Rejected`.
  - `ApprovalService.cs`: invoice submission, approval, rejection, sequencing, and business validation.
  - `IInvoiceRepository.cs`: domain repository interface, including atomic invoice operations.
- `backend/src/InvoiceApp.Api/`
  - `Program.cs`: dependency registration, middleware, and controller mapping.
  - `Controllers/`: invoice, approval, and authentication endpoints; `ApiControllerBase` maps domain errors to HTTP responses.
  - `Auth/`: demo-user configuration, `SessionStore`, bearer-session middleware, and `RequireRoleAttribute`.
  - `InMemoryInvoiceRepository.cs`: in-memory repository implementation with atomic per-invoice operations.
  - `wwwroot/`: static UI (`index.html`, `app.js`, and `styles.css`).
- `backend/tests/InvoiceApp.Tests/`
  - `ApprovalServiceTests.cs`: domain behavior tests.
  - `ApprovalsControllerAuthorizationTests.cs`: API authorization tests using ASP.NET Core TestServer.
  - `FakeInvoiceRepository.cs`: test repository.
- `e2e/pages/` and `e2e/tests/`: Playwright page object and browser tests.
- `specs/001-invoice-approval-limits/`: feature specification, plan, tasks, and supporting design/API artifacts.
- `.github/`: agent instructions, skills, hooks, PR review artifacts, and interaction logs.
- `docs/use-cases/`: feature use-case briefs.

## Verified implementation status

### US1 — implemented

The committed US1 implementation adds Manager and Director demo users in `backend/src/InvoiceApp.Api/appsettings.json`. The current configured roles are `Approver`, `Admin`, `Manager`, and `Director`.

`ApprovalService.Approve(invoiceId, approver, role)` validates the role and amount before mutation:

- Manager may approve an invoice up to and including 50,000.
- Director may approve an invoice up to and including 500,000, including amounts at or below 50,000.
- An independent single approval above 500,000 is not allowed; the US2 provisional-first-approval flow is described separately below.
- Invalid approval attempts throw before changing invoice state.

The approval endpoint is restricted to Manager/Director and obtains the identity and role from the authenticated session. The separate rejection endpoint is restricted to Approver/Admin. `ApiControllerBase.Handle(...)` maps missing invoices to 404, invalid state/business-rule errors to 409, and argument errors to 400.

The US1 tests include amount/role boundaries, state preservation for invalid attempts, API authorization through TestServer, and the required Manager over-limit Playwright scenario. The UI test confirms the invoice remains pending and final decision metadata is unset after rejection.

The review report `.github/PRs/us1-review-report.md` records F-01 as dismissed and F-02 as fixed; its overall merge decision remains a human decision.

### US2 — implemented through T041; commit milestone pending

For invoices above 500,000, either a Manager or Director may provide the first provisional approval. The service records the authenticated user's identity, role, and timestamp in `Invoice.ApprovalHistory`, while leaving the invoice `PendingApproval` and keeping `DecidedBy` and `DecidedAt` unset. A different authenticated Director must provide the second approval; only then is the invoice marked `Approved` and final decision metadata set.

The approval endpoint continues to derive identity and role from the session. The domain service owns approval sequencing and validation. Repository operations serialize each invoice's state transition. The client does not supply an approval stage or trusted identity. Invalid second approvals preserve invoice state and history; rejection remains a separate operation.

Backend tests cover Manager→Director and Director→Director success, invalid second-role and duplicate-user attempts, recovery with a different Director, finalized invoices, history integrity, and concurrent first approvals. The Playwright test `e2e/tests/high-value-two-approver-approval.spec.ts` verifies the Manager→Director flow in the UI and checks provisional and final persisted state.

`specs/001-invoice-approval-limits/tasks.md` marks T028–T041 complete. T042 is the separate commit milestone and remains unchecked. The US2 implementation is uncommitted. US3 tasks T043–T053 remain outstanding.

## Verified validation

Validation was run on the current checkout on 2026-11-01:

- Targeted API authorization tests: `dotnet test backend/InvoiceApp.sln --filter FullyQualifiedName~ApprovalsControllerAuthorizationTests` — passed, 14 passed, 0 failed, 0 skipped.
- Full backend suite: `dotnet test backend/InvoiceApp.sln` — passed, 37 passed, 0 failed, 0 skipped.
- Playwright: `npm test` from `e2e/` — 2 passed, as recorded during the US2 review. This E2E run preceded the current API-test assertion-only edit; application/UI code was not changed by that edit.
- `git diff --check` passed during the US2 review. Mutation testing was not run or verified.

Commands:

```powershell
dotnet run --project backend/src/InvoiceApp.Api
dotnet test backend/InvoiceApp.sln
Set-Location e2e; npm test
```

The E2E package also documents dependency/browser setup in `e2e/package.json` and `e2e/playwright.config.ts`.

## Logging and agent support

- `.github/hooks/logger.json` registers lifecycle, prompt, tool, and subagent hook events.
- `.github/hooks/log-interaction.mjs` writes interaction records to `.github/logs/ai-interactions.jsonl` and per-session files under `.github/logs/sessions/`.
- `.github/skills/ai-interaction-logger/SKILL.md` documents the logging requirements.
- `.github/skills/refresh-project-context/SKILL.md` defines this explicit refresh workflow.
- Project instructions are in `AGENTS.md`; assignment requirements are in `ASSIGNMENT.md`.

## Known limitations and pending work

- US3 rejection-integrity work remains outstanding; do not infer completion from US2 coverage.
- T042 remains pending human approval and has not been executed.
- The TestServer authorization fixture composes its own host and middleware pipeline rather than exercising production startup wiring. The review accepted this narrower startup-wiring coverage; the tests do exercise the real session store, middleware, role filter, controller, and error mapping.
- Mutation testing and remaining assignment deliverables are not verified complete.
- Preserve the current uncommitted interaction/session log paths; do not overwrite or discard them.
