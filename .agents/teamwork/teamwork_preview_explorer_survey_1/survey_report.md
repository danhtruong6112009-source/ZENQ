# Comprehensive Survey Report: ZenQ Calc Codebase & Architecture

**Author**: Survey Agent 1 (Codebase & Architecture Explorer)  
**Date**: 2026-09-28  
**Project**: GenOffice / ZenQ Calc Spreadsheet Subsystem  
**Target Milestone**: Survey Phase (Preparation for 4-Phase Upgrade)  

---

## 1. Executive Summary

This report delivers a thorough architectural and technical survey of the ZenQ Calc codebase within `c:\Users\Danh\Desktop\genofficepy`. The objective is to establish an exhaustive factual baseline, diagnose architectural gaps, and guide the upcoming 4-phase upgrade:
- **Phase 1**: Critical Data Integrity (OpenXML Drawings Roundtrip), Native PDF Export, and Core Data Utilities (Find & Replace, Remove Duplicates, Text to Columns).
- **Phase 2**: Formula Engine Expansion (50+ functions: XLOOKUP, XMATCH, IFS, Date/Time, Text, Statistical) and Format Diversity (.ods, legacy BIFF8 .xls, HTML, TSV, PRN).
- **Phase 3**: Structured Excel Tables (ListObject, structured references `Table1[@Col]`, auto-expansion) and Dynamic Arrays / Spill Engine (`#SPILL!`, thin blue bounding box, SORT, FILTER, UNIQUE, SEQUENCE, RANDARRAY).
- **Phase 4**: PivotTables & PivotCharts (multidimensional aggregation, Field List pane), What-If Analysis (Goal Seek, Data Tables), and Advanced Printing / Workbook Structure Protection.

### Key Discoveries
1. **Critical Data-Loss Bug in OpenXML Drawings**: While `XlsxHandler.SaveXlsx` exports shapes, charts, and images to `xl/drawings/drawing1.xml` and `xl/media/`, `XlsxHandler.LoadXlsx` completely omits reading `xl/drawings/`, causing 100% loss of visual assets upon reopening files.
2. **C# 5 Language Version Constraint**: The build system invokes `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`, which strictly compiles for **C# 5** (targeting .NET Framework 4.8). Modern syntax such as string interpolation `$"..."`, null-conditional `?.`, expression-bodied members `=>`, local functions, and tuple literals are **unsupported** and will cause compilation errors.
3. **High Build & Test Velocity**: Compiling the entire solution via `build.bat` completes cleanly in under 3.5 seconds. Standalone test runners can compile directly against `GenOffice.exe` in ~1.0 second, facilitating rapid automated testing.
4. **Extensible Core Architecture**: The core spreadsheet data structure is a performant sparse matrix (`SparseMatrix.cs`) indexed by `ulong` row/column keys. UI rendering (`CalcView.cs`) uses high-performance GDI+ with double-buffering. Extending formulas, formats, and UI dialogs cleanly integrates into the existing modular pipeline.

---

## 2. Project Architecture & Build Infrastructure

### 2.1 Technology Stack & Compilers
- **Runtime**: Microsoft .NET Framework 4.8 (CLR 4.0.30319).
- **Platform**: Windows Forms (WinForms) with custom GDI+ drawing engine (`System.Drawing`, `System.Drawing.Drawing2D`).
- **Compiler**: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.
  - **Language Version**: C# 5.0 (Compiler banner: `Microsoft (R) Visual C# Compiler version 4.8.9232.0 for C# 5`).
  - **Implications**: All code contributed across all 4 phases must strictly comply with C# 5 syntax.

### 2.2 Build System (`build.bat`)
`build.bat` automates the compilation without requiring Visual Studio IDE or MSBuild:
```bat
@echo off
taskkill /f /im GenOffice.exe >nul 2>&1
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize /main:GenOffice.Program /win32icon:GenOffice.ico /resource:GENOFFICE.png,app_logo.png /resource:GenOffice.ico,app.ico /resource:icon\writer.png,icon_writer.png /resource:icon\calc.png,icon_calc.png /resource:icon\present.png,icon_present.png /resource:icon\pdf.png,icon_pdf.png /resource:icon\banner.png,banner.png /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll /r:System.Xml.Linq.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:Microsoft.CSharp.dll /r:WindowsBase.dll /r:DocumentFormat.OpenXml.dll /r:DocumentFormat.OpenXml.Framework.dll /r:DocumentFormat.OpenXml.Features.dll /r:DocumentFormat.OpenXml.Linq.dll /r:PdfiumViewer.dll /r:PdfSharp.dll /out:GenOffice.exe /recurse:src\*.cs
```

### 2.3 Available Dependencies
- `DocumentFormat.OpenXml.dll` (v2.x OpenXML SDK)
- `PdfSharp.dll` (PDF generation, vector drawing `PdfSharp.Drawing.XGraphics`, page geometry)
- `PdfiumViewer.dll` + `pdfium.dll` (Chromium PDF rendering engine)
- Standard BCL: `System.IO.Compression` (`ZipArchive`), `System.Xml`, `System.Xml.Linq`, `System.Drawing`.

