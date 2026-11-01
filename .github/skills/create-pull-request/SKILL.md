---
name: create-pull-request
description: Create a review-ready GitHub pull request from the current feature branch. Inspect changes, determine the correct base branch, prepare a concise description using the mandatory four-section PR format, push the branch, and create the PR after explicit user authorization.
---

# Create Pull Request Skill

## Purpose

Create a complete, review-ready GitHub pull request for the current feature branch.

The skill must:
- Inspect the current branch and repository state.
- Identify the correct base branch.
- Review committed and uncommitted changes.
- Generate a PR title and description based on actual implementation.
- Include relevant business rules, test coverage, validation results, and limitations.
- Push the feature branch.
- Create an open GitHub pull request.
- Report the PR URL and creation details.

Never merge a pull request automatically.

## 1. Safety and authorization

1. Never commit automatically.
2. Never push without explicit user authorization.
3. Never create a PR without explicit user authorization.
4. Never merge a PR.
5. Never force-push unless explicitly requested and reviewed.
6. Never discard or overwrite user changes.
7. Never stage all files using `git add .` or `git add -A`.
8. Never include unrelated files, credentials, secrets, or logs unless explicitly requested.
9. Never claim tests passed without actual validation evidence.
10. Never invent requirements, test results, issue numbers, or links.
11. If the base branch is ambiguous, ask the user.
12. If unexpected changes or branch divergence are found, stop and report them.

## 2. Repository inspection

Inspect:

```bash
git status --short
git branch --show-current
git log -5 --oneline
git remote -v
git diff
git diff --cached
```

Compare against the intended base branch:

```bash
git log <base-branch>..HEAD --oneline
git diff <base-branch>...HEAD
```

Inspect untracked files separately.

Distinguish:
- Committed changes.
- Uncommitted tracked changes.
- Untracked files.
- Unrelated worktree changes.

Do not assume that the current branch or `main` is the correct base.

## 3. Base branch selection

For the invoice approval assignment, use these defaults:

| User story | Expected base |
|---|---|
| US1 | feature/invoice-approval-assignment |
| US2 | feature/us1-single-approval-limits |
| US3 | feature/us2-multi-approval-flow |
| Final integration | main |

Verify that the intended base exists and matches the actual branch history.

Do not silently substitute another base branch.

## 4. PR title

Use a concise title identifying the user story and functionality.

Examples:

- `US1: Implement single-approval invoice limits`
- `US2: Implement multi-approval flow for high-value invoices`
- `US3: Implement invoice rejection workflow`

## 5. Mandatory PR description format

**Every PR description MUST use exactly these four top-level headings, in this order:**

```markdown
## Summary

## Business rules and coverage

## Validation

## Limitations
```

Do not add other top-level sections such as Overview, Implementation Details, Review Summary, or Files Changed.

### 5.1 Summary

Use concise bullet points to explain:
- What functionality was implemented.
- Important API, domain, authorization, or UI changes.
- Relevant tests and documentation added.
- Important supporting artifacts included, when relevant.

Distinguish new functionality from preserved existing behavior.

Mention AI interaction logs, session files, agent definitions, review templates, specifications, or implementation documents only when they are actually included in the PR.

Do not claim files are included merely because they exist in the worktree.

### 5.2 Business rules and coverage

Describe:
- The actual business rules implemented.
- Important positive test scenarios.
- Important negative test scenarios.
- Boundary conditions.
- State preservation after failed operations.
- Authorization behavior.
- API and UI coverage.

Use concrete values and role names when relevant.

For invoice approval work, examples include:
- Manager single-approval limit: 50,000.
- Director single-approval limit: 500,000.
- High-value provisional first approval.
- Distinct Director second approval.
- Invalid approval attempts preserving invoice state.
- Explicit rejection as a separate operation.

Include only behavior relevant to the current PR and supported by the specification and implementation.

### 5.3 Validation

Report actual validation evidence.

Use concise bullets with:
- Exact commands executed.
- Passed and failed test counts.
- Test configuration (Debug or Release).
- Backend unit/integration test results.
- Playwright E2E results.
- Build results.
- `git diff --check` result.
- Mutation testing results, if run.

