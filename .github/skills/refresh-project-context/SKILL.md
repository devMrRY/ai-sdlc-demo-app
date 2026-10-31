---
name: refresh-project-context
description: Refresh PROJECT_CONTEXT.md and HANDOFF.md using verified repository state after implementation changes, testing, reviews, or when preparing to hand over to another AI agent.
---

# Refresh Project Context

## Purpose

Keep `PROJECT_CONTEXT.md` accurate as the repository evolves. Keep `HANDOFF.md` synchronized with current progress so another AI agent can resume work without relying on previous chat history.

Update context from verified repository evidence, not assumptions or claims about work that has not been checked.

## Manual invocation requirement

This skill must not run automatically. It is for a user-initiated checkpoint refresh only.

Before running this skill, confirm the user explicitly asked for a context refresh or explicitly invoked this skill. If the user’s intent is unclear, stop and ask for confirmation instead of refreshing `PROJECT_CONTEXT.md` or `HANDOFF.md`.

Do not refresh these files automatically during routine work, after incidental edits, or without clear user approval. When the user requests a refresh, treat it as a deliberate checkpoint update and verify the current repository state before editing either file.

## When to use this skill

Use this skill only after the user has explicitly approved a refresh or explicitly invoked it:

- After completing a meaningful implementation milestone.
- After changing business rules, architecture, APIs, tests, or configuration.
- After running tests or mutation testing.
- After completing a code review or evaluating review findings.
- Before ending a work session or handing work to another agent.
- When explicitly asked to refresh, update, or verify project context.

Do not refresh the files after every trivial edit. Batch related changes into a meaningful checkpoint.

## Step 1: Inspect repository state

Inspect:

- `git status --short --branch`
- `git rev-parse HEAD`
- Relevant recent commits
- The current diff, including staged and unstaged changes
- `AGENTS.md`
- `ASSIGNMENT.md`
- Existing `PROJECT_CONTEXT.md` and `HANDOFF.md`
- Relevant source code, configuration, and test files

Inspect only files relevant to the changes when possible. Do not overwrite existing context before understanding it.

## Step 2: Verify implementation facts

For the invoice approval assignment, verify the current state of:

- Domain models and `ApprovalService`
- Invoice lifecycle and state transitions
- Manager and Director approval limits
- The two-distinct-approver requirement for amounts above 500,000
- The requirement that the second approver be a Director
- Rejection behavior and state integrity
- API validation and error responses
- Unit, integration, and end-to-end tests
- AI interaction logging and skills/hooks
- Review findings and assignment deliverables

Distinguish implemented behavior from planned behavior. Do not claim a requirement is complete merely because documentation says it should be implemented.

## Step 3: Verify test and review evidence

Record test results only when supported by actual command output.

For each relevant test suite, record:

- Command executed
- Pass, fail, and skipped counts, when available
- Important failures or limitations
- Whether the test run occurred before or after the latest code changes

For mutation testing, record the actual mutation score and relevant surviving mutants.

For reviews, record findings and whether they were accepted, rejected, or left unresolved, including the rationale.

Never invent results or reuse an old passing result as proof that newer changes pass.

## Step 4: Update PROJECT_CONTEXT.md

Treat this file as the current, verified repository snapshot.

Keep it focused on:

1. Repository branch, commit, and working-tree status.
2. Application architecture and important file locations.
3. Current domain behavior and invoice lifecycle.
4. Verified implementation status of assignment requirements.
5. Commands for running the application and tests.
6. Latest verified test and mutation-testing results.
7. Current logging, hooks, and skill configuration.
8. Known limitations and unverified items.

Update only the sections affected by new evidence, plus any repository metadata that has changed.

Preserve useful existing facts. Remove or correct obsolete facts when evidence shows they are no longer accurate.

Do not copy the entire handoff history into this file. It describes the current state, not every step taken to reach it.

## Step 5: Update HANDOFF.md

Treat this file as the current work-session checkpoint.

Include:

- Date and checkpoint summary
- Current branch and commit
- Relevant uncommitted changes
- Work completed and evidence
- Tests and validation performed
- Important decisions and review findings
- Incomplete requirements and blockers
- The next recommended action

Give the next agent a concrete starting point. For example:

"Inspect the current ApprovalService implementation, add boundary tests for the 50,000 and 500,000 limits, then run the backend test suite."

Do not mark unverified tasks complete.

## Step 6: Preserve file responsibilities

- `AGENTS.md`: durable instructions for all coding agents.
- `ASSIGNMENT.md`: assignment requirements and deliverables.
- `PROJECT_CONTEXT.md`: verified current repository state.
- `HANDOFF.md`: progress, unresolved work, and next action.
- `AI_INTERACTION_LOG.md`: human-readable AI usage and review evidence.
- `.github/logs/ai-interactions.jsonl`: machine-readable hook records.

Do not change assignment requirements simply to match the implementation.

Do not replace the existing logging schema or hook configuration as part of a context refresh unless explicitly requested.

## Step 7: Final verification

After editing:

1. Review the diff for `PROJECT_CONTEXT.md` and `HANDOFF.md`.
2. Check that statements match inspected files and actual command output.
3. Preserve explicit distinctions between verified, pending, and unverified work.
4. Do not change application code just to refresh documentation.
5. Report which files were updated and what remains uncertain.

If repository evidence is unavailable, retain the relevant item as unverified instead of guessing.