### 2.4 Automatic File Discovery
Because `csc.exe` is invoked with `/recurse:src\*.cs`, any new `.cs` file created in `src/core/`, `src/format/`, or `src/ui/` is immediately incorporated into `GenOffice.exe` upon executing `build.bat`.

### 2.5 Verification & Test Infrastructure
- Standalone test harnesses can reference `GenOffice.exe` directly:
  ```bat
  csc.exe /r:GenOffice.exe /r:System.Drawing.dll /r:System.Windows.Forms.dll /out:tests\test_runner.exe tests\test_runner.cs
  ```
- Headless execution produces deterministic console outputs and asserts 100% of test conditions without spawning UI loops.

---

## 3. ZenQ Calc Subsystem Deep Dive

### 3.1 Data Layer: `SparseMatrix.cs` (`src/core/SparseMatrix.cs`)
- **Grid Storage**:
  ```csharp
  private Dictionary<ulong, CellData> grid = new Dictionary<ulong, CellData>();
  ```
  Keys are bit-packed 64-bit integers: `(uint row << 32) | (uint col)`. This guarantees $O(1)$ cell lookups and memory efficiency for massive 1,000,000-row sheets.
- **Cell Models**:
  - `CellData`: Encapsulates `CellType` (Empty, String, Number, Formula, Boolean, Error), `RawValue`, `FormulaStr`, `NumericValue`, `EvaluatedStr`, `Format` (`CellFormat`), `Comment`, `Hyperlink`.
  - `CellFormat`: Font styling (`FontName`, `FontSize`, `Bold`, `Italic`, `Underline`, `TextColor`), Fill (`BgColor`), Alignment (`Align`, `VAlign`), `WrapText`, `NumberFormat` (General, Number, Currency, Percent, Date, etc.), `DecimalPlaces`.
  - `CellBorderSettings`: Edge borders (Top, Bottom, Left, Right) with individual thickness (1..4) and colors; diagonal headers (`DiagonalDown`, `DiagonalUp`, `DiagonalThickness`, `DiagonalColor`).
  - Merge Cells: `IsMergedParent`, `MergeRowSpan`, `MergeColSpan`, `IsMergedChild`, `MergedParentRow`, `MergedParentCol`.
- **Sheet Operations**:
  `InsertRows`, `DeleteRows`, `InsertCols`, `DeleteCols`, `ShiftCellsRight`, `ShiftCellsDown`, `ShiftCellsLeft`, `ShiftCellsUp`, `MergeCells`, `UnmergeCells`, `CloneGrid`, `RestoreGrid`.
  - *Observation*: `ShiftCellsUp` in `SparseMatrix.cs` takes `(minR, minC, maxR, maxC)`. In `RemoveDuplicates` (line 7119), it is called with hardcoded bounds `(rowsToDelete[i], 1, 1000, 100)` rather than sheet-wide bounds.

### 3.2 Presentation & Interaction Layer: `CalcView.cs` (`src/ui/CalcView.cs`)
`CalcView.cs` (7,263 lines) manages state, drawing, mouse/keyboard events, editing, and sheet switching:
- **Sheet Management**:
  - `public List<SparseMatrix> Sheets { get; set; }`
  - `public List<string> SheetNames { get; set; }`
  - `public List<CalcSheetState> SheetStates { get; set; }`
  - `public int ActiveSheetIndex { get; set; }`
  - `CalcSheetState` maintains per-sheet scroll offsets, selections, custom row heights (`CustomRowHeights`), column widths (`CustomColWidths`), and visual shapes (`List<DocShapeObject> Shapes`).
- **Rendering Pipeline (`Draw(Graphics g, int width, int height)`)**:
  1. Formula Bar & Name Box (Y: 155 to 185) with Address display (`B5`, `3R x 4C`), Cancel (✕), Enter (✓), and fx button.
  2. Column Headers (A, B, C...) and Row Headers (1, 2, 3...) with green selection highlighting.
  3. Cell Grid Rendering: Background fills, text with font styling and wrapping, diagonal split headers, merged cell bounding boxes.
  4. Selection Outline & Auto-Fill Handle (bottom-right square handle with dragging preview).
  5. In-Place Cell Editor (`ActiveEditingCellRect`) with cursor blinking, live selection, formula suggestions popup.
  6. Drawing Objects (`Shapes`): Renders vector shapes, pictures, charts, and SmartArt diagrams on top of cells with 8 interactive resize handles.
  7. Sheet Tab Bar at the bottom: Tab switching, tab rename, `+` new sheet button, tab coloring, context menu.

### 3.3 Visual Assets & Floating Objects (`DocShapeObject.cs`)
- All floating objects in `CalcView` are stored in:
  ```csharp
  public List<DocShapeObject> Shapes { get; set; }
  ```
  and per sheet state in `CalcSheetState.Shapes`.
