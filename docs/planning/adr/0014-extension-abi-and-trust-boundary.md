# ADR 0014 — Extension ABI and In-Process Trust Boundary

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

MMCFE and custom routines need hooks into the solver. Extensions are native shared libraries loaded into the
solver process. That is a full-trust boundary: a bad extension can crash, corrupt memory, or read any file the
solver can read. The original plan had a header sketch but no ownership, error, or loading rules.

## Decision

### ABI

- The ABI is C, `extern "C"`, with `#[repr(C)]` structs and a single exported symbol
  `mocassin_extension_entry`.
- `abi_version` is an integer; the host rejects unknown major versions.
- The host callback table is versioned and append-only. It starts with a `size` field so newer hosts can add
  callbacks without breaking older extensions.
- Function pointers are non-null for required callbacks; optional callbacks may be null.
- All strings passed to or from an extension are UTF-8, NUL-terminated, and valid only for the duration of the
  call. The host copies any string it retains.
- Configuration is a bounded UTF-8 JSON object. The host owns the buffer during `initialize`; the extension
  must not retain it.
- Extensions must not unwind across the ABI. Rust extensions catch panics; C++ extensions must catch
  exceptions. A non-zero return code aborts the job with exit code `17`.
- Hooks run on the solver thread. An extension that spawns threads must join them before returning.
- The extension must not write the `.msl`; it may request a state save through the host.

### Loading and security

- Extensions are loaded only when explicitly requested (`--extension`), never automatically from a package or
  project.
- The loader checks the file exists, is a regular file, and matches the platform library suffix. Path
  traversal and world-writable locations are rejected by policy.
- The GUI shows a persistent warning before enabling an extension and never stores an extension path in a
  project file.
- Extensions run in-process in v1. A subprocess sandbox is a future ADR, not a v1 promise.
- The solver may run with `--no-extensions` to guarantee a pure run.

### MMCFE

- MMCFE is a built-in extension (or a reference `cdylib`) using the same hooks.
- Its log database has its own versioned schema in the data contracts.
- It must be resumable and must not depend on the legacy `mmcfelog.db`.

## Consequences

**Positive**

- C and Rust extensions have a clear, versioned contract.
- Crashes and panics have a defined failure mode.
- Users explicitly opt into the trust boundary.

**Negative**

- In-process extensions can still crash or compromise the solver.
- ABI changes require compatibility shims.
- Cross-platform loading remains platform-specific.

## Alternatives considered

- **Subprocess extension host:** safer, but more complex and slower; deferred.
- **WASM plugins:** safer and portable, but less suitable for HPC numerical code today.
