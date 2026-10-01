# ZenQ Calc Phase 1 & Phase 2 Authoritative Technical Specification Report

**Document Status**: Authoritative Specification & Architecture Guide  
**Target Environment**: Microsoft Visual C# 5.0 (`csc.exe` / .NET Framework 4.0 - 4.8.9232)  
**Author**: Survey Agent 2 (Phases 1 & 2 Spec & API Miner)  
**Assigned Work Item**: Investigation and Detailed Specification of OpenXML Drawings Roundtrip, Native Calc PDF Export, Core Data Utilities, Formula Engine 50+ Expansion, and Extended File Formats (ODS, XLS BIFF8, HTML, TSV, PRN).

---

## 1. Executive Summary & Critical Constraints

### 1.1 Scope & Objectives
This specification provides complete technical designs, data contracts, mathematical coordinate mappings, and error semantics for Phase 1 and Phase 2 of the ZenQ Calc upgrade:
1. **OpenXML Drawings Roundtrip**: Eliminating visual data loss when loading and saving `.xlsx` files containing shapes, pictures, charts, and SmartArt via `xl/drawings/drawing1.xml`, `xl/drawings/_rels/`, and `xl/media/`.
2. **Native Calc PDF Export**: High-fidelity vector PDF generation for spreadsheets honoring paper size, orientation, margins, gridlines, fonts, cell formatting, merged cells, headers/footers, and floating assets.
3. **Core Data Utilities**: Fully integrated WinForms dialogs and execution engines for Find & Replace, Remove Duplicates with column selection checklist, and Text to Columns (Delimited & Fixed Width).
4. **Formula Engine Expansion (52 Functions)**: Comprehensive function signatures, arguments, error handling (`#N/A`, `#VALUE!`, `#REF!`, `#DIV/0!`, `#NUM!`), and evaluation semantics across 6 functional domains.
5. **Multi-Format Storage**: Bidirectional OpenDocument Spreadsheet (`.ods`), genuine legacy Excel 97-2003 BIFF8 binary (`.xls`), HTML table exporter (`.html`), Tab-Separated Values (`.tsv`), and space-padded formatted text (`.prn`).

### 1.2 Mandatory Language & Platform Constraint: C# 5.0 Only
The project build harness (`build.bat`) invokes `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`. As observed directly during build diagnostics:
```text
Microsoft (R) Visual C# Compiler version 4.8.9232.0 for C# 5
```
**Strict Syntax Rules for All Implementation Code:**
- **NO C# 6+ String Interpolation**: `$"..."` is prohibited. Must use `string.Format(CultureInfo.InvariantCulture, "...", ...)`.
- **NO Expression-Bodied Members**: `public int X => 1;` is prohibited. Must use `public int X { get { return 1; } }`.
- **NO Null-Conditional Operators**: `obj?.Prop` or `cb?.Invoke()` is prohibited. Must use `if (obj != null)`.
- **NO Inline `out` Variable Declarations**: `int.TryParse(s, out int x)` is prohibited. Must declare `int x; int.TryParse(s, out x);`.
- **NO Tuple Literals**: `(a, b)` syntax is prohibited. Must use `Tuple.Create(a, b)` or `Tuple<T1, T2>`.
- **NO Auto-property Initializers**: `public int X { get; set; } = 5;` is prohibited. Must initialize in constructors.

---

## 2. Phase 1: OpenXML Drawings Roundtrip Specification

### 2.1 Problem Analysis & Current Root Cause
In `src/format/XlsxHandler.cs`, `SaveXlsx` (lines 1208–1425) serializes floating shapes, charts, and embedded images into OpenXML DrawingML:
- Inserts `xl/worksheets/_rels/sheet1.xml.rels` linking to `../drawings/drawing1.xml`.
- Writes embedded images into `xl/media/image{N}.png`.
- Writes relationship definitions into `xl/drawings/_rels/drawing1.xml.rels`.
- Generates DrawingML markup in `xl/drawings/drawing1.xml` with `<xdr:twoCellAnchor editAs="oneCell">`.

**However**, `LoadXlsx` (lines 300–778) currently only reads `xl/sharedStrings.xml`, `xl/styles.xml`, and `xl/worksheets/sheet{N}.xml`. It **never opens or parses** `xl/drawings/`, `xl/drawings/_rels/`, or `xl/media/`. As a consequence, upon opening an existing workbook, all shapes, charts, and pictures are discarded. When the user subsequently saves the workbook, `calcView.Shapes` is empty, completely wiping out all drawings from the file.

### 2.2 Model Unification: `FloatingObjects` and `DocShapeObject`
`DocShapeObject` in `src/ui/DocShapeObject.cs` is the suite-wide representation for shapes, images, charts, and SmartArt. In `CalcView.cs` and `CalcSheetState`, shapes are currently stored in `List<DocShapeObject> Shapes`.

To satisfy requirement 1.1 ("Restore all floating shapes, embedded images, and charts into `CalcView.FloatingObjects`"), we expose an explicit property alias on both classes:
```csharp
// In CalcView.cs and CalcSheetState.cs:
public List<DocShapeObject> FloatingObjects
{
    get { return Shapes; }
    set { Shapes = value; }
}
```

### 2.3 DrawingML Structure & Part Graph
An `.xlsx` file containing drawings consists of the following part hierarchy:
```text
[Content_Types].xml
  ├── Override: /xl/worksheets/sheet1.xml (application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml)
  └── Override: /xl/drawings/drawing1.xml (application/vnd.openxmlformats-officedocument.drawing+xml)
xl/
  ├── worksheets/
  │     ├── sheet1.xml (contains <drawing r:id="rIdDrawing1"/>)
  │     └── _rels/
  │           └── sheet1.xml.rels (Relationship Type=".../drawing" Target="../drawings/drawing1.xml")
  ├── drawings/
  │     ├── drawing1.xml (anchors: twoCellAnchor, oneCellAnchor, absoluteAnchor)
  │     └── _rels/
  │           └── drawing1.xml.rels (Relationship Type=".../image" Target="../media/image1.png")
  ├── charts/
  │     └── chart1.xml (optional: DrawingML Chart definition)
  └── media/
        └── image1.png, image2.jpeg, ...
```

### 2.4 Mathematical Coordinate Transformation
DrawingML expresses anchor coordinates in **EMUs (English Metric Units)** where:
- $1 \text{ inch} = 72 \text{ points} = 96 \text{ pixels (standard 96 DPI)} = 914,400 \text{ EMUs}$
- $1 \text{ pixel} = 914,400 / 96 = 9,525 \text{ EMUs}$

#### A. Anchor Coordinate Recovery (EMU to Screen Pixels)
For a `<xdr:twoCellAnchor>`:
- `<xdr:from>` contains `<xdr:col>`, `<xdr:colOff>`, `<xdr:row>`, `<xdr:rowOff>`.
- `<xdr:to>` contains `<xdr:col>`, `<xdr:colOff>`, `<xdr:row>`, `<xdr:rowOff>`.

Calculation for `PageX`, `PageY`, `Width`, and `Height`:
```csharp
// 1. Calculate X from Column and Offset
uint fromCol = uint.Parse(fromColNode.InnerText) + 1; // OpenXML is 0-indexed, CalcView is 1-indexed
long fromColOff = long.Parse(fromColOffNode.InnerText);
int startX = 0;
for (uint c = 1; c < fromCol; c++)
{
    startX += calcView.GetColWidth(c);
}
int pageX = startX + (int)(fromColOff / 9525);

// 2. Calculate Y from Row and Offset
uint fromRow = uint.Parse(fromRowNode.InnerText) + 1;
long fromRowOff = long.Parse(fromRowOffNode.InnerText);
int startY = 0;
for (uint r = 1; r < fromRow; r++)
{
    startY += calcView.GetRowHeight(r);
}
int pageY = startY + (int)(fromRowOff / 9525);

// 3. Calculate End X
uint toCol = uint.Parse(toColNode.InnerText) + 1;
long toColOff = long.Parse(toColOffNode.InnerText);
int endX = 0;
for (uint c = 1; c < toCol; c++)
{
    endX += calcView.GetColWidth(c);
}
endX += (int)(toColOff / 9525);

// 4. Calculate End Y
uint toRow = uint.Parse(toRowNode.InnerText) + 1;
long toRowOff = long.Parse(toRowOffNode.InnerText);
int endY = 0;
for (uint r = 1; r < toRow; r++)
{
    endY += calcView.GetRowHeight(r);
}
endY += (int)(toRowOff / 9525);

int baseW = Math.Max(20, endX - pageX);
int baseH = Math.Max(20, endY - pageY);
```

For `<xdr:oneCellAnchor>`:
- `<xdr:from>` gives `PageX` and `PageY`.
- `<xdr:ext cx="..." cy="..."/>` gives dimensions:
  `baseW = (int)(cx / 9525);`
  `baseH = (int)(cy / 9525);`

For `<xdr:absoluteAnchor>`:
- `<xdr:pos x="..." y="..."/>` gives:
  `pageX = (int)(x / 9525);`
  `pageY = (int)(y / 9525);`
- `<xdr:ext cx="..." cy="..."/>` gives `baseW` and `baseH`.

