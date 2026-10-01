# Task Assignment: Worker 2 (Shapes & SmartArt DOCX Fidelity)

## Working Directory
`c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai2_shapes_gen4`

## Specification & User Request
Read: `c:\Users\Danh\Desktop\genofficepy\.agents\ORIGINAL_REQUEST.md`
Read: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_orchestrator_4\PROJECT.md`

## File Ownership
- `src/ui/ShapeRenderer.cs`
- `src/ui/ShapesGalleryDropDownForm.cs`
- `src/ui/DocShapeObject.cs`
- `src/format/FormatHandlers.cs`
- `scratch/verify_shapes_smartart.cs`
(You MUST NOT edit files owned by Worker 1: WriterView.cs, RibbonBar.cs, AppForm.cs, scratch/verify_undo_redo_fidelity.cs)

## Objectives
1. Inspect the test suite `scratch/verify_shapes_smartart.cs` and run `scratch/verify_shapes_smartart.exe` to inspect the 5 remaining test failures:
   - Corner handle positioned at corner radius
   - TagCache preserves SmartArt Node Text Content
   - Native DrawingML diagram (data1.xml) SmartArt detection, layout1.xml mapping to Hierarchy_Tree, node extraction into TagCache in FormatHandlers.cs.
2. Fix the issues in `src/format/FormatHandlers.cs` and `src/ui/ShapeRenderer.cs` (and any related shape files you own).
3. Recompile and run `scratch/verify_shapes_smartart.exe` until 100% of the tests pass (all 40/40 tests pass with 0 failures).
4. Run `build.bat` to confirm clean build of `GenOffice.exe` (0 errors).
5. Document all verification steps, test output, and write `handoff.md` in your working directory.

## 2026-09-18T14:50:18Z
Received dispatch instructions to run `scratch/verify_shapes_smartart.exe`, fix 5 failing tests in `FormatHandlers.cs` and `ShapeRenderer.cs`, ensure all 40/40 tests pass, ensure `build.bat` passes with 0 errors, and produce `handoff.md`.
