# Project: ZenQ Calc 4-Phase Upgrade

## Architecture
- **Host Application**: ZenQ Office (GenOffice), a C# WinForms application built with .NET Framework 4.8.
- **Language & Compiler Strictness**: Strict **C# 5.0** compliance enforced by `csc.exe` (Microsoft (R) Visual C# Compiler version 4.8.9232.0 for C# 5). Prohibited: string interpolation `$"..."`, expression-bodied members `=>`, null-conditional `?.`, local functions, pattern matching, tuple literals, inline `out var`.
- **Core Subsystems**:
  1. **UI Layer** (`src/ui/`):
     - `CalcView.cs`: Main spreadsheet canvas, grid painting, mouse/keyboard input, selection, shapes/floating objects, zoom, page breaks, spill border rendering.
     - `CalcTableAndPivotDialogs.cs`: Dialogs for table creation, formatting, and pivot configuration.
     - `CalcFindReplaceDialog.cs`, `CalcRemoveDuplicatesDialog.cs`, `CalcTextToColumnsDialog.cs`: Core productivity dialogs.
     - `PivotTableFieldListControl.cs`: Interactive 4-zone drag-and-drop field list pane.
     - `GoalSeekDialogForm.cs`, `DataTableDialogForm.cs`, `ProtectWorkbookDialogForm.cs`: Analytics and protection modals.
     - `AppForm.cs`, `RibbonBar.cs`: Top-level ribbon dispatch, file menus, export commands.
  2. **Core Data & Calculation Engine** (`src/core/`):
     - `SparseMatrix.cs`: Sheet cell storage, formatting, row/col metadata, comments, merge cells, spill metadata.
     - `FormulaEngine.cs`: Formula parser, tokenization, dependency evaluation, 52 new built-in functions, dynamic array evaluation, structured table reference resolution (`Table1[@Sales]`), iterative numerical solving.
  3. **Format & Serialization Layer** (`src/format/`):
     - `XlsxHandler.cs`: OpenXML spreadsheet packaging (`.xlsx`), shared strings, styles, sheets, drawings (`drawing1.xml`, `_rels`, `media`), tables (`xl/tables/table{N}.xml`), pivot caches, workbook protection.
     - `FormatHandlers.cs`: PDF export bridge for Calc using `PdfSharp.dll`.
     - `OdsHandler.cs`: OpenDocument Spreadsheet (`.ods`) ZIP/XML reader and writer.
     - `XlsBiff8Handler.cs`: Genuine Excel 97-2003 (`.xls`) OLE2 Compound Document and BIFF8 binary record parser.
     - `DelimitedAndHtmlHandlers.cs`: HTML table export (`.html`), TSV (`.tsv`), and space-delimited text (`.prn`).

---

## Feature Inventory
Every feature from the survey and user request is enumerated below with its assigned milestone:

| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | OpenXML Drawings Parser | Parse `xl/drawings/drawing1.xml`, `xl/drawings/_rels/`, `xl/media/` in `XlsxHandler.LoadXlsx` | M1 | ORIGINAL_REQUEST §1.1 |
| 2 | Drawings EMU Coordinate Transform | Map DrawingML EMUs (1 px = 9,525 EMUs) and twoCellAnchor / oneCellAnchor coordinates | M1 | Survey Report 2 |
| 3 | Visual Assets Roundtrip | Restore shapes, embedded images, and charts into `CalcView.FloatingObjects` / `Shapes` without data loss | M1 | ORIGINAL_REQUEST §1.1 |
| 4 | Native PDF Export Engine | Vector PDF generation in `FormatHandlers.cs` / `CalcView.cs` honoring orientation, paper size, margins | M1 | ORIGINAL_REQUEST §1.2 |
| 5 | PDF Styling & Gridlines | Render cell gridlines, fonts, backgrounds, borders, and multi-page row/column pagination | M1 | ORIGINAL_REQUEST §1.2 |
| 6 | Find & Replace Dialog | Dedicated floating dialog supporting Match Case, Match Entire Cell, Search By Rows/Cols, Formulas/Values | M1 | ORIGINAL_REQUEST §1.3 |
| 7 | Remove Duplicates Modal | Column-selection checklist dialog identifying and purging duplicate rows in ranges | M1 | ORIGINAL_REQUEST §1.3 |
| 8 | Text to Columns Wizard | Wizard dialog supporting Delimited (Tab, Semicolon, Comma, Space, Custom) and Fixed-width parsing | M1 | ORIGINAL_REQUEST §1.3 |
| 9 | Formulas: Lookup & Reference | `INDEX`, `MATCH`, `XLOOKUP`, `XMATCH`, `ROW`, `COLUMN`, `ROWS`, `COLUMNS`, `INDIRECT`, `OFFSET` | M2 | ORIGINAL_REQUEST §2.1 |
| 10 | Formulas: Logical & Error | `IFS`, `SWITCH`, `IFERROR`, `IFNA`, `XOR` | M2 | ORIGINAL_REQUEST §2.1 |
| 11 | Formulas: Date & Time | `YEAR`, `MONTH`, `DAY`, `HOUR`, `MINUTE`, `SECOND`, `WEEKDAY`, `EDATE`, `EOMONTH`, `NETWORKDAYS`, `DATEDIF` | M2 | ORIGINAL_REQUEST §2.1 |
| 12 | Formulas: Text | `TEXT`, `VALUE`, `FIND`, `SEARCH`, `REPLACE`, `REPT`, `TEXTSPLIT`, `TEXTBEFORE`, `TEXTAFTER`, `CHAR`, `CODE`, `CLEAN` | M2 | ORIGINAL_REQUEST §2.1 |
| 13 | Formulas: Math & Statistical | `RANDBETWEEN`, `RAND`, `CEILING`, `FLOOR`, `SIGN`, `STDEV`, `VAR`, `RANK`, `MODE` | M2 | ORIGINAL_REQUEST §2.1 |
| 14 | Formulas: Information | `ISNUMBER`, `ISTEXT`, `ISBLANK`, `ISERROR`, `ISNA` | M2 | ORIGINAL_REQUEST §2.1 |
| 15 | ODS Reader & Writer | Bidirectional `.ods` OpenDocument Spreadsheet conversion (sheets, values, styles, formulas) | M2 | ORIGINAL_REQUEST §2.2 |
| 16 | XLS BIFF8 Binary Parser | Genuine OLE2 Compound File / BIFF8 binary workbook parser for legacy `.xls` files | M2 | ORIGINAL_REQUEST §2.2 |
| 17 | Web & Delimited Formats | HTML table export (`.html`), TSV (`.tsv`), and space-delimited text (`.prn`) | M2 | ORIGINAL_REQUEST §2.2 |
| 18 | Excel Tables (ListObject) Core | Create Table (`Ctrl+T`), zebra styling, header sorting and filtering drop-downs | M3 | ORIGINAL_REQUEST §3.1 |
| 19 | Table Dynamic Auto-Expansion | Automatically expand table boundaries when typing in adjacent rows or columns | M3 | ORIGINAL_REQUEST §3.1 |
| 20 | Table Total Row Aggregations | Total Row with per-column aggregations (`SUM`, `AVERAGE`, `COUNT`, `MIN`, `MAX`) | M3 | ORIGINAL_REQUEST §3.1 |
| 21 | Structured References | Parse and evaluate structured references like `Table1[@Sales] * 1.1` and `Table1[Column]` | M3 | ORIGINAL_REQUEST §3.1 |
| 22 | OpenXML Table Persistence | Read and write `xl/tables/table{N}.xml` and sheet `<tableParts>` in `XlsxHandler.cs` | M3 | Survey Report 3 |
| 23 | Dynamic Array Evaluation | Engine support for 2D array returns and formula spilling into adjacent empty cells | M3 | ORIGINAL_REQUEST §3.2 |
| 24 | Spill Collision & `#SPILL!` | Collision detection against non-empty cells with `#SPILL!` error and modern thin blue bounding border | M3 | ORIGINAL_REQUEST §3.2 |
| 25 | Array & Scoping Functions | Implement `SORT`, `SORTBY`, `FILTER`, `UNIQUE`, `SEQUENCE`, `RANDARRAY`, `LET`, `LAMBDA` | M3 | ORIGINAL_REQUEST §3.2 |
| 26 | PivotTable Engine & Cache | Multi-dimensional aggregation engine (Sum, Count, Average, Min, Max, % of Col/Row) with `PivotCache` | M4 | ORIGINAL_REQUEST §4.1 |
| 27 | PivotTable Field List Pane | Interactive task pane supporting drag-and-drop across Filters, Columns, Rows, Values | M4 | ORIGINAL_REQUEST §4.1 |
| 28 | Pivot Grouping & Linked Charts | Date/numeric grouping dialog and linked PivotCharts reflecting dynamic pivot aggregations | M4 | ORIGINAL_REQUEST §4.1 |
| 29 | Goal Seek Iterative Solver | Secant/bisection numerical solver converging within 0.001 of target value | M4 | ORIGINAL_REQUEST §4.2 |
| 30 | Data Tables Sensitivity Engine | 1-variable and 2-variable what-if sensitivity grids | M4 | ORIGINAL_REQUEST §4.2 |
| 31 | Workbook Structure Protection | Password-protected workbook structure locking sheet add/delete/rename/move | M4 | ORIGINAL_REQUEST §4.3 |
| 32 | Print Titles & Page Breaks | Print Titles (repeating header rows/cols) and manual Page Break management | M4 | ORIGINAL_REQUEST §4.3 |
| 33 | E2E Testing Suite (Tiers 1-4) | Comprehensive opaque-box test suite covering 100% of inventoried features | M-E2E / M5 | Dual Track Protocol |
| 34 | Adversarial Hardening (Tier 5) | White-box adversarial test cases and edge-case hardening | M5 | Dual Track Protocol |