### 2.5 Detailed Parser Implementation Pipeline for `LoadXlsx`
To be added to `LoadXlsx` in `XlsxHandler.cs`:
1. **Find Worksheet Drawing Relationship**:
   Inspect `xl/worksheets/_rels/sheet{N}.xml.rels`. Search for:
   `<Relationship Type=".../relationships/drawing" Target="..."/>`.
   Normalize target to archive entry (e.g. `../drawings/drawing1.xml` $\rightarrow$ `xl/drawings/drawing1.xml`).
2. **Find Drawing Relationships**:
   Inspect `xl/drawings/_rels/drawing{N}.xml.rels`. Build a lookup table:
   `Dictionary<string, string> relIdToTargetMap`.
3. **Parse `xl/drawings/drawing{N}.xml`**:
   Load `XmlDocument`. Enumerate all `<xdr:twoCellAnchor>`, `<xdr:oneCellAnchor>`, and `<xdr:absoluteAnchor>` nodes.
4. **Instantiate `DocShapeObject`**:
   - **For Pictures (`<xdr:pic>`)**:
     - Extract `name` and `id` from `<xdr:cNvPr>`.
     - Extract `r:embed` attribute from `<a:blip>`.
     - Lookup target in `relIdToTargetMap` (e.g. `../media/image1.png` $\rightarrow$ `xl/media/image1.png`).
     - Load bytes from archive entry, convert to `Image` via `MemoryStream`, assign to `sh.CachedImage`.
     - Set `sh.ShapeType = "Image"`.
   - **For Geometric Shapes (`<xdr:sp>`)**:
     - Extract `prst` attribute from `<a:prstGeom>`. Map OpenXML preset geometry names:
       - `"rect"` $\rightarrow$ `"Rectangle"`
       - `"roundRect"` $\rightarrow$ `"RoundedRectangle"`
       - `"ellipse"` $\rightarrow$ `"Oval"`
       - `"triangle"` $\rightarrow$ `"Triangle"`
       - `"rightArrow"` $\rightarrow$ `"RightArrow"`
       - `"star5"` $\rightarrow$ `"Star5"`
       - `"heart"` $\rightarrow$ `"Heart"`
       - Default $\rightarrow$ `"Rectangle"`
     - Extract solid fill color from `<a:solidFill><a:srgbClr val="RRGGBB"/></a:solidFill>`.
     - Extract line color from `<a:ln><a:solidFill><a:srgbClr val="RRGGBB"/></a:solidFill></a:ln>`.
     - Extract text content from `<xdr:txBody>`: aggregate all `<a:t>` inner text.
   - **For Charts (`<xdr:graphicFrame>`)**:
     - Check `<a:graphicData uri=".../chart">`.
     - Extract `r:id` from `<c:chart>`.
     - Follow relationship to `xl/charts/chart{M}.xml`.
     - Parse `<c:chartSpace>`: extract title (`<c:title>`), chart type (`<c:plotArea>` child, e.g. `<c:barChart>`, `<c:lineChart>`, `<c:pieChart>`), categories, series, and numeric points.
     - Set `sh.ShapeType = "Chart"`.
     - Instantiate and populate `sh.ChartData = new DocChartData()`.
5. **Assign to State**:
   Append `sh` to `st.Shapes` (and if sheet is active, to `calcView.Shapes`).

---

## 3. Phase 1: Native Calc PDF Export Specification

### 3.1 Architecture & Integration Points
PDF export for Calc integrates at three levels:
1. **Core Handler (`FormatHandlers.cs`)**:
   `public static bool ExportCalcToPdf(CalcView calcView, string filePath, bool exportAllSheets)`
2. **View Invocation (`CalcView.cs`)**:
   `public bool ExportToPdf(string filePath, bool exportAllSheets = false)`
3. **Application UI Hook (`AppForm.cs` line 4200)**:
   ```csharp
   else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
   {
       ok = CalcView.ExportToPdf(sfd.FileName);
   }
   ```

### 3.2 Page Setup & Geometric Dimensions
The export engine queries `CalcView` print properties:
- **Paper Size Table**:
  - `A4`: $595.28 \times 841.89 \text{ pt}$ ($210 \times 297 \text{ mm}$)
  - `Letter`: $612.0 \times 792.0 \text{ pt}$ ($8.5 \times 11 \text{ in}$)
  - `Legal`: $612.0 \times 1008.0 \text{ pt}$ ($8.5 \times 14 \text{ in}$)
  - `A3`: $841.89 \times 1190.55 \text{ pt}$ ($297 \times 420 \text{ mm}$)
  - Default: A4 ($595.28 \times 841.89 \text{ pt}$).
- **Orientation**: If `calcView.IsLandscape` is true, swap Width and Height.
- **Margins**:
  - `Normal` (Preset 0): Left = 54 pt, Right = 54 pt, Top = 54 pt, Bottom = 54 pt (0.75 in).
  - `Wide` (Preset 1): Left = 72 pt, Right = 72 pt, Top = 72 pt, Bottom = 72 pt (1.0 in).
  - `Narrow` (Preset 2): Left = 18 pt, Right = 18 pt, Top = 18 pt, Bottom = 18 pt (0.25 in).
- **Usable Printable Width & Height**:
  $W_{\text{usable}} = W_{\text{page}} - (M_{\text{left}} + M_{\text{right}})$  
  $H_{\text{usable}} = H_{\text{page}} - (M_{\text{top}} + M_{\text{bottom}})$

### 3.3 Printable Area Determination
1. If `calcView.HasPrintArea` is true:
   $R_{\min} = \text{PrintAreaStartRow}, R_{\max} = \text{PrintAreaEndRow}$  
   $C_{\min} = \text{PrintAreaStartCol}, C_{\max} = \text{PrintAreaEndCol}$
2. If `HasPrintArea` is false:
   Compute smallest bounding rectangle enclosing all non-empty cells in `CurrentSheet`:
   $R_{\min} = \min(r), R_{\max} = \max(r), C_{\min} = \min(c), C_{\max} = \max(c)$.
   If sheet is empty, default to $R_{\min}=1, R_{\max}=10, C_{\min}=1, C_{\max}=5$.

### 3.4 2D Pagination Algorithm (Row & Column Banding)
Spreadsheets can be both wider and taller than a single page. The exporter computes column bands and row bands:
1. **Horizontal Partitioning (Column Bands)**:
   Group contiguous columns $[C_1..C_k]$ such that $\sum_{c=C_1}^{C_k} \text{WidthPt}(c) \le W_{\text{usable}}$.
   (Scale factor: $1 \text{ pixel} \approx 0.75 \text{ pt}$).
2. **Vertical Partitioning (Row Bands)**:
   Group contiguous rows $[R_1..R_m]$ such that $\sum_{r=R_1}^{R_m} \text{HeightPt}(r) \le H_{\text{usable}} - H_{\text{headers}}$.
3. **Page Emission Sequence**:
   Follow Excel's standard "Over, then Down" (or "Down, then Over") order. For each (ColumnBand, RowBand) pair, instantiate a new `PdfPageModel`.

### 3.5 High-Fidelity Vector Rendering Pipeline
For each cell $(r, c)$ on the page:
1. **Background Fill**:
   If `cell.Format.BgColor` is not white/transparent:
   Emit PDF rectangle fill: `q r g b rg X Y W H re f Q`.
2. **Gridlines & Custom Borders**:
   If `calcView.ShowGridlines` is true or `cell.Format.Borders` has explicit borders:
   - Left: `X, Y` to `X, Y+H`
   - Top: `X, Y+H` to `X+W, Y+H`
   - Right: `X+W, Y` to `X+W, Y+H`
   - Bottom: `X, Y` to `X+W, Y`
   Emit stroked path with thickness: `0.5 w` for gridlines, custom thickness for styled borders.
3. **Text Formatting & Alignment**:
   - Determine text: `cell.EvaluatedStr` if present, else `cell.RawValue`.
   - Font tag: `/F2` if `cell.Format.Bold`, else `/F1`.
   - Font size: `cell.Format.FontSize * 0.9f`.
   - Horizontal Alignment: Left (offset +2 pt), Center (centered in cell width), Right (cell width - text width - 2 pt).
   - Vertical Alignment: Top, Middle, Bottom.
   - Emit text block: `BT /F# size Tf r g b rg X Y Td (escapedText) Tj ET`.
4. **Merged Cell Handling**:
   If `cell.Format.IsMergedParent`:
   Calculate span width across all spanned columns and height across spanned rows. Suppress child cells.
5. **Headers, Footers & Floating Objects**:
   - Render `HeaderLeft`, `HeaderCenter`, `HeaderRight` at $Y = H_{\text{page}} - M_{\text{top}} + 15$.
   - Render `FooterLeft`, `FooterCenter`, `FooterRight` (with dynamic Page X of Y) at $Y = M_{\text{bottom}} - 15$.
   - For all `DocShapeObject` in `FloatingObjects` intersecting the current page band, render shape contours or image bitmaps via PDF XObject images.

---

## 4. Phase 1: Core Data Utilities Specification

