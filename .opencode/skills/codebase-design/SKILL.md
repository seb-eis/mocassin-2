---
name: Codebase Design
description: Design deep modules with small interfaces at clean seams. Use when shaping a module interface, choosing where a seam or test boundary goes, or judging whether an abstraction is earning its place.
---

# Codebase Design

Read [`docs/development/codebase-design.md`](../../../docs/development/codebase-design.md).

Use the vocabulary exactly: module, interface, implementation, depth, seam, adapter, leverage, locality. Do not drift
into "component", "service", "API", or "boundary".

Aim for a small interface over substantial behaviour. Apply the deletion test: if deleting the module makes
complexity vanish, it was a pass-through; if complexity reappears at many callers, it was earning its place.

For a structural design, answer the design questions on the page. Sketch two or three alternative interfaces when
the shape matters, and use the question tool when the owner should choose. Keep tests at the module's interface; a
seam with only one possible adapter is usually speculative.

Follow `docs/development/code-structure.md` for folder ownership and limits, and
`docs/development/testing-and-parity.md` for the test strategy. Do not restate either here.
