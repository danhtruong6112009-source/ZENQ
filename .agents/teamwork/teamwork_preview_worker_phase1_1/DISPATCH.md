# DISPATCH: Phase 1 Worker (Implementation Specialist)

**Role**: Phase 1 Implementation Specialist
**Type**: teamwork_preview_worker
**Working Directory**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_worker_phase1_1
**Authoritative User Request**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md
**Project Index**: c:\Users\Danh\Desktop\genofficepy\PROJECT.md
**Explorer Blueprints**:
- OpenXML Drawings: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_1\drawings_plan.md`
- Native PDF Export: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_2\pdf_plan.md`
- Core Data Utilities: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_3\utilities_plan.md`
**Project Root**: c:\Users\Danh\Desktop\genofficepy

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Strict Compiler Rule
- Strictly enforce **C# 5.0** syntax (`csc.exe` in `build.bat`). Prohibited: `$"..."`, `?.`, `=>`, local functions, pattern matching, tuple literals, inline `out var`.
- You MUST run `cmd.exe /c build.bat` to verify 0 compilation errors and successful `GenOffice.exe` generation.

## Implementation Tasks

### 1. OpenXML Drawings Roundtrip
- In `src/format/XlsxHandler.cs`:
  - Implement drawing parts loading in `LoadXlsx`: inspect sheet relationships (`xl/worksheets/_rels/sheet{N}.xml.rels`) to find `drawing{M}.xml`.
  - Parse `drawing{M}.xml`: extract `<xdr:twoCellAnchor>` and `<xdr:oneCellAnchor>`, coordinates `<xdr:from>` and `<xdr:to>`.
  - Map DrawingML EMUs to pixel coordinates: `1 px = 9,525 EMUs`. Compute exact `PageX`, `PageY`, `BaseWidth`, `BaseHeight` using sheet column widths and row heights.
  - Parse `<xdr:pic>`: resolve `r:embed` to `xl/drawings/_rels/` to load image bytes from `xl/media/`.
  - Parse `<xdr:sp>`: map shapes to `DocShapeObject`.
  - Parse `<xdr:graphicFrame>`: parse `xl/charts/chart*.xml` to restore `DocChartData` (or fallback to chart snapshot).
  - Add restored objects to `sheetState.Shapes` and active sheet `calcView.Shapes`.
- In `src/ui/CalcView.cs`:
  - Add public property `FloatingObjects` on `CalcView` and `CalcSheetState` aliasing `Shapes`:
    ```csharp
    public List<DocShapeObject> FloatingObjects { get { return Shapes; } set { Shapes = value; } }
    ```

### 2. Native PDF Export for Calc
- Implement `src/format/CalcPdfExporter.cs` (namespace `GenOffice.Format`) using `PdfSharp.dll` per `pdf_plan.md`.
  - Support page size (A4, Letter, Legal, A3), orientation (Landscape / Portrait), margins.
  - Render cell text, fonts, bold, italic, alignments, text colors, background fills, cell borders (thin, medium, double).
  - Implement 2D pagination banding ("Down, then Over") with repeated header row.
  - Render embedded pictures and floating shapes/charts onto PDF pages.
- In `src/ui/CalcView.cs`:
  - Implement `public bool ExportToPdf(string filePath, bool exportAllSheets = false)`.
- In `src/ui/AppForm.cs` (around line 4200):
  - In `PerformExportPdf`: add branch for `OfficeAppMode.Calc` calling `CalcView.ExportToPdf(sfd.FileName)`.

### 3. Core Data Utilities
- Implement `src/ui/CalcFindReplaceDialog.cs`:
  - Floating modeless dialog with Find and Replace tabs.
  - Support Match Case, Match Entire Cell, Search By Rows / Columns, Look In Formulas / Values.
  - Find Next, Find Previous, Find All (results list), Replace, Replace All.
  - STRICT formula-safety: when Look In Values is selected, do NOT overwrite or corrupt formula cells!
- Implement `src/ui/CalcRemoveDuplicatesDialog.cs`:
  - Modal dialog with "My data has headers" checkbox and column checklist.
  - Multi-column row equality hashing, reverse-order safe row deletion/shifting.
  - Summary message: "X duplicate values found and removed; Y unique values remain."
- Implement `src/ui/CalcTextToColumnsDialog.cs`:
  - Modal wizard with Delimited (Tab, Semicolon, Comma, Space, Custom, consecutive delimiters) and Fixed-Width parsing.
  - Live preview DataGridView, destination column specification, collision warning.
- In `src/ui/CalcView.cs`, `src/ui/AppForm.cs`, and `src/ui/RibbonBar.cs`:
  - Wire dialog invocation methods and keyboard shortcuts (`Ctrl+F`, `Ctrl+H`).
  - Wire Ribbon Data tab buttons (Remove Duplicates, Text to Columns).

## Verification Requirements
- Run `cmd.exe /c build.bat` and document exit code and output in your report.
- Verify that GenOffice.exe compiles with 0 errors.
- Deliver `handoff.md` with Observation, Logic Chain, Caveats, Conclusion, and Verification results.

## 2026-09-28T10:58:24Z
You are the Phase 1 Implementation Specialist.
Your working directory is: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_worker_phase1_1
Your task assignment is in: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_worker_phase1_1\DISPATCH.md
The project index is: c:\Users\Danh\Desktop\genofficepy\PROJECT.md
The authoritative user request is: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Read DISPATCH.md, PROJECT.md, and the explorer blueprints in drawings_plan.md, pdf_plan.md, and utilities_plan.md.
Implement:
1. OpenXML Drawings Roundtrip in src/format/XlsxHandler.cs and CalcView.FloatingObjects.
2. Native PDF Export in src/format/CalcPdfExporter.cs, CalcView.cs, and AppForm.cs.
3. Core Data Utilities in src/ui/CalcFindReplaceDialog.cs, src/ui/CalcRemoveDuplicatesDialog.cs, src/ui/CalcTextToColumnsDialog.cs, and wire them into CalcView.cs, RibbonBar.cs, and AppForm.cs.

Ensure all code strictly follows C# 5.0 syntax. Run cmd.exe /c build.bat to verify 0 errors.
When complete, write handoff.md and send a completion message to parent.

