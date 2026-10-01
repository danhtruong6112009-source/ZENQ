# Project: GenOffice Shapes, SmartArt & Multi-Tab Undo/Redo

## Architecture
- `src/ui/WriterView.cs`: Core word processor surface, containing `WriterUndoSnapshot`, Undo/Redo stacks, deep snapshot cloning, and selection/table/shape state restoration.
- `src/ui/RibbonBar.cs`: Top ribbon bar and tab management (`DocTab`), holding isolated `UndoStack` and `RedoStack` per document tab.
- `src/ui/AppForm.cs`: Main application container, tab switching orchestration, save/restore of tab state during tab changes.
- `src/ui/ShapesGalleryDropDownForm.cs`: Shape gallery popup with 9 standard MS Office categories.
- `src/ui/ShapeRenderer.cs` & `DocShapeObject.cs`: Vector rendering, monoline graphics, in-shape text rendering, interactive resize/rotation/adjustment handles.
- `src/format/FormatHandlers.cs`: DOCX package handling, OpenXML DrawingML diagram/SmartArt detection, diagram data extraction (`data1.xml`, `layout1.xml`), `TagCache` population, and export.

## Code Layout & File Boundaries
- Worker 1 Ownership:
  - `src/ui/WriterView.cs`
  - `src/ui/RibbonBar.cs`
  - `src/ui/AppForm.cs`
  - `scratch/verify_undo_redo_fidelity.cs`
- Worker 2 Ownership:
  - `src/ui/ShapeRenderer.cs`
  - `src/ui/ShapesGalleryDropDownForm.cs`
  - `src/ui/DocShapeObject.cs`
  - `src/format/FormatHandlers.cs`
  - `scratch/verify_shapes_smartart.cs`

## Feature Inventory
| # | Feature | Description | Milestone | Status |
|---|---------|-------------|-----------|--------|
| 1 | Tab History Stack Isolation | Each DocTab has independent UndoStack and RedoStack. Switching tabs preserves history without leakage across tabs. | M1 | In Progress |
| 2 | Deep Snapshot Fidelity | WriterUndoSnapshot deeply clones text, tables, shapes, cursor, selection, alignments, indents. Restoring snapshot preserves all formatting. | M1 | In Progress |
| 3 | Undo/Redo Test Suite | `scratch/verify_undo_redo_fidelity.cs` compiles and runs with 100% PASS. | M1 | Pending |
| 4 | Corner Handle Position | Adjustment handle for rounded rectangle corner radius positioned correctly. | M2 | In Progress |
| 5 | TagCache SmartArt Node Text Content | Shape TagCache preserves node text content for SmartArt shapes during extraction. | M2 | In Progress |
| 6 | Native DrawingML SmartArt Detection | FormatHandlers.cs detects diagram `data1.xml`, maps `layout1.xml` to `Hierarchy_Tree`, extracts nodes into TagCache. | M2 | In Progress |
| 7 | Shapes/SmartArt Test Suite | `scratch/verify_shapes_smartart.exe` runs with 100% PASS (40/40 tests). | M2 | In Progress |

## Acceptance Criteria
1. `build.bat` builds cleanly with 0 errors.
2. `scratch/verify_undo_redo_fidelity.exe` executes with 100% PASS.
3. `scratch/verify_shapes_smartart.exe` executes with 100% PASS (40/40 tests, 0 failures).
4. No regression on core Writer features.
