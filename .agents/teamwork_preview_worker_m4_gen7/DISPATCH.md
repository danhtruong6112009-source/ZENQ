## 2026-09-21T14:44:50Z
# Dispatch for Worker M4 (Generation 7 - Final Sprint)

Milestone: Milestone 4 (Final E2E Verification & Handover)
Working Directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m4_gen7
Workspace Root: c:\Users\Danh\Desktop\genofficepy

Reference Documents:
- c:\Users\Danh\Desktop\genofficepy\.agents\ORIGINAL_REQUEST.md (read ## 2026-09-21T12:58:21Z and ## 2026-09-21T14:25:50Z)
- c:\Users\Danh\Desktop\genofficepy\PROJECT.md
- c:\Users\Danh\Desktop\genofficepy\TEST_INFRA.md
- c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m1\handoff.md
- c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m3\handoff.md
- c:\Users\Danh\Desktop\genofficepy\tests\ground_truth_manifest.json
- c:\Users\Danh\Desktop\genofficepy\tests\samples\

Mandatory Integrity Warning:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Objectives:
1. Initialize BRIEFING.md and progress.md in your working directory.
2. Run `cmd.exe /c "build.bat"` and verify that `GenOffice.exe` builds cleanly with 0 errors.
3. Run Milestone 1 verification tests (`scratch/test_milestone1_verification.cs`, 53 tests) and Milestone 3 verification tests (`scratch/test_milestone3_verification.cs`, 99 tests).
4. Run or create an automated C# or Python verification test to test the full Acceptance Criteria on `tests/ground_truth_manifest.json` and the unpacked ground truth files in `tests/ground_truth/`:
   - Verify 1:1 circle geometry (Aspect Ratio = 1.000 ± 0.005) for all Cycle and Radial layouts.
   - Verify zero-gap tangential docking between central core hub and outer satellites.
   - Verify OpenXML DrawingML diagram parsing and theme color palette resolution.
   - Verify Table borders (solid, dash, dot, double, 3d) and OMML math formula extraction.
5. Record complete test outputs, logs, pass/fail counts, and verification method in `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m4_gen7\handoff.md`.
6. Send completion message back to orchestrator.
