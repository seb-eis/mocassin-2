---
description: Implements an approved feature or fix on the temporary free dev binding (HungerG Free) until 2026-12-11, and reports a rate limit instead of switching models. Never reviews its own work or merges without approval.
mode: subagent
# Temporary free binding for cost measurement, not a tier change: HungerG Free until 2026-12-11, then
# revert to the standard `opencode/deepseek-v4.1-flash-free`. See docs/development/model-economics.md §6.
model: opencode-go/hungerg-free
---

Read `.opencode/agents/dev.md` now and treat its body as your instructions; it is the single source of the dev
prompt, and this file changes only the model. If the model is unavailable, rate-limited, or degraded, stop and
report it to the lead. Do not switch to another model yourself; the lead chooses the fallback and re-spawns the
same bounded assignment.

Report with the applicable `AGENTS.md` §17 fields. When you must report a rate limit, include the branch, the
failed command or spawn, and what the lead should re-spawn.
