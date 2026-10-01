# Dispatch for Worker M4 (E2E Verification & Acceptance Testing)

Milestone: Milestone 4 (Dual Track E2E Verification & Acceptance Criteria Validation)
Working Directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m4
Workspace Root: c:\Users\Danh\Desktop\genofficepy

Reference Documents:
- c:\Users\Danh\Desktop\genofficepy\.agents\ORIGINAL_REQUEST.md (read section ## 2026-09-21T12:58:21Z)
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
2. Run `build.bat` and verify that `GenOffice.exe` builds cleanly with 0 errors.
3. Run Milestone 1 verification tests (`scratch/test_milestone1_verification.cs`) and Milestone 3 verification tests (`scratch/test_milestone3_verification.cs`).
4. Execute comprehensive E2E verification test harness on generated canonical samples in `tests/samples/` (`smartart`, `suites`, `scenarios`):
   - Verify 1:1 circle aspect ratio across all Cycle/Radial diagrams (aspect ratio = 1.000 ± 0.005).
   - Verify zero-gap tangential docking between central core hub and outer satellites.
   - Verify table borders (solid, dash, dot, double, 3d) and OpenXML border cascade.
   - Verify OMML Math parsing and Rendering.
   - Verify DrawingML diagram and theme color mapping.
5. Provide detailed test results, pass/fail counts, and verification logs in `handoff.md`.
