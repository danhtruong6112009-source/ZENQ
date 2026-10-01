# BRIEFING — 2026-09-28T10:43:00Z

## Mission
Survey ZenQ Calc codebase and architecture to identify capabilities, gaps against Phases 1-4, build/test mechanisms, and report findings in survey_report.md.

## 🔒 My Identity
- Archetype: teamwork_preview_explorer
- Roles: Codebase & Architecture Explorer
- Working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_survey_1
- Original parent: ddf357e9-0181-4327-b59b-767a2b9b4c82
- Milestone: Survey Phase

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Inspect codebase at c:\Users\Danh\Desktop\genofficepy
- Write reports to c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_survey_1
- Do not modify project source code

## Current Parent
- Conversation ID: ddf357e9-0181-4327-b59b-767a2b9b4c82
- Updated: 2026-09-28T10:49:30Z

## Investigation State
- **Explored paths**: `build.bat`, `PROJECT.md`, `TEST_INFRA.md`, `src/core/SparseMatrix.cs`, `src/core/FormulaEngine.cs`, `src/ui/CalcView.cs`, `src/format/XlsxHandler.cs`, `src/format/FormatHandlers.cs`, `src/format/PdfHandler.cs`, `src/ui/AppForm.cs`, `src/ui/RibbonBar.cs`, `src/ui/FindReplaceForm.cs`, `src/ui/CalcTableAndPivotDialogs.cs`, `src/ui/CalcInsertDeleteDialogs.cs`, `src/ui/CalcHeaderFooterForm.cs`, `src/ui/InsertFunctionDialog.cs`, `scratch/run_test_calc.cs`.
- **Key findings**:
  1. OpenXML Drawings roundtrip is broken: `SaveXlsx` writes `drawing1.xml` & `media/`, but `LoadXlsx` completely ignores drawings. Reopening `.xlsx` loses 100% of shapes, images, charts.
  2. Compiler is strictly **C# 5** (`csc.exe` in .NET 4.8). Modern syntax (interpolation `$"..."`, `?.`, `=>`, local functions) causes build failure.
  3. `AppForm.cs` exports blank PDF for Calc; `CalcView.cs` needs `ExportToPdf` leveraging `PdfSharp.dll`.
  4. FormulaEngine has ~35 functions; 50+ missing functions cataloged for Phase 2.
  5. File formats `.ods`, BIFF8 `.xls`, and HTML table export are missing.
  6. Excel Tables (ListObject) lack auto-expansion, Total Row, and structured references.
  7. Dynamic arrays, spill engine (`#SPILL!`), PivotTable field list pane, Goal Seek, and workbook structure protection are missing.
- **Unexplored areas**: None remaining for the survey scope.

## Key Decisions Made
- Detailed all architectural findings and gap analysis in `survey_report.md`.
- Documented strict C# 5 syntax requirements for all subsequent implementation milestones.
- Formulated execution plan across Phases 1 to 4.

## Artifact Index
- survey_report.md — Comprehensive Survey Report (completed)
- handoff.md — 5-component handoff report (in progress)
- progress.md — Liveness heartbeat (active)