### 4.1 Find & Replace Dialog (`CalcFindReplaceDialog`)
- **File**: `src/ui/CalcFindReplaceDialog.cs`
- **Class**: `public class CalcFindReplaceDialog : Form`
- **Control Layout**:
  - `txtFind`: TextBox for target query string.
  - `txtReplace`: TextBox for replacement string.
  - `chkMatchCase`: CheckBox ("Match Case" / Khớp chữ hoa/thường).
  - `chkMatchEntire`: CheckBox ("Match Entire Cell" / Khớp toàn bộ nội dung ô).
  - `cboSearchBy`: ComboBox items: `"By Rows"` (default), `"By Columns"`.
  - `cboLookIn`: ComboBox items: `"Values"` (compares against `EvaluatedStr`), `"Formulas"` (compares against `FormulaStr` or raw input).
  - Buttons: `btnFindNext` ("Find Next"), `btnFindAll` ("Find All"), `btnReplace` ("Replace"), `btnReplaceAll` ("Replace All"), `btnClose` ("Close").
  - `lstResults`: Optional expandable ListView showing (Sheet, Cell, Content) for "Find All".
- **Search & Replace Algorithms in `CalcView`**:
  ```csharp
  public bool FindNext(string query, bool matchCase, bool matchEntire, bool byRows, bool lookInFormulas)
  {
      if (string.IsNullOrEmpty(query)) return false;
      StringComparison comp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
      
      // Determine scanning order
      // If byRows: outer loop row (1..maxR), inner loop col (1..maxC)
      // If byCols: outer loop col (1..maxC), inner loop row (1..maxR)
      // Wrap around from active selection (SelectedRow, SelectedCol)
      // On match: set SelectedRow, SelectedCol, SelEndRow, SelEndCol; ScrollCellIntoView(); Invalidate(); return true;
  }
  ```
- **Formula Safety Rule**:
  When `lookInFormulas == false`:
  If a cell contains a formula (`cell.Type == CellType.Formula`), the replace engine **MUST NOT** overwrite the formula string with raw text unless explicitly confirmed by the user. If the cell is a static value, replace text cleanly. When `lookInFormulas == true`, replace matches within the formula expression (e.g., replacing `TAX` with `VAT` transforms `=A1*TAX` into `=A1*VAT`).
  All modifications MUST be preceded by `SaveUndoState()` and followed by `FormulaEngine.EvaluateSheet(CurrentSheet)`.

### 4.2 Remove Duplicates Modal (`CalcRemoveDuplicatesDialog`)
- **File**: `src/ui/CalcRemoveDuplicatesDialog.cs`
- **Class**: `public class CalcRemoveDuplicatesDialog : Form`
- **UI Specification**:
  - Top Label: Displays selected range (e.g., `"Vùng dữ liệu được chọn: A1:E250"`).
  - `chkHasHeaders`: CheckBox ("Dữ liệu có tiêu đề / My data has headers", default checked if first row is non-numeric string).
  - `clbColumns`: `CheckedListBox` displaying available columns. When headers are checked, items show `Column A (Họ và Tên)`. When unchecked, items show `Column A`.
  - Action Buttons: `btnSelectAll` ("Chọn tất cả"), `btnUnselectAll` ("Bỏ chọn tất cả").
  - Dialog Buttons: `btnOK` ("Xóa trùng lặp"), `btnCancel` ("Hủy").
- **Deduplication Engine**:
  ```csharp
  public void ExecuteRemoveDuplicates(List<uint> checkedCols, bool hasHeaders)
  {
      SaveUndoState();
      uint startR = hasHeaders ? (minR + 1) : minR;
      HashSet<string> seenSignatures = new HashSet<string>();
      List<uint> rowsToDelete = new List<uint>();
      
      for (uint r = startR; r <= maxR; r++)
      {
          StringBuilder sb = new StringBuilder();
          for (int i = 0; i < checkedCols.Count; i++)
          {
              uint c = checkedCols[i];
              CellData cell = CurrentSheet.GetCell(r, c);
              string v = (cell != null) ? (!string.IsNullOrEmpty(cell.EvaluatedStr) ? cell.EvaluatedStr : cell.RawValue) : "";
              sb.Append(v).Append("\x1F"); // Unit separator delimiter
          }
          string sig = sb.ToString();
          if (seenSignatures.Contains(sig))
          {
              rowsToDelete.Add(r);
          }
          else
          {
              seenSignatures.Add(sig);
          }
      }
      
      // Delete rows in reverse order to preserve indexing
      for (int i = rowsToDelete.Count - 1; i >= 0; i--)
      {
          CurrentSheet.ShiftCellsUp(rowsToDelete[i], 1, 1000, 100);
      }
      
      FormulaEngine.EvaluateSheet(CurrentSheet);
      Invalidate();
      // Show result message: "Đã tìm thấy và xóa {0} dòng trùng lặp. Giữ lại {1} dòng duy nhất."
  }
  ```

### 4.3 Text to Columns Wizard (`CalcTextToColumnsDialog`)
- **File**: `src/ui/CalcTextToColumnsDialog.cs`
- **Class**: `public class CalcTextToColumnsDialog : Form`
- **Two Parsing Modes**:
  1. **Delimited Mode**:
     - CheckBoxes: Tab (`\t`), Semicolon (`;`), Comma (`,`), Space (` `), Other (`txtOtherDelim`).
     - CheckBox: `chkConsecutiveDelimiters` ("Treat consecutive delimiters as one").
     - ComboBox: `cboTextQualifier` (Options: `"\""`, `"'"` , `"{None}"`).
  2. **Fixed Width Mode**:
     - Visual ruler or comma-separated character offsets (e.g. `10, 25, 40`).
- **Live Preview Grid**:
  A read-only `DataGridView` displaying the first 10 rows of selected data split in real-time as the user toggles delimiter checkboxes.
- **Destination Cell**:
  TextBox defaulting to `$A$1` (the top-left cell of the selection).
- **Execution Logic**:
  ```csharp
  public void ExecuteTextToColumns(uint srcCol, uint startRow, uint endRow, uint destCol, 
                                   List<char> delimiters, bool consecutiveAsOne, char textQualifier)
  {
      SaveUndoState();
      for (uint r = startRow; r <= endRow; r++)
      {
          CellData srcCell = CurrentSheet.GetCell(r, srcCol);
          string raw = (srcCell != null) ? srcCell.RawValue : "";
          string[] tokens = SplitLine(raw, delimiters, consecutiveAsOne, textQualifier);
          
          for (int i = 0; i < tokens.Length; i++)
          {
              uint targetC = (uint)(destCol + i);
              string token = tokens[i].Trim();
              double numVal;
              if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out numVal))
              {
                  CurrentSheet.SetCellNumber(r, targetC, numVal);
              }
              else
              {
                  CurrentSheet.SetCellText(r, targetC, token);
              }
          }
      }
      FormulaEngine.EvaluateSheet(CurrentSheet);
      Invalidate();
  }
  ```

---

## 5. Phase 2: Formula Engine Architecture & Evaluation Pipeline

### 5.1 Current Architecture Overview
`FormulaEngine.cs` in `src/core/FormulaEngine.cs`:
- Evaluates sheets via `EvaluateSheet(SparseMatrix sheet)`.
- Dispatches formula strings through `EvaluateFormula(string formula, SparseMatrix sheet)`.
- Tokenizes top-level function names and extracts argument strings using `SplitArgs(string argsStr)`.
- Resolves nested expressions recursively using `SetResultFromToken(res, token, sheet)`.
- Handles binary arithmetic expressions via `ParseAndEvaluateNumeric(expr, sheet)`.

### 5.2 Canonical Error Model & Propagation
Excel defines seven canonical error constants. The upgraded engine must preserve and propagate these distinct error codes:
1. `#NULL!` - Intersection of two ranges that do not intersect.
2. `#DIV/0!` - Division by zero or square root of negative numbers in real domain.
3. `#VALUE!` - Wrong type of argument (e.g. attempting arithmetic on non-numeric text).
4. `#REF!` - Invalid cell reference (e.g. deleted cells, out-of-bounds `INDEX` / `OFFSET`).
5. `#NAME?` - Unrecognized function name or invalid range identifier.
6. `#NUM!` - Invalid numeric value (e.g. negative number to `SQRT`, or `DATEDIF` start > end).
7. `#N/A` - No value available (lookup failure in `MATCH`, `VLOOKUP`, `XLOOKUP`, `HLOOKUP`).

**Error Propagation Rule**:
If any argument evaluates to an error string starting with `#`, the formula immediately returns that error code, **except** when processed by error-handling functions:
- `IFERROR(val, fallback)`: catches all `#` errors and returns `fallback`.
- `IFNA(val, fallback)`: catches **only** `#N/A` errors and returns `fallback`; all other errors propagate untouched.
- `ISERROR(val)`: evaluates to `TRUE` for any `#` error.
- `ISNA(val)`: evaluates to `TRUE` only for `#N/A`.

---

## 6. Phase 2: Complete 52-Function Reference & Semantics

Below is the authoritative specification for all 52 high-demand functions requested in `ORIGINAL_REQUEST.md`.

### Group 1: Lookup & Reference Functions (10 Functions)

#### 1. `INDEX(array, row_num, [column_num])`
- **Arguments**:
  - `array`: Range reference (e.g. `A1:C10`) or array constant.
  - `row_num`: 1-based row index within `array`. If 0, refers to the entire row.
  - `column_num` *(optional, default 1)*: 1-based column index within `array`.
