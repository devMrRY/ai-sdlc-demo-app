# Pull Request Review Report

## 1. Review Information

| Field | Details |
|---|---|
| PR Title | |
| PR Number / URL | |
| Base Branch | |
| Head Branch | |
| User Stories Covered | |
| Review Date | |
| Lead Agent | review-pr |

## 2. Review Scope

### Documents Reviewed
- [ ] AGENTS.md
- [ ] Relevant specification (`spec.md`)
- [ ] Relevant implementation plan (`plan.md`)
- [ ] Relevant task breakdown (`tasks.md`)
- [ ] Complete PR diff
- [ ] Relevant source code
- [ ] Relevant tests

### Subagents Involved
| Agent | Responsibility | Review Completed |
|---|---|---|
| business-logic-reviewer | Business rules, domain behavior and requirement compliance | |
| test-quality-reviewer | Unit, integration and E2E test quality and coverage | |

## 3. Overall Review Summary

**Overall Verdict:** Ready to merge / Ready with conditions / Changes required

**Summary:**

[Briefly describe the overall quality of the implementation and the most important review observations.]

## 4. Review Findings

Record every finding identified by the lead agent or subagents. Assign unique finding IDs and remove duplicate reports of the same issue.

### Finding F-01

| Field | Details |
|---|---|
| Title | |
| Severity | Critical / High / Medium / Low |
| Reported By | review-pr / business-logic-reviewer / test-quality-reviewer |
| File and Line | |
| Category | Business Logic / Security / Architecture / Testing / Maintainability / Other |

**Description:**

[What is the issue?]

**Evidence:**

[Relevant code, behavior, test output, or missing coverage.]

**Impact:**

[Why does this matter? What could go wrong?]

**Recommended Action:**

[What should be changed or investigated?]

**Human Verdict:** Fixed / Dismissed / Deferred

**Human Reasoning:**

[Explain why you accepted or rejected the finding, or why it is postponed.]

**Verification:**

[If fixed, describe the correction and evidence that it works. If dismissed or deferred, provide supporting reasoning.]

---

[Repeat the Finding section for F-02, F-03, and every additional finding.]

## 5. Cross-Agent Observations

### Agreement
[Important findings independently identified by multiple agents.]

### Disagreements
[Findings where agents reached different conclusions, including the human reviewer's final judgment.]

## 6. Positive Observations

[Good implementation decisions, effective tests, appropriate architecture, or other strengths worth recording.]

## 7. Validation Results

| Validation | Result | Evidence |
|---|---|---|
| Unit tests | Passed / Failed / Not run | |
| Integration tests | Passed / Failed / Not run / Not applicable | |
| Playwright E2E tests | Passed / Failed / Not run / Not applicable | |
| Build | Passed / Failed / Not run | |
| Other relevant checks | | |

## 8. Final Human Review Decision

| Verdict | Count |
|---|---:|
| Fixed | |
| Dismissed | |
| Deferred | |

**Final Decision:** Approved / Changes requested / Review incomplete

**Human Reviewer's Assessment:**

[Explain whether you agree with the agent's overall verdict and why.]

**Outstanding Deferred Findings:**

[List deferred findings, their reasons, and the intended follow-up story or milestone.]

## 9. Review Completion Checklist

- [ ] Complete PR diff reviewed.
- [ ] Both subagent reviews completed.
- [ ] All findings consolidated and duplicates removed.
- [ ] Every finding has a human verdict.
- [ ] Fixed findings have been verified.
- [ ] Dismissed findings have evidence-based reasons.
- [ ] Deferred findings have reasons and follow-up scope.
- [ ] Required tests and validation checks reviewed.
- [ ] Final PR description updated with all findings and verdicts.
- [ ] Human reviewer has made the final merge recommendation.