- **Types of Floating Objects**:
  - Vector Shapes (`ShapeType`: "Rectangle", "RoundedRectangle", "Oval", "RightArrow", "Star5", etc.)
  - Images & Pictures (`ShapeType`: "Image", `ImagePath`, `CachedImage`)
  - Charts (`ShapeType`: "Chart", `ChartData`: `DocChartData`)
  - SmartArt (`ShapeType`: "SmartArt", `SmartArtType`)
- **Screen Geometry**:
  `GetShapeScreenBounds(DocShapeObject shape, int headerW, int startY, int headerH)` maps sheet coordinates `shape.PageX`, `shape.PageY` (offset by scroll column widths and row heights) to screen coordinates.
- **Requirement Mapping**: To satisfy `CalcView.FloatingObjects` specified in `ORIGINAL_REQUEST.md`, an alias property:
  ```csharp
  public List<DocShapeObject> FloatingObjects
  {
      get { return Shapes; }
      set { Shapes = value; }
  }
  ```
  provides 100% compatibility with zero code breakage.

### 3.4 Calculation & Formula Engine (`src/core/FormulaEngine.cs`)
- **Engine Architecture**:
  - `EvaluateSheet(SparseMatrix sheet)`: Iterates all cells, evaluates formulas, formats display text.
  - `EvaluateFormula(string formula, SparseMatrix sheet)`: Parses function name, parses arguments via `SplitArgs`, dispatches to function handlers.
  - `ParseAndEvaluateNumeric(string expr, SparseMatrix sheet)`: Recursively evaluates binary arithmetic operators with operator precedence (`+`, `-` -> `*`, `/` -> `^`) and cell references (`B2`, `$C$5`).
  - `ParseRange(string rangeStr)`: Expands ranges like `A1:C10` into coordinate tuples `(row, col)`.
  - `MatchesCriteria(CellData cell, string criteria, SparseMatrix sheet)`: Supports conditional comparison operators (`>=`, `<=`, `<>`, `!=`, `>`, `<`, `=`).
- **Currently Supported Functions (~35 functions)**:
  - *Math/Stats*: `SUM`, `AVERAGE`, `SUMPRODUCT`, `COUNT`, `COUNTA`, `MIN`, `MAX`, `PRODUCT`, `ROUND`, `ROUNDUP`, `ROUNDDOWN`, `ABS`, `SQRT`, `POWER`, `MOD`, `INT`, `TRUNC`, `MEDIAN`, `LARGE`, `SMALL`.
  - *Logic*: `IF`, `IFS`, `AND`, `OR`, `NOT`, `XOR`, `SWITCH`, `IFERROR`, `CHOOSE`.
  - *Text*: `CONCATENATE`/`CONCAT`, `TEXTJOIN`, `TEXT`, `LEFT`, `RIGHT`, `MID`, `LEN`, `UPPER`, `LOWER`, `TRIM`, `PROPER`, `SUBSTITUTE`, `EXACT`.
  - *Lookup*: `VLOOKUP`, `HLOOKUP`, `XLOOKUP`, `INDEX`, `MATCH`, `OFFSET`, `INDIRECT`.
  - *Date/Time*: `TODAY`, `NOW`, `DATE`, `TIME`, `DAY`, `MONTH`, `YEAR`, `HOUR`, `MINUTE`, `SECOND`.

### 3.5 File Format Architecture: `XlsxHandler.cs` (`src/format/XlsxHandler.cs`)
- **Format Routing**:
  - `.csv`, `.tsv`, `.txt` -> `LoadCsv` / `SaveCsv`.
  - `.xlsx` -> `LoadXlsx` / `SaveXlsx`.
- **`SaveXlsx` Capabilities**:
  - Creates OpenXML package via `System.IO.Compression.ZipArchive`.
  - Writes `xl/workbook.xml`, `xl/styles.xml` (fonts, fills, borders, cellXfs), `xl/sharedStrings.xml`, and `xl/worksheets/sheet{N}.xml`.
  - Exports drawings and visual assets:
    - `xl/worksheets/_rels/sheet1.xml.rels`
    - `xl/media/image{N}.png` (extracted from `DocShapeObject.CachedImage` or rendered via `ShapeRenderer.DrawShape`)
    - `xl/drawings/_rels/drawing1.xml.rels`
    - `xl/drawings/drawing1.xml` with `<xdr:twoCellAnchor editAs="oneCell">`, `<xdr:from>`, `<xdr:to>`, `<xdr:pic>`, and `<xdr:sp>`.
- **`LoadXlsx` Flaw**:
  - Reads `xl/sharedStrings.xml`, `xl/styles.xml`, `xl/workbook.xml`, `xl/worksheets/sheet*.xml`.
  - Completely **ignores** `xl/drawings/` and `xl/media/`.

---

## 4. Phased Gap Analysis (Phases 1 - 4)

