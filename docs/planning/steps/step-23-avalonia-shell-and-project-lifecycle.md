# Step 23 — Avalonia Shell and Project Lifecycle (superseded)

> **Superseded 2026-11-20.** The human owner split this step into two dependency-ordered steps because it was
> complexity class High with three independent acceptance stories and an audit surface too large for one
> sitting. This page is kept as a pointer so existing review and planning links resolve; it is **not** an
> implementation authorization.

| Step | Page | Outcome |
| --- | --- | --- |
| **23A** | [Workspace and Project Lifecycle](step-23a-workspace-and-project-lifecycle.md) | Shell host and boundaries, workspace/navigation, settings/theming/accessibility, project session lifecycle, and its own tests, documentation, and closeout |
| **23B** | [Editing, Diagnostics, and Recovery](step-23b-editing-diagnostics-and-recovery.md) | Edit pipeline and undo/redo, validation surface, dirty state, autosave, and crash recovery, and its own tests, documentation, and closeout |

- 23A depends on 01, 03–07, and 12; 23B depends on 23A. Step 24 depends on **23B**.
- The split decision, both complexity classes, and the evidence are recorded in
  [`04-roadmap.md`](../04-roadmap.md) §3.1 and on each step page.
- The design-review record for the unsplit design is [`reviews/step-23.md`](../reviews/step-23.md); it is closed
  as superseded and its rounds are unchanged. 23A has its own review record
  ([`reviews/step-23a.md`](../reviews/step-23a.md)); 23B has none until it is scheduled.
- The implementation status of the split steps is tracked on their own pages and in
  [`status.md`](../status.md) once the step agent updates the dashboard.

Do not implement from this page. Use the 23A or 23B page.
