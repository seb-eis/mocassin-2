# API Reference

The API links go directly to the first page of each generated reference instead of an intermediate landing
page. The `/api/dotnet/` and `/api/rust/` directories redirect to the same first pages so tool-internal links
and bookmarks continue to work.

| API | Entry point | Generator |
| --- | --- | --- |
| .NET libraries | [`Mocassin.Core.CoreModule`](dotnet/api/Mocassin.Core.CoreModule.html) | DocFX |
| Rust solver crates | [`mocassin-types`](rust/mocassin_types/index.html) | rustdoc (`cargo doc`) |
| Python analysis package | [Python API placeholder](python/index.md) | mkdocstrings (Step 22) |

These entry points are updated by the step that introduces the first stable public type or crate. The
generated HTML is never committed; `tools/docs/build-docs.*` copies it into `site/api/dotnet` and
`site/api/rust`.

Narrative pages should link to these entry points rather than duplicating API content.