- **Semantics**: Returns the value of the cell at the intersection of `row_num` and `column_num`.
- **Error Handling**: Returns `#REF!` if `row_num > rows in array` or `column_num > cols in array` or `row_num < 0` or `column_num < 0`. Returns `#VALUE!` if indices are non-numeric.
- **Example**: `=INDEX(A1:C5, 2, 3)` returns value at `C2`.

#### 2. `MATCH(lookup_value, lookup_array, [match_type])`
- **Arguments**:
  - `lookup_value`: Value to match (string or number).
  - `lookup_array`: 1D range (e.g. `A1:A10` or `A1:J1`).
  - `match_type` *(optional, default 1)*:
    - `0`: Exact match. Supports wildcards `*` (any characters) and `?` (single character) for strings.
    - `1`: Largest value $\le$ `lookup_value`. Requires `lookup_array` sorted ascending.
    - `-1`: Smallest value $\ge$ `lookup_value`. Requires `lookup_array` sorted descending.
- **Semantics**: Returns 1-based position of match in array.
- **Error Handling**: Returns `#N/A` if no match found. Returns `#REF!` if array is multi-dimensional.
- **Example**: `=MATCH("Apple", A1:A10, 0)` returns `3`.

#### 3. `XLOOKUP(lookup_value, lookup_array, return_array, [if_not_found], [match_mode], [search_mode])`
- **Arguments**:
  - `lookup_value`: Value to search for.
  - `lookup_array`: 1D range to search in.
  - `return_array`: 1D range of return values (same length as `lookup_array`).
  - `if_not_found` *(optional, default `#N/A`)*: Returned if no match is found.
  - `match_mode` *(optional, default 0)*:
    - `0`: Exact match.
    - `-1`: Exact match or next smaller item.
    - `1`: Exact match or next larger item.
    - `2`: Wildcard match (`*`, `?`, `~`).
  - `search_mode` *(optional, default 1)*:
    - `1`: Search first-to-last.
    - `-1`: Search last-to-first (reverse search).
    - `2`: Binary search (ascending).
    - `-2`: Binary search (descending).
- **Semantics**: Modern search replacement for VLOOKUP/HLOOKUP.
- **Error Handling**: Returns `if_not_found` if provided, else `#N/A`. Returns `#VALUE!` if array dimensions mismatch.
- **Example**: `=XLOOKUP(102, A2:A10, C2:C10, "Not Found")`.

#### 4. `XMATCH(lookup_value, lookup_array, [match_mode], [search_mode])`
- **Arguments**:
  - `lookup_value`: Value to match.
  - `lookup_array`: 1D array to search.
  - `match_mode` *(optional, default 0)*: 0 (exact), -1 (smaller), 1 (larger), 2 (wildcard).
  - `search_mode` *(optional, default 1)*: 1 (first-to-last), -1 (last-to-first), 2 (binary asc), -2 (binary desc).
- **Semantics**: Returns 1-based relative position matching the criteria.
- **Error Handling**: Returns `#N/A` if not found.
- **Example**: `=XMATCH(50, A1:A20, -1)`.

#### 5. `ROW([reference])`
- **Arguments**: `reference` *(optional)*: Cell reference. If omitted, returns row of calling cell.
- **Semantics**: Returns the 1-based row number.
- **Error Handling**: Returns `#REF!` if reference is invalid.
- **Example**: `=ROW(D15)` returns `15`.

#### 6. `COLUMN([reference])`
- **Arguments**: `reference` *(optional)*: Cell reference. If omitted, returns column of calling cell.
- **Semantics**: Returns 1-based column number (e.g. A=1, B=2, C=3).
- **Error Handling**: Returns `#REF!` if reference is invalid.
- **Example**: `=COLUMN(C5)` returns `3`.

#### 7. `ROWS(array)`
- **Arguments**: `array`: Range reference or array.
- **Semantics**: Returns the count of rows in the referenced range.
- **Error Handling**: Returns `#VALUE!` if argument is not a valid range.
- **Example**: `=ROWS(A1:C10)` returns `10`.

#### 8. `COLUMNS(array)`
- **Arguments**: `array`: Range reference or array.
- **Semantics**: Returns the count of columns in the referenced range.
- **Error Handling**: Returns `#VALUE!` if argument is not a valid range.
- **Example**: `=COLUMNS(B2:E8)` returns `4`.

#### 9. `INDIRECT(ref_text, [a1])`
- **Arguments**:
  - `ref_text`: String representing a cell address (e.g. `"A1"` or `"Sheet2!B5"`).
  - `a1` *(optional, default TRUE)*: TRUE indicates A1 notation; FALSE indicates R1C1 notation.
- **Semantics**: Returns cell data referenced by the text string.
- **Error Handling**: Returns `#REF!` if `ref_text` is not a valid cell reference.
- **Example**: `=INDIRECT("B" & 2)` returns value of cell `B2`.

#### 10. `OFFSET(reference, rows, cols, [height], [width])`
- **Arguments**:
  - `reference`: Base starting cell reference.
  - `rows`: Number of rows to offset (positive down, negative up).
  - `cols`: Number of columns to offset (positive right, negative left).
  - `height` *(optional)*: Number of rows in target range.
  - `width` *(optional)*: Number of columns in target range.
- **Semantics**: Returns reference or value shifted from base cell.
- **Error Handling**: Returns `#REF!` if offset points outside sheet boundaries ($r < 1$ or $c < 1$).
- **Example**: `=OFFSET(A1, 2, 3)` returns value at `D3`.

---

### Group 2: Logical & Error Functions (5 Functions)

#### 11. `IFS(logical_test1, value_if_true1, [logical_test2, value_if_true2], ...)`
- **Arguments**: Pairs of condition and result.
- **Semantics**: Evaluates conditions in order. Returns value corresponding to the first TRUE condition.
- **Error Handling**: Returns `#N/A` if no condition evaluates to TRUE. Returns `#VALUE!` if odd number of arguments.
- **Example**: `=IFS(A1>90, "A", A1>80, "B", TRUE, "C")`.

#### 12. `SWITCH(expression, value1, result1, [value2, result2], ..., [default])`
- **Arguments**:
  - `expression`: Value to compare.
  - One or more `value/result` pairs.
  - Optional trailing `default` value.
- **Semantics**: Compares `expression` against `valueN`. Returns corresponding `resultN`. If none match, returns `default`.
- **Error Handling**: Returns `#N/A` if no match and no `default` provided.
- **Example**: `=SWITCH(A1, 1, "One", 2, "Two", "Other")`.

#### 13. `IFERROR(value, value_if_error)`
- **Arguments**:
  - `value`: Expression to evaluate.
  - `value_if_error`: Fallback returned if `value` produces any error.
- **Semantics**: Traps `#DIV/0!`, `#VALUE!`, `#REF!`, `#N/A`, `#NUM!`, `#NAME?`. If no error, returns result of `value`.
- **Example**: `=IFERROR(A1/B1, 0)`.

#### 14. `IFNA(value, value_if_na)`
- **Arguments**:
  - `value`: Expression to evaluate.
  - `value_if_na`: Fallback returned **strictly** if `value` produces `#N/A`.
- **Semantics**: If `value` produces `#N/A`, returns `value_if_na`. If `value` produces `#DIV/0!` or other errors, that error is **not** trapped.
- **Example**: `=IFNA(VLOOKUP(10, A:B, 2, FALSE), "Not Present")`.

#### 15. `XOR(logical1, [logical2], ...)`
- **Arguments**: 1 or more logical expressions.
- **Semantics**: Returns TRUE if an odd number of arguments evaluate to TRUE; returns FALSE if an even number evaluate to TRUE.
- **Error Handling**: Returns `#VALUE!` if no arguments contain logical values.
- **Example**: `=XOR(TRUE, FALSE, TRUE)` returns `FALSE` (2 is even).

---

### Group 3: Date & Time Functions (11 Functions)

#### 16. `YEAR(serial_number)`
- **Arguments**: Serial date number (OADate) or date text string.
- **Semantics**: Returns integer year (1900–9999).
- **Error Handling**: Returns `#VALUE!` if argument cannot be parsed as date.
- **Example**: `=YEAR(44927)` returns `2023`.

#### 17. `MONTH(serial_number)`
- **Arguments**: Serial date number or date text.
- **Semantics**: Returns integer month (1–12).
- **Error Handling**: Returns `#VALUE!` if invalid.
- **Example**: `=MONTH("2026-09-28")` returns `9`.

#### 18. `DAY(serial_number)`
- **Arguments**: Serial date number or date text.
- **Semantics**: Returns integer day of the month (1–31).
- **Example**: `=DAY("2026-09-28")` returns `28`.

#### 19. `HOUR(serial_number)`
- **Arguments**: Serial time number or time text.
- **Semantics**: Returns integer hour (0–23).
- **Example**: `=HOUR("14:30:00")` returns `14`.

#### 20. `MINUTE(serial_number)`
- **Arguments**: Serial time number or time text.
- **Semantics**: Returns integer minute (0–59).
- **Example**: `=MINUTE("14:35:10")` returns `35`.

