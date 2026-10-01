# ZenQ Calc Phase 3 & Phase 4 Specification & Architecture Mining Report

**Author**: Survey Agent 3 (Phases 3 & 4 Spec & Architecture Miner)  
**Date**: 2026-09-28  
**Scope**: 
- Phase 3: Structured Tables (ListObject) & Dynamic Arrays (Spill Engine, LET, LAMBDA)
- Phase 4: PivotTables & PivotCharts, What-If Analysis (Goal Seek & Data Tables), Advanced Printing & Protection (Workbook Structure Protection, Print Titles, Manual Page Breaks)
**Project Root**: `c:\Users\Danh\Desktop\genofficepy`  
**Compiler Constraint**: Microsoft Visual C# Compiler version 4.8 (`csc.exe` for C# 5), target .NET Framework 4.8 WinForms (`build.bat`).

---

## 1. Executive Summary & Architectural Context

ZenQ Calc is a desktop spreadsheet component within the GenOffice suite built with .NET 4.8 WinForms. It relies on a custom `SparseMatrix` memory grid, `FormulaEngine` recursive descent parser, and `CalcView` vector rendering engine.

### Current Baseline & Gaps Found
1. **Excel Tables (ListObject)**:
   - *Existing*: Basic data structure `ExcelTableInfo` in `CalcTableAndPivotDialogs.cs`, basic zebra styling and table column sorting/filtering in `CalcView.cs` (lines 5095-5340, 6217-6300), and `DrawCalcTabTableDesign` in `RibbonBar.cs`.
   - *Gaps*: Missing `Ctrl+T` shortcut hook in `CalcView.HandleKeyDown`; dynamic auto-expansion on typing in adjacent cells (`EndRow + 1` or `EndCol + 1`) is not triggered during cell edit commit (`CommitEditAndMove`); Total Row only supports a hardcoded numeric sum instead of per-column selectable aggregations; structured formula references (e.g. `Table1[@Sales] * 1.1` or `Table1[Sales]`) are not parsed or resolved by `FormulaEngine`; OpenXML table part roundtripping (`xl/tables/table{N}.xml` and sheet `<tableParts>`) is entirely absent from `XlsxHandler.cs`.

2. **Dynamic Arrays & Spill Engine**:
   - *Existing*: Formula evaluation is strictly scalar (evaluates one cell to one number/string).
   - *Gaps*: No array return type (`CellValue[,]`) in `FormulaEngine`; no spill engine or collision detection; `#SPILL!` lifecycle is absent; no `#` spill operator; array functions (`SORT`, `SORTBY`, `FILTER`, `UNIQUE`, `SEQUENCE`, `RANDARRAY`) and scoping functions (`LET`, `LAMBDA`) are not implemented; `CalcView.Draw` lacks the modern Excel thin blue bounding rectangle around spilled ranges.

3. **PivotTables & PivotCharts**:
   - *Existing*: `CreatePivotTableDialogForm` exists, but `CalcView.CreatePivotTable` only emits a static, hardcoded summary table with fixed row labels.
   - *Gaps*: Missing interactive Field List Task Pane (`PivotTableFieldListControl`) with the 4 drag-drop zones (Filters, Columns, Rows, Values); missing multi-dimensional aggregation engine (Sum, Count, Average, Min, Max, % of Row/Col/Grand Total); missing numeric binning and date hierarchy grouping (Year/Quarter/Month/Day); missing linked PivotCharts that react dynamically to pivot aggregations; OpenXML pivot parts (`xl/pivotTables/` and `xl/pivotCache/`) are not handled.

4. **What-If Analysis**:
   - *Existing*: Completely absent from codebase.
   - *Gaps*: Goal Seek dialog (`GoalSeekDialogForm`) and Secant/Newton iterative solver (converging within 0.001 tolerance) are missing; Data Tables dialog (`DataTableDialogForm`) and 1-variable/2-variable sensitivity substitution engine are missing.

5. **Advanced Printing & Protection**:
   - *Existing*: Sheet-level protection exists (`ProtectSheetDialog.cs`, `CalcView.ToggleProtectSheet`). Print Area selection exists (`CalcView.SetPrintArea`).
   - *Gaps*: Workbook structure protection is missing (no model, dialog, or enforcement preventing sheet add/delete/rename/reorder/hide); Print Titles (`$1:$2` row repetition, `$A:$B` column repetition across printed pages) is missing; manual Page Breaks (horizontal row breaks and vertical column breaks with dashed line rendering and PDF/print pagination) are missing; OpenXML `<workbookProtection lockStructure="1"/>`, `<definedName name="_xlnm.Print_Titles">`, and `<rowBreaks>`/`<colBreaks>` are not saved or loaded.

---

