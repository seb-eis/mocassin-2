# Prototypes

This page is the task discipline for throwaway code that answers a design question. A prototype is not a feature,
not a spike that quietly becomes production, and not a substitute for the design class.

Use a prototype when a design decision needs a **runnable answer**: does this state model feel right, what should
this interaction look like, does this parser shape handle the awkward cases?

## 1. Rules

1. **One question.** Name the question before writing code. If there is no single answerable question, the work is
   exploration, not a prototype.
2. **Throwaway from day one.** Mark it clearly as a prototype. It has no production quality bar.
3. **Easy to run.** One command or one file. No setup ceremony.
4. **No persistence by default.** Keep state in memory unless persistence is the question. Use a scratch database
   or an obviously disposable file.
5. **Skip polish.** No production error handling, abstractions, or tests beyond what keeps it runnable.
6. **Surface the state.** Show the relevant state after every action so the owner can judge it.
7. **Never land it on `main`.** Put it under the gitignored `.tmp/prototypes/` or `/home/node/tmp`, or use a
   clearly named throwaway branch. It is never a step or feature branch.
8. **Capture the answer.** Record the question, the verdict, and the decision under
   [`docs/planning/prototypes/`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/prototypes/README.md)
   or in an ADR. Then delete the prototype, or park the branch and link it from the record.
9. **Never bypass the design class.** If the prototype's answer changes a contract, format, public API, or
   architecture boundary, the step is still `Full` and still needs its design review. A prototype is evidence for
   the design, not a substitute for it.

The design agent or dev may build a prototype after the lead authorizes it. The owner decides when the answer is
good enough.

## 2. Two useful shapes

- **Logic/state model:** a small harness that drives the state through the awkward cases and prints the full state
  after each action.
- **UI/interaction:** a few radically different variants on one route, switchable without a build or a new top
  level in the project structure.

Both are examples, not a required stack. Choose the cheapest shape that answers the question. Use the question tool
only when the question or shape is genuinely ambiguous; otherwise state the assumption in the brief and proceed.

## 3. What to record

Create the record from
[`docs/planning/prototypes/_template.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/prototypes/_template.md)
and record:

- the single question;
- the cheapest shape chosen and why;
- the prototype location or branch;
- the answer and the evidence the owner saw;
- the decision taken and where it is recorded;
- what was discarded.

Do not copy prototype code into production. Rewrite the validated decision under the normal design, TDD, and review
process.

## 4. Durability and security

- Keep prototype state under `/home/node/tmp` or the repository's gitignored `.tmp`; `/tmp` does not survive a
  container restart.
- Never put a real credential, token, private path, or personal data in a prototype or its record. Redact it, per
  [`agent-security.md`](agent-security.md).
- Do not commit generated output, databases, or screenshots that contain sensitive data.

## 5. Anti-patterns

- a prototype that grows tests, abstractions, and a deployment path;
- committing prototype code to `main` or a step branch;
- using a prototype to bypass the design review or the design class;
- keeping a prototype alive after its question is answered;
- recording only the code and not the decision;
- treating a good-looking UI variant as approval of the design.