Example:

```markdown
## Validation
- `dotnet test backend/InvoiceApp.sln --no-restore`: 15 passed, 0 failed.
- `npm test -- --reporter=list tests/manager-over-limit-approval.spec.ts` from `e2e`: 1 passed.
- `git diff --check`: passed.
```

If a command was not run, state that it was not run.

If a command failed because it was executed from the wrong directory, explain that accurately and report the successful rerun from the correct directory.

Do not combine results from different runs in a misleading way.

### 5.4 Limitations

Clearly state:
- Which user story this PR covers.
- Which future user stories are not implemented.
- Tests or validation that were not run.
- Deferred findings.
- Known environmental limitations.
- Any remaining scope restrictions.

Example:

```markdown
## Limitations
This PR covers US1 only. The >500,000 two-approver flow is not implemented here; the full E2E suite and mutation tests were not run.
```

Do not report optional improvements as blocking defects.

If there are no known limitations, say so explicitly.

## 6. Description quality rules

- Keep the exact four headings and their order.
- Use Markdown bullets under each section.
- Prefer concrete behavior over vague statements.
- Avoid repeating the same information across sections.
- Do not paste the entire specification.
- Do not include internal AI reasoning or confidential conversation content.
- Do not invent test counts or implementation details.
- Keep the description understandable to a reviewer unfamiliar with the codebase.
- Base all claims on the actual diff, specifications, and validation evidence.

## 7. File inclusion and exclusion

Review every changed file before publishing.

Include only files belonging to the intended user story.

Exclude by default:
- `.github/logs/ai-interactions.jsonl`
- `.github/logs/sessions/**`
- Temporary files.
- IDE-generated files.
- Local environment files.
- Credentials and secrets.
- Unrelated user story changes.
- Review reports unless explicitly intended to be part of the PR.

If assignment requirements explicitly require AI logs or agent artifacts to be included, verify their intended scope with the user rather than excluding them automatically.

Never stage or discard files without reviewing them.

## 8. Pre-push checks

Before publishing:

1. Confirm current branch.
2. Confirm base branch.
3. Inspect commit history.
4. Inspect committed and uncommitted diffs.
5. Confirm all intended changes have been committed.
6. Confirm no unrelated changes will be published.
7. Confirm the remote branch is not unexpectedly divergent.
8. Generate the final PR description using the mandatory format.
9. Obtain explicit user authorization to push and create the PR.

If the branch contains uncommitted implementation changes, stop and ask the user to review and commit them separately.

## 9. Push branch

After authorization:

```bash
git push -u origin <feature-branch>
```

Do not force-push.

Verify the push succeeded before proceeding.

## 10. Create GitHub pull request

Before creating:
- Check whether a PR already exists for the same source and base branches.
- Avoid duplicate PRs.
- Verify source and target branches.
- Use the generated title.
- Use the complete four-section description.

Example:

```bash
gh pr create \
  --base <base-branch> \
  --head <feature-branch> \
  --title "<PR title>" \
  --body-file <description-file>
```

Use the available GitHub integration if GitHub CLI is unavailable.

Default PR state: Open.

Do not merge, approve, or assign reviewers automatically.

## 11. Post-creation verification

Verify:
- PR number.
- PR URL.
- Title.
- Source branch.
- Base branch.
- PR state.
- Description.
- Included commits.

Report any failed verification accurately.

## 12. Final response

Use a concise completion report:

### Pull Request
- Number:
- URL:
- Title:
- Source:
- Base:
- Status:

### Summary
Brief description of the PR.

### Validation
Actual test results.

### Git status
- Latest commit.
- Push result.
- Remaining local changes.

Confirm explicitly that the PR is open and has not been merged.

## 13. Stop conditions

Stop and ask the user when:
- Base branch is unclear.
- Uncommitted implementation changes remain.
- Unexpected files are included.
- Branch divergence exists.
- A duplicate PR is detected.
- GitHub permissions are insufficient.
- A force-push appears necessary.
- Publishing has not been explicitly authorized.

Never report success until the push and PR creation have been confirmed.