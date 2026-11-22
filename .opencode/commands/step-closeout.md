---
description: Run the step closeout, audit, and merge-approval sequence
agent: lead
---

Read `AGENTS.md` §3.4 and §3.10, `docs/development/orchestration.md`, and `docs/planning/closeout/README.md`.

Close out step `$1`. The argument is `<NN-slug>`; use the zero-padded `NN` for record and evidence names and the
full `NN-slug` for the step branch and tag.

1. spawn `dev-free` (fallback `dev` on a rate limit, a degraded model, or unavailability) to finish the closeout
   feature: status, documentation, deviations, parity; merge the current `main` into `step/NN-slug`; run
   `./tools/verify.sh all --require`; push the branch; dispatch one closeout CI run and record its run id and
   `head_sha`;
2. the dev runs the pre-audit gate in `docs/development/design-proportionality.md` §3, fills
   `docs/planning/closeout/step-NN.json` (audited commit, closeout CI, `main` SHA at freeze, merge candidate,
   merge-tree CI, platform CI, tag), runs `python tools/check_closeout_evidence.py
   docs/planning/closeout/step-NN.json`, and creates or updates `docs/planning/audits/step-NN.md` with the request
   fields, status `Ready for audit`, and a link to the evidence file. Do not restate the SHAs in the record;
3. freeze the branch at that commit;
4. spawn `review` (the audit uses the same review agent as design review) with the frozen commit, the record, and
   repository links only;
5. if the audit returns `Fixes required`, route the findings to the dev, dispatch a replacement closeout run, and
   spawn a fresh `review` for a new round;
6. present the cumulative approval report and stop for my approval before the `--no-ff` merge and the `step-NN`
   tag.

Do not merge, tag, push `main`, edit any file, or write the record yourself. The evidence check and the audit must
pass before step 6.
