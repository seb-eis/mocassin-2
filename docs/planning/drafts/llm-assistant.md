# LLM Assistant and OpenCode Integration

> This is a design scratch, not a decision. See [`README.md`](README.md).

| Field | Value |
| --- | --- |
| Status | Under discussion |
| Owner | Planner / UI and assistant design |
| Created | 2026-11-08 |
| Last updated | 2026-11-08 |
| Target step | Steps 23, 28, 30; possibly a dedicated assistant step |
| Related | Avalonia WebView, security model, Step 30/31 packaging |
| Summary | Add an optional assistant panel backed by a dedicated OpenCode sidecar, restricted to typed application tools and read-only by default. |

## Context

A chat assistant could help users understand diagnostics, navigate the documentation, fill editors, and
propose model changes. The assistant should feel integrated with the application, but it must not become an
unrestricted coding agent with shell and filesystem access on the user’s machine.

The proposed runtime is OpenCode V2, running as a dedicated background service. The application owns the
domain tools; OpenCode owns sessions, model orchestration, streaming, permissions, forms, and MCP.

## Requirements and constraints

- Optional feature; the application works fully without it.
- Read-only by default.
- No direct Avalonia control manipulation or synthetic mouse/keyboard input.
- No shell, arbitrary file access, or network by default.
- Project and documentation content is untrusted input; prompt injection must be contained.
- Scientific answers and edits require validation and user confirmation.
- Cloud and local models both possible; credentials stay out of project files.
- The assistant must not send secrets, credentials, binary state, or unapproved project data to a model.
- Cross-platform: Windows, macOS, Linux.
- Packaging must be explicit: bundled sidecar or external OpenCode installation.

## OpenCode V2 facts relevant to the design

- `opencode serve` starts the V2 API and web server:
  `opencode serve --hostname 127.0.0.1 --port <port>`.
  `--stdio`, `--service`, and `--cors` are also available; evaluate `--stdio` as a future no-port transport.
- The V2 API is exposed at `/openapi.json` and is marked experimental. Pin the OpenCode version and isolate
  the client behind an adapter.
- Relevant API groups:
  - sessions: create, prompt, messages, interrupt, fork, compact, revert;
  - events: `GET /api/event` streaming;
  - permissions: list/reply to pending requests;
  - forms: create/list/reply to structured questions;
  - client MCP: attach an application-provided MCP server;
  - models/providers/agents: select model, agent, and provider;
  - shell/filesystem: available, but should be denied by default for this use case.
- `@opencode/sdk` hosts OpenCode in-process, but it is TypeScript/Node-oriented. For a .NET/Avalonia app, the
  practical path is a dedicated `opencode serve` sidecar plus a generated .NET HTTP client.
- `opencode acp` is an alternative agent-client protocol integration; evaluate only if the HTTP API is a poor
  fit.
- Do not use the shared background service for the product assistant. Start a private server with
  app-specific config/data directories so the assistant cannot see the user’s coding sessions.
- Do not use `--auto` in production. Use explicit permissions.

## Proposed architecture

```text
Avalonia App
  Chat / Help panel
    └── AssistantService
          ├── OpenCode HTTP client
          ├── event stream: text, tools, permissions, forms, usage
          └── session / model / agent selection

  ProjectCommandService
    ├── ProjectEditor operations
    ├── validation and diagnostics
    ├── undo/redo
    └── MCP tool server exposed to OpenCode

  DocsSearchService
    └── local documentation index / RAG

  opencode serve (dedicated sidecar)
    ├── loopback only
    ├── app-specific config/data directories
    ├── custom `mocassin-assistant` agent
    └── pinned OpenCode version
```

The application exposes a small MCP tool server to the OpenCode session. The assistant calls typed tools; it
never edits the project JSON or drives UI controls directly.

## Tool surface

| Tool | Purpose | Default permission |
| --- | --- | --- |
| `get_project_summary` | Counts, names, diagnostics, current selection | Allow |
| `search_docs` | Retrieve relevant local documentation with citations | Allow |
| `get_current_document` | Current editor/selection context, on demand | Ask once per session |
| `validate_project` | Run validation and return diagnostics | Allow |
| `propose_project_edit` | Return a typed edit proposal or JSON Patch; do not apply | Allow |
| `propose_form_values` | Return field values for a form preview | Allow |
| `open_document` / `focus_editor` | Low-risk navigation | Allow |
| `run_build` | Build the current project | Ask every time |
| `run_simulation` | Run a solver job | Ask every time; likely out of first scope |