---

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M-E2E | E2E Testing Track | Design test runner, test harness, and Tiers 1-4 test suites (Features 33) | none | DONE |
| M1 | Phase 1: Data Integrity & Core Productivity | OpenXML Drawings roundtrip, PDF export, Find/Replace, Remove Duplicates, Text to Columns (Features 1-8) | none | IN_PROGRESS |
| M2 | Phase 2: Formula Engine & File Formats | 52 new functions, ODS reader/writer, XLS BIFF8 parser, HTML/TSV/PRN export (Features 9-17) | M1 contracts | PLANNED |
| M3 | Phase 3: Tables & Dynamic Arrays | ListObject tables, auto-expansion, Total Row, structured references, Dynamic Arrays, Spill Engine, 8 array functions (Features 18-25) | M2 formula engine | PLANNED |
| M4 | Phase 4: PivotTables & Advanced Analytics | Pivot engine, Field List pane, Goal Seek, Data Tables, Workbook Protection, Print Titles, Page Breaks (Features 26-32) | M3 tables/arrays | PLANNED |
| M5 | Final Acceptance & Adversarial Hardening | Pass 100% E2E test suite (Tiers 1-4) + Adversarial Coverage Hardening (Tier 5) (Features 33-34) | M-E2E, M1, M2, M3, M4 | PLANNED |

---

## Interface Contracts

### 1. Drawings & Visual Assets (`XlsxHandler.cs` ↔ `CalcView.cs`)
- `CalcView.FloatingObjects`: Exposes `List<DocShapeObject>` as alias to `Shapes`.
- `XlsxHandler.LoadXlsx(Stream stream, CalcView calcView)`:
  - Parses `xl/worksheets/_rels/sheet{N}.xml.rels` to find target `../drawings/drawing{M}.xml`.
  - Parses `xl/drawings/drawing{M}.xml` extracting `<xdr:twoCellAnchor>` and `<xdr:oneCellAnchor>`.
  - Resolves `r:embed` references via `xl/drawings/_rels/drawing{M}.xml.rels` to `xl/media/*`.
  - Populates `calcView.FloatingObjects` with `DocShapeObject` having correct `ShapeType`, `Bounds` (converted from EMUs: `X = fromCol * colWidth + fromColOff / 9525`), `CachedImage`, and `ChartData`.

