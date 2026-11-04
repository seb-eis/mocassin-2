# Step 23 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 23 — Avalonia Shell and Project Lifecycle |
| Request owner | Step agent |
| Audit status | Not requested |
| Audited commit | — |
| Step branch | — |
| Step tag | — |
| CI run | — |
| Audit date | — |
| Auditor | — |

## Step summary

| Field | Value |
| --- | --- |
| Phase | F. GUI |
| Depends on | 01 |
| Delivers | The Avalonia application shell, dependency injection, navigation, document tabs, project open/save/close, dirty tracking, undo/redo, settings, logging, and crash recovery. |

Step page: [`Step 23 — Avalonia Shell and Project Lifecycle`](../steps/step-23-avalonia-shell-and-project-lifecycle.md)

## Step-specific acceptance checklist

- [ ] The app starts on Windows, Linux, and macOS with a welcome tab.
- [ ] A project can be created, edited, saved, closed, and reopened without loss.
- [ ] Dirty state, autosave, and crash recovery work.
- [ ] Undo/redo works across editors.
- [ ] Recent files, settings, and locks behave correctly.
- [ ] Keyboard-only navigation can reach every primary action.
- [ ] No unhandled exception kills the process silently.
- [ ] Headless tests pass on all three OSes.
- [ ] Documentation written

## Standard audit checklist

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] CI is green at the audited commit.
- [ ] Format/version/fixture changes are documented and tested.
- [ ] Security, dependency, licence, and secret-scan gates pass.
- [ ] No unrelated changes or unapproved scope.
- [ ] Branch, tag, and status hygiene are correct.

## Evidence

Commands run:

```text

```

## Findings

| Severity | Finding | Evidence | Resolution |
| --- | --- | --- | --- |
| — | — | — | — |

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner:
Follow-up issue/step:
