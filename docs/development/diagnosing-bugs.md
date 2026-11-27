# Diagnosing bugs

This page is the task discipline for hard bugs and performance regressions. The goal is a tight, red-capable
feedback loop before a theory, then a minimal fix with a regression test.

Use it when something is broken, failing, throwing, or slow. Read
[`GLOSSARY.md`](https://github.com/seb-eis/mocassin-2/blob/main/GLOSSARY.md) and the relevant ADRs first so names
and constraints are clear. Redact secrets from every command, log, trace, and artifact before showing it; the
security rules are in [`agent-security.md`](agent-security.md).

## 1. Build a red-capable loop

The first job is a signal that goes **red on this bug** and green when it is fixed. Without it, reading code and
guessing is the failure mode this page prevents.

Ways to build one, in rough order:

1. a failing test at a seam that reaches the bug;
2. a CLI invocation against a sample project or fixture;
3. replaying a captured package, state file, payload, trace, or event log;
4. a throwaway harness that exercises only the bug path;
5. a property or fuzz loop for intermittent wrong output;
6. a bisection harness over commits or data versions;
7. a differential loop comparing the original reference and the reimplementation;
8. a headless or screenshot check for a UI bug;
9. a human-in-the-loop script as the last resort; keep it structured and feed the captured output back to the
   agent.

### Tighten the loop

- Make it faster: cache setup, narrow the scope.
- Make the signal sharper: assert the user's exact symptom, not "did not crash".
- Make it deterministic: pin time and seeds, isolate the filesystem and network. MOCASSIN runs must use the injected
  RNG and a fixed seed, never `thread_rng` or a time-based seed.

For an intermittent bug, raise the reproduction rate until it is debuggable rather than demanding a perfect repro.

### If no loop can be built

Stop and say so. List what you tried and ask the lead for access to the reproducing environment, a redacted
artifact, or permission for temporary instrumentation. Do not proceed to hypotheses without a loop.

## 2. Reproduce and minimise

Run the loop and confirm it produces the symptom the user described, not a nearby failure. Capture the exact error,
wrong output, or timing.

Then shrink the scenario one element at a time, re-running the loop after every cut. Keep only what is load-bearing
for the failure. The minimal repro narrows the hypothesis space and becomes the regression test.

## 3. Rank hypotheses

Write **three to five falsifiable hypotheses** before testing any of them. For each, state:

> If <cause> is true, then <change> will make the bug disappear or get worse.

A hypothesis without a prediction is a vibe; discard or sharpen it. When the owner is available, present the
ranked list with the question tool (or numbered text when it is not) and ask for domain knowledge; do not block if
the owner is away.

## 4. Instrument one variable at a time

Each probe must map to a prediction. Prefer a debugger or REPL over logs. Use targeted logs at the boundaries that
distinguish hypotheses, not "log everything and grep".

Tag temporary debug output with a unique prefix such as `[DEBUG-a4f2]` so cleanup is one search. For a performance
regression, establish a baseline measurement first, then bisect or profile; do not start with logs.

## 5. Fix and lock it down

- If a correct seam exists, turn the minimal repro into a failing regression test before the fix.
- Watch it fail, apply the fix, and watch it pass.
- Re-run the original, unminimised loop.
- If no correct seam exists, record that as a finding: the architecture is preventing the bug from being locked
  down.

Every bug fix needs a regression test. The test policy, mock/fake boundary, and fixture rules are in
[`testing-and-parity.md`](testing-and-parity.md) and
[`docs/planning/03-quality-strategy.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/03-quality-strategy.md).
A scientific defect also needs a physics or parity test.

## 6. Clean up and record

Before declaring done:

- [ ] The original reproduction no longer fails.
- [ ] The regression test passes, or the missing seam is recorded.
- [ ] All temporary instrumentation is removed.
- [ ] Throwaway probes are deleted or clearly marked outside `main`.
- [ ] The correct hypothesis is stated in the step page's feature log or the commit message.

Record the symptom, the root cause, the regression test, and the fix in the step page or the record that owns the
work. If the bug reached `main` or a release, follow
[`incident-and-rollback.md`](incident-and-rollback.md).

## 7. Stop-and-ask

Stop and report to the lead when:

- the root cause is a public contract, format, schema, or scientific convention;
- the fix would change accepted behaviour or a historical record;
- the reproduction needs a real credential, production data, or an environment you do not have;
- the same file has been edited three times without a green gate.

See [`agent-workflow.md`](agent-workflow.md) §8 for the full trigger list.

## 8. Anti-patterns

- reading code to form a theory before a red-capable loop exists;
- "I think I know" without a prediction or a probe;
- changing several variables at once;
- leaving tagged debug logs in the code;
- fixing the nearby failure instead of the user's symptom;
- closing a bug without a regression test or a recorded reason it cannot have one;
- pasting a credential, token, or private path into a command, log, or record.
