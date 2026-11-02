# ADR 0006 — 3D Rendering Approach

- **Status:** Accepted (with a spike gate)
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

The original 3D viewer uses HelixToolkit.Wpf.SharpDX 2.15 on Direct3D 11, which is Windows-only. It renders
cell frames, atoms, transition paths, pair interactions, group clusters, and parameterization overlays. It has
no object picking; hit testing is coordinate-based against a symmetry-expanded site catalog.

The new viewer must work on Windows, Linux, and macOS, handle 10⁵ atoms at ≥30 FPS on integrated graphics, and
support selection synchronized with the project tree.

## Decision

- Introduce `Mocassin.Rendering`, a renderer-agnostic scene layer:
  - `Scene`, `SceneNode`, `Camera`, `Material`, `Mesh`, `InstanceBuffer`, `Picker`.
  - Model-to-scene builders for cell frame, sites, transitions, pairs, clusters, and overlays.
- First backend: **Silk.NET.OpenGL** hosted in Avalonia's `OpenGlControlBase`.
  - Instanced sphere/cylinder/arrow meshes.
  - One directional + one point light, Blinn–Phong, per-instance color.
  - MSAA/FXAA, optional SSAO later.
  - CPU ray picking against spheres/cylinders (or an ID-color pass) for selection.
- Keep the scene layer independent so a **Veldrid** or **Metal/Vulkan** backend can be added if macOS OpenGL
  performance or deprecation becomes a blocker.
- A two-week spike at the start of step 27 validates:
  - Avalonia + Silk.NET OpenGL integration on all three OSes,
  - 10⁵ instanced spheres at target FPS,
  - picking latency < 50 ms,
  - image export via framebuffer readback.

If the spike fails on macOS, fall back to Veldrid with a Metal backend, keeping the scene layer unchanged.

## Consequences

**Positive**

- Cross-platform from day one.
- Full control over instancing, picking, and overlays.
- The renderer can be tested with headless/offscreen contexts.

**Negative**

- We own ~1–2k lines of renderer code.
- OpenGL is deprecated on macOS (still functional), which is a medium-term risk.
- Advanced effects (SSAO, shadows) must be implemented or deferred.

## Alternatives considered

- **HelixToolkit.Avalonia:** maturity and feature parity uncertain; not chosen as the primary path.
- **Veldrid:** good multi-backend abstraction but community Avalonia integration; kept as fallback.
- **CPU/2.5D projection:** simple and deterministic, but cannot meet parity for 3D interaction; may be a
  preview mode.

## Scope for first release

Required: cell frame, atoms, bonds/pairs, transition arrows, cluster polygons, parameterization color
overlays, orbit/pan/zoom, orthographic toggle, selection, visibility filters, screenshot export.

Deferred: shadows, ambient occlusion, transparency sorting beyond simple alpha, volume rendering, animation
of trajectories.