#### 21. `SECOND(serial_number)`
- **Arguments**: Serial time number or time text.
- **Semantics**: Returns integer second (0–59).
- **Example**: `=SECOND("14:35:59")` returns `59`.

#### 22. `WEEKDAY(serial_number, [return_type])`
- **Arguments**:
  - `serial_number`: Date value.
  - `return_type` *(optional, default 1)*:
    - `1`: Sunday=1 to Saturday=7.
    - `2`: Monday=1 to Sunday=7.
    - `3`: Monday=0 to Sunday=6.
- **Semantics**: Returns day-of-week index according to mapping type.
- **Example**: `=WEEKDAY("2026-09-28", 2)` returns `1` (Monday).

#### 23. `EDATE(start_date, months)`
- **Arguments**:
  - `start_date`: Valid date.
  - `months`: Number of months before (negative) or after (positive).
- **Semantics**: Adds months to date, clamping day to target month's end if needed (e.g. Jan 31 + 1 month = Feb 28). Returns OADate.
- **Example**: `=EDATE("2026-01-31", 1)` returns `2026-02-28`.

#### 24. `EOMONTH(start_date, months)`
- **Arguments**:
  - `start_date`: Valid date.
  - `months`: Number of months before or after.
- **Semantics**: Returns serial date of the last day of the month indicated.
- **Example**: `=EOMONTH("2026-02-10", 0)` returns `2026-02-28`.

#### 25. `NETWORKDAYS(start_date, end_date, [holidays])`
- **Arguments**:
  - `start_date`: Start date.
  - `end_date`: End date.
  - `holidays` *(optional)*: Range of excluded holiday dates.
- **Semantics**: Calculates working days between two dates, excluding Saturdays, Sundays, and holiday dates. Inclusive of start and end. If `start_date > end_date`, returns negative count.
- **Example**: `=NETWORKDAYS("2026-09-01", "2026-09-30")` returns `22`.

#### 26. `DATEDIF(start_date, end_date, unit)`
- **Arguments**:
  - `start_date`, `end_date`: Two dates.
  - `unit`: String format code:
    - `"Y"`: Complete years.
    - `"M"`: Complete months.
    - `"D"`: Total days.
    - `"MD"`: Day difference ignoring months and years.
    - `"YM"`: Month difference ignoring years.
    - `"YD"`: Day difference ignoring years.
- **Error Handling**: Returns `#NUM!` if `start_date > end_date`. Returns `#VALUE!` if unit is unknown.
- **Example**: `=DATEDIF("2020-01-01", "2026-09-28", "Y")` returns `6`.

---

### Group 4: Text Functions (12 Functions)

#### 27. `TEXT(value, format_text)`
- **Arguments**:
  - `value`: Numeric or date value.
  - `format_text`: Format specification string (e.g. `"0.00"`, `"#,##0"`, `"YYYY-MM-DD"`, `"0%"`).
- **Semantics**: Formats number into text string according to custom number format tokens.
- **Example**: `=TEXT(1234.5, "#,##0.00")` returns `"1,234.50"`.

#### 28. `VALUE(text)`
- **Arguments**: `text`: String containing numbers, currency symbols (`$`, `€`, `₫`), thousand separators, or percentage symbols (`%`).
- **Semantics**: Converts formatted text to a numeric float.
- **Error Handling**: Returns `#VALUE!` if string cannot be converted.
- **Example**: `=VALUE("$1,250.50")` returns `1250.5`.

#### 29. `FIND(find_text, within_text, [start_num])`
- **Arguments**:
  - `find_text`: Substring to locate.
  - `within_text`: Text to search within.
  - `start_num` *(optional, default 1)*: 1-based character start index.
- **Semantics**: **Case-sensitive** string search. Returns 1-based position. Does **not** support wildcards.
- **Error Handling**: Returns `#VALUE!` if `find_text` not found or `start_num < 1`.
- **Example**: `=FIND("c", "Pacific")` returns `5`.

#### 30. `SEARCH(find_text, within_text, [start_num])`
- **Arguments**:
  - `find_text`: Substring to locate. Supports `*` and `?` wildcards.
  - `within_text`: Text to search within.
  - `start_num` *(optional, default 1)*: 1-based start index.
- **Semantics**: **Case-insensitive** string search. Returns 1-based position.
- **Error Handling**: Returns `#VALUE!` if not found.
- **Example**: `=SEARCH("c", "Pacific")` returns `3` (matches first 'C').

#### 31. `REPLACE(old_text, start_num, num_chars, new_text)`
- **Arguments**:
  - `old_text`: Original string.
  - `start_num`: 1-based position to start replacement.
  - `num_chars`: Number of characters to replace.
  - `new_text`: Replacement string.
- **Semantics**: Replaces character span with new string.
- **Example**: `=REPLACE("abcdef", 3, 2, "ZZ")` returns `"abZZef"`.

#### 32. `REPT(text, number_times)`
- **Arguments**:
  - `text`: String to repeat.
  - `number_times`: Integer repetition count $\ge 0$.
- **Semantics**: Returns string repeated $N$ times. If `number_times == 0`, returns `""`.
- **Error Handling**: Returns `#VALUE!` if `number_times < 0`.
- **Example**: `=REPT("*-", 3)` returns `"*-*-*-"`.

#### 33. `TEXTSPLIT(text, col_delimiter, [row_delimiter], [ignore_empty], [match_mode], [pad_with])`
- **Arguments**:
  - `text`: String to split.
  - `col_delimiter`: Delimiter character or string to split columns.
  - `row_delimiter` *(optional)*: Delimiter for rows.
  - `ignore_empty` *(optional, default FALSE)*: Skip consecutive delimiters.
  - `match_mode` *(optional, default 0)*: 0 (case-sensitive), 1 (case-insensitive).
  - `pad_with` *(optional, default `#N/A`)*: Value to pad ragged 2D matrix.
- **Semantics**: Splits text string across columns and rows (Excel 365 dynamic text function).
- **Example**: `=TEXTSPLIT("A,B,C", ",")` returns array `{"A", "B", "C"}`.

#### 34. `TEXTBEFORE(text, delimiter, [instance_num], [match_mode], [match_end], [if_not_found])`
- **Arguments**:
  - `text`: Source text.
  - `delimiter`: Boundary string.
  - `instance_num` *(optional, default 1)*: Which occurrence (negative counts from end).
  - `match_mode` *(optional, default 0)*: 0 (case-sensitive), 1 (case-insensitive).
  - `match_end` *(optional, default 0)*: 1 treats end of string as delimiter.
  - `if_not_found` *(optional)*: Value if delimiter not found.
- **Semantics**: Returns substring occurring before delimiter.
- **Error Handling**: Returns `#N/A` if delimiter not found and `if_not_found` omitted.
- **Example**: `=TEXTBEFORE("john.doe@domain.com", "@")` returns `"john.doe"`.

#### 35. `TEXTAFTER(text, delimiter, [instance_num], [match_mode], [match_end], [if_not_found])`
- **Arguments**: Same parameter signature as `TEXTBEFORE`.
- **Semantics**: Returns substring occurring after delimiter.
- **Example**: `=TEXTAFTER("john.doe@domain.com", "@")` returns `"domain.com"`.

#### 36. `CHAR(number)`
- **Arguments**: `number`: Integer between 1 and 255.
- **Semantics**: Returns character corresponding to character code (ASCII / Windows-1252).
- **Error Handling**: Returns `#VALUE!` if `number < 1` or `number > 255`.
- **Example**: `=CHAR(65)` returns `"A"`, `=CHAR(10)` returns newline `\n`.

#### 37. `CODE(text)`
- **Arguments**: `text`: String.
- **Semantics**: Returns numeric code (1–255) of the first character of `text`.
- **Error Handling**: Returns `#VALUE!` if `text` is empty.
- **Example**: `=CODE("A")` returns `65`.

#### 38. `CLEAN(text)`
- **Arguments**: `text`: String.
- **Semantics**: Removes all non-printable ASCII control characters (characters with ASCII code 0 to 31) from string.
- **Example**: `=CLEAN(CHAR(7) & "Hello" & CHAR(9))` returns `"Hello"`.

---

### Group 5: Math & Statistical Functions (9 Functions)

#### 39. `RANDBETWEEN(bottom, top)`
- **Arguments**:
  - `bottom`: Minimum integer.
  - `top`: Maximum integer.
- **Semantics**: Returns random integer in range $[bottom, top]$ inclusive.
- **Error Handling**: Returns `#NUM!` if `bottom > top`. Returns `#VALUE!` if non-numeric.
- **Example**: `=RANDBETWEEN(1, 100)`.

#### 40. `RAND()`
- **Arguments**: None.
- **Semantics**: Returns evenly distributed random floating-point number in $[0, 1)$.
- **Example**: `=RAND()`.

#### 41. `CEILING(number, significance)`
- **Arguments**:
  - `number`: Numeric value.
  - `significance`: Multiple to round up to.
- **Semantics**: Rounds `number` up (away from zero) to nearest multiple of `significance`.
- **Error Handling**: Returns `#DIV/0!` if `significance == 0`. Returns `#NUM!` if `number` and `significance` have opposite signs.
- **Example**: `=CEILING(2.5, 1)` returns `3`, `=CEILING(-2.5, -1)` returns `-3`.

