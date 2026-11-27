---
name: Diagnosing Bugs
description: Diagnose a hard bug or performance regression by building a tight red-capable feedback loop before forming a theory, then minimising, ranking falsifiable hypotheses, instrumenting one variable, and adding a regression test. Use when something is broken, failing, throwing, or slow.
---

# Diagnosing Bugs

Read [`docs/development/diagnosing-bugs.md`](../../../docs/development/diagnosing-bugs.md).

Do not start by reading code to form a theory. Build one command that exercises the user's exact symptom and can go
red on this bug. Tighten it until it is fast, deterministic, and agent-runnable.

Then reproduce, minimise to the load-bearing elements, and write three to five falsifiable hypotheses before testing
any. Use the question tool to show the ranked hypotheses when the owner is available; do not block if they are away.
Instrument one variable at a time, tagging temporary debug output with a unique prefix.

Fix with a regression test at a correct seam. If no correct seam exists, record that as a finding. Remove all
instrumentation and re-run the original reproduction. Redact every secret in commands, logs, traces, and artifacts.
Record the root cause and regression test in the step page or the owning record.
