# Rust API

This placeholder is only used by a narrative-only build before rustdoc output exists. The published site links
directly to the first Rust API content page, `mocassin_types/index.html`, and `/api/rust/` is a generated
redirect to that page.

`cargo doc --manifest-path src/rust/Cargo.toml --workspace --no-deps` writes HTML to `target/doc/`; the
documentation build copies it into `site/api/rust/`. Public items require doc comments, and missing docs are
denied. Generated output is not committed.

See the [API overview](../index.md) for the other language references.
