# AI in SDLC Assignment — Persistent Context

## 1. Assignment Objective

Complete one feature in the `ai-sdlc-demo-app` repository, starting from the written specification and ending with a reviewed pull request.

This assignment is intended to demonstrate responsible AI-assisted software development. The AI agent performs the implementation work, while the developer remains responsible for validating requirements, reviewing changes, running tests, and accepting or rejecting findings.

The workflow must demonstrate specification-driven development, automated testing, AI interaction logging, multiple agents, skills/hooks, mutation testing, end-to-end testing, code review, and a final demonstration.

This assignment is the source of truth for the required work and deliverables. Where there is a contradiction between this file and any earlier repository-local version of the assignment, the requirements below take priority.

## 2. What this assignment is

You will take one feature from a written specification through to a reviewed pull request on the workshop demo repository, using an AI agent to do the work while you hold the checks.

The feature itself is small and could be hand-written in an afternoon. That is deliberate. What is being asked for is the evidence around it: a context file that earns its place, tests that would catch a real mistake, and a judgement call on every review finding.

Though the assignment is not tied to one vendor, we suggest using a free tier of GitHub Copilot rather than a paid AI tool. Alternatives are welcome as long as they meet compliance expectations.

## 3. Feature Requirements: Invoice Approval Limits

Add approval limits by role to the invoice approval service in `ai-sdlc-demo-app`:

- A Manager may approve invoices up to 50,000.
- A Director may approve invoices up to 500,000.
- Anything above 500,000 needs two distinct approvers, and the second approver must be a director.
- The same user may never count as both approvers on one invoice.
- A rejected attempt returns a clear reason, and nothing about the invoice changes.

The last two rules are where an agent is most likely to produce something that looks right but is not. Expect that and plan your tests around it.

### Approval limits

- Manager: may approve invoices up to and including 50,000.
- Director: may approve invoices up to and including 500,000.
- Invoices above 500,000 require two distinct approvers.
- The second approver must be a Director.
- The same user must never count as both approvers.

### Rejection behavior

- Unauthorized or invalid approval attempts must be rejected.
- The rejection response must clearly explain why the attempt failed.
- A rejected attempt must leave the invoice state unchanged.
- Failed approval attempts must not partially update approval history or other invoice state.
- An already-Rejected invoice must not be eligible for approval.
- An already-Approved invoice must not be eligible for rejection.
- Only users with the Manager or Director role may perform invoice approval or rejection. Approver, Admin, and unsupported roles remain able to authenticate if configured, but must be forbidden from these operations.
- Forbidden invoice approval or rejection requests return HTTP 403 with `A Manager or Director role is required to perform this operation.` and leave invoice state unchanged.

### Boundary cases

Explicitly test amounts at and around the limits:

- 50,000
- Just above 50,000
- 500,000
- Just above 500,000

Also test duplicate approvers, incorrect approver roles, invalid approval order, attempts to approve an already-Rejected invoice, and attempts to reject an already-Approved invoice.

Use the monetary type and conventions already established in the application. Do not introduce unrelated business rules.

## 4. Repository Context

The application is a .NET 8 API with a simple web UI and an in-memory data store. It does not currently require a database for this assignment.

Known project locations:

- `backend/src/InvoiceApp.Domain` — domain models and business rules; approval rules should belong in the domain layer.
- `backend/src/InvoiceApp.Api` — API endpoints, controllers, and web UI.
- `backend/tests/InvoiceApp.Tests` — xUnit unit and integration tests.
- `e2e` — Playwright end-to-end tests.
- `AGENTS.md` — existing repository-level agent instructions.

The configured demo users include Approver, Admin, Manager, and Director roles. Preserve the Approver and Admin demo users and allow them to sign in; only Manager and Director may approve or reject invoices. Reuse existing conventions and keep these role permissions consistent in the API, UI, and tests.

The repo reference is:

- `https://github.com/NITIN1670/ai-sdlc-demo-app.git`

There is no language constraint for this assignment, so any language is acceptable as long as it meets the repository and workflow requirements.

### Commands

Run the API:

```bash
dotnet run --project backend/src/InvoiceApp.Api
```

Run backend tests:

