# Handoff Report: Survey Agent 3 (Phases 3 & 4 Spec & Architecture Miner)

## 1. Observation
1. **Build Environment & Compiler Target**:
   - `build.bat` invokes:
     `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize /main:GenOffice.Program ... /out:GenOffice.exe /recurse:src\*.cs`
   - Command execution output:
     `Microsoft (R) Visual C# Compiler version 4.8.9232.0 for C# 5`
     `[SUCCESS] GenOffice.exe built successfully!`
     Codebase compiles with exit code 0 under C# 5 syntax restrictions.

2. **Subsystem 1: Excel Tables (ListObject)**:
   - File `src/ui/CalcTableAndPivotDialogs.cs` (lines 11-63) defines `ExcelTableInfo` with properties:
     `Name, StartRow, EndRow, StartCol, EndCol, HasHeaders, HasTotalRow, BandedRows, FirstColumn, LastColumn, BandedColumns, ShowFilterButtons, StyleIndex, ColumnFilters, HeaderFilterButtonBounds, ResizeCornerBounds`.
   - File `src/ui/CalcView.cs` (lines 5095-5340) defines table manipulation methods:
     `IsSelectionInsideTable`, `AutoExpandTable`, `ApplyTableStyle`, `ToggleTableTotalRow`, `ConvertTableToRange`, `RemoveTableDuplicates`.
   - In `CalcView.CommitEditAndMove` (lines 3799-3819):
     `AutoExpandTable` is **not** called upon committing an edit into adjacent cells (`EndRow + 1` or `EndCol + 1`).
   - In `CalcView.HandleKeyDown` (lines 3828-4095):
     `Ctrl+T` or `Ctrl+L` is **not** handled to open `CreateTableDialogForm`.
   - In `FormulaEngine.cs` (lines 100-1447):
     No parsing or resolution for structured references (`Table1[@Col]`, `Table1[Col]`, `Table1[[#Totals],[Col]]`).
   - In `src/format/XlsxHandler.cs` (lines 780-860):
     `xl/tables/table{N}.xml` and sheet `<tableParts>` are not written or read.

3. **Subsystem 2: Dynamic Arrays & Spill Engine**:
   - File `src/core/FormulaEngine.cs`:
     `EvaluateFormula` returns scalar `CellData` (`res.NumericValue`, `res.EvaluatedStr`). There is no `ArrayValue` return type or 2D matrix evaluation.
   - Functions `SORT`, `SORTBY`, `FILTER`, `UNIQUE`, `SEQUENCE`, `RANDARRAY`, `LET`, and `LAMBDA` do not exist in `FormulaEngine.cs`.
   - In `src/core/SparseMatrix.cs`:
     `CellData` contains no spill metadata (`IsSpillAnchor`, `IsSpillTarget`, `SpillWidth`, `SpillHeight`, `IsSpillBlocked`).
   - In `src/ui/CalcView.cs` (lines 1653-1675):
     `Draw` renders the green selection rectangle for user selection, but does not render the modern Excel thin blue bounding rectangle for dynamic array spills or dashed border for `#SPILL!` collisions.

4. **Subsystem 3: PivotTables & PivotCharts**:
   - File `src/ui/CalcTableAndPivotDialogs.cs` (lines 157-340):
     `CreatePivotTableDialogForm` collects source range, new sheet vs existing sheet location.
   - File `src/ui/CalcView.cs` (lines 6116-6215):
     `CreatePivotTable` writes a static summary table with fixed row labels ("Mục 1", "Mục 2") and simple sum values. It does not provide an interactive multi-dimensional aggregation engine.
   - No Field List Task Pane (`PivotTableFieldListControl`) exists with the 4 drag-drop zones (Filters, Columns, Rows, Values).
   - No date or numeric grouping logic exists.
   - In `XlsxHandler.cs`:
     No handling for `xl/pivotTables/pivotTable{N}.xml` or `xl/pivotCache/`.

5. **Subsystem 4: What-If Analysis**:
   - Grep search for `GoalSeek` and `DataTable` in `src/` yielded 0 results.
   - Neither single-variable iterative solver nor 1-var / 2-var sensitivity table engines are present.

6. **Subsystem 5: Advanced Printing & Protection**:
   - File `src/ui/ProtectSheetDialog.cs` and `CalcView.ToggleProtectSheet` implement sheet-level cell protection, but Workbook Structure Protection (`IsWorkbookProtected`, locking sheet additions, deletions, renames, and moves) is absent.
   - File `src/ui/CalcView.cs` (lines 302-306, 7127-7139):
     `HasPrintArea, PrintAreaStartRow, PrintAreaStartCol, PrintAreaEndRow, PrintAreaEndCol` exist for print area, but Print Titles (`PrintTitleRows`, `PrintTitleCols`) and manual Page Breaks (`RowPageBreaks`, `ColPageBreaks`) are missing.
   - In `XlsxHandler.cs`:
     `<workbookProtection>`, `_xlnm.Print_Titles`, and `<rowBreaks>`/`<colBreaks>` are not saved or loaded.

