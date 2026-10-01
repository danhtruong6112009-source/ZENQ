# Task Assignment: Worker 1 (Undo/Redo Per-Tab & Deep Snapshot Fidelity)

## Working Directory
`c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai1_undo_gen4`

## Specification & User Request
Read: `c:\Users\Danh\Desktop\genofficepy\.agents\ORIGINAL_REQUEST.md`
Read: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_orchestrator_4\PROJECT.md`

## File Ownership
- `src/ui/WriterView.cs`
- `src/ui/RibbonBar.cs`
- `src/ui/AppForm.cs`
- `scratch/verify_undo_redo_fidelity.cs`
(You MUST NOT edit files owned by Worker 2: ShapeRenderer.cs, ShapesGalleryDropDownForm.cs, DocShapeObject.cs, FormatHandlers.cs, scratch/verify_shapes_smartart.cs)

## Objectives
1. Verify per-tab Undo/Redo isolation in `AppForm.cs` and `RibbonBar.cs` (`SaveTabState` / `LoadTabState`). When switching tabs or operating across multiple tabs, undo/redo stacks must remain completely isolated without cross-contamination.
2. Verify deep snapshot fidelity in `WriterView.cs` (`WriterUndoSnapshot`), ensuring text, tables, shapes, cursor, selection, alignments, indents, margins are fully restored on Undo (Ctrl+Z) and Redo (Ctrl+Y).
3. Inspect, fix compilation issues in `scratch/verify_undo_redo_fidelity.cs`, compile it with `csc.exe`, run `scratch/verify_undo_redo_fidelity.exe`, and ensure 100% of the tests pass.
4. Run `build.bat` to confirm clean build of `GenOffice.exe` (0 errors).
5. Document all verification steps, test output, and write `handoff.md` in your working directory.