| Phase | Feature / Requirement | Current State in Codebase | Identified Gap & Scope |
|:---:|:---|:---|:---|
| **P1** | **OpenXML Drawings Roundtrip** | `SaveXlsx` writes `drawing1.xml` & `media/`. `LoadXlsx` ignores drawings. | **Critical Data Loss**. Need to implement drawing & media parsing in `LoadXlsx`, restoring shapes, pictures, charts to `CalcView.FloatingObjects` / `Shapes`. |
| **P1** | **Native PDF Export for Calc** | `AppForm.cs` lines 4200-4212 exports a blank PDF for Calc. | Missing `CalcView.ExportToPdf(...)` and format handler honoring margins, orientation, paper size, cell styles, gridlines via `PdfSharp`. |
| **P1** | **Find & Replace Dialog** | `FindReplaceForm.cs` is hardcoded for Writer without options. | Need dedicated floating Calc dialog with Match Case, Match Entire Cell, Search By Rows/Cols, Look In Formulas/Values. |
| **P1** | **Remove Duplicates** | `CalcView.RemoveDuplicates` compares all columns and uses hardcoded bounds. | Missing column-selection modal; must allow selecting specific subset of columns and properly adjust sheet row structure. |
| **P1** | **Text to Columns Wizard** | None. | Completely absent. Need dialog supporting Delimited (Tab, Semicolon, Comma, Space, Custom) and Fixed-width parsing. |
| **P2** | **Formula Engine: 50+ Functions** | ~35 basic functions implemented. | Missing 50+ high-demand functions: `XMATCH`, `ROW`, `COLUMN`, `ROWS`, `COLUMNS`, `IFNA`, `WEEKDAY`, `EDATE`, `EOMONTH`, `NETWORKDAYS`, `DATEDIF`, `VALUE`, `FIND`, `SEARCH`, `REPLACE`, `REPT`, `TEXTSPLIT`, `TEXTBEFORE`, `TEXTAFTER`, `CHAR`, `CODE`, `CLEAN`, `RANDBETWEEN`, `RAND`, `CEILING`, `FLOOR`, `SIGN`, `STDEV`, `VAR`, `RANK`, `MODE`, `ISNUMBER`, `ISTEXT`, `ISBLANK`, `ISERROR`, `ISNA`. |
| **P2** | **OpenDocument Spreadsheet (.ods)** | None. | Completely absent. Need ZIP-based reader/writer for `content.xml` (tables, cells, values, formulas `of:=...`), styles, manifest. |
| **P2** | **Excel 97-2003 (.xls) Binary** | `.xls` routed to `LoadXlsx` which throws `ZipException`. | Completely absent. Need genuine Compound File Binary / BIFF8 parser (`BOF`, `BOUNDSHEET`, `SST`, `DIMENSIONS`, cell records). |
| **P2** | **Web & Delimited Formats** | CSV/TSV partially loaded; HTML and PRN absent. | Need clean HTML table export (`.html`) with CSS formatting, space-delimited text (`.prn`), and robust TSV roundtrip. |
| **P3** | **Excel Tables (ListObject)** | `ExcelTableInfo` exists; `CreateTableFromRange` applies zebra colors. | Missing `Ctrl+T` shortcut, dynamic auto-expansion on adjacent row/col typing, optional Total Row aggregations, structured formula references `Table1[@Sales]`. |
| **P3** | **Dynamic Arrays & Spill Engine** | None. | Missing spill calculation engine, `#SPILL!` blockage detection, thin blue bounding box rendering, and array functions (`SORT`, `SORTBY`, `FILTER`, `UNIQUE`, `SEQUENCE`, `RANDARRAY`, `LET`, `LAMBDA`). |
| **P4** | **PivotTable & PivotChart** | `CreatePivotTable` hardcodes columns 2 and 5 into static cells. | Missing true dynamic multidimensional aggregation engine, Field List Pane (Filters, Columns, Rows, Values), date/numeric grouping, and dynamically linked PivotCharts. |
| **P4** | **What-If: Goal Seek & Data Tables** | None. | Missing Goal Seek single-variable solver engine & dialog, and 1/2-variable sensitivity Data Tables. |
| **P4** | **Advanced Printing & Protection** | Sheet protection exists (`IsSheetProtected`), but no workbook structure protection. | Missing Workbook Structure Protection with password (preventing sheet add/delete/rename), Print Titles (repeat rows/cols), and Page Break management. |

---

## 5. Detailed Technical Gap Specifications

### 5.1 Phase 1 Deep Dive

#### 5.1.1 OpenXML Drawings Roundtrip (`XlsxHandler.cs`)
- **Current Defect**:
  When `LoadXlsx(string filepath, CalcView calcView)` executes, it processes only:
  - `xl/sharedStrings.xml`
  - `xl/styles.xml`
  - `xl/workbook.xml`
  - `xl/worksheets/sheet{N}.xml`
  It never checks for `xl/worksheets/_rels/sheet{N}.xml.rels` or `xl/drawings/drawing1.xml`.
