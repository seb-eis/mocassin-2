# Step 02 Amendment 2 — Documentation Math Rendering with KaTeX

## Request

| Field | Value |
| --- | --- |
| Amendment ID | `step-02-amend-2` |
| Parent step | 02 — Symmetry and Geometry |
| Status | Design review |
| Branch | `feature/step-02-symmetry-and-geometry/amend-2-docs-math` |
| Design review | `docs/planning/reviews/step-02-amend-2.md` |
| Parent audit | `docs/planning/audits/step-02.md` |
| Integration commit | — |
| Combined audit commit | — |
| Tag | `step-02` |
| Date | 2026-11-07 |
| Owner | Planner |

Parent step page: [`Step 02 — Symmetry and Geometry`](../steps/step-02-symmetry-and-geometry.md)

## Rationale

The Step 02 documentation currently uses fenced `text` blocks for formulas. They are readable as source but
render as monospace boxes on the published site, which looks poor and loses mathematical structure.

Zensical already enables `pymdownx.arithmatex.generic`. The missing pieces are:

- KaTeX CSS/JS assets;
- a small initializer that renders after Zensical's instant navigation;
- converting the Step 02 formulas from fenced `text` blocks to `$$...$$` / `$...$`;
- a documentation convention so future docs do not regress.

This is a documentation and site-configuration change. It does not change code, public APIs, formats, or
scientific behavior.

## Scope

- Add `docs/javascripts/katex.js`.
- Add pinned KaTeX assets to `zensical.toml` via `extra_javascript` and `extra_css`.
- Convert the published Step 02 math in `docs/physics/symmetry.md` to KaTeX-compatible markup.
- Add the math authoring convention to `AGENTS.md` and `docs/development/contributing.md`.
- Add KaTeX attribution to `NOTICE.md`.
- Verify the strict Zensical build and the rendered page.

## Non-goals

- Replacing Zensical or switching to MathJax.
- PDF or offline documentation.
- Converting internal `docs/planning/` math; that tree is excluded from the published site.
- Changing code, public APIs, data contracts, or scientific formulas.
- Adding a general math renderer to the application UI.

## Design

### KaTeX configuration

Use the pinned KaTeX release `0.19.1` from the Zensical-documented integration. Add to `zensical.toml`:

```toml
[project]
extra_javascript = [
  "javascripts/katex.js",
  "https://unpkg.com/katex@0.19.1/dist/katex.min.js",
  "https://unpkg.com/katex@0.19.1/dist/contrib/auto-render.min.js",
]
extra_css = [
  "https://unpkg.com/katex@0.19.1/dist/katex.min.css",
]
```

`pymdownx.arithmatex.generic = true` is already present under `[project.markdown_extensions]`; do not duplicate
it.

If offline or privacy requirements later prohibit a CDN, vendor the same KaTeX version under
`docs/assets/katex/` and point the same configuration at the local files. The initializer does not change.

### Initializer

Create `docs/javascripts/katex.js`:

```js
document$.subscribe(({ body }) => {
  renderMathInElement(body, {
    delimiters: [
      { left: "$$", right: "$$", display: true },
      { left: "$", right: "$", display: false },
      { left: "\\(", right: "\\)", display: false },
      { left: "\\[", right: "\\]", display: true },
    ],
  });
});
```

This hooks KaTeX into Zensical's instant navigation, matching the official Zensical KaTeX example.

### Math markup

Use:

- `$$ ... $$` for display math;
- `$ ... $` or `\( ... \)` for inline math;
- `\begin{aligned} ... \end{aligned}` inside `$$` for aligned equations.

Do not use fenced `text` blocks for formulas.

### Formulas to convert

In `docs/physics/symmetry.md`, replace the existing fenced `text` formulas with display math. At minimum:

```markdown
$$
x \mapsto R x + t
$$
```

```markdown
$$
D = 24 \times 10^{12} = 24{,}000{,}000{,}000{,}000
$$
```

```markdown
$$
\det G = 1 - \cos^2\alpha - \cos^2\beta - \cos^2\gamma
         + 2\cos\alpha\cos\beta\cos\gamma
$$
```

```markdown
$$
\begin{aligned}
A &= (a, 0, 0) \\
B &= (b\cos\gamma,\; b\sin\gamma,\; 0) \\
C &= \left(
       c\cos\beta,\;
       \frac{c(\cos\alpha - \cos\beta\cos\gamma)}{\sin\gamma},\;
       \frac{c\sqrt{\det G}}{\sin\gamma}
     \right)
\end{aligned}
$$
```

