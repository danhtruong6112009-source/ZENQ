# BRIEFING — 2026-09-28T10:56:30Z

## Mission
Formulate the exact C# 5.0 implementation strategy and technical architecture for Core Data Utilities: CalcFindReplaceDialog, CalcRemoveDuplicatesDialog, CalcTextToColumnsDialog, and their integration with CalcView.cs.

## 🔒 My Identity
- Archetype: teamwork_preview_explorer
- Roles: Core Data Utilities Specialist
- Working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_3
- Original parent: ddf357e9-0181-4327-b59b-767a2b9b4c82
- Milestone: Phase 1 Deep Exploration & Architecture Specification

## 🔒 Key Constraints
- Read-only investigation — do NOT implement source code in genofficepy directly
- Write only to working directory: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_3`
- Strict C# 5.0 compliance (target framework .NET Framework 4.5.2 / C# 5 compiler: no `?.`, no `nameof`, no string interpolation `$""`, no expression-bodied members `=>`, no pattern matching)
- Produce `utilities_plan.md` and `handoff.md`

## Current Parent
- Conversation ID: ddf357e9-0181-4327-b59b-767a2b9b4c82
- Updated: 2026-09-28T10:56:30Z

## Investigation State
- **Explored paths**:
  - `src/ui/FindReplaceForm.cs` (Writer dialog baseline)
  - `src/ui/CalcView.cs` (lines 4940-5095, 7062-7125, 3830-4100, 850-890)
  - `src/core/SparseMatrix.cs` (lines 1-707: CellData, CellType, ShiftCellsUp, DeleteRows, SetCellText, etc.)
  - `src/ui/RibbonBar.cs` (lines 11640-11680, 19820-19850, 20260-20300)
  - `src/ui/AppForm.cs` (lines 2985-3060)
  - Survey reports 1 and 2, ORIGINAL_REQUEST.md, PROJECT.md
- **Key findings**:
  - `FindReplaceForm` was hardcoded for Writer; `CalcView.OpenFindReplaceDialog` instantiated it; `ReplaceAllInSheet` directly mutated raw text without formula protection.
  - `RemoveDuplicates` in `CalcView` was an unconfigured stub with hardcoded bounds `(rowsToDelete[i], 1, 1000, 100)` and no user dialog.
  - `Text to Columns` was completely absent from both UI and logic.
  - Undo/Redo is cleanly supported via `CalcView.SaveUndoState()`, capturing full grid snapshots.
- **Unexplored areas**: None within Core Data Utilities scope; complete architecture specified.

## Key Decisions Made
- Designed `CalcFindReplaceDialog` as a floating modeless Form with 2 tabs ("Tìm kiếm" and "Thay thế"), supporting By Rows/By Cols, Match Case, Match Entire Cell, Look in Values/Formulas, and interactive Find All results ListView.
- Enforced strict Formula-Safety Rule: replacing while looking in Values skips and protects formula cells from corruption; replacing in Formulas modifies formula strings and invokes `FormulaEngine.EvaluateSheet()`.
- Designed `CalcRemoveDuplicatesDialog` as a modal dialog with auto-header detection, column checklist, multi-column row equality hashing, and reverse-order safe row deletion/shifting.
- Designed `CalcTextToColumnsDialog` with Delimited and Fixed-Width engines, consecutive delimiter collapsing, text qualifiers, collision warning, and real-time live preview `DataGridView`.
- Specified keyboard shortcuts `Ctrl+F` and `Ctrl+H` across `CalcView.HandleKeyDown` and `AppForm.ProcessCmdKey`.

## Artifact Index
- `DISPATCH.md` — Assignment instructions
- `BRIEFING.md` — Persistent situational awareness
- `progress.md` — Liveness heartbeat
- `utilities_plan.md` — Detailed technical architecture and implementation strategy for core data utilities
- `handoff.md` — 5-component handoff report