## 2. Logic Chain
1. *From Observation 1*: The build pipeline uses C# 5 (`csc.exe` in .NET 4.0/4.8). Therefore, all architectural specifications and subsequent implementations for Phases 3 & 4 must strictly adhere to C# 5 language constructs (avoid string interpolation `$"..."`, null-conditional `?.`, expression-bodied members `=>`, and pattern matching).
2. *From Observation 2*: While basic UI tables exist in `CalcTableAndPivotDialogs.cs` and `CalcView.cs`, they lack full Excel ListObject parity. Specifically:
   - Wiring `Ctrl+T` in `CalcView.HandleKeyDown` is required to invoke `CreateTableDialogForm`.
   - Calling `AutoExpandTable` inside `CalcView.CommitEditAndMove` is necessary to dynamically expand tables when typing in adjacent cells.
   - Extending `ExcelTableInfo` with `List<ExcelTableColumnInfo>` and `TotalsRowFunction` is required to allow per-column aggregations (SUM, AVERAGE, COUNT, MIN, MAX).
   - Adding a structured reference pre-processor in `FormulaEngine` is required to translate expressions like `Table1[@Sales]` into absolute coordinates before numeric evaluation.
   - Adding table XML serialization in `XlsxHandler.cs` is required to prevent data and format loss upon saving `.xlsx`.
3. *From Observation 3*: Dynamic arrays require fundamentally extending the formula pipeline:
   - An `ArrayValue` (rows x cols) class is needed so functions can return 2D matrices.
   - A Spill Engine is required to detect collision with non-empty cells, return `#SPILL!` when blocked, write results to ghost/spilled cells when clean, and resolve the `#` spill operator (e.g. `A1#`).
   - Vector drawing of a thin blue rectangle in `CalcView.Draw` is needed to provide visual feedback matching modern Excel.
   - Implementation of 6 array functions (`SORT`, `SORTBY`, `FILTER`, `UNIQUE`, `SEQUENCE`, `RANDARRAY`) and 2 scoping functions (`LET`, `LAMBDA`) in `FormulaEngine`.
4. *From Observation 4*: PivotTables currently emit a static mockup. A real multi-dimensional pivot engine requires:
   - A `PivotCache` extracting unique field members and records.
   - A multi-dimensional aggregator computing cross-tabulations across Row and Column dimensions with Subtotals, Grand Totals, and measures (Sum, Count, Average, % of Row/Col).
   - A `PivotTableFieldListControl` docking pane with drag-and-drop support across 4 zones.
   - Date and numeric grouping dialog (`GroupDialogForm`).
   - Linked PivotCharts reflecting dynamic pivot updates.
5. *From Observation 5*: What-If Analysis requires:
   - `GoalSeekDialogForm` and a Secant-based iterative numerical solver converging to within 0.001 tolerance within 100 iterations.
   - `DataTableDialogForm` and a sensitivity engine supporting 1-variable (row or column) and 2-variable sensitivity substitution grids.
6. *From Observation 6*: Advanced Printing & Protection requires:
   - `IsWorkbookProtected` model and `ProtectWorkbookDialogForm` with password hashing, disabling sheet tab operations (`AddSheet`, `DeleteSheet`, `RenameSheet`, `DuplicateSheet`, sheet reordering).
   - Print Titles model (`PrintTitleRows`, `PrintTitleCols`) repeated across pages in print/PDF output and stored via `_xlnm.Print_Titles`.
   - Manual Page Breaks model (`RowPageBreaks`, `ColPageBreaks`), visual dashed line rendering, and OpenXML `<rowBreaks>`/`<colBreaks>` roundtripping.

## 3. Caveats
- No implementation code was written during this survey turn; this investigation was strictly read-only per the Spec Miner mandate.
- OpenXML ISO/IEC 29500 schemas were mined from official specifications and existing handlers; actual binary packing will require adding new zip entries in `XlsxHandler.cs`.
- No other caveats.

## 4. Conclusion
The codebase already has initial foundational hooks for Tables, Sheet Protection, and Print Areas, but completely lacks Dynamic Arrays/Spill Engine, Structured References, true Multi-Dimensional PivotTables, What-If Analysis, Workbook Protection, Print Titles, and Page Breaks.

The technical specifications, data models, algorithm pseudocode, UI designs, and OpenXML schemas have been comprehensively documented in `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_3\spec_report.md`.

## 5. Verification Method
1. Inspect `spec_report.md` in `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_3\spec_report.md`.
2. Verify clean build of the current baseline:
   Run `.\build.bat` from `c:\Users\Danh\Desktop\genofficepy`. Expected: exit code 0, `GenOffice.exe` generated.
3. Invalidation Conditions:
   - If any proposed feature requires C# 6+ syntax that fails compilation on .NET 4.8 `csc.exe`.
   - If proposed data models omit any required Phase 3 or Phase 4 requirements from `ORIGINAL_REQUEST.md`.