```markdown
$$
\text{index} = P
  + \text{cellC}\cdot P_{\text{count}}
  + \text{cellB}\cdot P_{\text{count}}C_{\text{count}}
  + \text{cellA}\cdot P_{\text{count}}C_{\text{count}}B_{\text{count}}
$$
```

```markdown
$$
\text{code} = \sum_{i=0}^{N-1} \text{particleIndex}(s_i)\,2^{6i}
$$
```

Use inline math for symbols such as `$G_0$`, `$G_{\text{set}}$`, `$G_{\text{seq}}$`, `$P$`, `$1\ \text{fm}$`,
and `$[0,1)$` where it improves readability. Do not convert ordinary prose into math.

Audit the published docs for other fenced `text` blocks that contain formulas. Leave structural diagrams,
file trees, and command transcripts in `text` blocks.

### Documentation convention

Add to `AGENTS.md` §9 and `docs/development/contributing.md`:

- use `$$...$$` for display math;
- use `$...$` or `\(...\)` for inline math;
- never use fenced `text` blocks for formulas;
- preview the rendered page before closing a documentation change.

### Attribution

KaTeX is MIT-licensed. Add a short entry to `NOTICE.md`:

```text
KaTeX 0.19.1 (https://katex.org/) is used by the documentation site for math rendering.
It is distributed under the MIT licence.
```

Record whether the asset is loaded from the pinned CDN or vendored locally.

## API and contract impact

None. This amendment changes documentation assets and site configuration only.

## Temporary implementation and migration

- The current `docs/physics/symmetry.md` fenced `text` formulas are temporary.
- The amendment replaces them with `$$`/`$` markup.
- Planning docs remain unchanged; they are excluded from the published site.
- No migration or compatibility shim is required.

## Acceptance criteria

- [ ] `zensical build --strict` succeeds.
- [ ] The built symmetry page references `katex.min.css`, `katex.min.js`, and `javascripts/katex.js`.
- [ ] The published Step 02 formulas render as math, not monospace boxes.
- [ ] No fenced `text` math blocks remain in `docs/physics/symmetry.md`.
- [ ] KaTeX is pinned to `0.19.1` and attributed in `NOTICE.md`.
- [ ] The math authoring convention is recorded in `AGENTS.md` and the contributing guide.
- [ ] No code, public API, data-contract, or scientific-behavior changes.

## Tests

| Test | Assertion |
| --- | --- |
| Docs build | `zensical build --strict` passes |
| Asset references | Built HTML contains the KaTeX CSS/JS and initializer references |
| Formula conversion | No fenced `text` math blocks remain in the published Step 02 docs |
| Navigation | KaTeX renders after Zensical instant navigation |
| Accessibility | KaTeX output includes MathML or an equivalent accessible representation |
| Manual visual check | The symmetry page renders fractions, roots, subscripts, and aligned equations correctly |

## Documentation

- [ ] `docs/physics/symmetry.md` converted.
- [ ] `zensical.toml` updated.
- [ ] `docs/javascripts/katex.js` added.
- [ ] `AGENTS.md` and `docs/development/contributing.md` updated.
- [ ] `NOTICE.md` updated.
- [ ] Amendment page and index updated.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| CDN availability or privacy concern | Pin `0.19.1`; vendor the same assets locally if required |
| KaTeX syntax differs from MathJax | Use KaTeX-supported commands; build and visually check each converted formula |
| Instant navigation does not re-render math | Use the `document$.subscribe` initializer from the Zensical example |
| Accessibility regression | Keep KaTeX's MathML/accessible output; check the rendered page |
| New docs regress to `text` boxes | Add the authoring convention and a docs review checklist item |

## Dependencies

- Step 02 documentation exists and is still active.
- The Step 02 audit has not been frozen.
- Zensical `0.0.74` and `pymdownx.arithmatex` are already in the documentation toolchain.

## Integration

- Branch: `feature/step-02-symmetry-and-geometry/amend-2-docs-math`
- Parent step branch: `step/02-symmetry-and-geometry`
- Approval: normal feature approval; design review before implementation
- Squash commit: —
- Parent audit request updated: —
- Combined frozen commit: —

## Audit

- Parent audit record: `docs/planning/audits/step-02.md`
- Audit verdict: —
- Merge commit: —
- Tag: `step-02`
- Post-merge fallback required: no
