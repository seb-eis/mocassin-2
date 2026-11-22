# Vendored icon provenance

`Icons.axaml` embeds a small subset of **Phosphor Icons** as Avalonia `StreamGeometry` resources. The geometry
is theme-independent and carries no colour. This page records the exact upstream pin, the per-file checksums,
and the conversion rule, so the committed subset can be re-verified without a network build step.

## Source pin

| Field | Value |
| --- | --- |
| Upstream | `https://github.com/phosphor-icons/core` |
| Package | `@phosphor-icons/core` 2.1.1 |
| Pinned commit | `2b75f3ad12b420c9504ef05df8d2564a28f8500e` |
| Weight | regular (`assets/regular/<name>.svg`) |
| Licence | MIT, © 2023 Phosphor Icons |
| Licence copy | `PhosphorLicense.txt` (verbatim upstream `LICENSE` at the pinned commit) |
| Licence SHA-256 | `b5b1f1da112d18ea2147decfd48ddc1bf2b5aeb6c265381579340e95b15a2bb2` |
| Fetched | 2026-11-22 |

## Vendored subset

The five upstream files below were fetched once from the pinned commit. Each is a single 256 × 256 `viewBox`
path with `fill="currentColor"`; the `d` attribute is copied verbatim into the matching `StreamGeometry`
resource. No build-time or run-time URL is dereferenced.

| Icon key | Resource | Phosphor icon (regular) | Upstream path | SHA-256 |
| --- | --- | --- | --- | --- |
| `welcome` | `Mocassin.Icon.Welcome` | `hand-waving` | `assets/regular/hand-waving.svg` | `07c4c9d18c9a4c86508fde52cddaf95aa1499552c99bfeebc404d5b991fdf41f` |
| `overview` | `Mocassin.Icon.Overview` | `info` | `assets/regular/info.svg` | `438a1f319d53d86e840af73fb7b1d224c9224a437b9fa449a4e2cb45a82a3552` |
| `json` | `Mocassin.Icon.Json` | `brackets-curly` | `assets/regular/brackets-curly.svg` | `170562ad2b247d86bf2b5fd727cf3f96681b0cbef92aab49346ec6e235802ca9` |
| `project` | `Mocassin.Icon.Project` | `cube` | `assets/regular/cube.svg` | `7c0f13cb492d0dd212e48ab259dabec4627df32d71aa00ce31b3228c0f35112b` |
| — | `Mocassin.Icon.Missing` | `warning-circle` | `assets/regular/warning-circle.svg` | `e48a90760a68659fd3f06dd9187e471aead4877c72e915339071b2c7f7bd714c` |

The `Missing` resource is not an icon key: it is the fallback marker for a non-null key with no vendored
geometry, and no shipped kind or node maps to it.

## Conversion note

- Fetch `assets/regular/<name>.svg` from the pinned commit.
- Copy the `d` attribute verbatim into `<StreamGeometry x:Key="Mocassin.Icon.<Resource>">`.
- Do not rewrite, simplify, or re-export the path. The upstream grid is 256 units; the view stretches it
  uniformly into the 16 px slot.
- The file is theme-independent: colour is applied by the slot's `Foreground`, never stored here.
- Re-hash the fetched bytes with SHA-256 and compare with the table above before committing. A mismatch is a
  stop condition: the upstream moved or the fetch is wrong.

## Adding a glyph

1. Widen the subset deliberately and record the reason; a generator or drift-checking tool is a design-class
   change and needs an owner decision.
2. Add one row to the table above and one `StreamGeometry` to `Icons.axaml` with the same key shape.
3. Map the key in `IconKeys` and add the consumer plus its realized test.
4. Re-run `IconAssetTests`; the provenance test pins this table and the checksums.