### 2. PDF Export (`FormatHandlers.cs` ↔ `CalcView.cs` ↔ `AppForm.cs`)
- `CalcView.ExportToPdf(string filePath)`: Returns `bool`.
- Reads `PageSetup` (orientation, margins, paper size, gridlines, print area, print titles).
- Uses `PdfSharp` vector graphics (`XGraphics`, `XFont`, `XPen`, `XBrush`) to render paginated spreadsheet pages into `PdfDocument`.
- `AppForm.PerformExportPdf`: When `Ribbon.CurrentMode == OfficeAppMode.Calc`, invokes `CalcView.ExportToPdf(sfd.FileName)`.

### 3. Formula Engine (`FormulaEngine.cs` ↔ `CalcView.cs`)
- Return Type Model: Formulas evaluate to `CellData` or `ArrayValue` (2D matrix of objects).
- Error Constants: Canonical strings `"#N/A"`, `"#VALUE!"`, `"#REF!"`, `"#DIV/0!"`, `"#NUM!"`, `"#NAME?"`, `"#SPILL!"`.
- Structured Reference Resolution: Before token parsing, formulas containing `Table[Column]` or `Table[@Column]` are resolved to coordinate ranges `A2:A100` via active table lookup.

### 4. File Format Handlers (`FormatHandlers.cs` ↔ `CalcView.cs`)
- `OdsHandler.LoadOds(string filePath, CalcView calcView)` / `SaveOds(string filePath, CalcView calcView)`.
- `XlsBiff8Handler.LoadXls(string filePath, CalcView calcView)`.
- `DelimitedAndHtmlHandlers.ExportHtml(string filePath, CalcView calcView)`, `ExportTsv(string filePath, CalcView calcView)`, `ExportPrn(string filePath, CalcView calcView)`.

---

## Code Layout
- `src/ui/`:
  - `CalcView.cs`: Core spreadsheet view, canvas painting, keyboard/mouse dispatch, table/spill render hooks.
  - `CalcFindReplaceDialog.cs`: Find & Replace dialog.
  - `CalcRemoveDuplicatesDialog.cs`: Remove Duplicates column picker dialog.
  - `CalcTextToColumnsDialog.cs`: Text to Columns delimited & fixed-width wizard.
  - `CalcTableAndPivotDialogs.cs`: Table and Pivot dialogs.
  - `PivotTableFieldListControl.cs`: 4-zone drag-and-drop PivotTable task pane.
  - `GoalSeekDialogForm.cs`: Goal Seek solver dialog.
  - `DataTableDialogForm.cs`: 1-var and 2-var What-If Data Table dialog.
  - `ProtectWorkbookDialogForm.cs`: Password-protected workbook structure dialog.
- `src/core/`:
  - `FormulaEngine.cs`: Formula tokenizer, evaluator, 52 new functions, structured references, Dynamic Arrays.
  - `SparseMatrix.cs`: Cell storage, formatting, spill metadata (`IsSpillAnchor`, `IsSpillTarget`, `SpillWidth`, `SpillHeight`).
  - `GoalSeekSolver.cs`: Numerical iterative root finder (Secant/bisection).
- `src/format/`:
  - `XlsxHandler.cs`: OpenXML XLSX packager/unpacker, drawings, tables, pivot cache, workbook protection.
  - `FormatHandlers.cs`: PDF export for Calc via `PdfSharp`.
  - `OdsHandler.cs`: OpenDocument Spreadsheet XML/ZIP handler.
  - `XlsBiff8Handler.cs`: OLE2 Compound File Binary BIFF8 handler.
  - `DelimitedAndHtmlHandlers.cs`: HTML, TSV, and PRN exporters.
- `tests/`:
  - `TestRunner.cs` / standalone console harness: Automated test runner executing formula tests, format roundtrip tests, table tests, and E2E scenarios.
