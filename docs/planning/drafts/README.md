# Planning Drafts

This directory holds **non-normative design scratches**: exploratory notes, future-work ideas, option
comparisons, and questions that are not yet part of the reviewed plan.

Drafts are useful because they let us capture thinking without pretending it is a decision. They are not
design reviews, audits, ADRs, or step pages, and nothing in this directory authorizes implementation.

## Rules

- A draft is **not a decision**. Do not implement from a draft without promoting it.
- A draft may be incomplete, speculative, or contain mutually exclusive options.
- A draft that changes a step, contract, architecture, or public API must be promoted through the normal
  process:
  - an ADR for an expensive-to-reverse decision;
  - a step-page update for a step-local design;
  - an amendment for a change after a step is frozen;
  - a design review and audit where the normal gates require them.
- Do not put secrets, credentials, personal data, or copied licensed content in a draft.
- `docs/planning/` is excluded from the published documentation site; drafts are internal.
- A promoted draft keeps its history and is marked `Promoted` with links to the ADR, step page, or commit.
  A rejected or parked draft stays in the record rather than being silently deleted.

## Status values

| Status | Meaning |
| --- | --- |
| **Scratch** | Early notes; not yet coherent enough for discussion. |
| **Under discussion** | A concrete proposal or option set is ready for feedback. |
| **Parked** | Intentionally deferred; no action now. |
| **Promoted** | The decision moved into an ADR, step page, amendment, or reviewed design. |
| **Rejected** | Considered and not adopted; keep the rationale. |

## Drafts

| Draft | Status | Target | Related |
| --- | --- | --- | --- |
| [Packaged documentation and in-app help](packaged-documentation.md) | Under discussion | Steps 23, 30, 31 | ADR 0008, Step 30, Step 31 |
| [LLM assistant and OpenCode integration](llm-assistant.md) | Under discussion | Steps 23, 28, 30 | Avalonia WebView, security model, Step 30/31 |

Use [`draft-template.md`](draft-template.md) when starting a new draft.
