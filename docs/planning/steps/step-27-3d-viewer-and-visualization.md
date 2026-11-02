# Step 27 — 3D Viewer and Visualization

**Phase:** F. GUI  
**Depends on:** 24, 03–05
**Delivers:** A cross-platform 3D viewer for the cell, atoms, transitions, pairs, clusters, and
parameterization overlays, with camera controls, picking, selection sync, styles, and image export.

## Goal

Make the model spatially understandable. The viewer must render the idealized crystal and its symmetry
expansions, show migration paths and interactions, color by parameterization values, and connect selection to
the rest of the GUI.

## Original reference

- Two viewers: legacy WPF `Media3D`/HelixToolkit and current HelixToolkit.Wpf.SharpDX 2.15 + SharpDX D3D11
  (Windows-only).
- Renders:
  - cell frame;
  - atoms (spheres, mesh quality configurable);
  - pair interactions (two cylinders split at the midpoint);
  - group interactions (triangle fans);
  - transitions (two-headed arrows for dynamic, thin cylinders for static);
  - P1/symmetry path extension modes (`None`, `Local`, `Full`, `FullWithClip`);
  - render box/clipping.
- No GPU picking/object selection; hit testing is a coordinate catalog against symmetry-expanded sites.
- Per-object style stored in project `Resources`: color, material, mesh quality, scaling, wireframe, visibility,
  inactive flag, display name; render box stored in project resources.
- 27 viewport settings: MSAA, FXAA, SSAO, camera modes, background/info colors, lights, coordinate system, view
  cube, render stats, infinite spin, FOV/near/far, export size, draw mode, manual camera.
- Parallel scene building, instancing/batching, MSAA downgrade during interaction, image export.
- No selection, no editing in 3D.

## Design

### 1. Renderer abstraction

```csharp
public interface IRenderBackend : IDisposable
{
    void Initialize();
    void Resize(int width, int height);
    void Render(Scene scene, Camera camera);
    void Dispose();
}

public sealed class Scene
{
    public IReadOnlyList<SceneNode> Nodes { get; }
    public RenderBox Box { get; }
    public Lighting Lighting { get; }
}

public sealed class SceneNode
{
    public SceneNodeKind Kind { get; }       // Cell, Atom, Bond, Arrow, Polygon, Label
    public Transform Transform { get; }
    public Material Material { get; }
    public object? Tag { get; }              // model object id
    public bool IsVisible { get; }
}
```

- `Mocassin.Rendering` owns the scene graph and camera; backends are replaceable.
- `AvaloniaRenderHost` hosts an `OpenGlControlBase` (or a native control for another backend).
- The first backend is Silk.NET.OpenGL; Veldrid/Metal is the fallback (ADR 0006).
- Scene builders are pure: model + settings → `Scene`; they run off the UI thread.

### 2. Scene content

**Cell and positions**

- Unit cell edges and optional supercell replication.
- Stable sites as spheres; unstable sites as smaller/translucent spheres.
- Particles colored by species (stable, deterministic palette) or by parameterization value.
- Occupancy from the default building block or a selected job lattice (future: load a result lattice).
- Render box defined in fractional coordinates; clip nodes outside.

**Transitions**

- KMC paths as arrows along the symmetry-expanded reference paths.
- Dynamic vs. static linkers styled differently.
- Selected transition highlights all equivalent paths.
- Path extension modes:
  - `None`: reference path only;
  - `Local`: paths in the render box;
  - `Full`: all symmetry-equivalent paths;
  - `FullWithClip`: full paths clipped to the box.
- Arrow length shortened to avoid intersecting atom spheres (site catalog correction).

**Pairs and clusters**

- Stable/unstable pair interactions as cylinders between the reference positions, split/colored by endpoint.
- Base geometry and all symmetry-equivalent interactions (toggle).
- Clusters as polygons/fans from the center to surrounding positions; selected cluster highlighted.
- Parameterization overlay: color pairs/clusters by energy (diverging palette, zero at neutral), with a legend
  and min/max labels.
- Chiral partner indication.

**Labels and guides**

- Optional site labels, indices, distances, axes triad, view cube.
- Scale bar in Å.
- Hover tooltip: object name, type, distance/energy.

### 3. Camera and interaction

- Orbit (turntable), pan, zoom, reset; optional trackball/walk modes if inexpensive.
- Perspective/orthographic toggle; FOV/near/far.
- Focus on selection; frame all; frame render box.
- Smooth animation for camera moves; no animation for large scenes if it hurts FPS.
- Mouse/touch:
  - left drag = orbit; middle/right drag = pan; wheel = zoom;
  - click = pick; Ctrl+click = multi-select; double-click = focus.
- Keyboard: arrows rotate, +/- zoom, R reset, F focus.
- Camera state persists per project.

### 4. Picking and selection

- CPU ray picking against spheres/cylinders/polygons, or an ID-color framebuffer pass.
- Pick returns the topmost `SceneNode.Tag` (model object ID) and hit point.
- Selection synchronizes with the project tree/inspector:
  - selecting a site/transition/cluster highlights it and shows details;
  - selecting in 3D opens/navigates to the editor.
