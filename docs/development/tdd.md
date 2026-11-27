# Test-driven development

This page is the task discipline for the red-green loop. It adds the loop and test-quality rules that
[`testing-and-parity.md`](testing-and-parity.md) and
[`docs/planning/03-quality-strategy.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/03-quality-strategy.md)
do not cover; the test layers, doubles policy, fixtures, tiers, and definition of done remain there.

TDD is a default for feature work and bug fixes, not a ritual for generated files, documentation, or mechanical
renames.

## 1. Agree the seams first

A **seam** is the public boundary where you observe behaviour without reaching inside. Tests live at seams.

Before writing tests, write down:

- the public interface under test;
- which seams will be tested;
- what each seam catches and what it misses.

Confirm the seams with the design record or the owner. Use the question tool when it is available and the seams are
not already settled. Do not write a test at an unconfirmed seam. Prefer an existing high seam over a new one; one
good seam is usually better than five shallow ones.

When the interface shape is unclear, use [`codebase-design.md`](codebase-design.md) before choosing a seam.

## 2. The loop

1. **Red.** Write one failing test for one behaviour. Run it and see it fail for the expected reason.
2. **Green.** Write only enough code to pass that test.
3. **Repeat.** Take the next vertical slice: one test, one implementation.
4. **Refactor after the loop.** Improve names and structure once the behaviour is covered. Refactoring is not a
   reason to skip the failing test.

Do not write all the tests first and then all the implementation. That is horizontal slicing: it tests imagined
behaviour, locks in a structure before you understand it, and produces tests that are insensitive to real change.

## 3. What makes a good test

- It describes behaviour a caller or user cares about.
- It uses the public interface, not private methods or internal collaborators.
- It survives a refactor that does not change behaviour.
- Its expected value comes from an independent source: a known-good literal, a worked example, a reference
  observable, or the spec.
- It is deterministic, fast, and readable.

MOCASSIN's stronger expectations still apply: real objects, real files, and real writers where practical; test
doubles only at true process/OS/UI/network/time boundaries or for failure injection; statistical or parity evidence
for scientific behaviour. See [`testing-and-parity.md`](testing-and-parity.md).

## 4. Test tiers

Declare the tier for every test as `PR-fast`, `PR-medium`, `nightly`, or `release`. PR-fast tests must not depend on
nightly assets. A test that is slow for no reason is a maintenance cost; keep the fast loop fast.

## 5. Bug fixes

For a bug, first build a red-capable reproduction using [`diagnosing-bugs.md`](diagnosing-bugs.md). If a correct
seam exists, turn the minimised reproduction into a failing regression test before the fix. If no correct seam
exists, record that as a finding and explain what was tested instead.

Every bug fix gets a regression test. A scientific change gets a physics or parity test. There is no exception for
"small" fixes.

## 6. Budget

- Test the critical paths and the complex logic, not every getter.
- One strong acceptance test is worth more than many private-helper tests.
- If the loop feels heavy for a one-line mechanical change, record why the change is exempt; do not silently skip
  the regression test for a real bug.

## 7. Anti-patterns

- **Implementation-coupled:** mocks internal collaborators, asserts private calls, or verifies through a side
  channel instead of the interface. The test breaks on a refactor that preserves behaviour.
- **Tautological:** the expected value is recomputed the same way as the implementation, so the test passes by
  construction. Use an independent literal or oracle.
- **Horizontal slicing:** all tests first, then all code.
- **Coverage theatre:** adding call-count or trivial tests to raise a percentage.
- **Unconfirmed seams:** testing a boundary nobody agreed on.
- **No regression test:** fixing a bug without a test that fails before the fix, unless the absence of a correct
  seam is recorded as a finding.