```bash
dotnet test backend/InvoiceApp.sln
```

Install and run end-to-end tests:

```bash
cd e2e
npm install
npx playwright install chromium
npm test
```

Mutation testing note:

```bash
dotnet tool install -g dotnet-stryker --version 4.13.0
```

The newest Stryker release targets a later runtime and will fail to install. Use the version above explicitly.

The baseline environment previously reported .NET SDK `8.0.426`. The baseline test run had 7 passing tests, 0 failures, and 0 skipped tests. Re-run the tests before relying on this baseline.

## 5. Required Development Workflow

### Stage 1 — Set up the context and specify the feature

1. Read `AGENTS.md` and the relevant domain, API, UI, and test files.
2. Identify the existing approval flow and invoice state transitions.
3. Set up the project context so an agent arriving cold understands the approval rules, the layering, the test command, and review expectations.
4. Write or update a formal feature specification using the specification tool required by the assignment.
5. Define acceptance criteria, edge cases, and expected rejection behavior.
6. Identify which tests would fail if a business rule were implemented incorrectly.
7. Extend `AGENTS.md` so an agent arriving cold knows the approval rules, the layering, the test command, and your review expectations.

Do not begin broad implementation before the specification and acceptance criteria are clear.

### Stage 2 — Implement incrementally

1. Implement domain-level approval rules.
2. Add or update API validation and error responses.
3. Update the UI only where necessary to support the required workflow.
4. Add automated tests for valid and invalid approval scenarios.
5. Keep changes small and reviewable.
6. Complete at least two implementation stages and create at least three meaningful commits across the assignment.
7. Do not ask for the whole feature in a single prompt.
8. Land the single-approver limits first, with tests, then the two-approver rule.
9. Each step is its own commit with a message that says what changed and why.

Do not fabricate commit history. Each commit must correspond to real changes and should have a clear purpose.

### Stage 3 — Validate

- Run backend unit and integration tests.
- Run Playwright end-to-end tests.
- Use mutation testing to evaluate whether tests detect incorrect business logic.
- Record the mutation score, test results, and any limitations.
- Investigate failures instead of weakening or deleting valid tests merely to obtain a passing build.
- Make the tests mean something.

### Stage 4 — AI-assisted review

- Use multiple agents as required by the assignment.
- Have agents review implementation and tests from distinct perspectives.
- Record every meaningful review finding.
- Evaluate each finding individually.
- Mark each finding as accepted, rejected, or requiring further investigation, with a reason.
- Fix accepted findings and re-run relevant tests.
- Record the final review verdict and unresolved issues.
- Review your own pull request with the agent, then judge the review.
- Open the pull request, run the review prompt from the workshop against the diff, and in the description list every finding with one of three verdicts: fixed, dismissed with a reason, or deferred with a reason.

### Stage 5 — Pull request and demonstration

- Prepare a clear pull request description.
- Include the specification, implementation summary, test evidence, mutation-testing results, review findings, and AI usage evidence.
- Prepare an 8–10 minute demonstration covering the workflow and its results.
- Do not claim a pull request was created, merged, or approved unless that action actually occurred.

## 6. AI Interaction Logging

The assignment requires AI interactions to be logged using appropriate skills/hooks capabilities.

The objective is to maintain a useful audit trail without generating a separate human-readable summary for every low-level hook event.

Preferred output:

`.github/logs/ai-interactions.jsonl`

Each completed prompt/agent turn should produce one consolidated JSON record containing:

- `timestamp` — when the turn was finalized.
- `sessionId` — the agent session identifier.
- `prompt` — the submitted user prompt.
- `tool` — tools used during the turn.
- `actionTook` — recorded actions taken by the agent.
- `error` — relevant errors or failures, if any.
- `toolResult` — relevant tool results.
- `assistantConclusion` — the final assistant response, when available from a supported response field or transcript.

Intermediate hook events may be recorded in temporary session files and aggregated at the end of the turn. Temporary files must not be deleted before aggregation succeeds.

### Logging integrity requirements

