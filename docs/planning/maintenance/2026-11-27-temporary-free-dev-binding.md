# Temporary `dev-free` binding (HungerG Free) — 2026-11-27

**Status:** In effect from 2026-11-27 until **2026-12-11**, then revert. Branch `docs/temporary-hungerg-free-dev`
from `main` at `8e9afefcc4b5c9e3aec4bcd976831662f79710d1`; merge pending owner approval.
**Owner:** human owner.
**Decision date:** 2026-11-27.
**Design class:** `None` (process/bookkeeping change inside the existing binding boundary; one-line record only,
no design review). It is not a tier change and not a public contract change.

## 1. Decision

The owner approved a temporary cost experiment: the `dev-free` binding runs HungerG Free
(`opencode-go/hungerg-free`, cost 0 in the provider model catalog) instead of the standard
`opencode/deepseek-v4.1-flash-free`.

- **Scope:** the `dev-free` binding only.
- **Unchanged:** the paid `dev` binding (`opencode-go/deepseek-v4.1-flash`) stays the default and the fallback;
  `lead`, `design`, and `review` stay on `opencode-go/deepseek-v4.1-flash`; `senior` stays on
  `opencode-go/deepseek-v4-pro`.
- **Duration:** from 2026-11-27 until **2026-12-11**, inclusive of the start date.
- **Nature:** a temporary exception to "free variant of the default tier" in
  [`docs/development/model-economics.md`](../../development/model-economics.md) §2 rule 1. It is not a tier
  change, not a new tier, and not a permanent binding.
- **Fallback behaviour is unchanged:** if the model is unavailable, rate-limited, or degraded, `dev-free` stops,
  reports it, and the lead re-spawns the same bounded assignment on `dev`. The dev never switches models itself.

## 2. Files that carry the binding

| File | Current state |
| --- | --- |
| `.opencode/agents/dev-free.md` | `model: opencode-go/hungerg-free`; description and comment name the temporary free binding and the revert date |
| `docs/development/model-economics.md` §6 | Binding table row `dev-free` → `opencode-go/hungerg-free`, plus a dated note; §2 rule 1 points at that note |
| `tools/tests/test_opencode_binding.py` | Exact-ID assertion plus a table cross-check that fails when an agent file and the §6 table disagree |
| `docs/planning/status.md` | Dated Notes entry that links this record and states the revert obligation |

## 3. Revert steps on 2026-12-11

On a `docs/revert-temporary-free-dev-binding` maintenance branch from `main`, with the normal owner approval for
the merge:

1. `.opencode/agents/dev-free.md`: set `model: opencode/deepseek-v4.1-flash-free`; restore a description and
   comment that describe the standard "free variant of the default tier" binding, with no temporary/date
   wording.
2. `docs/development/model-economics.md` §6: restore the table row to the standard
   `dev-free` → `opencode/deepseek-v4.1-flash-free` binding, replace the dated temporary-exception
   note with a short dated revert note (or remove it), and drop the temporary clause from §2 rule 1.
3. `tools/tests/test_opencode_binding.py`: set the exact-ID assertion back to
   `opencode/deepseek-v4.1-flash-free`; the table cross-check needs no change and must stay green.
4. `docs/planning/status.md`: add a dated Notes entry recording that the temporary binding was reverted and
   linking this record. Do not rewrite the 2026-11-27 entry.
5. Update this record's status line to record the revert date and merge commit; leave the decision history
   intact.
6. Run `python tools/check_process_consistency.py`, `python -m unittest discover -s tools/tests`,
   `./tools/verify.sh docs --require`, and `./tools/verify.sh policy --require`.

If the experiment ends early, apply the same steps on the end date and record the earlier revert date in the
status entry.

## 4. Risks

- HungerG Free is a free, time-limited model; it can be withdrawn or rate-limited before 2026-12-11.
  The fallback path is the paid `dev` binding, unchanged.
- The revert obligation is time-based, not CI-enforced, so it depends on the dated status entry and this record
  being read. `tools/tests/test_opencode_binding.py` enforces only that the agent files and the §6 table agree,
  not that the exempt binding has been reverted.
