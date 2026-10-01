# Original User Request

## 2026-09-28T10:39:51Z

Execute a comprehensive 4-phase upgrade for ZenQ Calc to resolve data-loss bugs, expand file format compatibility (PDF, ODS, XLS binary, HTML), add 50+ essential formulas, and introduce modern Excel 365 features (Tables, Dynamic Arrays, PivotTables) while maintaining code quality and zero data regressions.

Working directory: c:\Users\Danh\Desktop\genofficepy
Integrity mode: development

## Roadmap & Phased Requirements

### Phase 1: Critical Data Integrity & Core Productivity Tools
1. **Fix OpenXML Drawings Roundtrip**:
   - Update `XlsxHandler.cs` `LoadXlsx` to parse `xl/drawings/drawing1.xml`, `xl/drawings/_rels/`, and `xl/media/`.
   - Restore all floating shapes, embedded images, and charts into `CalcView.FloatingObjects` so saving and reopening `.xlsx` retains 100% of visual assets.
2. **Native PDF Export for Calc**:
   - Implement spreadsheet PDF export in `FormatHandlers.cs` and `CalcView.cs` honoring page orientation, margins, paper size, gridlines, fonts, and cell styling.
3. **Core Data Utilities**:
   - **Find & Replace Dialog**: Dedicated floating dialog supporting Match Case, Match Entire Cell, Search By Rows/Cols, and Look In Formulas/Values.
   - **Remove Duplicates**: Column-selection modal identifying and removing duplicate rows across selected ranges.
   - **Text to Columns Wizard**: Multi-step or direct dialog supporting Delimited (Tab, Semicolon, Comma, Space, Custom) and Fixed-width parsing.

### Phase 2: Formula Engine Expansion (50+ Functions) & File Format Diversity
1. **Formula Engine Expansion (50+ high-demand functions)**:
   - *Lookup & Reference*: `INDEX`, `MATCH`, `XLOOKUP`, `XMATCH`, `ROW`, `COLUMN`, `ROWS`, `COLUMNS`, `INDIRECT`, `OFFSET`.
   - *Logical & Error*: `IFS`, `SWITCH`, `IFERROR`, `IFNA`, `XOR`.
   - *Date & Time*: `YEAR`, `MONTH`, `DAY`, `HOUR`, `MINUTE`, `SECOND`, `WEEKDAY`, `EDATE`, `EOMONTH`, `NETWORKDAYS`, `DATEDIF`.
   - *Text*: `TEXT`, `VALUE`, `FIND`, `SEARCH`, `REPLACE`, `REPT`, `TEXTSPLIT`, `TEXTBEFORE`, `TEXTAFTER`, `CHAR`, `CODE`, `CLEAN`.
   - *Math & Statistical*: `RANDBETWEEN`, `RAND`, `CEILING`, `FLOOR`, `SIGN`, `STDEV`, `VAR`, `RANK`, `MODE`.
   - *Information*: `ISNUMBER`, `ISTEXT`, `ISBLANK`, `ISERROR`, `ISNA`.
2. **Additional File Format Support**:
   - **OpenDocument Spreadsheet (.ods)**: Bidirectional reader/writer converting sheets, styles, values, formulas.
   - **Excel 97-2003 (.xls)**: Genuine BIFF8 binary workbook parser for legacy files.
   - **Web & Delimited Formats**: HTML table export (`.html`), TSV (`.tsv`), and space-delimited text (`.prn`).

### Phase 3: Structured Tables (ListObject) & Dynamic Arrays
1. **Excel Tables (ListObject)**:
   - Create Table from range (`Ctrl+T`), styled with zebra striping and header sorting/filtering.
   - Structured formula references (e.g. `Table1[@Sales] * 1.1`).
   - Dynamic auto-expansion upon typing in adjacent rows/columns.
   - Optional Total Row with per-column aggregations (SUM, AVERAGE, COUNT, MIN, MAX).
2. **Dynamic Arrays & Spill Engine**:
   - Calculation engine supporting array-valued formulas spilling into adjacent empty cells with thin blue bounding borders.
   - Implement `SORT`, `SORTBY`, `FILTER`, `UNIQUE`, `SEQUENCE`, `RANDARRAY`.
   - Basic support for `LET` and `LAMBDA` scoping.

### Phase 4: PivotTables & Advanced Analytics
1. **PivotTable & PivotChart**:
   - PivotTable creation dialog with field list pane (Filters, Columns, Rows, Values).
   - Multi-dimensional aggregation engine (Sum, Count, Average, Min, Max, % of Column/Row).
   - Date and numeric grouping.
   - Linked PivotCharts reflecting dynamic pivot aggregations.
2. **What-If Analysis**:
   - **Goal Seek**: Iterative single-variable solver to reach a target cell value.
   - **Data Tables**: One-variable and two-variable sensitivity tables.
3. **Advanced Printing & Protection**:
   - Workbook structure protection with password prompt.
   - Print Titles (repeat header rows/columns on every page) and manual Page Break management.

## Acceptance Criteria

### Phase 1 Verification
- [ ] Saving an .xlsx file with 1 chart, 1 shape, and 1 picture, then reloading it through `LoadXlsx` reproduces all 3 objects at their exact cell anchor coordinates.
- [ ] Exporting a multi-sheet table to PDF produces a valid, readable PDF file matching print preview.
- [ ] Find & Replace successfully finds occurrences and replaces all matching cell values without damaging formulas.
- [ ] Remove Duplicates removes redundant rows and updates sheet row spans correctly.
- [ ] Text to Columns correctly splits comma-separated and tab-separated cell text across adjacent columns.

### Phase 2 Verification
- [ ] An automated test suite executing all 50+ new formulas passes 100% of test assertions matching Excel output.
- [ ] A sample `.ods` file opens, loads all cell contents and formatting, and saves back out cleanly.
- [ ] A legacy `.xls` (BIFF8) file opens without ZIP/XML parsing exceptions.
- [ ] Export to HTML produces clean table markup rendering accurately in standard web browsers.

### Phase 3 Verification
- [ ] Creating an Excel Table formats the range, shows filter drop-downs, and automatically extends when typing below the last row.
- [ ] Structured references evaluate correctly in FormulaEngine.
- [ ] `UNIQUE` and `SORT` formulas spill downward and rightward into empty cells, and display `#SPILL!` if blocked.

### Phase 4 Verification
- [ ] A PivotTable summarizes a 5,000-row sales dataset, permitting drag-and-drop rearrangement of row and column dimensions.
- [ ] Goal Seek converges to within 0.001 of target value for non-linear formula goals.
- [ ] The solution builds cleanly (`build.bat`) with 0 compilation errors.
