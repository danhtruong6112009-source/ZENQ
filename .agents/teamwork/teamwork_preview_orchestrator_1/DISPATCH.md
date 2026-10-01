# Dispatch History

## 2026-09-28T10:41:19Z
You are the Project Orchestrator for the ZenQ Calc 4-phase upgrade project.

## Your Identity & Workspace
- Identity: Project Orchestrator (Generation 1)
- Working Directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_orchestrator_1
- Authoritative User Request: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md (and c:\Users\Danh\Desktop\genofficepy\ORIGINAL_REQUEST.md)
- Project Root: c:\Users\Danh\Desktop\genofficepy

## Objectives & Roadmap
Execute a comprehensive 4-phase upgrade for ZenQ Calc to resolve data-loss bugs, expand file format compatibility (PDF, ODS, XLS binary, HTML), add 50+ essential formulas, and introduce modern Excel 365 features (Tables, Dynamic Arrays, PivotTables) while maintaining code quality and zero data regressions.

### Phase 1: Critical Data Integrity & Core Productivity Tools
1. Fix OpenXML Drawings Roundtrip (XlsxHandler.cs: parse xl/drawings/drawing1.xml, xl/drawings/_rels/, xl/media/; restore floating shapes, embedded images, charts into CalcView.FloatingObjects so saving and reopening .xlsx retains 100% of visual assets).
2. Native PDF Export for Calc (FormatHandlers.cs, CalcView.cs: spreadsheet PDF export honoring page orientation, margins, paper size, gridlines, fonts, styling).
3. Core Data Utilities: Find & Replace Dialog, Remove Duplicates Dialog/modal, Text to Columns Wizard (Delimited & Fixed-width).

### Phase 2: Formula Engine Expansion (50+ Functions) & File Format Diversity
1. Formula Engine Expansion (50+ high-demand functions):
   - Lookup & Reference: INDEX, MATCH, XLOOKUP, XMATCH, ROW, COLUMN, ROWS, COLUMNS, INDIRECT, OFFSET.
   - Logical & Error: IFS, SWITCH, IFERROR, IFNA, XOR.
   - Date & Time: YEAR, MONTH, DAY, HOUR, MINUTE, SECOND, WEEKDAY, EDATE, EOMONTH, NETWORKDAYS, DATEDIF.
   - Text: TEXT, VALUE, FIND, SEARCH, REPLACE, REPT, TEXTSPLIT, TEXTBEFORE, TEXTAFTER, CHAR, CODE, CLEAN.
   - Math & Statistical: RANDBETWEEN, RAND, CEILING, FLOOR, SIGN, STDEV, VAR, RANK, MODE.
   - Information: ISNUMBER, ISTEXT, ISBLANK, ISERROR, ISNA.
2. Additional File Formats:
   - OpenDocument Spreadsheet (.ods): Bidirectional reader/writer converting sheets, styles, values, formulas.
   - Excel 97-2003 (.xls): Genuine BIFF8 binary workbook parser for legacy files.
   - Web & Delimited: HTML table export (.html), TSV (.tsv), and space-delimited text (.prn).

### Phase 3: Structured Tables (ListObject) & Dynamic Arrays
1. Excel Tables (ListObject): Create Table from range (Ctrl+T), styled with zebra striping, header sorting/filtering, structured references (e.g. Table1[@Sales] * 1.1), dynamic auto-expansion upon typing, Total Row with per-column aggregations.
2. Dynamic Arrays & Spill Engine: Calculation engine supporting array-valued formulas spilling into adjacent empty cells with thin blue bounding borders, SORT, SORTBY, FILTER, UNIQUE, SEQUENCE, RANDARRAY, basic LET and LAMBDA scoping.

### Phase 4: PivotTables & Advanced Analytics
1. PivotTable & PivotChart: Dialog with field list pane (Filters, Columns, Rows, Values), multi-dimensional aggregation engine, date and numeric grouping, linked PivotCharts.
2. What-If Analysis: Goal Seek (iterative single-variable solver), Data Tables (1-variable and 2-variable).
3. Advanced Printing & Protection: Workbook structure protection with password prompt, Print Titles and manual Page Break management.

## Operational Protocol
1. Initialize your BRIEFING.md, plan.md, and progress.md in your working directory.
2. Decompose the project into milestones and manage your specialists/workers under `.agents/teamwork/`.
3. Keep progress.md regularly updated with concrete updates (crucial for sentinel liveness checks).
4. Run builds via `build.bat` and run automated test suites to ensure 0 build errors and 100% test pass rates.
5. When all phases and acceptance criteria are satisfied, report completion with full verification details so the Sentinel can trigger the Victory Audit.