## 2. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Tables | Create Table (`Ctrl+T`) | Converts selected range into a formatted Excel Table with headers and filter drop-downs | Selected range string, `HasHeaders` bool | `ExcelTableInfo` added to sheet, styled range | If selection < 1x1, prompt user; overlaps handled | ORIGINAL_REQUEST §Phase 3.1, `CalcTableAndPivotDialogs.cs` |
| 2 | Tables | Dynamic Auto-Expansion | Automatically expands table boundary when user types in row immediately below table or column immediately to the right | Cell edit commit at `(EndRow+1, c)` or `(r, EndCol+1)` | Table `EndRow++` or `EndCol++`, format extended, total row pushed down | Validates boundary limits; avoids expanding if input is empty | ORIGINAL_REQUEST §Phase 3.1, `CalcView.cs:5110` |
| 3 | Tables | Per-Column Total Row Aggregations | Bottom Total Row supporting per-column aggregation functions (SUM, AVERAGE, COUNT, COUNTA, MIN, MAX, STDDEV, VAR) | Column index, aggregation enum | Emits `=SUBTOTAL(funcCode, Table[Col])` and calculates total | Disables or falls back to count for text columns | ORIGINAL_REQUEST §Phase 3.1, `CalcView.cs:5260` |
| 4 | Tables | Structured References | Formula syntax referencing table elements (e.g. `Table1[@Sales] * 1.1`, `Table1[Sales]`, `Table1[[#Totals],[Sales]]`) | Formula text containing `TableName[...]` or `[...]` | Resolved cell range or single cell reference | Returns `#REF!` if column or table not found; `#VALUE!` if `@` outside table row | ORIGINAL_REQUEST §Phase 3.1, Excel Calc Specs |
| 5 | Tables | OpenXML Table Persistence | Roundtrips table definitions in `xl/tables/table{N}.xml` and sheet `<tableParts>` | `ExcelTableInfo` objects | ECMA-376 Table XML parts and relationships | Missing table parts gracefully ignored on open | ECMA-376 §12.3.23 / `XlsxHandler.cs` |
| 6 | Dynamic Arrays | 2D Array Return Type & Spill Mechanics | Calculation engine evaluates formulas returning 2D arrays and spills them into adjacent empty cells | Array-valued formula expression | Array output written to anchor and ghost/spilled cells | Returns `#SPILL!` if any destination cell is non-empty | ORIGINAL_REQUEST §Phase 3.2, Excel 365 Specs |
| 7 | Dynamic Arrays | Collision Detection & `#SPILL!` Lifecycle | Detects when spilled array footprint is blocked by non-empty cells or sheet borders | Bounding box `[r0..r0+H-1, c0..c0+W-1]` | Sets `#SPILL!` on anchor cell; highlights collision zone | Clear blocking cells to automatically re-spill | ORIGINAL_REQUEST §Phase 3.2, Excel 365 Specs |
| 8 | Dynamic Arrays | Spill Reference Operator (`#`) | References entire spilled array range originating at a given cell (e.g. `=SUM(A1#)`) | Cell reference followed by `#` (e.g. `A1#`) | Evaluates to range `A1:C10` | Returns `#REF!` if referenced cell is not a spill anchor | Excel 365 Dynamic Array Spec |
| 9 | Dynamic Arrays | Thin Blue Bounding Border | Vector rendering of thin blue border enclosing spilled array cells when any cell in the spill is focused | Active selection inside spilled range | GDI+ blue rectangle around `[r0, c0]..[r0+H-1, c0+W-1]` | Dashed amber border when `#SPILL!` collision occurs | ORIGINAL_REQUEST §Phase 3.2, `CalcView.cs:1653` |
| 10 | Dynamic Arrays | `SORT` Function | Sorts range or array contents by specified column/row in ascending or descending order | `array, [sort_index], [sort_order], [by_col]` | Sorted 2D array | `#VALUE!` on invalid sort index; `#SPILL!` on collision | ORIGINAL_REQUEST §Phase 3.2 |
| 11 | Dynamic Arrays | `SORTBY` Function | Sorts range/array based on values in one or more parallel criteria arrays | `array, by_array1, [order1], ...` | Sorted 2D array | `#VALUE!` if criteria array dimensions mismatch | ORIGINAL_REQUEST §Phase 3.2 |
| 12 | Dynamic Arrays | `FILTER` Function | Filters array based on boolean condition array; includes optional default value | `array, include, [if_empty]` | Filtered 2D array subset | Returns `if_empty` or `#CALC!` if no rows match | ORIGINAL_REQUEST §Phase 3.2 |
| 13 | Dynamic Arrays | `UNIQUE` Function | Returns distinct rows or columns from range/array, with optional `exactly_once` filter | `array, [by_col], [exactly_once]` | 2D array of unique items | Empty array handling returns empty cell | ORIGINAL_REQUEST §Phase 3.2 |
| 14 | Dynamic Arrays | `SEQUENCE` Function | Generates grid of sequential numbers | `rows, [columns], [start], [step]` | 2D numeric array | `#VALUE!` if rows or cols <= 0 | ORIGINAL_REQUEST §Phase 3.2 |
| 15 | Dynamic Arrays | `RANDARRAY` Function | Generates grid of random numbers (float or integer) | `[rows], [columns], [min], [max], [whole_number]` | 2D random numeric array | `#VALUE!` if min > max or rows/cols <= 0 | ORIGINAL_REQUEST §Phase 3.2 |
| 16 | Dynamic Arrays | `LET` Function | Assigns names to calculation results for local reuse and performance | `name1, val1, [name2, val2, ...], calculation` | Result of final calculation | `#NAME?` if invalid variable identifier; `#VALUE!` on odd args | ORIGINAL_REQUEST §Phase 3.2 |
| 17 | Dynamic Arrays | `LAMBDA` Function | Creates custom, reusable function definitions with parameter binding | `[param1, param2, ...], calculation` | Callable function object or evaluated result | `#VALUE!` on parameter count mismatch; `#NAME?` on unbound tokens | ORIGINAL_REQUEST §Phase 3.2 |
| 18 | PivotTables | Field List Task Pane | Right-side docking pane with source field checklist and 4 drag-drop zones (Filters, Cols, Rows, Values) | Source table schema, user drag/drop events | Updates `PivotTableDefinition` model and triggers layout refresh | Handles invalid field moves gracefully | ORIGINAL_REQUEST §Phase 4.1 |
| 19 | PivotTables | Multi-Dimensional Aggregation Engine | Multi-level aggregation calculating measures across row/column dimensions with subtotals and grand totals | Source data records, dimension keys, measures | Formatted pivot grid emitted into worksheet | Handles missing/null values, division by zero (`% of`) | ORIGINAL_REQUEST §Phase 4.1, `CalcView.cs:6116` |
| 20 | PivotTables | Date & Numeric Grouping | Groups dates into Years/Quarters/Months/Days, and numbers into custom interval bins | Field reference, grouping interval parameters | Virtual grouped hierarchy fields in pivot cache | Validates min <= max and step > 0; non-dates skipped | ORIGINAL_REQUEST §Phase 4.1 |
| 21 | PivotTables | Linked PivotCharts | Chart linked to PivotTable model updating automatically on field, filter, or grouping changes | PivotTable data source, chart type | Vector chart synchronized with pivot grid | Handles empty pivot results without crashing | ORIGINAL_REQUEST §Phase 4.1, `ChartRenderer.cs` |
| 22 | PivotTables | OpenXML Pivot Persistence | Saves and loads `xl/pivotTables/pivotTable{N}.xml` and `xl/pivotCache/` parts | PivotTable models and cache | Standard OpenXML spreadsheet pivot parts | Fallback to cached cells if pivot engine not present | ECMA-376 §12.3.24 |
| 23 | What-If | Goal Seek Iterative Solver | Single-variable iterative numerical solver finding input value to achieve target formula result | `SetCell`, `ToValue` (double), `ByChangingCell` | Converged input value (within 0.001) | Reports "Goal Seek may not have found a solution" if divergent | ORIGINAL_REQUEST §Phase 4.2 |
| 24 | What-If | Data Tables (1-Var & 2-Var) | Sensitivity analysis grid computing formula outcomes across row/column variable substitutions | Table range, `RowInputCell`, `ColumnInputCell` | Evaluated grid of sensitivity values | Reverts substitutions if formula errors occur | ORIGINAL_REQUEST §Phase 4.2 |
| 25 | Protection | Workbook Structure Protection | Password-protected lock preventing adding, deleting, renaming, moving, or hiding worksheets | Password string, lock structure bool | Disables sheet tab actions, locks workbook structure | Prompts for password to unlock; rejects wrong password | ORIGINAL_REQUEST §Phase 4.3, `CalcView.cs:624` |
| 26 | Protection | OpenXML Workbook Protection | Reads/writes `<workbookProtection lockStructure="1" workbookPassword="..."/>` in `xl/workbook.xml` | Workbook protection state | ECMA-376 XML tag in workbook part | Password hash verification | ECMA-376 §18.2.29 / `XlsxHandler.cs` |
| 27 | Printing | Print Titles Model & UI | Configuration to repeat designated header rows and/or columns on every printed page | `PrintTitleRows` (e.g. `$1:$2`), `PrintTitleCols` (e.g. `$A:$B`) | Stored in sheet model, repeated during PDF/print pagination | Validates address syntax; ignores out-of-range bounds | ORIGINAL_REQUEST §Phase 4.3 |
| 28 | Printing | OpenXML Print Titles | Reads/writes `_xlnm.Print_Titles` defined name in `xl/workbook.xml` | Print title row/col references | `<definedName name="_xlnm.Print_Titles">` | Handled seamlessly alongside standard defined names | ECMA-376 §18.2.6 / `XlsxHandler.cs` |
| 29 | Printing | Manual Page Breaks (Row & Col) | Insert, remove, and reset horizontal and vertical manual page breaks with visual grid line indicator | Row index, column index, break command | `RowPageBreaks`, `ColPageBreaks` lists; dashed line on grid | Cannot insert break at row 1 or col 1; duplicates ignored | ORIGINAL_REQUEST §Phase 4.3, `CalcView.cs:1075` |
| 30 | Printing | OpenXML Page Breaks | Reads/writes `<rowBreaks>` and `<colBreaks>` in worksheet XML parts | Manual page break collections | ECMA-376 `<brk>` elements with `man="1"` | Preserved during roundtrip | ECMA-376 §18.3.1.70 / `XlsxHandler.cs` |