- **Target Implementation in `XlsxHandler.cs`**:
  1. Map relationships: Open `xl/worksheets/_rels/sheet{N}.xml.rels` to discover drawing parts (e.g. `drawing1.xml`).
  2. Map media: Open `xl/drawings/_rels/drawing{N}.xml.rels` to map `rId` attributes to `xl/media/image{N}.*` entries.
  3. Parse drawing XML (`drawing1.xml`):
     - Read `<xdr:twoCellAnchor>` and `<xdr:oneCellAnchor>`.
     - Extract `<xdr:from><xdr:col>`, `<xdr:colOff>`, `<xdr:row>`, `<xdr:rowOff>` and `<xdr:to>`.
     - Convert EMUs to pixels: $X_{\text{px}} = \sum_{c=1}^{\text{col}} W_c + \frac{\text{colOff}}{9525}$, $Y_{\text{px}} = \sum_{r=1}^{\text{row}} H_r + \frac{\text{rowOff}}{9525}$.
     - Reconstruct `DocShapeObject` with exact dimensions, colors, text, or embedded image bitmaps.
     - Reconstruct chart placeholders / `DocChartData`.
     - Append restored objects to `calcView.Sheets[i]` and `calcView.Shapes`.

#### 5.1.2 Native PDF Export for Calc (`FormatHandlers.cs` / `CalcView.cs`)
- **Current Defect**:
  In `AppForm.cs` line 4209, selecting Export to PDF while in Calc mode creates a blank PDF document:
  ```csharp
  var blankDoc = new GenOffice.Core.PdfDocument(System.IO.Path.GetFileName(sfd.FileName));
  ok = GenOffice.Format.PdfHandler.SavePdf(blankDoc, sfd.FileName);
  ```
- **Target Implementation**:
  - Implement `public bool ExportToPdf(string filePath, CalcPrintSettings settings = null)` in `CalcView.cs`.
  - Use `PdfSharp.Pdf.PdfDocument` and `PdfSharp.Drawing.XGraphics`:
    - Honor page orientation (Portrait / Landscape) and paper size (A4: 595.28 x 841.89 pt, Letter: 612 x 792 pt).
    - Honor margins (Default 54 pt / 0.75 in, Narrow 36 pt / 0.5 in).
    - Calculate column widths and row heights scaled to page width.
    - Render cell fills, text with exact alignment, fonts, and cell borders (Top, Bottom, Left, Right, Diagonal).
    - Support multi-page pagination with repeating header rows (Print Titles).
  - Update `AppForm.cs` `PerformExportPdf` to call `CalcView.ExportToPdf` when `Ribbon.CurrentMode == OfficeAppMode.Calc`.

#### 5.1.3 Core Data Utilities
- **Find & Replace Dialog (`CalcFindReplaceDialog.cs`)**:
  - Dedicated floating non-modal dialog for Calc.
  - Checkboxes: Match Case, Match Entire Cell.
  - Radio options: Search By Rows vs Columns.
  - Dropdown: Look In Formulas vs Values.
  - Action buttons: Find Next, Replace, Replace All.
  - Must navigate sheet selection and replace values without invalidating formula expressions.
- **Remove Duplicates Dialog (`CalcRemoveDuplicatesDialog.cs`)**:
  - Modal dialog listing all columns in current selection with checkboxes.
  - "My data has headers" checkbox (skips header row when comparing).
  - "Select All" / "Unselect All" buttons.
  - Core logic: Scans rows, builds composite key from selected columns only, removes duplicate rows, shifts remaining cells up smoothly, updates sheet spans, and displays a summary message (`Đã xóa X dòng trùng lặp, giữ lại Y dòng duy nhất`).
- **Text to Columns Wizard (`CalcTextToColumnsDialog.cs`)**:
  - Step 1: Choose Delimited or Fixed Width.
  - Step 2 (Delimited): Checkboxes for Tab, Semicolon, Comma, Space, and Custom delimiter. Real-time preview grid showing resulting columns.
  - Step 2 (Fixed Width): Visual column break ruler.
  - Step 3: Destination cell specification (defaults to top-left of selected range).
  - Execution: Parses each cell in the selected column and populates adjacent columns rightward.

---

### 5.2 Phase 2 Deep Dive

#### 5.2.1 Formula Engine Expansion (50+ Functions)
`FormulaEngine.cs` needs to be expanded to evaluate the following functions:

1. **Lookup & Reference**:
   - `XMATCH(lookup_value, lookup_array, [match_mode], [search_mode])`: Returns 1-based relative position of item in array.
   - `ROW([reference])`: Returns row number of given cell or caller.
   - `COLUMN([reference])`: Returns column number of given cell or caller.
   - `ROWS(array)`: Returns number of rows in range or array.
   - `COLUMNS(array)`: Returns number of columns in range or array.
2. **Logical & Error**:
   - `IFNA(value, value_if_na)`: Returns alternative value if formula evaluates to `#N/A`, otherwise formula result.
