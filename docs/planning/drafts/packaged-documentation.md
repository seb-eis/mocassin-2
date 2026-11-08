# Packaged Documentation and In-App Help

> This is a design scratch, not a decision. See [`README.md`](README.md).

| Field | Value |
| --- | --- |
| Status | Under discussion |
| Owner | Planner / UI and release design |
| Created | 2026-11-08 |
| Last updated | 2026-11-08 |
| Target step | Steps 23, 30, 31 |
| Related | [ADR 0008](../adr/0008-standalone-documentation-toolchain.md), [Step 23](../steps/step-23-avalonia-shell-and-project-lifecycle.md), [Step 30](../steps/step-30-standalone-documentation.md), [Step 31](../steps/step-31-packaging-and-release.md) |
| Summary | Ship a versioned offline HTML documentation bundle and display it in an Avalonia 12 `NativeWebView`, with a system-browser fallback. |

## Context

The documentation site is built with Zensical, DocFX, rustdoc, and mkdocstrings. Users should be able to open
the documentation from the desktop application without network access, and context help should be able to
open the matching page or anchor.

The current planning state is inconsistent:

- [Step 30](../steps/step-30-standalone-documentation.md) says versioning, PDF, and offline documentation are
  deferred; the first release publishes the current version only.
- [Step 31](../steps/step-31-packaging-and-release.md) lists `docs` in the release archives.
- [`05-open-decisions.md`](../05-open-decisions.md) defers “PDF/offline documentation” to post-1.0.

This draft assumes **offline HTML documentation is in scope** and **PDF remains deferred**. That scope change
must be accepted before Step 30/31 are frozen.

## Current facts

- Avalonia is pinned in `Directory.Packages.props` at **11.3.22** for `Avalonia`, `Avalonia.Desktop`,
  `Avalonia.Fonts.Inter`, and `Avalonia.Themes.Fluent`.
- `docs/planning/01-target-architecture.md` and
  [ADR 0003](../adr/0003-avalonia-mvvm-stack.md) also say **Avalonia 11.x**.
- Avalonia 12 includes the MIT-licensed `Avalonia.Controls.WebView` package with:
  - `NativeWebView` for embedded web content;
  - `NativeWebDialog` for a separate web dialog;
  - `WebAuthenticationBroker`;
  - native platform renderers, not bundled Chromium.
- Platform prerequisites:
  - Windows 11: WebView2 preinstalled; Windows 10 may need the WebView2 runtime.
  - macOS: WKWebView is built in.
  - Linux: WebKitGTK 4.1 + libsoup3 + GTK3 are the baseline; WPE WebKit is optional;
    `NativeWebDialog` always uses WebKitGTK. (`NativeWebView` falls back to WebKitGTK when WPE is absent.)
- The app projects are still placeholders, so an Avalonia 12 upgrade before Step 23 is inexpensive.
- The current `zensical.toml` loads KaTeX from `unpkg.com`; packaged docs must vendor KaTeX, fonts, search,
  and any other runtime asset.

## Requirements

- Fully offline after installation.
- Versioned with the application; help must match the running version.
- Search, internal links, anchors, KaTeX math, and generated API docs must work.
- No CDN or network dependency at runtime.
- Cross-platform: Windows, macOS, and Linux.
- Read-only install directories must remain usable; extract to a user cache if needed.
- Context help from the UI, e.g. “open documentation for this diagnostic”.
- External links go to the system browser.
- The application still starts and works if the documentation bundle or WebView runtime is unavailable.

## Options

| Option | Pros | Cons | Notes |
| --- | --- | --- | --- |
| `NativeWebView` + virtual origin / request interception | Embedded, offline, no port, search works | Needs Avalonia 12; interception API must be verified; platform runtimes | Preferred if request interception is available |
| `NativeWebView` + loopback HTTP server | Embedded, simple content loading, works with existing static site | Local port, firewall/antivirus edge cases, server lifecycle | Good fallback |
| `NativeWebDialog` | Separate docs window, less embedding work, Linux fallback | Separate window; less integrated | Good secondary entry point |
| System browser + `mocassin docs` | No WebView dependency, CLI-friendly | Leaves the app, browser-tab UX, server lifetime | Keep as fallback |
| `file://` static site | No server, no WebView | Search/fetch and some generated pages are fragile | Not primary |
| Native HTML renderer | No web engine | KaTeX, DocFX, rustdoc, search, CSS are too much | Not recommended |

## Tentative recommendation

**Phase 1 — packaged docs**

- Build the complete static site at release and package it as a versioned offline bundle.
- Vendor all runtime assets and remove CDN references from the packaged build.
- Add `docs-manifest.json` with app version, docs commit, and asset hashes.
- Ship the bundle compressed; extract to `%LOCALAPPDATA%/Mocassin/docs/<version>` or
  `~/.cache/mocassin/docs/<version>` for installed apps; use a sibling `docs/` folder for portable builds.
- Add a `mocassin docs` command and a Help menu entry.

**Phase 2 — embedded viewer**

- Upgrade the UI stack to Avalonia 12 before Step 23 expands the app.
- Use `NativeWebView` with a virtual origin / request interception mapping to the bundle.
- Fall back to a loopback-only HTTP server if interception is not available in the pinned version.
- Use `NativeWebDialog` for a standalone Help window or Linux fallback.
- Keep the system-browser path for CLI and recovery.

**Phase 3 — context help**

- Map UI actions and diagnostics to doc paths and anchors.
- Add a Help search surface and “open the relevant page” actions.
- Keep external links in the system browser.

## Packaging sketch

```text
docs/
  index.html
  search/
  assets/
  api/dotnet/
  api/rust/
  api/python/
  docs-manifest.json
```

- Version the bundle with the product version.
- Show the docs version in Help/About.
- If docs and app versions differ, warn rather than silently mixing.
- If full API docs dominate the size, consider a core narrative bundle plus an optional API bundle.

## Security and offline checklist

- Loopback only, never `0.0.0.0`, if a server is used.
- Ephemeral port and per-session token or random path.
- Manifest allowlist; no directory traversal, directory listing, or symlink escape.
- CSP, `nosniff`, `no-referrer`.
- External links open in the system browser.
- WebView: disable devtools, host objects, and arbitrary file access in release.
- Test with no network access, paths containing spaces/non-ASCII characters, read-only installs, multiple
  instances, missing WebView2, and missing WebKitGTK.
- Include third-party notices for KaTeX, Zensical, DocFX, and any bundled web runtime.

## Open questions

1. Is embedded in-app help required, or is opening the system browser acceptable for the first release?
2. Do we ship the full API reference offline, or only the narrative/tutorial documentation?
3. What installer-size increase is acceptable?
4. Must documentation remain readable after the application closes?
5. Which Linux packaging formats are release-critical: tarball, `.deb`, `.rpm`, AppImage, Flatpak?
6. Should the docs bundle be part of the app package or a separate optional download?

## Next steps

- Decide the offline HTML scope in Step 30/31.
- Upgrade the Avalonia pin and Step 23 design to 12.x if the WebView is adopted.
- Prototype `NativeWebView` with a virtual origin against the current Zensical site.
- Measure the built bundle size with and without DocFX/rustdoc.
- Promote the final decision through an ADR and the Step 23/30/31 design reviews.
