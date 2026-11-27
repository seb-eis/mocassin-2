# Review axes: Standards and Spec

This page is the normative home for the review agent's two separate questions:

- **Standards:** does the change follow this repository's documented engineering standards?
- **Spec:** does the change implement what the frozen design, feature request, and acceptance criteria asked for?

Use it when reviewing a design, an audit, or a cross-cutting record, and when writing a finding in any of them.

The gate rules in [`AGENTS.md` §3.9/§3.10](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md) and the
review, audit, and cross-cutting READMEs decide *when* a review or audit runs. This page decides *what* the reviewer
looks at and how to report it.

The two axes are the default for a **step audit**. A **design review** reviews the Spec axis and the five design
questions in [`design-proportionality.md`](design-proportionality.md); code layout, file limits, prose, and other
mechanical standards stay at the pre-audit gate. A design review may use a Standards pass only for design-surface
standards such as contracts, interfaces, and ADR consistency. A **cross-cutting review** uses its own template and
may label findings by axis where useful, but the two-axis rule is not a substitute for its repository-wide
checklist.

A change can pass one axis and fail the other:

- it follows every standard but implements the wrong thing → **Standards pass, Spec fail**;
- it does exactly what the spec asked but breaks the project's conventions → **Spec pass, Standards fail**.

Report the axes separately. Do not merge or re-rank their findings.

## 1. Pin the fixed point

The reviewer records the base commit and the reviewed commit. Use a three-dot diff so the review is against the
merge base:

```sh
git diff <fixed-point>...<reviewed-commit>
```

Confirm the ref resolves and the diff is non-empty before reviewing. A bad ref or an empty diff is a failed
request, not a reason to guess. For a design review the fixed point is the frozen `main` or design branch base; for
an audit it is the step-branch base or the previous audited commit; the request records both SHAs.

## 2. Spec axis

Find the originating request in this order:

1. the step page and its design decisions;
2. the feature request for the current feature;
3. the design-review record;
4. the audit evidence and acceptance criteria;
5. the closeout evidence and feature log;
6. the commit messages and diffs for a small fix.

Then report:

- requirements that are missing or partial;
- behaviour in the diff that was not asked for (scope creep);
- requirements that look implemented but whose implementation is wrong;
- acceptance evidence that is claimed but not reproducible.

Quote the requested line or criterion for every finding. If there is no spec, report that as a finding and continue
the review. For a required gate, the missing spec is at least a Major because the gate cannot be judged; do not
invent one and do not use it to skip the Standards axis.

## 3. Standards axis

The Standards axis is the default for a step audit. A design review uses it only for design-surface standards;
mechanical layout and bookkeeping stay in the pre-audit gate.

Read the repository's documented standards first:

- [`AGENTS.md`](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md) and the relevant section of
  [`contributing.md`](contributing.md);
- [`code-structure.md`](code-structure.md);
- [`testing-and-parity.md`](testing-and-parity.md) and
  [`docs/planning/03-quality-strategy.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/03-quality-strategy.md);
- [`codebase-design.md`](codebase-design.md);
- [`design-decisions.md`](design-decisions.md);
- language and framework conventions the project has adopted.

Skip anything tooling already enforces: formatters, linters, the structure test, ApiCompat, the contract generator,
and the SPDX/secret scans. Report a documented-standard violation as a hard finding and cite the rule.

### Smell baseline

When the repository documents nothing specific, use this judgement baseline from Fowler's *Refactoring*. The
repository's own standards override it. A smell is always a labelled judgement call, never a mechanical violation.

| Smell | What it looks like | Usual response |
| --- | --- | --- |
| **Mysterious Name** | A name does not reveal what it does or holds. | Rename it; if no honest name exists, the design is murky. |
| **Duplicated Code** | The same logic shape appears in several hunks or files. | Extract one shared shape. |
| **Feature Envy** | A method reaches into another object's data more than its own. | Move the method to the data. |
| **Data Clumps** | The same few fields or parameters keep travelling together. | Introduce one type. |
| **Primitive Obsession** | A primitive or string stands in for a domain concept. | Give the concept a small typed value. |
| **Repeated Switches** | The same switch or `if` cascade recurs on the same type. | Share one map or use polymorphism. |
| **Shotgun Surgery** | One logical change forces scattered edits across many files. | Gather what changes together. |
| **Divergent Change** | One file changes for several unrelated reasons. | Split by reason to change. |
| **Speculative Generality** | Hooks or parameters exist for needs the spec does not have. | Delete them; inline until a real need appears. |
| **Message Chains** | Long `a.b().c().d()` navigation leaks structure. | Hide the walk behind one method. |
| **Middle Man** | A class or function mostly delegates onward. | Remove it and call the target directly. |
| **Refused Bequest** | A subclass ignores or overrides most of what it inherits. | Prefer composition. |

## 4. Review shape

A design review does one pass: the Spec axis plus the five design questions. It does not spend a round on code
layout, prose, or bookkeeping.

A step audit uses one review agent and two passes:

1. read the spec and review the **Spec** axis;
2. read the standards and review the **Standards** axis;
3. write the findings in the assigned record, labelling each finding with its axis.

Do not spawn parallel review sub-agents by default. Do not create one record per axis. The audit verdict reflects
the worst outcome across both axes; it does not average or re-rank them.

## 5. Reporting

Use the single findings table in the design-review, audit, or cross-cutting record and label every finding with its
axis, `Standards` or `Spec`. The templates carry an `Axis` column:

| Record | Findings columns |
| --- | --- |
| Design review | `ID` \| `Axis` \| `Severity` \| `Origin` \| `Finding` \| `Evidence` \| `Recommendation` |
| Step audit | `ID` \| `Axis` \| `Severity` \| `Origin` \| `Finding` \| `Evidence` \| `Recommendation` |
| Cross-cutting | `ID` \| `Axis` \| `Severity` \| `Area` \| `Finding` \| `Evidence` \| `Recommendation` |

The three record families stay aligned on `ID`, `Axis`, and `Severity`; `Origin` (design review and audit) and
`Area` (cross-cutting) are family-specific and are not merged.

Under each finding:

- give the severity and evidence;
- quote the standard or the requested line;
- distinguish a hard violation from a judgement call;
- end with the worst issue in that axis, or state that none was found.

Keep the axes separate for findings, but still write one gate verdict. The verdict reflects the worst outcome
across both axes; it does not average or re-rank them. Do not pick one overall winner across axes. The separation is
what prevents a standards pass from hiding a spec failure, or the reverse.

## 6. Anti-patterns

- merging both axes into one ranked list;
- treating a code smell as a hard violation when the repository permits it;
- flagging something the linter, formatter, type checker, structure test, or contract generator already enforces;
- reviewing against an imagined spec instead of the frozen record;
- using a missing spec as a reason to skip the Standards axis or to approve the gate;
- spending a design-review round on code layout, prose, or status bookkeeping;
- asking for a preferred verdict or re-running until the axes agree;
- creating one record per axis or a separate axis verdict.
