# API Reference

The combined documentation site publishes three generated API references under stable subpaths:

| API | Published path | Generator |
| --- | --- | --- |
| .NET libraries and CLI | `/api/dotnet/` | DocFX |
| Rust solver crates | `/api/rust/` | rustdoc (`cargo doc`) |
| Python analysis package | `/api/python/` | mkdocstrings |

The pages below are tracked placeholders so the strict narrative build passes before the generated API output
exists. The documentation build copies DocFX and rustdoc output into `site/api/dotnet` and `site/api/rust`;
the Python reference is rendered natively by mkdocstrings once the package is implemented.

- [.NET API](dotnet/index.md)
- [Rust API](rust/index.md)
- [Python API](python/index.md)

Generated API HTML is never committed. Narrative pages should link to the stable subpaths rather than
duplicating API content.
