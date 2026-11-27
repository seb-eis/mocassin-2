# Codebase design

This page is the task discipline for designing modules. It complements
[`code-structure.md`](code-structure.md), which decides where files and folders live, and
[`testing-and-parity.md`](testing-and-parity.md), which decides how behaviour is tested. The design-class rules are
in [`design-proportionality.md`](design-proportionality.md).

Use it when shaping a module interface, choosing where a seam or test boundary goes, or judging whether an
abstraction is earning its place.

The goal is **deep modules**: a lot of behaviour behind a small interface at a clean seam, testable through that
interface.

## 1. Vocabulary

Use these terms consistently. Do not substitute "component", "service", "API", or "boundary" for them.

| Term | Meaning |
| --- | --- |
| **Module** | Anything with an interface and an implementation: a function, class, project, or slice. |
| **Interface** | Everything a caller must know to use the module correctly: signature, invariants, ordering, error modes, configuration, and performance characteristics. |
| **Implementation** | The code inside the module. |
| **Depth** | Leverage at the interface: how much behaviour a caller or test can exercise per unit of interface learned. |
| **Seam** | The location where behaviour can be changed without editing in place; where the interface lives. |
| **Adapter** | A concrete implementation that satisfies an interface at a seam. |
| **Leverage** | What callers gain from depth: more capability per unit of interface. |
| **Locality** | What maintainers gain: change, bugs, and verification concentrate in one place. |

A module is **deep** when a small interface hides substantial behaviour. It is **shallow** when the interface is
nearly as complex as the implementation.

## 2. Principles

1. **Depth is a property of the interface, not the implementation.** A deep module may use small internal pieces;
   they are not part of its interface.
2. **Apply the deletion test.** If deleting the module makes complexity vanish, it was a pass-through. If complexity
   reappears at many callers, it was earning its place.
3. **The interface is the test surface.** Callers and tests cross the same seam; a test that reaches past it usually
   means the module is the wrong shape.
4. **One adapter is a hypothetical seam; two adapters make it real.** Do not add a port unless production and a
   test, or two real implementations, actually need it.
5. **Accept dependencies, do not create them.** Inject a boundary dependency instead of constructing it inside.
6. **Return results, do not mutate hidden state.** Prefer a result the caller can observe and test.
7. **Keep the surface small.** Fewer methods and simpler parameters mean less to learn and fewer tests.

## 3. Design questions

Before freezing a `Full` design or a structural refactor, answer:

- What is the module, and what sits behind its interface?
- Which behaviour should callers not need to know?
- Where is the seam, and is it at the highest useful point?
- Which dependency category is this: in-process, locally substitutable, remote but owned, or true external?
- What test crosses the interface and proves the behaviour?
- Would deleting the module concentrate complexity or merely move it?
- Is this a real seam, or a speculative abstraction?

For a `Light` change, use the first four questions only. Record the answers in the step page's design section, not
in chat.

## 4. Interface alternatives

When the interface shape matters, sketch **two or three radically different interfaces** before choosing:

- one minimising the interface;
- one maximising flexibility;
- one optimising for the most common caller.

Compare them by depth, locality, and seam placement. In this harness, present the alternatives with the question
tool when it is available; otherwise ask the lead or owner in numbered text. Recommending an option is not deciding;
the design agent or owner records the decision.

## 5. Relationship to tests

Test at the seam, not against internal helpers. If a test needs to know implementation details, either the test is
too deep or the module is too shallow. See [`tdd.md`](tdd.md) for the loop and test-quality rules, and
[`testing-and-parity.md`](testing-and-parity.md) for the test layers, doubles policy, and fixtures.

## 6. Durable output

- module shape and seam decisions: the step page's design section or the design-review record;
- an irreversible architecture decision: an ADR under
  [`docs/planning/adr/`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/adr/README.md);
- an open question: [`docs/planning/05-open-decisions.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/05-open-decisions.md).

## 7. Anti-patterns

- a large interface over a thin pass-through;
- extracting a seam with only one possible adapter;
- exposing internal seams through the public interface so tests can reach them;
- a "utility" module that hides several unrelated concepts;
- testing private helpers instead of the module's public behaviour;
- deepening a module that the step is about to delete.