- Hover highlight and tooltip.
- Multi-select with Ctrl; box select optional.
- Hidden/inactive nodes are not pickable.
- Picking latency target < 50 ms for 10⁵ nodes.

### 5. Styling and settings

- Per-object style:
  - visible, color, material/roughness, mesh quality (sphere subdivisions), scale, wireframe, opacity.
- Global settings:
  - background, lighting, MSAA/FXAA, SSAO (optional), render stats, axes/view cube, atom scale, bond radius,
  - arrow thickness, label visibility, quality preset (low/medium/high).
- Presets: "Structure", "Transitions", "Interactions", "Publication".
- Styles persist in `Project.ViewSettings` by stable object ID; unknown keys survive.
- Reset per-object/all.

### 6. Performance

- Instanced rendering for spheres/cylinders/arrows.
- Static geometry cached per model hash; rebuild only changed categories.
- Level of detail: billboard/impostor spheres at low quality/large scenes; reduce subdivisions during camera
  movement.
- Frustum culling; optional occlusion is deferred.
- Target ≥ 30 FPS for 10⁵ atoms on integrated graphics; degrade quality gracefully.
- Scene build off the UI thread with progress for large models.
- Memory budget: < 500 MB GPU for a 20×20×20 cell at medium quality.

### 7. Export

- Screenshot current viewport (PNG) at configurable resolution (up to 4K).
- Transparent background option.
- Export scene summary (camera, visible categories, styles) as JSON for reproducibility.
- No trajectory/video export in v1.

### 8. Accessibility and fallbacks

- 2D projection fallback for machines without a usable GPU/OpenGL context.
- Keyboard-only camera controls and selection.
- High-contrast colors; colorblind-safe palette.
- A "renderer unavailable" banner with a link to diagnostics.

### 9. Spike gate

Before full implementation, a two-week spike must demonstrate on all three OSes:

- Avalonia + Silk.NET OpenGL context creation;
- 10⁵ instanced spheres at ≥ 30 FPS;
- picking < 50 ms;
- screenshot export;
- resize/DPI handling.

If macOS fails, switch to Veldrid/Metal; the scene layer stays unchanged.

## Implementation tasks

- [ ] Implement the scene graph, camera, materials, and render-box types.
- [ ] Implement the Silk.NET OpenGL backend and Avalonia host.
- [ ] Implement instanced sphere/cylinder/arrow/polygon renderers.
- [ ] Implement scene builders for cell, atoms, transitions, pairs, clusters, overlays.
- [ ] Implement path extension modes and clipping.
- [ ] Implement CPU/ID picking and selection sync.
- [ ] Implement camera controls, focus/frame, and settings.
- [ ] Implement per-object styles and presets.
- [ ] Implement screenshot export.
- [ ] Implement the 2D fallback and diagnostics.
- [ ] Add performance benchmarks and screenshot tests.

## Acceptance criteria

- [ ] The ceria cell renders with correct atoms, cell frame, and site positions.
- [ ] Transitions show all symmetry-equivalent paths with dynamic/static styling.
- [ ] Pair/cluster overlays color by parameterization energy with a legend.
- [ ] Clicking an atom/path/cluster selects it and navigates to the editor.
- [ ] Camera orbit/pan/zoom/reset/focus work with mouse and keyboard.
- [ ] Styles persist across save/reopen.
- [ ] 10⁵ atoms render at ≥ 30 FPS on the reference integrated GPU.
- [ ] Screenshot export produces the expected image on all OSes.
- [ ] Renderer failure falls back gracefully.
- [ ] Documentation written

## Documentation

At this point, document the 3D viewer:

- `docs/user-guide/visualization.md` — camera, picking, selections, styles, overlays, export.
- `docs/tutorials/ceria-kmc.md` — screenshots and inspection steps.
- `docs/development/ui-architecture.md` — renderer abstraction, scene builders, picking, performance.
- `docs/troubleshooting/installation.md` — GPU/OpenGL fallback and renderer diagnostics.

## Tests

| Test | Assertion |
| --- | --- |
| Scene builder | Node counts/positions for ceria |
| Symmetry paths | Expanded paths match engine |
| Picking | Ray/ID picks correct object |
| Selection | 3D ↔ tree/inspector sync |
| Styles | Persist/round-trip/unknown keys |
| Camera | Matrices/orthographic/focus |
| Performance | Instance/FPS budget |
| Export | PNG dimensions/background |
| Fallback | Banner and 2D mode |
| Screenshots | Visual baselines per OS |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Renderer integration instability | Spike gate and Veldrid fallback |
| Large scene performance | Instancing, LOD, culling, quality presets |
| macOS OpenGL deprecation | Backend abstraction; Metal fallback |
| Picking complexity | CPU ray first; ID buffer if needed |
| Visual regressions | Screenshot tests and style fixtures |

## Done when

The viewer renders the model and its symmetry-expanded interactions/transitions at interactive frame rates on
all platforms, with reliable picking, selection sync, styling, and export.
