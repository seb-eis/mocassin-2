---
name: TDD
description: Build features and fix bugs in a red-green loop at pre-agreed seams, with tests that describe observable behaviour. Use when implementing approved feature work, adding a regression test, or asked for test-first development.
---

# TDD

Read [`docs/development/tdd.md`](../../../docs/development/tdd.md), then the test layers, doubles policy, and
fixtures in [`docs/development/testing-and-parity.md`](../../../docs/development/testing-and-parity.md) and
`docs/planning/03-quality-strategy.md`.

Agree the seams before writing tests. Prefer an existing high seam. Use the question tool to put the proposed seams
to the owner when the design record does not already settle them. For each vertical slice: write one failing test,
see it fail for the right reason, write only enough code to pass, then repeat. Refactor after the behaviour is
covered.

Test observable behaviour through the public interface. Do not mock internal collaborators, assert private calls, or
recompute the expected value the same way as the implementation. For a bug, use the `diagnosing-bugs` skill to build
the red reproduction first, and keep the minimised repro as the regression test.