- Use the actual hook payload and transcript format supplied by the installed runtime.
- Do not invent tool actions, results, prompts, or assistant conclusions.
- Distinguish missing data from a genuine empty result.
- Capture assistant output only when it is available through a verified payload or transcript.
- Avoid storing secrets, access tokens, credentials, or unnecessary personal data.
- Ensure session identifiers are stable and that separate sessions cannot overwrite each other's temporary state.
- Avoid duplicate summaries for the same turn.
- Document fields that cannot be captured reliably.

The hook logger is an audit mechanism, not a record of hidden model reasoning.

## 7. Skills and Hook Requirements

Use appropriate skills for the agents and appropriate hooks for generating the structured output in the same format.

Required expectations:

- Use the repo’s logging skill and hook configuration in `.github/`.
- Use the actual supported hook payload format supplied by the runtime.
- Preserve a consistent JSONL schema with the required fields.
- Keep the log file machine-readable as evidence.
- Use multiple agents and sub-agents for the assignment.

## 8. Specification and Task Breakdown

Write the specification with a spec-driven tool. Use Spec Kit, OpenSpec, or BMAD, whichever you prefer, to produce the specification and task breakdown.

Requirements:

- Commit the files generated by the specification tool.
- Use the generated spec and task breakdown as the basis for implementation.
- Keep the specification and implementation aligned.

## 9. Testing Requirements

Make the tests mean something.

- Write the unit tests for the approval rules and review every generated test.
- Add one Playwright end-to-end test that covers a rejected over-limit approval through the user interface.
- Focus on the negative rules and failure conditions, not just happy-path approvals.
- Ensure tests would catch a real mistake.

The assignment explicitly requires tests for:

- Manager approvals up to the limit
- Director approvals up to the limit
- Over-limit approvals
- Duplicate approvers
- Incorrect approver roles
- Invalid approval order
- Already-finalized invoice attempts
- Approval attempts on already-Rejected invoices
- Rejection attempts on already-Approved invoices
- Clear rejection reason and no state mutation on rejected attempts

## 10. Context and Handoff Files

Use these files to preserve project knowledge and progress:

- `ASSIGNMENT.md` — requirements, constraints, acceptance criteria, and workflow.
- `PROJECT_CONTEXT.md` — verified architecture, important files, domain flow, and commands.
- `HANDOFF.md` — current branch and commit, completed tasks, test results, pending tasks, and the next action.
- `AI_INTERACTION_LOG.md` — readable summary of AI usage, review decisions, and evidence.
- `.github/skills/ai-interaction-logger/SKILL.md` — instructions for consistent logging and audit summaries.
- `.github/hooks/` — hook configuration and logger implementation, using the actual supported configuration format.

The JSONL interaction log is machine-readable evidence; it does not replace the assignment's final report or review documentation.

Update `HANDOFF.md` after significant milestones so a new agent can resume from the last verified checkpoint.

## 11. Developer Responsibilities

The AI agent must not treat its own output as automatically correct.

The developer must:

- Verify the specification against the assignment.
- Review changes before accepting them.
- Ensure tests exercise real failure conditions.
- Check authorization, state consistency, and boundary conditions.
- Evaluate each review finding.
- Confirm that logs reflect actual observed events.
- Verify all claimed results using real commands or evidence.
- Retain control over commits, pull requests, and final acceptance.

The AI agent should prefer focused changes and avoid unnecessary repository-wide refactoring.

## 12. Cost and Tool Constraints

Minimize unnecessary AI usage and preserve available free-tier quota.

- Prefer short, focused prompts.
- Reuse this context instead of repeatedly rediscovering established facts.
- Ask agents to inspect only relevant files.
- Avoid repeating completed work.
- Use deterministic local commands for tests and validation whenever practical.
- Do not assume a paid model, paid extension, or paid service is available.

## 13. Current Progress and Unverified Items

Previously established:

- The .NET 8 application and its backend test project are available.
- The baseline test run reported 7 passing tests.
- The repository has an `AGENTS.md` file.
- Work is underway on Copilot hook-based interaction logging.
- The intended logging design uses temporary per-session events and a consolidated JSONL record.

Still to verify:

