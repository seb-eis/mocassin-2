# ADR 0010 — Canonical MessagePack Projection for Signatures

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

Model, parameterization, and lattice signatures are SHA-256 values shared by the .NET builder, the Rust
solver, and the contract tests. The stored payloads are MessagePack maps compressed with Zstd, but a stored
payload is not a stable signature input:

- map key order may differ between serializers;
- optional fields may be omitted or written as `nil`;
- integers may use different widths;
- `-0.0`, NaN, and Inf handling can differ;
- Zstd output depends on the library and level.

Step 11 and Step 13 require C# and Rust to agree on the signature. The method must therefore be specified
independently of the stored encoding.

## Decision

Signatures are computed over a **canonical MessagePack projection** of the logical object.

### Projection rules

1. **Field order and types come from `contracts/manifest.json`.** The manifest defines the projection layout
   for every signed type.
2. **Structs are encoded as MessagePack arrays**, not maps, in manifest order. Dynamic maps are encoded as
   MessagePack maps with UTF-8 keys sorted by byte order.
3. **Integers are fixed-width.** Non-negative counts and indices use `uint64`; signed values use `int64`.
   Do not use the smallest-width encoding.
4. **All floating-point values use `float64`.** Normalize `-0.0` to `+0.0`. Reject NaN and Inf in fields
   that the schema requires to be finite.
5. **Strings are UTF-8 bytes as stored**, with no Unicode normalization. Byte arrays use MessagePack `bin`.
6. **Every field is present.** Absent optional values are encoded as `nil`; defaults are not omitted.
7. **No compression, timestamps, runner metadata, file names, or SQLite layout** is part of the projection.
8. **Domain separation:** hash
   `SHA-256(domain_prefix || 0x00 || projection)`, with these prefixes:
   - `mocassin/model-signature/v1`
   - `mocassin/parameterization-signature/v1`
   - `mocassin/lattice-signature/v1`

The stored `.msl` payload may remain a MessagePack map with Zstd compression. Readers decode it, re-encode the
canonical projection, and compare the SHA-256 value with the stored signature.

### Tooling

- `tools/contracts` emits the projection field order and type information from the manifest.
- C# and Rust implement the projection encoder; Python may verify signatures but is not required to write
  them.
- Contract fixtures include canonical projection digests and cross-language test vectors.
- A manifest change that affects projection order or types is a format-breaking change.

## Consequences

**Positive**

- C# and Rust can verify signatures without agreeing on a general-purpose MessagePack serializer.
- Stored payloads stay flexible and compressed.
- The signature is independent of Zstd and SQLite implementation details.
- Test vectors make drift detectable.

**Negative**

- Every signed type needs a projection layout in the manifest.
- C# and Rust need a small dedicated encoder.
- Adding a field requires a manifest update and, for signed types, a format version bump.

## Alternatives considered

- **Sign the stored MessagePack payload:** rejected; serializer details leak into the contract.
- **Sign canonical JSON:** workable, but larger and slower for large compiled models, and still needs a
  separate canonicalization pass.
- **Generate full encoders from the manifest:** stronger, but more tooling than needed for the current set of
  signed types.