#### 42. `FLOOR(number, significance)`
- **Arguments**:
  - `number`: Numeric value.
  - `significance`: Multiple to round down to.
- **Semantics**: Rounds `number` down (toward zero) to nearest multiple of `significance`.
- **Error Handling**: Returns `#DIV/0!` if `significance == 0`. Returns `#NUM!` if opposite signs.
- **Example**: `=FLOOR(2.8, 1)` returns `2`.

#### 43. `SIGN(number)`
- **Arguments**: `number`: Numeric value.
- **Semantics**: Returns `1` if number is positive, `0` if zero, `-1` if negative.
- **Example**: `=SIGN(-42)` returns `-1`.

#### 44. `STDEV(number1, [number2], ...)`
- **Arguments**: Numbers or range of numbers (e.g. `A1:A50`).
- **Semantics**: Calculates sample standard deviation:
  $$s = \sqrt{\frac{\sum_{i=1}^n (x_i - \bar{x})^2}{n - 1}}$$
- **Error Handling**: Returns `#DIV/0!` if count of numeric values $n < 2$.
- **Example**: `=STDEV(10, 12, 23, 23, 16, 23, 21, 16)` returns `5.2372`.

#### 45. `VAR(number1, [number2], ...)`
- **Arguments**: Numbers or range of numbers.
- **Semantics**: Calculates sample variance:
  $$s^2 = \frac{\sum_{i=1}^n (x_i - \bar{x})^2}{n - 1}$$
- **Error Handling**: Returns `#DIV/0!` if $n < 2$.
- **Example**: `=VAR(2, 4, 6)` returns `4.0`.

#### 46. `RANK(number, ref, [order])`
- **Arguments**:
  - `number`: Value to rank.
  - `ref`: Range of numbers to rank against.
  - `order` *(optional, default 0)*:
    - `0` (or omitted): Descending order (largest number is rank 1).
    - Non-zero: Ascending order (smallest number is rank 1).
- **Semantics**: Returns rank position. Duplicate numbers receive same rank.
- **Error Handling**: Returns `#N/A` if `number` does not exist in `ref`.
- **Example**: `=RANK(7, {3, 5, 7, 9}, 0)` returns `2`.

#### 47. `MODE(number1, [number2], ...)`
- **Arguments**: Numbers or range of numbers.
- **Semantics**: Returns most frequently occurring value in dataset.
- **Error Handling**: Returns `#N/A` if no duplicate values exist.
- **Example**: `=MODE(1, 2, 2, 3, 4)` returns `2`.

---

### Group 6: Information Functions (5 Functions)

#### 48. `ISNUMBER(value)`
- **Arguments**: `value`: Any token or cell reference.
- **Semantics**: Returns TRUE if `value` is a numeric type (or cell with `CellType.Number`); returns FALSE for text, booleans, empty, or errors.
- **Example**: `=ISNUMBER(123)` returns `TRUE`, `=ISNUMBER("123")` returns `FALSE`.

#### 49. `ISTEXT(value)`
- **Arguments**: `value`: Any token or cell reference.
- **Semantics**: Returns TRUE if `value` is a text string (or cell with `CellType.String`); returns FALSE for numbers, booleans, or errors.
- **Example**: `=ISTEXT("Hello")` returns `TRUE`, `=ISTEXT(45)` returns `FALSE`.

#### 50. `ISBLANK(reference)`
- **Arguments**: `reference`: Cell address (e.g. `A1`).
- **Semantics**: Returns TRUE if referenced cell has no data (`CellType.Empty` or null); returns FALSE if cell contains text, numbers, or formulas (even if formula evaluates to `""`).
- **Example**: `=ISBLANK(Z100)` returns `TRUE`.

#### 51. `ISERROR(value)`
- **Arguments**: `value`: Any token or formula expression.
- **Semantics**: Returns TRUE if `value` evaluates to any error (`#N/A`, `#VALUE!`, `#REF!`, `#DIV/0!`, `#NUM!`, `#NAME?`, `#NULL!`); FALSE otherwise.
- **Example**: `=ISERROR(1/0)` returns `TRUE`.

#### 52. `ISNA(value)`
- **Arguments**: `value`: Any token or formula expression.
- **Semantics**: Returns TRUE **strictly** if `value` is `#N/A`; returns FALSE for all other errors (e.g. `#DIV/0!`) and valid values.
- **Example**: `=ISNA(#N/A)` returns `TRUE`, `=ISNA(#VALUE!)` returns `FALSE`.

---

## 7. Phase 2: File Format Parsers & Exporters Architecture

### 7.1 OpenDocument Spreadsheet (.ods) Handler
- **Architecture**: Bidirectional Zip packaging complying with OASIS OpenDocument v1.2 / v1.3 standard.
- **Package Layout**:
  - `mimetype`: Uncompressed file containing `application/vnd.oasis.opendocument.spreadsheet`. Must be first file in ZIP archive.
  - `META-INF/manifest.xml`: Declares `mimetype`, `content.xml`, `styles.xml`.
  - `content.xml`: Primary spreadsheet payload.
- **Parsing Schema (`content.xml`)**:
  - Root: `<office:document-content>` $\rightarrow$ `<office:body>` $\rightarrow$ `<office:spreadsheet>`.
  - Sheets: `<table:table table:name="SheetName">`.
  - Rows: `<table:table-row table:number-rows-repeated="N">`.
  - Cells: `<table:table-cell office:value-type="float|string|date|boolean" office:value="..." office:date-value="..." table:formula="..." table:number-columns-repeated="M">`.
  - Cell Text: `<text:p>Cell text</text:p>`.
- **ODS Formula Translation**:
  - OpenDocument prefixes formulas with `of:=` (e.g. `of:=SUM([.A1:.A10])`).
  - Strip `of:=` and transform bracket notation `[.A1]` to `A1`.
  - When saving to ODS, translate `=SUM(A1:A10)` to `of:=SUM([.A1:.A10])`.

### 7.2 Excel 97-2003 (.xls) Genuine BIFF8 Binary Reader
- **Container Architecture**: Microsoft Compound File Binary Format (CFBF / OLE2 structured storage).
  - Header validation: 8-byte magic number `0xD0 0xCF 0x11 0xE0 0xA1 0xB1 0x1A 0xE1`.
  - Read SAT (Sector Allocation Table) and Directory Entry Sector.
  - Locate Directory Entry with name `"Workbook"` (or `"Book"`).
  - Stream all chained sectors to assemble contiguous `byte[] workbookStream`.
- **BIFF8 Record Engine**:
  Records are parsed sequentially as:
  ```text
  [RecordID: uint16 (2 bytes)] [RecordLength: uint16 (2 bytes)] [RecordData: byte[RecordLength]]
  ```
- **Crucial BIFF8 Record Handlers**:
  1. `0x0809` (`BOF`): Beginning of Stream. Type `0x0005` indicates Workbook Globals; Type `0x0010` indicates Worksheet.
  2. `0x00FC` (`SST` - Shared String Table):
     Reads total unique strings count, extracts 8-bit compressed or 16-bit uncompressed UTF-16LE characters, handling boundary-crossing `0x003C` (`CONTINUE`) records.
  3. `0x0085` (`BOUNDSHEET`):
     Reads 4-byte stream file position offset to worksheet `BOF`, sheet visibility byte, and string sheet name.
  4. `0x0031` (`FONT`) & `0x00E0` (`XF`):
     Builds font table and cell formatting lookup indices.
  5. **Worksheet Substream Cell Records**:
     - `0x00FD` (`LABELSST`): `Row (uint16)`, `Col (uint16)`, `XFIndex (uint16)`, `SSTIndex (uint32)`. Resolves string from SST.
     - `0x0203` (`NUMBER`): `Row (uint16)`, `Col (uint16)`, `XFIndex (uint16)`, `Value (IEEE 754 64-bit double)`.
     - `0x027E` (`RK`): Decodes compressed 30-bit integer/float format.
     - `0x00BD` (`MULRK`): Array of RK cells in a single row.
     - `0x0006` (`FORMULA`): Reads evaluated result and token stream.
     - `0x0207` (`STRING`): Cached string result following `FORMULA`.
     - `0x0001` (`BLANK`) & `0x00BE` (`MULBLANK`): Formatted blank cells.
     - `0x000A` (`EOF`): End of Worksheet.

### 7.3 Web & Delimited Format Exporters
1. **HTML Table Export (`.html`)**:
   - Generates complete HTML5 document with inline responsive CSS.
   - Outputs table markup: `<table>`, `<tr>`, `<td>`, `<th>`.
   - Multi-sheet support: Generates tabbed sheet selector at bottom with vanilla JavaScript tab switcher.
   - Styling: Exports cell fonts, colors, borders, background colors, and column widths (`style="width: ...px"`).
   - Cell spanning: Correctly maps `MergeRowSpan` $\rightarrow$ `rowspan` and `MergeColSpan` $\rightarrow$ `colspan`.
2. **TSV Export (`.tsv`)**:
   - Tab-separated values with `\t` separator and `\r\n` line endings.
   - Escapes quotes `""` and handles multiline cells.
3. **PRN Export (`.prn`)**:
   - Lotus/Excel formatted space-delimited text.
   - Calculates max visual column width for each column.
   - Formats numbers right-aligned and strings left-aligned, padded with ASCII spaces (`0x20`).

