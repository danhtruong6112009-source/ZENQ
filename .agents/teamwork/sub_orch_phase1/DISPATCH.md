# DISPATCH: Phase 1 Sub-Orchestrator

**Role**: Phase 1 Sub-Orchestrator
**Type**: teamwork_preview_orchestrator
**Working Directory**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\sub_orch_phase1
**Parent Conversation ID**: ddf357e9-0181-4327-b59b-767a2b9b4c82
**Authoritative User Request**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md
**Project Index**: c:\Users\Danh\Desktop\genofficepy\PROJECT.md
**Survey Reports**:
- Codebase Survey: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_survey_1\survey_report.md`
- Phase 1 & 2 Spec: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_2\spec_report.md`
**Project Root**: c:\Users\Danh\Desktop\genofficepy

## Milestone Scope: Phase 1 (Critical Data Integrity & Core Productivity)
1. **OpenXML Drawings Roundtrip**:
   - `XlsxHandler.cs`: Implement drawing extraction in `LoadXlsx` by parsing `xl/worksheets/_rels/sheet1.xml.rels`, `xl/drawings/drawing1.xml`, `xl/drawings/_rels/drawing1.xml.rels`, and `xl/media/`.
   - Calculate exact cell coordinates from DrawingML EMUs (1 pixel = 9,525 EMUs) for `twoCellAnchor` and `oneCellAnchor`.
   - Restore floating shapes, embedded images, and charts into `CalcView.FloatingObjects` (and `Shapes`).
   - Guarantee that saving and reopening an `.xlsx` file with 1 chart, 1 shape, and 1 picture reproduces all 3 objects at their exact cell anchor coordinates.
2. **Native PDF Export for Calc**:
   - In `FormatHandlers.cs` and `CalcView.cs` (and hooked in `AppForm.cs`), implement spreadsheet vector PDF export using `PdfSharp.dll`.
   - Honor page orientation, margins, paper size, gridlines, fonts, and cell styling.
3. **Core Data Utilities**:
   - **Find & Replace Dialog**: Dedicated floating dialog (`CalcFindReplaceDialog.cs`) supporting Match Case, Match Entire Cell, Search By Rows/Cols, Look In Formulas/Values.
   - **Remove Duplicates**: Column-selection modal (`CalcRemoveDuplicatesDialog.cs`) identifying and removing duplicate rows across selected ranges.
   - **Text to Columns Wizard**: Multi-step or direct dialog (`CalcTextToColumnsDialog.cs`) supporting Delimited (Tab, Semicolon, Comma, Space, Custom) and Fixed-width parsing.

## Strict Compiler Constraint
- Strictly enforce **C# 5.0** compatibility (`csc.exe` in `build.bat`). Zero C# 6+ syntax.
- Verify clean compilation with `build.bat` after any changes.

## Orchestration Procedure
- Create `SCOPE.md`, `BRIEFING.md`, `progress.md` in your working directory.
- Assess and decompose Phase 1 into sub-milestones or run the standard iteration loop:
  * Spawn Explorer(s)
  * Spawn Worker (include mandatory integrity warning)
  * Spawn Reviewers (2)
  * Spawn Challengers (2)
  * Spawn Forensic Auditor (`teamwork_preview_auditor`)
  * Gate check (strict AND: build passes, reviewers approve, challengers approve, auditor CLEAN)
- Report completion and handoff back to parent orchestrator.
