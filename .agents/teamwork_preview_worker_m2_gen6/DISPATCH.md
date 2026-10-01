# Dispatch for Worker M2 Gen 6

Target Milestone: Milestone 2 (Word Sample Generator & Ground Truth Pipeline)
Working Directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m2_gen6
Workspace Root: c:\Users\Danh\Desktop\genofficepy
Reference Documents:
- c:\Users\Danh\Desktop\genofficepy\.agents\ORIGINAL_REQUEST.md (read section ## 2026-09-21T12:58:21Z)
- c:\Users\Danh\Desktop\genofficepy\PROJECT.md
- c:\Users\Danh\Desktop\genofficepy\TEST_INFRA.md
- c:\Users\Danh\Desktop\genofficepy\tools\generate_word_samples.ps1
- c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m2\progress.md

Write Ownership:
- `tools/generate_word_samples.ps1`
- `tools/extract_ground_truth.py`
- `tests/` directory and generated sample files

Mandatory Integrity Warning:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## 2026-09-21T14:00:34Z
You are Worker M2 Gen 6 for Milestone 2 (Word Sample Generator & Ground Truth Pipeline) of GenOffice.

Your working directory is: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m2_gen6`.
Workspace root: `c:\Users\Danh\Desktop\genofficepy`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

MANDATORY REFERENCE DOCUMENTS:
You MUST read:
- `c:\Users\Danh\Desktop\genofficepy\.agents\ORIGINAL_REQUEST.md` (read section `## 2026-09-21T12:58:21Z`)
- `c:\Users\Danh\Desktop\genofficepy\PROJECT.md`
- `c:\Users\Danh\Desktop\genofficepy\TEST_INFRA.md`
- `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m2_gen6\DISPATCH.md`
- `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m2\progress.md`

Write Ownership:
You own exclusively:
- `tools/generate_word_samples.ps1`
- `tools/extract_ground_truth.py`
- `tests/` directory and generated samples

Objective:
1. Initialize your BRIEFING.md, and progress.md in your working directory `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m2_gen6`.
2. Inspect `tools/generate_word_samples.ps1`. Execute it to generate the comprehensive Word canonical samples (200+ SmartArt layouts, full Shape library, multi-style table borders, OMML formulas) in `tests/samples/` (`smartart`, `suites`, `scenarios`).
3. If Word COM has any quirks or if any adjustments are needed, fix `tools/generate_word_samples.ps1` properly so all samples are generated.
4. Implement `tools/extract_ground_truth.py` to unpack DOCX parts (`drawing1.xml`, `data1.xml`, `layout1.xml`, `theme1.xml`, `document.xml`) and compile `tests/ground_truth_manifest.json` indexing all extracted attributes (bounding boxes, layout types, border styles, math tags). Run it and ensure the manifest is generated and valid.
5. Verify that all samples and ground truth files are produced and verified.
6. Write `changes.md` and `handoff.md` in your working directory, and send a completion message with summary back to your parent orchestrator.