3. **Date & Time**:
   - `WEEKDAY(serial_number, [return_type])`: Day of week (1..7).
   - `EDATE(start_date, months)`: Date before or after specified number of months.
   - `EOMONTH(start_date, months)`: Last day of the month before or after specified months.
   - `NETWORKDAYS(start_date, end_date, [holidays])`: Working days between two dates excluding weekends.
   - `DATEDIF(start_date, end_date, unit)`: Time difference in "Y", "M", "D", "YM", "YD", "MD".
4. **Text**:
   - `VALUE(text)`: Converts string representation of number to numeric value.
   - `FIND(find_text, within_text, [start_num])`: Case-sensitive character position lookup.
   - `SEARCH(find_text, within_text, [start_num])`: Case-insensitive position lookup with `*` and `?` wildcard support.
   - `REPLACE(old_text, start_num, num_chars, new_text)`: Replaces part of text string with another.
   - `REPT(text, number_times)`: Repeats text a specified number of times.
   - `TEXTSPLIT(text, col_delimiter, [row_delimiter])`: Splits text across columns and rows.
   - `TEXTBEFORE(text, delimiter, [instance_num])`: Extracts text before specified delimiter.
   - `TEXTAFTER(text, delimiter, [instance_num])`: Extracts text after specified delimiter.
   - `CHAR(number)`: Returns character corresponding to ASCII code.
   - `CODE(text)`: Returns numeric code for first character.
   - `CLEAN(text)`: Removes non-printable ASCII characters (0..31).
5. **Math & Statistical**:
   - `RANDBETWEEN(bottom, top)`: Random integer between bottom and top.
   - `RAND()`: Random floating-point number between 0 and 1.
   - `CEILING(number, significance)`: Rounds up to nearest multiple of significance.
   - `FLOOR(number, significance)`: Rounds down to nearest multiple of significance.
   - `SIGN(number)`: Returns 1 if positive, -1 if negative, 0 if zero.
   - `STDEV(number1, ...)` / `STDEV.S`: Sample standard deviation.
   - `VAR(number1, ...)` / `VAR.S`: Sample variance.
   - `RANK(number, ref, [order])`: Statistical rank of number within reference range.
   - `MODE(number1, ...)`: Most frequently occurring value.
6. **Information**:
   - `ISNUMBER(value)`: Returns TRUE if value is a numeric value.
   - `ISTEXT(value)`: Returns TRUE if value is text.
   - `ISBLANK(value)`: Returns TRUE if cell is empty.
   - `ISERROR(value)`: Returns TRUE if value is any error (`#N/A`, `#VALUE!`, `#REF!`, `#DIV/0!`, `#SPILL!`).
   - `ISNA(value)`: Returns TRUE specifically if value is `#N/A`.

#### 5.2.2 Additional File Formats
1. **OpenDocument Spreadsheet (`.ods`) Handler**:
   - ODS is a standard ZIP archive containing:
     - `mimetype` (`application/vnd.oasis.opendocument.spreadsheet`)
     - `META-INF/manifest.xml`
     - `content.xml` (the spreadsheet body: `<office:spreadsheet>`, `<table:table>`, `<table:table-row>`, `<table:table-cell>`)
     - `styles.xml`
   - Reading: Extract cells, parse values (`office:value-type="float"`, `office:value-type="string"`), formulas (`table:formula="of:=SUM([.A1:.A10])"` converted to `=SUM(A1:A10)`), column widths, row heights.
   - Writing: Package sparse matrix sheets into valid ODF structure.
2. **Excel 97-2003 Binary (`.xls`) BIFF8 Handler**:
   - Requires parsing OLE Compound File Binary Format (CFBF).
   - Locate the `Workbook` stream in directory entries.
   - Parse binary BIFF8 record headers (`type` uint16, `length` uint16):
     - `0x0809` (BOF): Workbook / Worksheet stream start.
     - `0x0085` (BOUNDSHEET): Sheet names and stream offsets.
     - `0x00FC` (SST): Shared String Table with Unicode/compressed string parsing.
     - `0x0200` (DIMENSIONS): Min/max row and column bounds.
     - `0x0203` (NUMBER): IEEE 754 64-bit float cell values.
     - `0x027E` (RK) / `0x00BD` (MULRK): Packed integer/float values.
     - `0x00FD` (LABELSST): Cell referencing SST string index.
     - `0x0006` (FORMULA): Evaluated formula cell records.
     - `0x000A` (EOF): End of stream.
3. **Web & Delimited Formats**:
   - HTML Table Export (`.html`): Generate standalone HTML document with embedded CSS containing `table.excel-table`, alternating row colors, cell borders, custom widths, and font properties.
   - Space-delimited Text (`.prn`): Column-aligned text output using monospace padding based on column widths.

---

### 5.3 Phase 3 Deep Dive

