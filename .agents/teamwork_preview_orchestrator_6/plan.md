# Execution Plan - Project Orchestrator Gen 6

## Objective
Finalize GenOffice OpenXML extraction, Word sample & ground truth generation (Milestone 2), and execute Dual Track E2E Verification & Adversarial Hardening (Milestone 4).

## Step-by-Step Plan

### Phase 1: Initialize Orchestrator State & Timers
- [x] Create `DISPATCH.md`, `BRIEFING.md`, `plan.md`, `progress.md`
- [ ] Start recurring heartbeat cron (every 10 minutes)

### Phase 2: Milestone 2 Completion (Word Sample Generator & Ground Truth)
- [ ] Dispatch Worker M2 to:
  - Run `tools/generate_word_samples.ps1` to produce canonical Word sample documents (SmartArt, Shapes, Borders, Math).
  - Implement/run `tools/extract_ground_truth.py` to unpack XML parts and build `tests/ground_truth_manifest.json`.
  - Validate that `tests/samples/` has all required samples and XML manifests.
  - Produce handoff report.
- [ ] Verify Milestone 2 deliverables and gate check.

### Phase 3: Milestone 4 Dual Track E2E Verification & Adversarial Hardening
- [ ] Dispatch E2E Verification Worker / Reviewer to:
  - Run full test suite (`build.bat` check, 0 errors).
  - Run headless rendering and geometry verification (1:1 circle aspect ratio, zero-gap outer node snapping, border styles, theme color resolution).
  - Verify against generated ground truth samples.
- [ ] Dispatch 2 Reviewers independently to assess correctness, completeness, and adherence to acceptance criteria.
- [ ] Dispatch 2 Challengers to perform adversarial stress-testing (extreme aspect ratios, stress cases, edge cases).
- [ ] Dispatch Forensic Auditor (`teamwork_preview_auditor`) for integrity verification.
- [ ] Gate evaluation in `GATE_STATUS.md`.

### Phase 4: Final Reporting & Handoff
- [ ] Synthesize all test and review results.
- [ ] Update `PROJECT.md` milestones to DONE.
- [ ] Send comprehensive final handoff message to Sentinel/Parent.