- The current Git branch and commit.
- The current working-tree changes.
- Whether the specification tool has been used and what artifact it produced.
- Which approval rules have already been implemented.
- The current backend and Playwright test results.
- Mutation-testing results.
- Whether the required implementation stages and commits are complete.
- Which agents and skills have actually been used.
- Whether the final assistant response can be captured reliably from the installed hook runtime.
- The current review findings, pull request status, and demonstration readiness.

Do not assume any unverified item is complete. Inspect the repository and update `HANDOFF.md` with evidence before continuing.

## 14. Instructions for the Next AI Agent

1. Read this file and `AGENTS.md` first.
2. Read `HANDOFF.md` if it exists.
3. Inspect Git status and recent commits.
4. Verify the current implementation and test results before making changes.
5. Identify the highest-priority incomplete assignment requirement.
6. Propose a small, concrete next step and execute it only after understanding its impact.
7. Update the handoff and audit records after completing work.
8. Never fabricate evidence or mark unfinished requirements as complete.

## 15. What to do

1. Set up the context, then show that it matters. Fork the repository and extend `AGENTS.md` so an agent arriving cold knows the approval rules, the layering, the test command, and your review expectations.
2. Use appropriate skills for the agents.
3. Use appropriate hooks for generating the structured and same-format output.
4. Write the specification with a spec-driven tool. Use Spec Kit, OpenSpec, or BMAD, whichever you prefer, to produce the specification and task breakdown. Commit the files it generates.
5. Implement it in at least two steps. Do not ask for the whole feature in a single prompt. Land the single-approver limits first, with tests, then the two-approver rule. Each step is its own commit, with a message that says what changed and why.
6. Make the tests mean something.
   1. Write the unit tests for the approval rules and review every generated test.
   2. Add one Playwright end-to-end test that covers a rejected over-limit approval through the user interface.
7. Review your own pull request with the agent, then judge the review. Open the pull request, run the review prompt from the workshop against the diff, and in the description list every finding with one of three verdicts: fixed, dismissed with a reason, or deferred with a reason.
8. Log all AI interactions in a log file using the capabilities of skills/hooks.
9. Create multiple agents and sub-agents for the assignment.

## 16. Demo Video

Record one demo video of 8 to 10 minutes. Something must run live on screen: a test, the mutation run, or the end-to-end test. A read-through of files is not a demo.

Use:

- Teams by starting a meeting with only yourself and pressing Record, or
- any screen recorder that produces an MP4 file.

## 17. What to hand in

1. A pull request on your fork, with at least three commits that each stand on their own.
2. The updated `AGENTS.md`.
3. The specification files from whichever spec-driven tool you used.
4. Unit tests for every approval rule, including the two negative rules.
5. One Playwright test covering a rejected over-limit approval.
6. Mutation scores before and after, pasted as text in the write-up.
7. A pull request description listing every review finding with a verdict.
8. A log file capturing all AI interactions.
9. `ASSIGNMENT.md` at the repository root, holding the write-up and the video link.
10. The 8–10-minute demo video, uploaded and linked.

## 18. Final note

The assignment is complete only when all required deliverables are actually present, the pull request exists and has been reviewed, the tests and mutation checks are verified, the logs are in place, and the demo video link is included in the final write-up.

### Context evidence requirement (hard gate)

This requirement is not satisfied by creating a context file alone. `PROJECT_CONTEXT.md`, `AGENTS.md`, and related context artifacts must materially influence the implementation and review.

The implementation must show that the context was used to preserve the repository’s conventions and project goal, specifically:

- business rules remain in the domain layer and do not leak into controllers or startup configuration;
- the approval logic follows the repository’s existing layering and role model conventions;
- the tests are written to catch real failure modes, not just happy paths;
- the implementation and review decisions stay aligned with the app’s stated purpose and existing architecture.

This point is considered complete only when the work includes objective evidence that the context was used in decision-making, for example:

- a design or implementation decision that explicitly follows the repository context and conventions;
- a review finding or PR discussion that references the project context as the reason a change was kept in the correct layer or pattern;
- code or tests that would have been wrong without the preserved context, and were corrected because the context was available.

A context-only document without evidence of operational impact does not count as complete. This requirement must be marked complete in the PR description or review record with a brief explanation of how the context materially improved the implementation.