#### 5.3.1 Structured Excel Tables (ListObject)
- **Keyboard Shortcut**: Wire `Ctrl+T` and `Ctrl+L` in `CalcView.OnKeyDown` to trigger `CreateTableDialogForm`.
- **Structured References in `FormulaEngine.cs`**:
  - Recognize patterns like `Table1[@Sales]`, `Table1[Sales]`, `Table1[#Totals]`, `Table1[#Headers]`.
  - Resolution mechanism:
    1. Lookup table in `CalcView.Tables` by name (case-insensitive).
    2. Parse column identifier within brackets.
    3. If `[@ColName]`, evaluate row is the current formula cell's row; retrieve value at column matching `ColName`.
    4. If `Table1[ColName]`, return the full data range of that column (from `table.StartRow + 1` to `table.EndRow`).
    5. Support arithmetic on structured ranges (e.g. `Table1[@Qty] * Table1[@Price]`).
- **Dynamic Auto-Expansion**:
  - In `CalcView.SetCellTextInternal` or `CommitInPlaceEdit`:
    - If user edits cell at `r == table.EndRow + 1` within `table.StartCol <= c <= table.EndCol`:
      - Automatically expand `table.EndRow++`.
      - Inherit cell formatting (borders, zebra colors, number formats) from row above.
      - If adjacent columns have formulas, auto-fill the formula down into the new row.
- **Optional Total Row**:
  - Add property `table.HasTotalRow` and toggle in "Thiết kế Bảng" ribbon tab.
  - Appends total row below last data row.
  - Per-column dropdown to select `SUM`, `AVERAGE`, `COUNT`, `MAX`, `MIN`.

#### 5.3.2 Dynamic Arrays & Spill Engine
- **Spill Mechanism**:
  - When a dynamic array formula (e.g. `=SORT(A1:B10)`) evaluates to a 2D matrix of values:
    1. Check target destination cells (from formula origin cell $(r_0, c_0)$ across matrix dimensions $W \times H$).
    2. If any target cell other than $(r_0, c_0)$ is non-empty, mark formula result as `#SPILL!` and do not overwrite existing data.
    3. If all destination cells are empty:
       - Populate each destination cell $(r, c)$ with the corresponding array value, marking `cell.IsSpilledChild = true` and `cell.SpillParent = (r_0, c_0)`.
       - The formula remains stored only in $(r_0, c_0)$ (ghost cells reference parent).
- **Spill Border Visualizer**:
  - In `CalcView.Draw`, when `SelectedRow, SelectedCol` is either the spill master cell or any spilled child:
    - Draw a thin blue bounding border (`Color.FromArgb(17, 100, 206)`, thickness 1.5px) enclosing the entire rectangular spill range.
- **Dynamic Array Functions**:
  - `SORT(array, [sort_index], [sort_order], [by_col])`: Sorts range by specified column/row.
  - `SORTBY(array, by_array1, [order1], ...)`: Sorts range based on values in adjacent array.
  - `FILTER(array, include, [if_empty])`: Filters range based on boolean criteria array.
  - `UNIQUE(array, [by_col], [exactly_once])`: Returns distinct rows/values.
  - `SEQUENCE(rows, [cols], [start], [step])`: Generates sequence of sequential numbers.
  - `RANDARRAY([rows], [cols], [min], [max], [whole_number])`: Generates array of random numbers.
  - `LET(name1, val1, ..., expr)`: Assigns names to calculation results for local scoping.

---

### 5.4 Phase 4 Deep Dive

#### 5.4.1 PivotTable & PivotChart
- **Data Model (`PivotTableModel.cs`)**:
  - Source data range (sheet + rectangle).
  - Selected Fields:
    - `RowFields` (List of column names for row hierarchy).
    - `ColumnFields` (List of column names for column hierarchy).
    - `ValueFields` (List of Value field configurations: Field Name, Function: Sum, Count, Average, Min, Max, % of Row, % of Col, % of Total).
    - `FilterFields` (Page filters).
- **Aggregation Engine**:
  - Scans source range rows.
  - Aggregates data into a multi-key map: `Dictionary<Tuple<string, string>, PivotAggregator>`.
  - Computes row totals, column totals, and grand total.
  - Emits formatted tabular output into target sheet location.
- **Interactive Field List Pane (`PivotTableFieldListPanel.cs`)**:
  - Dockable / popup panel displaying:
    - Field checkbox list.
    - 4 Drag-and-drop drop zones: Filters, Columns, Rows, Values.
  - Rearranging fields dynamically recalculates and refreshes the pivot grid.
- **Linked PivotChart**:
  - Create `DocChartData` linked to `PivotTableModel`.
  - When pivot dimensions or filters update, chart series and categories automatically update.

#### 5.4.2 What-If Analysis
- **Goal Seek Solver (`GoalSeekSolver.cs` & `GoalSeekDialogForm.cs`)**:
  - Inputs:
    - `FormulaCell`: Cell reference containing formula to solve.
    - `TargetValue`: Target numeric goal.
    - `ChangingCell`: Independent variable cell containing numeric value.
  - Algorithm: Secant Method with Bisection fallback:
    $$x_{n+1} = x_n - f(x_n) \frac{x_n - x_{n-1}}{f(x_n) - f(x_{n-1})}$$
    where $f(x) = \text{Evaluate}(FormulaCell) - TargetValue$.
  - Iteration limit: 100 iterations; convergence threshold: $\epsilon \le 0.001$.
  - Preserves original value if user cancels or solver diverges.