---

## 8. Features Discovered & Edge Cases Tables

### 8.1 Features Discovered
| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | OpenXML Drawings | Load DrawingML Shapes | Parse `xl/drawings/drawing1.xml` for `<xdr:sp>` geometry | DrawingML XML | `DocShapeObject` (Rectangle, Oval, etc.) | Unknown geometry falls back to Rectangle | `src/format/XlsxHandler.cs` line 1293 |
| 2 | OpenXML Drawings | Load Embedded Images | Parse `xl/drawings/_rels/` & `xl/media/` for `<xdr:pic>` | PNG/JPEG bytes | `DocShapeObject.CachedImage` | Corrupt image falls back to null | `src/format/XlsxHandler.cs` line 1235 |
| 3 | OpenXML Drawings | Load Embedded Charts | Parse `xl/charts/chart1.xml` for `<xdr:graphicFrame>` | ChartML XML | `DocShapeObject.ChartData` | Missing chart xml yields empty chart | `src/format/XlsxHandler.cs` line 1250 |
| 4 | OpenXML Drawings | `CalcView.FloatingObjects` | Unified shape/picture property alias | `List<DocShapeObject>` | Access to `Shapes` | No error | `ORIGINAL_REQUEST.md` item 1.1 |
| 5 | PDF Export | Calc Sheet PDF Exporter | Vector PDF export for spreadsheet sheets | `CalcView` model | Standard PDF-1.7 binary file | Returns false on disk write failure | `src/ui/AppForm.cs` line 4210 |
| 6 | PDF Export | Multi-page 2D Pagination | Splits oversized grids across row & col page bands | Grid bounding box | Multi-page `PdfDocument` | Empty sheet produces 1 empty page | `src/core/PdfDocumentModel.cs` |
| 7 | Data Utilities | Calc Find & Replace | Floating search/replace modal with formula safety | Search strings, options | Navigates & updates cells | Not found displays notification | `src/ui/FindReplaceForm.cs` |
| 8 | Data Utilities | Calc Remove Duplicates | Modal dialog with column selection checklist | Selected range, col checklist | Shifts rows up, removes duplicates | Single row warns user | `src/ui/CalcView.cs` line 7062 |
| 9 | Data Utilities | Text to Columns Wizard | Delimited and fixed-width column parsing | Delimiters, qualifier | Splits text into adjacent columns | Overwrite warns user | `ORIGINAL_REQUEST.md` item 1.3 |
| 10 | Formulas: Lookup | `INDEX` | Array row & column value extraction | Array, row, [col] | Value at coordinate | Out of bounds $\rightarrow$ `#REF!` | `FormulaEngine.cs` line 850 |
| 11 | Formulas: Lookup | `MATCH` | Linear search in 1D array | Val, array, [type] | 1-based index | Not found $\rightarrow$ `#N/A` | `FormulaEngine.cs` line 873 |
| 12 | Formulas: Lookup | `XLOOKUP` | Advanced bidirectional lookup | Val, lookup, return | Matched value | Not found $\rightarrow$ `#N/A` or fallback | `FormulaEngine.cs` line 817 |
| 13 | Formulas: Lookup | `XMATCH` | Advanced position lookup | Val, lookup, [modes] | 1-based index | Not found $\rightarrow$ `#N/A` | `ORIGINAL_REQUEST.md` item 2.1 |
| 14 | Formulas: Lookup | `ROW` / `COLUMN` | Coordinate number extraction | [Reference] | Integer 1-based index | Invalid ref $\rightarrow$ `#REF!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 15 | Formulas: Lookup | `ROWS` / `COLUMNS` | Dimension size calculation | Range array | Count of rows/columns | Invalid range $\rightarrow$ `#VALUE!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 16 | Formulas: Lookup | `INDIRECT` | Dynamic string address reference | Text address, [a1] | Target cell value | Invalid address $\rightarrow$ `#REF!` | `FormulaEngine.cs` line 914 |
| 17 | Formulas: Lookup | `OFFSET` | Relative reference displacement | Base, rows, cols | Offset cell value | Out of bounds $\rightarrow$ `#REF!` | `FormulaEngine.cs` line 892 |
| 18 | Formulas: Logical | `IFS` | Multi-branch conditional | Pairs of cond/val | Matched branch value | None TRUE $\rightarrow$ `#N/A` | `FormulaEngine.cs` line 933 |
| 19 | Formulas: Logical | `SWITCH` | Multi-value expression match | Expr, val/res pairs | Matched result | No match $\rightarrow$ `#N/A` or default | `FormulaEngine.cs` line 958 |
| 20 | Formulas: Logical | `IFERROR` | Universal error trapping | Expr, fallback | Expr or fallback | Traps all `#` errors | `FormulaEngine.cs` line 978 |
| 21 | Formulas: Logical | `IFNA` | `#N/A`-specific error trapping | Expr, fallback | Expr or fallback | Traps strictly `#N/A` | `ORIGINAL_REQUEST.md` item 2.1 |
| 22 | Formulas: Logical | `XOR` | Exclusive OR logic | Logical expressions | Boolean TRUE/FALSE | No logicals $\rightarrow$ `#VALUE!` | `FormulaEngine.cs` line 946 |
| 23 | Formulas: Date | `YEAR`/`MONTH`/`DAY` | Date component extraction | Date serial/string | Integer year/month/day | Unparseable $\rightarrow$ `#VALUE!` | `FormulaEngine.cs` line 1040 |
| 24 | Formulas: Date | `HOUR`/`MINUTE`/`SECOND` | Time component extraction | Time serial/string | Integer hour/min/sec | Unparseable $\rightarrow$ `#VALUE!` | `FormulaEngine.cs` line 1064 |
| 25 | Formulas: Date | `WEEKDAY` | Day of week calculation | Date serial, [type] | Integer 1..7 | Invalid date $\rightarrow$ `#VALUE!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 26 | Formulas: Date | `EDATE` | Month arithmetic | Date, months | Shifted OADate | Invalid date $\rightarrow$ `#VALUE!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 27 | Formulas: Date | `EOMONTH` | End of month date | Date, months | Month-end OADate | Invalid date $\rightarrow$ `#VALUE!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 28 | Formulas: Date | `NETWORKDAYS` | Business working days count | Start, end, [holidays] | Integer day count | Invalid date $\rightarrow$ `#VALUE!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 29 | Formulas: Date | `DATEDIF` | Date span difference | Start, end, unit | Integer span in unit | Start > End $\rightarrow$ `#NUM!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 30 | Formulas: Text | `TEXT` | Number format serialization | Number, format string | Formatted string | Invalid format $\rightarrow$ raw number | `FormulaEngine.cs` line 1022 |
| 31 | Formulas: Text | `VALUE` | Text to numeric conversion | Text string | Double float | Non-numeric $\rightarrow$ `#VALUE!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 32 | Formulas: Text | `FIND` | Case-sensitive substring find | Find, within, [start] | 1-based char position | Not found $\rightarrow$ `#VALUE!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 33 | Formulas: Text | `SEARCH` | Case-insensitive wildcard find | Find, within, [start] | 1-based char position | Not found $\rightarrow$ `#VALUE!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 34 | Formulas: Text | `REPLACE` | Span replacement | Text, start, len, new | Modified string | Out of bounds $\rightarrow$ `#VALUE!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 35 | Formulas: Text | `REPT` | String repetition | Text, count | Repeated string | Count < 0 $\rightarrow$ `#VALUE!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 36 | Formulas: Text | `TEXTSPLIT` | 2D dynamic text splitting | Text, col_delim, [row] | Token array | Empty text $\rightarrow$ `""` | `ORIGINAL_REQUEST.md` item 2.1 |
| 37 | Formulas: Text | `TEXTBEFORE` / `TEXTAFTER` | Text extraction around delim | Text, delim, [inst] | Extracted substring | Delim not found $\rightarrow$ `#N/A` | `ORIGINAL_REQUEST.md` item 2.1 |
| 38 | Formulas: Text | `CHAR` / `CODE` | ASCII code/character convert | Int code / String char | Char / Int code | Out of range $\rightarrow$ `#VALUE!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 39 | Formulas: Text | `CLEAN` | Strips control characters | Text string | Cleaned string | None | `ORIGINAL_REQUEST.md` item 2.1 |
| 40 | Formulas: Math | `RANDBETWEEN` / `RAND` | Pseudo-random number gen | [Bottom, top] | Random int / float | Bottom > Top $\rightarrow$ `#NUM!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 41 | Formulas: Math | `CEILING` / `FLOOR` | Round to significance multiple | Number, significance | Rounded double | Sig == 0 $\rightarrow$ `#DIV/0!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 42 | Formulas: Math | `SIGN` | Mathematical sign function | Number | +1, 0, or -1 | Non-numeric $\rightarrow$ `#VALUE!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 43 | Formulas: Math | `STDEV` / `VAR` | Sample standard dev & variance | Numbers / Range | Calculated float | Count < 2 $\rightarrow$ `#DIV/0!` | `ORIGINAL_REQUEST.md` item 2.1 |
| 44 | Formulas: Math | `RANK` | Relative numerical rank | Number, ref, [order] | 1-based rank | Not in ref $\rightarrow$ `#N/A` | `ORIGINAL_REQUEST.md` item 2.1 |
| 45 | Formulas: Math | `MODE` | Most frequent dataset value | Numbers / Range | Modal value | No duplicates $\rightarrow$ `#N/A` | `ORIGINAL_REQUEST.md` item 2.1 |
| 46 | Formulas: Info | `ISNUMBER` | Numeric type detector | Value token | Boolean TRUE/FALSE | Never throws error | `ORIGINAL_REQUEST.md` item 2.1 |
| 47 | Formulas: Info | `ISTEXT` | Text type detector | Value token | Boolean TRUE/FALSE | Never throws error | `ORIGINAL_REQUEST.md` item 2.1 |
| 48 | Formulas: Info | `ISBLANK` | Empty cell detector | Cell reference | Boolean TRUE/FALSE | Never throws error | `ORIGINAL_REQUEST.md` item 2.1 |
| 49 | Formulas: Info | `ISERROR` | Any error detector | Value token | Boolean TRUE/FALSE | Never throws error | `ORIGINAL_REQUEST.md` item 2.1 |
| 50 | Formulas: Info | `ISNA` | `#N/A` specific detector | Value token | Boolean TRUE/FALSE | Never throws error | `ORIGINAL_REQUEST.md` item 2.1 |
| 51 | Formats: ODS | OpenDocument Spreadsheet Reader | Parses `.ods` ZIP & `content.xml` | `.ods` file path | Populated `CalcView` | Invalid ZIP throws false | `ORIGINAL_REQUEST.md` item 2.2 |
| 52 | Formats: ODS | OpenDocument Spreadsheet Writer | Writes `.ods` ZIP & `content.xml` | `CalcView` model | Valid `.ods` file | Write failure returns false | `ORIGINAL_REQUEST.md` item 2.2 |
| 53 | Formats: XLS | BIFF8 Binary Reader | Parses OLE2 CFBF & BIFF8 records | Legacy `.xls` file path | Populated `CalcView` | Corrupted OLE2 returns false | `ORIGINAL_REQUEST.md` item 2.2 |
| 54 | Formats: Web | HTML Table Exporter | Generates HTML5 with sheet tabs | `CalcView` model | `.html` file | Write failure returns false | `ORIGINAL_REQUEST.md` item 2.2 |
| 55 | Formats: Delim | TSV Exporter | Exports tab-separated values | `CalcView` model | `.tsv` file | Write failure returns false | `ORIGINAL_REQUEST.md` item 2.2 |
| 56 | Formats: Delim | PRN Exporter | Exports space-aligned text | `CalcView` model | `.prn` file | Write failure returns false | `ORIGINAL_REQUEST.md` item 2.2 |

