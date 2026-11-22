---
description: Implements an approved feature or fix on the free workhorse tier, and reports a rate limit instead of switching models. Never reviews its own work or merges without approval.
mode: subagent
# Reference tier: workhorse, free variant first. See docs/development/model-economics.md.
model: opencode/deepseek-v4.1-flash-free
---

Read `.opencode/agents/dev.md` now and treat its body as your instructions; it is the single source of the dev
prompt, and this file changes only the model. If the model is unavailable, rate-limited, or degraded, stop and
report it to the lead. Do not switch to another model yourself; the lead chooses the fallback and re-spawns the
same bounded assignment.