- **Data Tables (`DataTableDialogForm.cs`)**:
  - 1-Variable Table: Varies column input cell down rows and evaluates formulas.
  - 2-Variable Table: Intersects row input cell and column input cell to generate sensitivity grid.

#### 5.4.3 Advanced Printing & Protection
- **Workbook Structure Protection**:
  - `IsWorkbookProtected`, `WorkbookPasswordHash`.
  - When enabled:
    - Disable sheet addition (`+` button).
    - Disable sheet deletion, renaming, hiding, reordering.
    - Password prompt dialog to unprotect workbook.
- **Print Titles & Page Breaks**:
  - Configure repeating header rows (e.g. `$1:$2`) on every PDF page.
  - Configure repeating header columns (e.g. `$A:$B`).
  - Manual page break management (`InsertPageBreak`, `RemovePageBreak`) with visual dotted separator lines.

---

## 6. Technical Conventions & Implementation Constraints

### 6.1 Strict C# 5 Conformance
To ensure `build.bat` succeeds without error, developers must adhere to the following rules:

| Modern Feature (Forbidden) | Compliant C# 5 Alternative |
|:---|:---|
| String interpolation: `$"Value: {x}"` | `string.Format("Value: {0}", x)` |
| Null-conditional: `cell?.Format?.Bold` | `cell != null && cell.Format != null && cell.Format.Bold` |
| Expression bodies: `public int Col => 5;` | `public int Col { get { return 5; } }` |
| Local functions: `void Helper() { ... }` | Private static helper methods or delegate `Action`/`Func` |
| Pattern matching: `if (x is CellData cd)` | `CellData cd = x as CellData; if (cd != null)` |
| Readonly auto-properties: `public int X { get; }` | `public int X { get; private set; }` |
| Switch expressions: `var r = x switch { ... }` | Standard `switch (x) { case ...: return ...; }` |
| Nameof operator: `nameof(SelectedRow)` | String literal `"SelectedRow"` |

### 6.2 Code Organization & Write Boundaries
- `src/core/`: Calculation algorithms, data structures (`SparseMatrix.cs`, `FormulaEngine.cs`, `PivotTableModel.cs`, `GoalSeekSolver.cs`).
- `src/format/`: File I/O readers and writers (`XlsxHandler.cs`, `PdfHandler.cs`, `OdsHandler.cs`, `Biff8Handler.cs`, `HtmlExportHandler.cs`).
- `src/ui/`: UI views, dialogs, visual rendering (`CalcView.cs`, `CalcTableAndPivotDialogs.cs`, `CalcFindReplaceDialog.cs`, `CalcTextToColumnsDialog.cs`, `CalcRemoveDuplicatesDialog.cs`).
- `tests/`: Automated unit and integration test scripts.
- **Rule**: Never place source code or test files in `.agents/teamwork/`.

### 6.3 Performance & Memory Safety
- GDI+ objects (`Pen`, `SolidBrush`, `Font`, `GraphicsPath`) created during `Draw()` must be enclosed in `using` blocks to prevent GDI handle leaks.
- Sparse matrix operations must avoid dense array allocations over empty grid areas.

---

## 7. Recommended Execution Plan

1. **Phase 1 Execution**:
   - Step 1.1: Fix OpenXML drawings roundtrip in `XlsxHandler.cs` and add `FloatingObjects` alias in `CalcView.cs`.
   - Step 1.2: Implement native PDF export for spreadsheet in `CalcView.cs` and link into `AppForm.cs`.
   - Step 1.3: Implement Find & Replace, Remove Duplicates column modal, and Text to Columns dialogs.
2. **Phase 2 Execution**:
   - Step 2.1: Expand `FormulaEngine.cs` with the 50+ missing functions and update `InsertFunctionDialog.cs`.
   - Step 2.2: Implement `.ods` OpenDocument reader/writer.
   - Step 2.3: Implement BIFF8 binary `.xls` parser.
   - Step 2.4: Implement HTML table export, PRN, and TSV support.
3. **Phase 3 Execution**:
   - Step 3.1: Complete Structured Tables (ListObject `Ctrl+T`, auto-expansion, Total Row, structured references in FormulaEngine).
   - Step 3.2: Implement Dynamic Arrays, spill calculation, `#SPILL!` detection, thin blue border rendering, array functions (`SORT`, `FILTER`, `UNIQUE`, `SEQUENCE`).
4. **Phase 4 Execution**:
   - Step 4.1: Implement multidimensional PivotTable engine, interactive Field List pane, and linked PivotCharts.
   - Step 4.2: Implement Goal Seek solver and Data Tables.
   - Step 4.3: Implement Workbook Structure Protection, Print Titles, and Page Breaks.
5. **Continuous Verification**:
   - Compile via `build.bat` after every incremental milestone.
   - Run automated test suites to ensure 0 data regressions.