### 8.2 Edge Cases
| # | Feature | Input / Scenario | Observed / Expected Behavior |
|---|---------|------------------|------------------------------|
| 1 | OpenXML Drawings | Workbook has drawing parts but no `xl/media/` | Shapes load properly; missing images display placeholder contour without crashing |
| 2 | OpenXML Drawings | Anchor coordinates reference column > 250 or row > 1000 | Safely clamped to max loaded matrix boundaries; prevents out-of-range exceptions |
| 3 | OpenXML Drawings | File saved without drawings opened then saved | `Shapes` list is preserved; empty list skips drawing tags cleanly |
| 4 | PDF Export | Empty sheet with 0 populated cells | Emits single A4 page with default 10x5 gridlines instead of throwing divide-by-zero |
| 5 | PDF Export | Grid wider than page width (e.g. 50 columns) | Splits horizontally into column page bands ("Page 1: Cols A-H", "Page 2: Cols I-P") |
| 6 | PDF Export | Merged cells spanning page boundary | Clips merged box cleanly at page margin edge; child cells stay suppressed |
| 7 | Find & Replace | Query not found in sheet | Displays friendly MessageBox "Không tìm thấy nội dung yêu cầu", selection unchanged |
| 8 | Find & Replace | "Look in Formulas" replacing cell reference | Replaces within `=SUM(A1:A10)` to `=SUM(B1:A10)`; triggers sheet re-evaluation pass |
| 9 | Find & Replace | "Look in Values" on formula cell | Does not alter underlying `FormulaStr`, preserving user calculation logic |
| 10 | Remove Duplicates | Entire range selected has 1 row | Shows warning "Vui lòng chọn vùng dữ liệu có nhiều dòng để xóa trùng lặp" |
| 11 | Remove Duplicates | No columns checked in checklist modal | Shows warning requiring at least 1 column checked before proceeding |
| 12 | Remove Duplicates | Multi-column composite key deduplication | Rows match only if values in all checked columns match simultaneously |
| 13 | Text to Columns | Consecutive delimiters with "Treat as one" ON | Multiple spaces or commas collapse into single column split |
| 14 | Text to Columns | Delimiter inside quoted string (`"New York, NY", USA`) | Comma inside quotes is ignored; splits only outer commas |
| 15 | Formulas: `INDEX` | Row or column index is 0 | Returns full row or column slice array |
| 16 | Formulas: `INDEX` | Row or column index exceeds range bounds | Returns `#REF!` immediately |
| 17 | Formulas: `MATCH` | Search in unsorted array with `match_type = 0` | Scans linearly; returns exact match index or `#N/A` |
| 18 | Formulas: `XLOOKUP` | Key not found with `if_not_found` specified | Returns user-supplied `if_not_found` literal without returning `#N/A` |
| 19 | Formulas: `IFNA` | Value expression produces `#DIV/0!` | Does NOT trap `#DIV/0!`; returns `#DIV/0!` unchanged |
| 20 | Formulas: `IFERROR` | Value expression produces `#DIV/0!` | Traps `#DIV/0!`; returns user fallback value |
| 21 | Formulas: `DATEDIF` | `start_date > end_date` | Returns `#NUM!` per Excel standard |
| 22 | Formulas: `VALUE` | String contains currency or commas (`"$1,250.50"`) | Strips formatting symbols and parses to numeric `1250.5` |
| 23 | Formulas: `FIND` vs `SEARCH` | Searching `"A"` in `"apple"` | `FIND` returns `#VALUE!`; `SEARCH` returns `1` |
| 24 | Formulas: `STDEV` | Only 1 numeric value provided | Returns `#DIV/0!` because denominator is $N - 1 = 0$ |
| 25 | Formats: ODS | Cells with `table:number-columns-repeated="50"` | Expands repeated cells across 50 columns without infinite loops |
| 26 | Formats: XLS BIFF8 | Shared String Table spanning `CONTINUE` records | Assembles split Unicode character buffers across record boundaries |
| 27 | Formats: PRN | Numbers and text in same column | Numbers formatted right-aligned; text formatted left-aligned |

---

## 9. Verification & Acceptance Criteria Matrix

| Item | Phase | Acceptance Criterion | Verification Method | Status |
|:---:|:---:|---|---|:---:|
| 1 | P1 | Saving an `.xlsx` with 1 chart, 1 shape, and 1 picture, then reloading through `LoadXlsx` reproduces all 3 objects at exact anchor coordinates | End-to-end roundtrip test: save `.xlsx` with visual assets, reload into new `CalcView`, assert `FloatingObjects.Count == 3` with matching positions and types | Spec Complete |
| 2 | P1 | Exporting a multi-sheet table to PDF produces a valid, readable PDF file matching print preview | Invoke `CalcView.ExportToPdf`, verify PDF header `%PDF-1.7`, check page count and cross-reference table | Spec Complete |
| 3 | P1 | Find & Replace successfully finds occurrences and replaces all matching cell values without damaging formulas | Automated search/replace tests across static cells and formula cells; verify formula AST integrity | Spec Complete |
| 4 | P1 | Remove Duplicates removes redundant rows and updates sheet row spans correctly | Execute deduplication on test dataset with duplicate rows; assert row count reduction | Spec Complete |
| 5 | P1 | Text to Columns correctly splits comma-separated and tab-separated cell text across adjacent columns | Run delimited parser on CSV/TSV text strings; assert adjacent column cell values | Spec Complete |
| 6 | P2 | Automated test suite executing all 52 new formulas passes 100% of test assertions matching Excel output | Headless C# test harness calling `FormulaEngine.EvaluateFormula` against 52 truth tables | Spec Complete |
| 7 | P2 | Sample `.ods` file opens, loads all cell contents and formatting, and saves back out cleanly | Load `.ods` archive, assert cell values in `CalcView`, save to new `.ods`, verify roundtrip fidelity | Spec Complete |
| 8 | P2 | Legacy `.xls` (BIFF8) file opens without ZIP/XML parsing exceptions | Load binary `.xls` via BIFF8 reader, verify no `InvalidDataException`, assert sheet names and cells | Spec Complete |
| 9 | P2 | Export to HTML produces clean table markup rendering accurately in standard web browsers | Save `.html`, verify valid HTML5 structure, `<table>`, `<tr>`, `<td>`, and embedded stylesheet | Spec Complete |
| 10 | Build | Solution builds cleanly (`build.bat`) with 0 compilation errors under C# 5 compiler | Execute `build.bat` via command line; assert exit code 0 | Verified Clean (C# 5) |