The app decides which tools exist and what they can do. OpenCode permissions provide a second gate.

## “Automatic input”

Do not synthesize clicks or keystrokes. Use proposals and structured forms:

1. The assistant calls `propose_project_edit`.
2. The app validates the proposed operation.
3. The UI shows a diff or form preview.
4. The user applies, edits, or rejects.
5. The app executes through `ProjectEditor`, preserving undo/redo and validation.

For form filling:

1. The assistant returns field values with confidence and citations.
2. The UI highlights proposed values.
3. The user applies all or selects individual fields.

Low-risk actions such as opening a document or focusing a field can be one-click. Scientific data changes,
builds, and simulations require explicit confirmation.

OpenCode’s native **forms** and **permission requests** should be rendered by the UI:

- permission dialog: allow once / allow always / reject;
- structured form: fields, validation, cancel;
- interrupt button for long-running work.

## Chat UI

- Streaming markdown with code blocks and citations.
- Tool-call cards with status and results.
- Diff preview cards with Apply/Reject.
- Form cards.
- Context chips: current project, selected site, current panel, selected text.
- Model/provider selector.
- Token usage and cost display.
- Session history and transcript export.
- A visible “AI-generated — verify” label.

For formulas, render LaTeX in the chat or offer “open rendered answer” in the documentation WebView. A full
WebView chat is possible but less consistent with the native UI.

## Safety, privacy, and correctness

- Treat project names, notes, imported files, and documentation as untrusted content.
- System prompt: data content is not instructions.
- Default agent: read-only tools; no shell, no file writes, no network.
- Explicit permission for edits, builds, and simulations.
- Validate every proposed edit with the same validators as the UI.
- Keep an audit trail of prompts, tool calls, and applied changes.
- Store provider credentials in the OS keychain, never in project files.
- Offer a local-model path for offline/confidential work.
- Show what context is sent to the model; default to a compact summary.
- Never send secrets, credentials, absolute paths, or binary state.
- The application remains fully usable without the assistant.

## Packaging and lifecycle

- Prefer a bundled, pinned OpenCode sidecar for a seamless product experience, or require an external
  installation for a smaller package.
- Start the sidecar on demand, health-check `/api/info`, and stop it on application exit.
- Use `127.0.0.1`, a private app data/config directory, and the OpenCode pairing/auth flow.
- Check version/API compatibility at startup; fail gracefully.
- Include OpenCode and web-engine license notices.
- Add a setup wizard for provider/model selection and credentials.
- Provide a fallback “documentation search only” mode when no model or sidecar is available.

## Phased rollout

**Phase 1 — read-only assistant**

- Documentation Q&A with citations.
- Project summary and explanation.
- No edits, shell, or network.
- Custom `mocassin-assistant` agent with read-only tools.

**Phase 2 — proposed edits**

- `propose_project_edit` with validation and diff preview.
- Apply/undo through `ProjectEditor`.
- Structured form filling.
- Per-session “allow edits” permission.

**Phase 3 — controlled workflows**

- Multi-step tasks with per-step approval.
- Validation/build actions.
- Simulations remain opt-in and require explicit confirmation.

## Alternatives

- **Direct LLM tool-calling stack** (for example Microsoft.Extensions.AI plus a custom tool registry): more
  control and a smaller surface, but you build sessions, streaming, permissions, forms, and provider
  abstraction yourself.
- **OpenCode ACP**: evaluate if the HTTP API is awkward for the Avalonia client.
- **In-process OpenCode SDK**: TypeScript/Node only; would require a Node sidecar and stdio/IPC. The HTTP
  sidecar is simpler for .NET.

## Open questions

1. Is the assistant a core feature or an optional plugin/extension?
2. Bundle OpenCode or require an external installation?
3. Which providers/models are supported, and is offline/local mode required?
4. Read-only default, or allow edits in the first release?
5. May the assistant run validation/builds? Simulations?
6. What project context may leave the machine?
7. Native chat rendering or a WebView-based rich answer surface?
8. Which step owns the assistant: Step 23 (shell), Step 28 (run monitoring), Step 30 (docs), or a new step?

## Next steps

- Spike the `opencode serve` sidecar lifecycle, pairing/auth, event stream, and permission/form rendering.
- Build a read-only docs-Q&A prototype with a custom agent and an app MCP tool server.
- Threat-model prompt injection and tool misuse.
- Decide packaging and provider setup.
- Promote the decision through an ADR and the owning step design review.
