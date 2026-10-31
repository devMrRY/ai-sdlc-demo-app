# Project Context

This file captures only verified repository facts at the current checkpoint.

Manual refresh requirement: this document is updated only when the user explicitly asks for a context refresh or explicitly invokes the `refresh-project-context` skill. It is not refreshed automatically during normal work or incidental edits.

## Verified repository state

- Repository branch: `main`
- Current HEAD: `66cb3f9005af1feafb7890f06bbc41fd652b432a`
- Working tree state from `git status --short --branch`:
  - untracked: `.github/hooks/`
  - untracked: `.github/logs/`
  - untracked: `.github/skills/`
  - untracked: `AI_INTERACTION_LOG.md`
  - untracked: `ASSIGNMENT.md`
  - untracked: `HANDOFF.md`
  - untracked: `PROJECT_CONTEXT.md`

## Application overview

This is a .NET 8 invoice approval demo app with a simple API and static web UI.

### Backend structure

- `backend/src/InvoiceApp.Domain`
  - domain model and business rules
  - `ApprovalService` owns the invoice lifecycle
  - `Invoice` holds `Id`, `CustomerName`, `Amount`, `Status`, `CreatedAt`, `DecidedBy`, `DecidedAt`, `DecisionNote`
- `backend/src/InvoiceApp.Api`
  - controllers and authentication middleware
  - in-memory repository implementation
  - static web client under `wwwroot/`
- `backend/tests/InvoiceApp.Tests`
  - xUnit tests for the domain logic

### Domain flow

The current verified invoice lifecycle is:

- `Draft`
- `PendingApproval`
- `Approved`
- `Rejected`

The `ApprovalService` currently implements:

- `Submit(string invoiceId)`
  - moves an invoice from `Draft` to `PendingApproval`
  - rejects non-draft invoices
- `Approve(string invoiceId, string approver)`
  - requires the invoice to be in `PendingApproval`
  - sets `Status = Approved`, `DecidedBy`, and `DecidedAt`
- `Reject(string invoiceId, string approver, string reason)`
  - requires a non-empty rejection reason
  - requires the invoice to be in `PendingApproval`
  - sets `Status = Rejected`, `DecidedBy`, `DecidedAt`, and `DecisionNote`

## Verified commands

Run the app:

```bash
dotnet run --project backend/src/InvoiceApp.Api
```

Run backend tests:

```bash
dotnet test backend/InvoiceApp.sln
```

## Verified test result

The current backend test run completed successfully:

- Passed: 7
- Failed: 0
- Skipped: 0

This was verified with:

```bash
dotnet test backend/InvoiceApp.sln --nologo
```

## Verified logging setup

The repository currently contains Copilot hook logging support under `.github`.

- `.github/hooks/logger.json` registers hook commands for `SessionStart`, `UserPromptSubmit`, `PreToolUse`, `PostToolUse`, `PostToolUseFailure`, `SubagentStart`, `SubagentStop`, `Stop`, and `SessionEnd`
- `.github/hooks/log-interaction.mjs` reads stdin JSON payloads, groups events by `sessionId`, and appends summary records to `.github/logs/ai-interactions.jsonl`
- `.github/logs/sessions/` is used as temporary per-session storage until the session is finalized
- `.github/skills/ai-interaction-logger/SKILL.md` describes the logging intent and expected audit-shape

The current logger implementation in `log-interaction.mjs` is a prompt + tool summary logger. It records:

- `timestamp`
- `sessionId`
- `prompt`
- `tool`
- `actionTook`
- `error`
- `toolResult`

It does not currently extract a final assistant summary from transcript data. The script intentionally strips large `code` payloads from logged tool arguments to keep the audit log compact.

## Verified documentation files

- `AGENTS.md` — repository conventions for agents and app expectations
- `README.md` — app overview, run instructions, roles, and basic project layout
- `ASSIGNMENT.md` — assignment requirements and workflow expectations
- `AI_INTERACTION_LOG.md` — current AI usage/logging record

## Unverified or not yet completed at this checkpoint

These are not confirmed as complete based on the current repo state:

- feature implementation for approval limits beyond the current core approval workflow
- mutation testing results
- Playwright end-to-end test execution
- final pull request creation and review
- the presence of a finished `PROJECT_CONTEXT.md`/`HANDOFF.md` before this file
- completion of the full assignment workflow beyond the verified baseline app and tests

## Notes for the next agent

Before making product changes, verify the intended requirement against `ASSIGNMENT.md` and the current app behavior. The repo is currently in a verified baseline state: the app builds and the existing backend tests pass, but the broader assignment work is not yet claimed complete.

## Auth and role flow

The current app authenticates users by username and password against the configured demo users in `backend/src/InvoiceApp.Api/appsettings.json`.

- `AuthController.Login` accepts a `{ username, password }` payload and checks it against the configured users list.
- If the credentials match, it creates a bearer token via `SessionStore` and returns it to the client.
- `SessionAuthMiddleware` enforces auth for all `/api` routes except `/api/auth/login` by validating the bearer token.
- `RequireRoleAttribute` enforces role-based authorization on controller actions by checking the session role against the allow-list defined on the controller action.
- The current configured demo roles are:
  - `Approver`
  - `Admin`
- `Manager` and `Director` are required by the assignment, but they are not yet present in the current runtime configuration and must be added before a cold agent can sign in as them.

## Configured demo users

The current runtime users in `appsettings.json` are:

- `demo.approver` / `ChangeMe123!` (role `Approver`)
- `demo.admin` / `ChangeMe123!` (role `Admin`)

The assignment requires additional demo users:

- `demo.manager` / `ChangeMe123!` (role `Manager`)
- `demo.director` / `ChangeMe123!` (role `Director`)

These must be added to the runtime configuration before they can be used for approval testing or sign-in.