---

## 3. Edge Cases & Boundary Conditions

| # | Feature | Input / Condition | Observed / Required Behavior |
|---|---------|-------------------|-----------------------------|
| 1 | Table Auto-Expansion | User enters data 2 rows below table (gap row) | Do NOT expand table. Only expand if cell is immediately adjacent (`EndRow + 1`). |
| 2 | Table Auto-Expansion | User enters data at `EndRow + 1` when Total Row is active | Shift Total Row down by 1 row (`EndRow++`), insert new row as data, maintain Total Row at bottom. |
| 3 | Table Structured Ref | `Table1[@Sales]` evaluated from a cell outside `Table1` | Return `#VALUE!` error because implicit intersection `@` has no row context outside table. |
| 4 | Table Structured Ref | `Table1[[#Headers],[Sales]]` | Evaluates specifically to header cell at `(StartRow, ColIndex)` regardless of where formula resides. |
| 5 | Table Duplicate Name | User names a table with existing name or invalid chars | Display validation warning; table names must be unique workbook-wide and follow identifier rules. |
| 6 | Dynamic Array Spill | Spilled formula encounters cell containing spaces or empty string formula `=""` | Collision detected: return `#SPILL!` (even blank-looking cells containing formulas block spill). |
| 7 | Dynamic Array Spill | Spilled range extends beyond sheet maximum (row > 1,048,576 or col > 16,384) | Return `#SPILL!` (range overflows sheet limits). |
| 8 | Dynamic Array Spill | User edits a spilled ghost cell directly | Block edit or prompt warning; if edited, master cell turns into `#SPILL!` due to collision. |
| 9 | `FILTER` Function | Criteria array has no `TRUE` values and `if_empty` omitted | Return `#CALC!` error (standard Excel behavior). |
| 10 | `FILTER` Function | Criteria array height does not match data array height | Return `#VALUE!` error. |
| 11 | `UNIQUE` Function | Empty input range | Return empty cell or empty array without crashing. |
| 12 | `SORT` Function | Array contains mixed numbers, text, blanks, and errors | Excel ordering: Numbers (asc) -> Text (asc) -> Booleans -> Errors -> Blanks (always last). |
| 13 | `LET` Function | Variable redefined twice: `=LET(x, 1, x, 2, x)` | Return `#BAD_NAME!` or `#VALUE!` (duplicate variable definition in same scope). |
| 14 | `LAMBDA` Recursion | Recursive LAMBDA with no termination condition | Detect recursion depth limit (e.g. max depth 1,000) and return `#NUM!` instead of stack overflow. |
| 15 | Goal Seek | Target formula is constant (does not depend on changing cell) | Terminate after iteration checks; report no solution found. |
| 16 | Goal Seek | Derivative is zero ($f'(x) \approx 0$) / flat slope | Perturb input with small random offset or bisection step to escape local flat region. |
| 17 | Goal Seek | Target value already matched ($|f(x_0)| < 0.001$) | Terminate immediately on iteration 0; report success. |
| 18 | Data Tables | Formula references cell outside table | Supported: table evaluates formula against specified row/column input cells. |
| 19 | PivotTable Grouping | Date column contains invalid/text strings or blanks | Place invalid/blank records into separate `(Blank)` group; group valid dates normally. |
| 20 | PivotTable Aggregation | Column has mixed text and numbers with `SUM` | Ignore text values in `SUM`; only sum numeric cells. In `COUNT`, count non-blanks; in `COUNT NUMBERS`, count numbers only. |
| 21 | Workbook Protection | Sheet tab drag-and-drop or rename when structure protected | Suppress drag operation; suppress rename prompt; display notification that workbook structure is protected. |
| 22 | Print Titles | Sheet has 0 rows or print title range is `$10:$20` while data is rows 1-5 | Ignore print titles if they do not intersect printable data area. |
| 23 | Manual Page Breaks | User inserts page break inside merged cell range | Place page break at top/left boundary of merged area to avoid splitting merged cells. |

---

## 4. Deep-Dive Specification: Subsystem by Subsystem

### 4.1 Subsystem 1: Excel Tables (ListObject)

#### 4.1.1 Data Models & Class Architecture
In `src/ui/CalcTableAndPivotDialogs.cs` and `src/core/SparseMatrix.cs`:

```csharp
public enum TotalsRowFunction
{
    None,
    Average,
    Count,
    CountNums,
    Max,
    Min,
    Sum,
    StdDev,
    Var,
    Custom
}

public class ExcelTableColumnInfo
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public TotalsRowFunction TotalsFunction { get; set; }
    public string TotalsFormula { get; set; }
    public string TotalsCustomLabel { get; set; }
    public string CalculatedColumnFormula { get; set; }

    public ExcelTableColumnInfo(uint id, string name)
    {
        Id = id;
        Name = name;
        TotalsFunction = TotalsRowFunction.None;
        TotalsFormula = "";
        TotalsCustomLabel = "";
        CalculatedColumnFormula = "";
    }
}
```

Extend `ExcelTableInfo`:
```csharp
public List<ExcelTableColumnInfo> Columns { get; set; }
public uint DataStartRow { get { return HasHeaders ? (StartRow + 1) : StartRow; } }
public uint DataEndRow { get { return HasTotalRow ? (EndRow - 1) : EndRow; } }

public ExcelTableColumnInfo GetColumn(string colName)
{
    if (Columns == null) return null;
    foreach (var c in Columns)
    {
        if (string.Equals(c.Name, colName, StringComparison.OrdinalIgnoreCase))
            return c;
    }
    return null;
}
```

#### 4.1.2 Interaction Mechanics & Shortcuts
1. **Shortcut `Ctrl+T` / `Ctrl+L`**:
   - In `CalcView.HandleKeyDown`:
     ```csharp
     if (ctrl && (rawKey == Keys.T || rawKey == Keys.L))
     {
         using (CreateTableDialogForm dlg = new CreateTableDialogForm(this))
         {
             dlg.ShowDialog();
         }
         return;
     }
     ```
2. **Auto-Expansion on Commit**:
   - In `CalcView.CommitEditAndMove`:
     Check if edited cell `(SelectedRow, SelectedCol)` is adjacent to any table in `Tables`:
     - If `SelectedRow == tbl.EndRow + 1` and `SelectedCol >= tbl.StartCol && SelectedCol <= tbl.EndCol`:
       Expand downward. If `tbl.HasTotalRow`, move Total Row down by 1 row, insert new row at `tbl.EndRow`, update `tbl.EndRow++`.
     - If `SelectedCol == tbl.EndCol + 1` and `SelectedRow >= tbl.StartRow && SelectedRow <= tbl.EndRow`:
       Expand rightward. Increment `tbl.EndCol++`, add header `ColumnN`, extend formatting.
3. **Total Row SUBTOTAL Function Codes**:
   When user selects a total function for a column:
   - `Average`: `=SUBTOTAL(101, [ColumnName])`
   - `Count`: `=SUBTOTAL(103, [ColumnName])`
   - `CountNums`: `=SUBTOTAL(102, [ColumnName])`
   - `Max`: `=SUBTOTAL(104, [ColumnName])`
   - `Min`: `=SUBTOTAL(105, [ColumnName])`
   - `Sum`: `=SUBTOTAL(109, [ColumnName])`
   - `StdDev`: `=SUBTOTAL(107, [ColumnName])`
   - `Var`: `=SUBTOTAL(110, [ColumnName])`

#### 4.1.3 Structured References in `FormulaEngine`
Structured references must be resolved prior to formula evaluation.
Regex pattern: `(?:(?<tableTag>[A-Za-z0-9_]+))?\[(?<inner>[^\[\]]*(?:\[[^\[\]]*\][^\[\]]*)*)\]`

Rules for substitution into standard cell coordinates:
1. `Table1[@Sales]` inside table row $R$: resolves to single cell reference `SparseMatrix.ColIndexToName(colIdx) + R`.
2. `Table1[Sales]` or `Table1[[#Data],[Sales]]`: resolves to range `ColName + DataStartRow + ":" + ColName + DataEndRow`.
3. `Table1[[#Headers],[Sales]]`: resolves to `ColName + StartRow`.
4. `Table1[[#Totals],[Sales]]`: resolves to `ColName + EndRow` (when `HasTotalRow` is true).
5. `Table1[#All]`: resolves to `StartColName + StartRow + ":" + EndColName + EndRow`.
6. `Table1[[Sales]:[Profit]]`: multi-column range resolving to `SalesCol + DataStartRow + ":" + ProfitCol + DataEndRow`.

#### 4.1.4 OpenXML ECMA-376 Table Schema
Table part is saved as `/xl/tables/table{N}.xml`:
```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<table xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
       id="1" name="Table1" displayName="Table1" ref="A1:E10" totalsRowShown="1">
  <autoFilter ref="A1:E10"/>
  <tableColumns count="5">
    <tableColumn id="1" name="Date" totalsRowLabel="Total"/>
    <tableColumn id="2" name="Product" totalsRowFunction="none"/>
    <tableColumn id="3" name="Quantity" totalsRowFunction="sum"/>
    <tableColumn id="4" name="Price" totalsRowFunction="average"/>
    <tableColumn id="5" name="Sales" totalsRowFunction="sum"/>
  </tableColumns>
  <tableStyleInfo name="TableStyleMedium2" showFirstColumn="0" showLastColumn="0"
                  showRowStripes="1" showColumnStripes="0"/>
</table>
```
In `xl/worksheets/_rels/sheet1.xml.rels`:
```xml
<Relationship Id="rIdT1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/table" Target="../tables/table1.xml"/>
```
In `xl/worksheets/sheet1.xml`:
```xml
<tableParts count="1">
  <tablePart r:id="rIdT1"/>
</tableParts>
```
In `[Content_Types].xml`:
```xml
<Override PartName="/xl/tables/table1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.table+xml"/>
```

---

### 4.2 Subsystem 2: Dynamic Arrays & Spill Engine

#### 4.2.1 Core Architectural Model
To handle arrays in a clean, C# 5-compatible architecture:

```csharp
public class ArrayValue
{
    public int Rows { get; private set; }
    public int Columns { get; private set; }
    private CellData[,] _data;

    public ArrayValue(int rows, int cols)
    {
        Rows = Math.Max(1, rows);
        Columns = Math.Max(1, cols);
        _data = new CellData[Rows, Columns];
    }

    public CellData this[int r, int c]
    {
        get { return _data[r, c]; }
        set { _data[r, c] = value; }
    }
}
```

In `CellData`:
```csharp
public bool IsSpillAnchor { get; set; }
public bool IsSpillTarget { get; set; }
public uint SpillOriginRow { get; set; }
public uint SpillOriginCol { get; set; }
public int SpillWidth { get; set; }
public int SpillHeight { get; set; }
public bool IsSpillBlocked { get; set; }
public ArrayValue CachedArrayResult { get; set; }
```

#### 4.2.2 Spill Lifecycle & Collision Engine
When formula at cell $(r_0, c_0)$ evaluates to `ArrayValue` $A$ of dimensions $H \times W$:
1. If $H = 1$ and $W = 1$:
   - Set scalar value from $A[0, 0]$ into $(r_0, c_0)$. `IsSpillAnchor = false`.
2. If $H > 1$ or $W > 1$:
   - Check collision across all cells $(r, c)$ where $r \in [r_0, r_0 + H - 1]$ and $c \in [c_0, c_0 + W - 1]$ excluding $(r_0, c_0)$:
     * A cell is collision-blocking if:
       - It contains literal data (`Type != CellType.Empty`).
       - It contains another formula (`Type == CellType.Formula`).
       - It is an anchor of another spill or is inside an Excel Table.
   - If collision detected:
     * Set $(r_0, c_0)$ value to `#SPILL!`.
     * `IsSpillBlocked = true`, record attempted $H \times W$.
   - If clean (no collision):
     * Set $(r_0, c_0)$ `IsSpillAnchor = true`, `SpillWidth = W`, `SpillHeight = H`, `IsSpillBlocked = false`.
     * Anchor displays value $A[0, 0]$.
     * For each target cell $(r, c)$:
       - Mark `IsSpillTarget = true`, `SpillOriginRow = r0`, `SpillOriginCol = c0`.
       - Copy value and display string from $A[r - r_0, c - c_0]$.

#### 4.2.3 Spill Reference Operator (`#`)
In `FormulaEngine.ParseRange`:
- If address ends with `#` (e.g. `A1#`):
  * Strip `#`, parse address `A1` to $(r_0, c_0)$.
  * Fetch cell at $(r_0, c_0)$.
  * If `cell.IsSpillAnchor`: return range $(r_0, c_0)$ to $(r_0 + \text{SpillHeight} - 1, c_0 + \text{SpillWidth} - 1)$.
  * Otherwise: return single cell $(r_0, c_0)$.

#### 4.2.4 Visual Bounding Box Rendering
In `CalcView.Draw`:
- When `SelectedRow, SelectedCol` falls inside any spilled array range:
  ```csharp
  int sX = GetColScreenX(anchorCol, headerW);
  int sY = GetRowScreenY(anchorRow, startY, headerH);
  int sW = 0; for (uint c = anchorCol; c < anchorCol + spillW; c++) sW += GetColWidth(c);
  int sH = 0; for (uint r = anchorRow; r < anchorRow + spillH; r++) sH += GetRowHeight(r);
  Rectangle spillRect = new Rectangle(sX, sY, sW, sH);

  using (Pen spillPen = new Pen(isBlocked ? Color.FromArgb(217, 119, 6) : Color.FromArgb(37, 99, 235), 1.5f))
  {
      if (isBlocked) spillPen.DashStyle = DashStyle.Dash;
      g.DrawRectangle(spillPen, spillRect);
  }
  ```

#### 4.2.5 Specification of Dynamic Array Functions

1. **`SORT(array, [sort_index], [sort_order], [by_col])`**:
   - `array`: Range or ArrayValue.
   - `sort_index`: 1-based index (default 1).
   - `sort_order`: 1 for ascending (default), -1 for descending.
   - `by_col`: boolean (default FALSE = sort rows; TRUE = sort columns).
   - Returns sorted `ArrayValue`.

2. **`SORTBY(array, by_array1, [order1], ...)`**:
   - Sorts `array` using primary and secondary sort keys from `by_array` vectors.

3. **`FILTER(array, include, [if_empty])`**:
   - `array`: 2D source range.
   - `include`: boolean range/array matching data height (or width).
   - `if_empty`: fallback scalar value if no rows evaluate to TRUE.
   - Returns filtered `ArrayValue`. If empty, returns `if_empty` or `#CALC!`.

4. **`UNIQUE(array, [by_col], [exactly_once])`**:
   - `by_col`: FALSE = unique rows (default); TRUE = unique columns.
   - `exactly_once`: FALSE = all distinct records (default); TRUE = records appearing exactly once.
   - Row hashing via concatenated delimited cell string values.

5. **`SEQUENCE(rows, [columns], [start], [step])`**:
   - Generates arithmetic sequence grid.
   - Defaults: `columns = 1`, `start = 1`, `step = 1`.

6. **`RANDARRAY([rows], [columns], [min], [max], [whole_number])`**:
   - Generates pseudo-random numbers using `System.Random`.
   - Defaults: `rows = 1`, `columns = 1`, `min = 0.0`, `max = 1.0`, `whole_number = FALSE`.

7. **`LET(name1, val1, [name2, val2, ...], calculation)`**:
   - Scoping engine maintains `Dictionary<string, object> localScope`.
   - Evaluates pairs from left to right; variables can reference earlier variables.
   - Replaces variable tokens in `calculation` expression and evaluates.

8. **`LAMBDA([param1, param2, ...], calculation)`**:
   - Parameter declaration list + calculation expression body.
   - Callable via direct invocation syntax `=LAMBDA(x, y, x+y)(10, 20)` or via registered defined name.

---

### 4.3 Subsystem 3: PivotTables & PivotCharts

#### 4.3.1 Architecture & Pivot Engine
```csharp
public enum PivotFieldOrientation
{
    None,
    PageFilter,
    Column,
    Row,
    DataValue
}

public enum PivotAggregationType
{
    Sum,
    Count,
    Average,
    Min,
    Max,
    Product,
    StdDev,
    Var,
    PercentOfRow,
    PercentOfColumn,
    PercentOfGrandTotal
}

public class PivotFieldDefinition
{
    public string Name { get; set; }
    public PivotFieldOrientation Orientation { get; set; }
    public PivotAggregationType Aggregation { get; set; }
    public string DisplayName { get; set; }
    public string NumberFormat { get; set; }
    public bool IsGrouped { get; set; }
    public string GroupType { get; set; } // "Date" or "Numeric"
    public double GroupInterval { get; set; }
}

public class PivotTableDefinition
{
    public string Name { get; set; }
    public string SourceRange { get; set; }
    public string DestinationSheet { get; set; }
    public uint DestRow { get; set; }
    public uint DestCol { get; set; }
    public List<PivotFieldDefinition> Fields { get; set; }
    public bool ShowRowGrandTotals { get; set; }
    public bool ShowColGrandTotals { get; set; }
}
```

#### 4.3.2 Multi-Dimensional Aggregation Algorithm
1. **Cache Loading**:
   - Scan source table range: row 1 gives field names; rows 2..N contain records.
   - Store records in columnar or row-tuple format.
2. **Dimension Coordinate Extraction**:
   - Filter records by active `PageFilter` fields.
   - For each record:
     * Construct Row Key: $K_r = (v_{r1}, v_{r2}, \dots)$
     * Construct Column Key: $K_c = (v_{c1}, v_{c2}, \dots)$
     * Accumulate into cell bucket `Map<(KeyR, KeyC), Accumulator>`:
       - `Sum += val; Count++; Min = Math.Min(Min, val); Max = Math.Max(Max, val);`
3. **Layout Rendering**:
   - Sort Row Keys and Column Keys.
   - Emit Column Headers (nested if multiple column fields).
   - Emit Row Headers and computed Measure Cells.
   - Compute Subtotals per group level.
   - Compute Grand Totals across rows and columns.
   - If `% of Row` / `% of Column`: divide cell measure by corresponding subtotal/grand total.

#### 4.3.3 Date and Numeric Grouping
- **Numeric Grouping**:
  * Inputs: Start Value, End Value, Interval Size.
  * Formula: Bin index $b = \lfloor (val - \text{Start}) / \text{Interval} \rfloor$.
  * Labels: `"< 0"`, `"0 - 10"`, `"11 - 20"`, `"> 100"`.
- **Date Grouping**:
  * Inputs: Selections from `{ Years, Quarters, Months, Days }`.
  * Transforms raw date `dt` into virtual hierarchical fields:
    - Year: `dt.Year`
    - Quarter: `"Qtr" + ((dt.Month - 1) / 3 + 1)`
    - Month: `dt.ToString("MMM")`
    - Day: `dt.Day`

#### 4.3.4 Field List Task Pane UI (`PivotTableFieldListControl`)
- WinForms `UserControl` dockable to the right edge of `AppForm`:
  * Top half: Search box and CheckedListBox of all source fields.
  * Bottom half: 4 drop boxes in 2x2 layout:
    1. Filters (Page)
    2. Columns
    3. Rows
    4. Values
  * Drag-and-drop support: Drag items from field list into zones or between zones.
  * Context menu on Value items: "Value Field Settings..." to pick `PivotAggregationType` (Sum, Count, Average...) and number format.

#### 4.3.5 Linked PivotCharts
- A `DocShapeObject` or chart instance flagged with `IsPivotChart = true` and `LinkedPivotTableName = "PivotTable1"`.
- When PivotTable refreshes:
  * Categories = distinct Row Keys of the PivotTable.
  * Series = distinct Column Keys (or Value Fields).
  * Data Points = evaluated matrix cells.
  * Chart redraws immediately via `ChartRenderer.cs`.

---

### 4.4 Subsystem 4: What-If Analysis

#### 4.4.1 Goal Seek Iterative Solver
- **User Inputs**:
  1. `SetCell`: Address of formula cell (e.g. `B5`).
  2. `ToValue`: Target value (double, e.g. `25000.0`).
  3. `ByChangingCell`: Address of variable cell (e.g. `B1`).
- **Algorithm (Secant Method with Bisection Fallback)**:
  * Target precision: $\epsilon = 0.001$. Maximum iterations: $N_{\max} = 100$.
  * Objective function: $f(x) = \text{Evaluate}(SetCell \text{ with } ByChangingCell = x) - ToValue$.
  * Step 0: Save original cell value $x_{\text{orig}}$. Let $x_0 = x_{\text{orig}}$ (if 0, use 1.0).
  * Step 1: Let $x_1 = x_0 \times 1.05 + 0.1$.
  * Step 2: Loop $k = 1 \dots N_{\max}$:
    $$x_{k+1} = x_k - f(x_k) \cdot \frac{x_k - x_{k-1}}{f(x_k) - f(x_{k-1})}$$
    * If $|f(x_k) - f(x_{k-1})| < 10^{-12}$: perturb $x_k$ by small step.
    * Evaluate $f(x_{k+1})$.
    * If $|f(x_{k+1})| \le \epsilon$: Converged! Exit loop.
  * Step 3: Present `GoalSeekStatusDialog`:
    - Shows target value, achieved value, and iterations.
    - "OK": commits new value $x_{k+1}$ to `ByChangingCell`.
    - "Cancel": restores $x_{\text{orig}}$.

#### 4.4.2 Data Tables (Sensitivity Analysis)
- **1-Variable Data Table**:
  * Input values arranged in a column (or row).
  * User selects range including headers, formula reference, and inputs.
  * User specifies `ColumnInputCell` (or `RowInputCell`).
  * Engine substitutes each input value into the target input cell, recomputes formula, records output in corresponding table cell, and restores original value when done.
- **2-Variable Data Table**:
  * Top-left cell references base formula.
  * Top row contains values for Variable 1 (`RowInputCell`).
  * Left column contains values for Variable 2 (`ColumnInputCell`).
  * Engine systematically evaluates grid: for each row $i$ and column $j$, inject $(x_i, y_j)$, evaluate sheet, record result into cell $(i, j)$.
  * Excel Formula representation: `{=TABLE(RowInput, ColumnInput)}`.

---

### 4.5 Subsystem 5: Advanced Printing & Protection

#### 4.5.1 Workbook Structure Protection
- **Model**:
  * In `CalcView.cs`:
    ```csharp
    public bool IsWorkbookProtected { get; set; }
    public string WorkbookProtectPassword { get; set; }
    public bool LockStructure { get; set; }
    public bool LockWindows { get; set; }
    ```
- **UI & Interaction**:
  * Menu / Ribbon: Review -> "Bảo vệ Sổ làm việc (Protect Workbook)".
  * Dialog: `ProtectWorkbookDialogForm`:
    - Password prompt (optional).
    - Checkbox: "Structure" (default checked).
    - Checkbox: "Windows" (default unchecked).
  * Enforcement:
    - Add sheet (`+` button, `AddSheet`): blocked with error prompt.
    - Delete sheet (`DeleteSheet`): blocked.
    - Rename sheet (`RenameSheet`): blocked.
    - Move sheet (`MoveSheetLeft`, `MoveSheetRight`): blocked.
    - Hide / Unhide sheet: blocked.
- **OpenXML Storage**:
  In `xl/workbook.xml`:
  ```xml
  <workbookProtection lockStructure="1" workbookPassword="CCDF"/>
  ```

#### 4.5.2 Print Titles
- **Model**:
  * In `SparseMatrix.cs` or `CalcSheetState`:
    ```csharp
    public bool HasPrintTitles { get; set; }
    public uint PrintTitleStartRow { get; set; }
    public uint PrintTitleEndRow { get; set; }
    public uint PrintTitleStartCol { get; set; }
    public uint PrintTitleEndCol { get; set; }
    ```
- **Print / PDF Engine Integration**:
  * When exporting or printing pages:
    - On page 2 and subsequent vertical pages, render rows `PrintTitleStartRow..PrintTitleEndRow` at the top before rendering page data.
    - On page 2 and subsequent horizontal pages, render columns `PrintTitleStartCol..PrintTitleEndCol` on the left before rendering page data.
- **OpenXML Storage**:
  Stored in `xl/workbook.xml` as standard defined name:
  ```xml
  <definedName name="_xlnm.Print_Titles" localSheetId="0">'Sheet1'!$1:$2,'Sheet1'!$A:$B</definedName>
  ```

#### 4.5.3 Manual Page Breaks
- **Model**:
  ```csharp
  public List<uint> RowPageBreaks { get; set; } // Horizontal breaks above row R
  public List<uint> ColPageBreaks { get; set; } // Vertical breaks left of col C
  ```
- **Operations**:
  * `InsertPageBreak()`: Adds `SelectedRow` to `RowPageBreaks` and `SelectedCol` to `ColPageBreaks`.
  * `RemovePageBreak()`: Removes break at `SelectedRow` or `SelectedCol`.
  * `ResetAllPageBreaks()`: Clears all manual breaks.
- **Visual Grid Rendering**:
  In `CalcView.Draw`:
  * For each row in `RowPageBreaks`: draw dashed horizontal line across sheet at row boundary (`DashStyle.Dash`, color `#2563EB`).
  * For each col in `ColPageBreaks`: draw dashed vertical line across sheet at col boundary.
- **OpenXML Storage**:
  In `xl/worksheets/sheet{N}.xml`:
  ```xml
  <rowBreaks count="1" manualBreakCount="1">
    <brk id="10" max="16384" man="1"/>
  </rowBreaks>
  <colBreaks count="1" manualBreakCount="1">
    <brk id="5" max="1048576" man="1"/>
  </colBreaks>
  ```

---

## 5. Integration Architecture & Source Touchpoints

| Source File | Subsystem / Responsibility | Key Modifications Required |
|-------------|----------------------------|----------------------------|
| `src/core/FormulaEngine.cs` | Calc Engine | Add Structured References resolver (`Table1[@Col]`), Array return type evaluation, Dynamic Array functions (`SORT`, `SORTBY`, `FILTER`, `UNIQUE`, `SEQUENCE`, `RANDARRAY`), scoping (`LET`, `LAMBDA`), spill `#` operator resolution. |
| `src/core/SparseMatrix.cs` | Grid Data Model | Add spill metadata (`IsSpillAnchor`, `IsSpillTarget`, `SpillWidth`, `SpillHeight`, `IsSpillBlocked`), Page break lists (`RowPageBreaks`, `ColPageBreaks`), Print Title properties. |
| `src/ui/CalcView.cs` | View & Controller | Add `Ctrl+T` shortcut; AutoExpandTable on typing in `CommitEditAndMove`; render Dynamic Array blue bounding box in `Draw()`; enforce `IsWorkbookProtected` on sheet operations; render manual page break dashed lines; handle Total Row dropdown clicks. |
| `src/ui/CalcTableAndPivotDialogs.cs` | Dialogs & Models | Extend `ExcelTableInfo` with `ExcelTableColumnInfo` and `TotalsRowFunction`; add `GoalSeekDialogForm` & solver; add `DataTableDialogForm`; add `ProtectWorkbookDialogForm`; add `PrintTitlesDialogForm`; add `PivotTableFieldListControl`. |
| `src/ui/RibbonBar.cs` | Ribbon UI | Add What-If Analysis dropdown (Data tab); Breaks & Print Titles (Page Layout tab); Protect Workbook (Review tab); Table Design total functions dropdown. |
| `src/format/XlsxHandler.cs` | OpenXML IO | Save & Load Table XML parts (`xl/tables/table{N}.xml`); Save & Load Workbook Protection (`<workbookProtection>`); Save & Load Print Titles (`_xlnm.Print_Titles`); Save & Load Page Breaks (`<rowBreaks>`, `<colBreaks>`); Save & Load PivotTable parts. |

---

## 6. Verification & Test Plan

1. **Compilation Verification**:
   - Run `build.bat` using .NET 4.8 `csc.exe` (C# 5 compiler).
   - Ensure 0 errors, 0 warnings regressions.

2. **Automated Unit & Scenario Tests**:
   - `test_tables_structured_refs.cs`:
     * Create table over range A1:C5.
     * Verify `=Table1[@Col2] * 2` computes correctly per row.
     * Verify `=SUM(Table1[Col2])` computes sum of data cells only.
     * Verify typing in cell A6 auto-expands table to A1:C6.
   - `test_dynamic_arrays_spill.cs`:
     * Test `SEQUENCE(4, 3)` fills 4 rows and 3 columns.
     * Test placing a non-empty cell in the target footprint triggers `#SPILL!`.
     * Test clearing blocking cell auto-resolves `#SPILL!`.
     * Test `UNIQUE` and `SORT` on 100-row sample dataset.
     * Test `LET(x, 10, y, 20, x * y)` evaluates to 200.
     * Test `=SUM(A1#)` evaluates sum over spilled array.
   - `test_pivottables_aggregation.cs`:
     * Feed 5,000-row sales dataset into `PivotEngine`.
     * Validate Sum, Count, Average, Min, Max, % of Row match Excel benchmark output.
     * Test date grouping into Years/Quarters.
   - `test_what_if_analysis.cs`:
     * Non-linear formula `=B1^2 + 3*B1 - 50 = 0`.
     * Run Goal Seek to reach 0. Verify converged $B1$ within 0.001 tolerance.
     * Run 2-Variable Data Table. Verify all interior grid values match formula outcomes.
   - `test_printing_protection.cs`:
     * Protect workbook with password. Attempt `AddSheet` and `DeleteSheet`, verify rejected.
     * Set Print Titles `$1:$2`. Verify OpenXML roundtrip retains `_xlnm.Print_Titles`.
     * Insert row page break at row 15. Verify OpenXML roundtrip retains `<rowBreaks>`.
