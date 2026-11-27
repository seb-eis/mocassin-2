---
name: Prototype
description: Build throwaway code to answer one design question, then record the decision and discard or park the code. Use when a state model, interaction, or other design choice needs a runnable answer before the design is frozen.
---

# Prototype

Read [`docs/development/prototypes.md`](../../../docs/development/prototypes.md) and create the brief from
[`docs/planning/prototypes/_template.md`](../../../docs/planning/prototypes/_template.md).

Name the single question first. Choose the cheapest shape that answers it: a logic/state harness or a few
UI/interaction variants. Use the question tool if the question or shape is genuinely ambiguous; otherwise state the
assumption in the brief and proceed.

Keep it throwaway: no tests, abstractions, persistence, or polish unless persistence is the question. Surface the
state after every action. Put it under `.tmp/prototypes/`, `/home/node/tmp`, or a clearly named throwaway branch,
never `main`, a step branch, or a feature branch.

Record the question, answer, decision, and location in the brief or an ADR. Then delete the prototype or park the
branch. Rewrite the validated decision through the normal design, TDD, and review process. A prototype never
bypasses the design class.
