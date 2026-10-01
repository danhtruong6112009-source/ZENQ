# Authoritative Technical Implementation Plan: Core Data Utilities
**Subsystem**: ZenQ Calc Spreadsheet — Core Data Utilities  
**Target Architecture**: C# 5.0 / Microsoft (R) Visual C# Compiler version 4.8.9232.0 (.NET Framework 4.8)  
**Author**: Phase 1 Explorer 3 (Core Data Utilities Specialist)  
**Scope**: `CalcFindReplaceDialog.cs`, `CalcRemoveDuplicatesDialog.cs`, `CalcTextToColumnsDialog.cs`, and integration with `CalcView.cs`, `RibbonBar.cs`, `AppForm.cs`

---

## 1. Executive Summary & Design Scope

The Core Data Utilities upgrade equips ZenQ Calc with three essential Excel-grade productivity tools:
1. **Find & Replace Dialog (`CalcFindReplaceDialog.cs`)**:
   A dedicated floating modeless dialog providing standard spreadsheet search and replacement across active sheets or workbooks, supporting Match Case, Match Entire Cell, Search By Rows / Search By Columns, Look In Values / Look In Formulas, Find Next, Find Previous, Find All (with interactive result navigator), Replace, and Replace All with formula-safety protection.
2. **Remove Duplicates Dialog (`CalcRemoveDuplicatesDialog.cs`)**:
   A modal dialog with automatic table header detection, column checklist with Select All / Unselect All, multi-column row equality hashing, data-safe reverse row deletion, cell shift-up routines that preserve sheet formatting and merge spans, and localized summary reporting ("*X duplicate values found and removed; Y unique values remain*").
3. **Text to Columns Wizard (`CalcTextToColumnsDialog.cs`)**:
   A modal dialog supporting both Delimited (Tab, Semicolon, Comma, Space, Custom, consecutive delimiters, text qualifiers) and Fixed-Width character partitioning, featuring a real-time live preview `DataGridView`, destination cell specification, overwrite protection warnings, and automatic type inference (number vs string).
4. **Subsystem Integration**:
   Complete wiring into `CalcView.cs`, `RibbonBar.cs` (Data tab and Home tab), and application-level keyboard shortcuts (`Ctrl+F` for Find, `Ctrl+H` for Replace), backed by ZenQ Calc's 50-step undo/redo snapshot engine (`SaveUndoState()`).

---

## 2. Strict C# 5.0 Language & Platform Conventions

The project build harness (`build.bat`) invokes `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` in strict C# 5 mode. All code in this specification obeys the following constraints:

| Prohibited C# 6+ Syntax | Mandatory C# 5 Compliant Idiom |
|---|---|
| String interpolation `$"Row: {r}"` | `string.Format(CultureInfo.InvariantCulture, "Row: {0}", r)` |
| Expression bodies `public int W => 5;` | `public int W { get { return 5; } }` |
| Null propagation `cell?.RawValue` | `(cell != null ? cell.RawValue : null)` |
| Inline out declarations `int.TryParse(s, out int v)` | `int v; int.TryParse(s, out v);` |
| Value tuples `(r, c)` | `Tuple.Create(r, c)` or `Tuple<uint, uint>` |
| Auto-property initializers `public int X { get; set; } = 1;` | Initialize in constructor |
| Pattern matching `if (obj is CellData cd)` | `CellData cd = obj as CellData; if (cd != null)` |
| `nameof(...)` operator | Use literal strings, e.g. `"SelectedRow"` |

---

## 3. Find & Replace Architecture (`CalcFindReplaceDialog.cs`)

### 3.1 Dialog Structure & UI Layout
`CalcFindReplaceDialog` is a WinForms floating Form designed to remain open while the user navigates and interacts with the spreadsheet canvas.

```
+--------------------------------------------------------------------------+
|  Tìm kiếm và Thay thế (Find & Replace)                            [_][X] |
+--------------------------------------------------------------------------+
|  [ Tab: Tìm kiếm (Find) ]  [ Tab: Thay thế (Replace) ]                   |
|                                                                          |
|  Nội dung tìm kiếm (Find what): [______________________________________] |
|  Thay thế bằng (Replace with):  [______________________________________] |
|                                                                          |
|  [x] Khớp chữ hoa/thường (Match case)                                    |
|  [ ] Khớp toàn bộ nội dung ô (Match entire cell contents)                 |
|                                                                          |
|  Tìm kiếm theo: [ Theo Hàng (By Rows)        |v]                         |
|  Tìm trong:     [ Giá trị (Values)           |v]                         |
|                                                                          |
|  [Tìm tiếp]  [Tìm trước]  [Thay thế]  [Thay thế tất cả]  [Đóng]          |
|                                                                          |
|  Kết quả tìm kiếm (Find All Results):                                     |
|  +--------+--------+---------------------------------------------------+ |
|  | Sheet  | Ô      | Giá trị / Công thức                               | |
|  +--------+--------+---------------------------------------------------+ |
|  | Trang 1| B5     | 1,250.00                                          | |
|  | Trang 1| D12    | =SUM(B5:B11)                                      | |
|  +--------+--------+---------------------------------------------------+ |
|  Trạng thái: Đã tìm thấy 2 kết quả phù hợp.                              |
+--------------------------------------------------------------------------+
```

### 3.2 Control Inventory
- **TabControl** `tabMain`:
  - `tabFind`: Configures dialog for Find mode (`Ctrl+F`). Hides `txtReplace` and Replace action buttons when focused.
  - `tabReplace`: Configures dialog for Replace mode (`Ctrl+H`). Displays `txtReplace`, `btnReplace`, and `btnReplaceAll`.
- **Text Inputs**:
  - `txtFind`: Query string TextBox. Pressing Enter executes `btnFindNext`.
  - `txtReplace`: Replacement string TextBox.
- **Search Options**:
  - `chkMatchCase`: CheckBox ("Khớp chữ hoa/thường / Match case").
  - `chkMatchEntire`: CheckBox ("Khớp toàn bộ nội dung ô / Match entire cell").
  - `cboSearchBy`: ComboBox containing `"Theo Hàng (By Rows)"` (Index 0, default) and `"Theo Cột (By Columns)"` (Index 1).
  - `cboLookIn`: ComboBox containing `"Giá trị (Values)"` (Index 0, default) and `"Công thức (Formulas)"` (Index 1).
- **Actions**:
  - `btnFindNext`: Locates next matching cell in scan order.
  - `btnFindPrev`: Locates previous matching cell (reverse scan order).
  - `btnFindAll`: Populates `lvResults` with all occurrences across the sheet/workbook.
  - `btnReplace`: Replaces matching content in current cell (if matched) or finds next.
  - `btnReplaceAll`: Replaces all matching occurrences in one atomic undoable transaction.
  - `btnClose`: Hides or closes the dialog.
- **Results Navigator**:
  - `lvResults`: `ListView` with columns: Sheet (`SheetName`), Cell (`A1`), and Content (`DisplayText`/`FormulaStr`). Double-clicking or selecting an item instantly focuses `CalcView` on that cell.
  - `lblStatus`: Displays match counts and replacement summaries.

### 3.3 Search & Navigation Engine (`CalcView.cs`)

#### A. Scanning Order & Coordinate Enumeration
Spreadsheet cells can be traversed in two canonical orders:
1. **By Rows**: Outer loop row $r$ from $1 \dots R_{\max}$, inner loop column $c$ from $1 \dots C_{\max}$.
2. **By Columns**: Outer loop column $c$ from $1 \dots C_{\max}$, inner loop row $r$ from $1 \dots R_{\max}$.

To support arbitrary sparse sheets without iterating empty non-existent rows/columns out to 1,000,000:
1. Retrieve candidate populated cell keys from `CurrentSheet.GetAllCells()`.
2. Extract coordinates `(r, c)` via `SparseMatrix.DecodeKey(key)`.
3. Sort candidate coordinates:
   - If `byRows`: primary key `coord.Item1` (row), secondary key `coord.Item2` (col).
   - If `byCols`: primary key `coord.Item2` (col), secondary key `coord.Item1` (row).
4. If `searchBackwards` (Find Prev): reverse the sorted list.
5. Identify current active cell index in the sorted list. Wrap around cyclically:
   - For `FindNext`: inspect candidates after current cell index; if none, loop from index 0.
   - For `FindPrev`: inspect candidates before current cell index; if none, loop from end.

#### B. Cell Content Extraction & Matching Rules
For candidate cell `CellData cell`:
1. **Target Content**:
   - If `lookInFormulas`:
     - If `cell.Type == CellType.Formula`: inspect `cell.FormulaStr` (e.g. `"=SUM(A1:A10)"`).
     - Else: inspect `cell.RawValue ?? ""`.
   - If `lookInValues`:
     - Inspect `cell.GetDisplayText()` (which returns `cell.EvaluatedStr`, formatted number, or `cell.RawValue`).
2. **Match Evaluation**:
   - Comparison: `StringComparison comp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;`
   - If `matchEntire`:
     `bool matched = string.Equals(targetText, query, comp);`
   - Else (substring):
     `bool matched = targetText.IndexOf(query, comp) >= 0;`

#### C. Formula-Safety Specification for Replace
**Critical Rule**: Spreadsheet users must never have their calculation formulas silently corrupted by a text replacement on evaluated numbers.
1. **When `lookInFormulas == false` (Look in Values)**:
   - If a cell contains a formula (`cell.Type == CellType.Formula`):
     - The cell's *evaluated result* may match the query (e.g. formula `=SUM(A1:A5)` evaluates to `100`), but the replace engine **MUST NOT** overwrite the formula with raw text.
     - Formula cells are **strictly protected** and skipped during Replace/Replace All when looking in Values.
   - Static cells (String, Number, Boolean) are replaced safely:
     - If the cell was numeric and the replacement remains numeric, store via `cell.SetNumber(num)`.
     - Else, store as string via `cell.SetString(newText)`.
2. **When `lookInFormulas == true` (Look in Formulas)**:
   - The user explicitly requested to inspect and modify formulas (e.g. renaming a range name, updating a sheet name `=Sheet1!A1` to `=Sheet2!A1`, or replacing a multiplier).
   - Replacement is performed directly on `cell.FormulaStr` and `cell.RawValue`.
   - After replacement, `FormulaEngine.EvaluateSheet(CurrentSheet)` immediately recalculates the workbook so dependent cells reflect the updated formula.

---

## 4. Remove Duplicates Architecture (`CalcRemoveDuplicatesDialog.cs`)

### 4.1 Dialog Structure & UI Layout
`CalcRemoveDuplicatesDialog` is a modal dialog (`ShowDialog(parentForm)`) invoked when cleaning duplicate records from a selected data range.

```
+--------------------------------------------------------------------------+
|  Xóa Trùng Lặp (Remove Duplicates)                                   [X] |
+--------------------------------------------------------------------------+
|  Để xóa các dòng trùng lặp, hãy chọn một hoặc nhiều cột chứa dữ liệu.    |
|  Vùng dữ liệu: A1:D100                                                   |
|                                                                          |
|  [x] Dữ liệu có hàng tiêu đề (My data has headers)                       |
|                                                                          |
|  Các cột cần so sánh:                                                    |
|  +---------------------------------------------------------------------+ |
|  | [x] Cột A: Mã Nhân Viên                                             | |
|  | [x] Cột B: Họ và Tên                                                | |
|  | [x] Cột C: Phòng Ban                                                | |
|  | [ ] Cột D: Mức Lương                                                | |
|  +---------------------------------------------------------------------+ |
|                                                                          |
|  [ Chọn tất cả (Select All) ]    [ Bỏ chọn tất cả (Unselect All) ]       |
|                                                                          |
|                                         [ Xóa trùng lặp (OK) ]  [ Hủy ]  |
+--------------------------------------------------------------------------+
```

### 4.2 Automatic Header Detection Heuristic
When the dialog opens, it inspects the first row (`minR`) vs subsequent rows (`minR + 1`):
1. Count non-empty text cells vs numeric/date cells in row `minR` across columns `minC..maxC`.
2. If row `minR` contains string values (e.g. "ID", "Name", "Department") while row `minR + 1` contains numbers, dates, or distinctly different formatting:
   `chkHasHeaders.Checked = true;`
3. If all rows appear homogenous (e.g. purely numbers or purely strings without distinct header casing):
   `chkHasHeaders.Checked = false;`
4. When `chkHasHeaders.CheckedChanged` fires:
   - Re-populate `clbColumns` labels:
     - If checked: `"Cột {ColLetter}: {CellText(minR, col)}"`
     - If unchecked: `"Cột {ColLetter}"`

### 4.3 Deduplication & Cell Shifting Algorithm

#### A. Composite Equality Hashing
Let the active range be $[R_{\min} \dots R_{\max}, C_{\min} \dots C_{\max}]$ and checked columns be $C_{\text{checked}} \subseteq [C_{\min} \dots C_{\max}]$.
1. Start row $R_{\text{start}} = \text{hasHeaders} ? (R_{\min} + 1) : R_{\min}$.
2. Maintain `HashSet<string> seenSignatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);`
3. Maintain `List<uint> rowsToDelete = new List<uint>();`
4. For row $r$ from $R_{\text{start}}$ to $R_{\max}$:
   - Construct composite signature:
     ```csharp
     StringBuilder sb = new StringBuilder();
     for (int i = 0; i < checkedCols.Count; i++)
     {
         uint col = checkedCols[i];
         CellData cell = CurrentSheet.GetCell(r, col);
         string val = (cell != null) ? (cell.EvaluatedStr ?? cell.RawValue ?? "") : "";
         sb.Append(val.Trim()).Append('\x1F'); // 0x1F Unit Separator prevents delimiter collisions
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
     ```

#### B. Safe Row Deletion & Upward Shifting
To maintain index validity during deletion:
1. Call `SaveUndoState()`.
2. Sort `rowsToDelete` in descending order (highest row number to lowest).
3. For each row $r$ in `rowsToDelete`:
   - If the selected range spans the entire sheet width ($C_{\min} = 1$ and $C_{\max} \ge \text{SheetMaxCol}$):
     Call `CurrentSheet.DeleteRows(r, 1);`
   - Else (sub-range table deduplication):
     Shift cells up **strictly within the column bounds** $[C_{\min} \dots C_{\max}]$ from $r + 1$ up to $R_{\max}$:
     ```csharp
     for (uint currR = r; currR < currentMaxR; currR++)
     {
         for (uint c = minC; c <= maxC; c++)
         {
             CellData nextCell = CurrentSheet.GetCell(currR + 1, c);
             if (nextCell != null)
             {
                 CurrentSheet.SetCell(currR, c, nextCell.Clone());
             }
             else
             {
                 CurrentSheet.ClearCell(currR, c);
             }
         }
     }
     // Clear the vacated trailing row of the sub-range
     for (uint c = minC; c <= maxC; c++)
     {
         CurrentSheet.ClearCell(currentMaxR, c);
     }
     currentMaxR--;
     ```
4. Update view selection: `SelEndRow = currentMaxR;`
5. Recalculate dependent formulas: `FormulaEngine.EvaluateSheet(CurrentSheet);`
6. Display result confirmation message:
   `string.Format("{0} giá trị trùng lặp đã được tìm thấy và xóa bỏ; {1} giá trị duy nhất còn lại.", rowsToDelete.Count, seenSignatures.Count);`

---

## 5. Text to Columns Wizard Architecture (`CalcTextToColumnsDialog.cs`)

### 5.1 Dialog Structure & UI Layout
`CalcTextToColumnsDialog` is an interactive modal wizard allowing the user to partition a single column of delimited or fixed-width text across adjacent columns.

```
+--------------------------------------------------------------------------+
|  Chuyển Văn Bản Thành Cột (Convert Text to Columns Wizard)           [X] |
+--------------------------------------------------------------------------+
|  Kiểu dữ liệu gốc (Original Data Type):                                  |
|  (*) Phân tách bằng ký tự (Delimited) - Phân cách bởi Tab, phẩy, v.v.    |
|  ( ) Độ rộng cố định (Fixed width)    - Các trường canh đều theo cột     |
|                                                                          |
|  Ký tự phân cách (Delimiters):                                           |
|  [x] Phím Tab      [x] Dấu phẩy (,)        [ ] Khoảng trắng              |
|  [ ] Dấu chấm phẩy (;)  [ ] Khác: [__]     [x] Coi các dấu liên tiếp là 1|
|                                                                          |
|  Ký tự định danh văn bản (Text qualifier): [ " (Dấu ngoặc kép)       |v] |
|  Ô đích (Destination):                     [ $B$2                    ]   |
|                                                                          |
|  Xem trước dữ liệu (Data Preview):                                       |
|  +---------------------+---------------------+-------------------------+ |
|  | Cột 1               | Cột 2               | Cột 3                   | |
|  +---------------------+---------------------+-------------------------+ |
|  | Nguyen Van A        | 28                  | Ha Noi                  | |
|  | Tran Thi B          | 32                  | Da Nang                 | |
|  | Le Van C            | 25                  | TP Ho Chi Minh          | |
|  +---------------------+---------------------+-------------------------+ |
|                                                                          |
|                                           [ Thực hiện (OK) ]   [ Hủy ]   |
+--------------------------------------------------------------------------+
```

### 5.2 Parsing Engines

#### A. Delimited Parsing with Text Qualifiers
A robust character scanner that respects quoted segments:
```csharp
public static List<string> ParseDelimitedLine(string line, List<char> delimiters, 
                                              bool consecutiveAsOne, char textQualifier)
{
    List<string> tokens = new List<string>();
    if (string.IsNullOrEmpty(line))
    {
        tokens.Add("");
        return tokens;
    }

    StringBuilder current = new StringBuilder();
    bool insideQuotes = false;
    bool hasQualifier = (textQualifier != '\0' && textQualifier != ' ');

    for (int i = 0; i < line.Length; i++)
    {
        char ch = line[i];

        if (hasQualifier && ch == textQualifier)
        {
            // Escaped quote check (e.g. "" inside quoted string)
            if (insideQuotes && i + 1 < line.Length && line[i + 1] == textQualifier)
            {
                current.Append(ch);
                i++; // skip next quote
            }
            else
            {
                insideQuotes = !insideQuotes;
            }
        }
        else if (!insideQuotes && delimiters.Contains(ch))
        {
            if (consecutiveAsOne)
            {
                // Push current token and skip adjacent delimiters
                tokens.Add(current.ToString());
                current.Clear();
                while (i + 1 < line.Length && delimiters.Contains(line[i + 1]))
                {
                    i++;
                }
            }
            else
            {
                tokens.Add(current.ToString());
                current.Clear();
            }
        }
        else
        {
            current.Append(ch);
        }
    }
    tokens.Add(current.ToString());
    return tokens;
}
```

#### B. Fixed-Width Character Parsing
Splits strings at user-defined column offset boundaries $[p_1, p_2, \dots, p_k]$:
```csharp
public static List<string> ParseFixedWidthLine(string line, List<int> breaks)
{
    List<string> tokens = new List<string>();
    if (line == null) line = "";
    int lastPos = 0;
    for (int i = 0; i < breaks.Count; i++)
    {
        int p = breaks[i];
        if (p > lastPos)
        {
            if (lastPos < line.Length)
            {
                int len = Math.Min(p - lastPos, line.Length - lastPos);
                tokens.Add(line.Substring(lastPos, len));
            }
            else
            {
                tokens.Add("");
            }
            lastPos = p;
        }
    }
    if (lastPos < line.Length)
    {
        tokens.Add(line.Substring(lastPos));
    }
    else if (tokens.Count == 0)
    {
        tokens.Add(line);
    }
    return tokens;
}
```

### 5.3 Live Preview DataGrid
- `dgvPreview` is an unbound `DataGridView` placed in the lower half of `CalcTextToColumnsDialog`.
- Whenever any delimiter checkbox, other character TextBox, consecutive flag, text qualifier, or break position is altered, `UpdateLivePreview()` runs synchronously:
  1. Reads the top 10 rows of the selected source column.
  2. Parses each row with the active settings.
  3. Reconfigures `dgvPreview.Columns` count to match the maximum token length.
  4. Populates rows with parsed tokens.

### 5.4 Overwrite Protection & Execution
Before committing changes to the sheet:
1. Parse destination cell address from `txtDestination.Text` via `SparseMatrix.ParseCellAddress(txtDest)`. Default is top-left cell $(R_{\text{start}}, C_{\text{src}})$.
2. Calculate target span: Rows $[R_{\text{dest}} \dots R_{\text{dest}} + (R_{\text{end}} - R_{\text{start}})]$, Columns $[C_{\text{dest}} \dots C_{\text{dest}} + \text{maxTokens} - 1]$.
3. **Collision Detection**:
   Inspect all cells in target span that do *not* overlap the source column.
   If any cell has non-empty content:
   ```csharp
   DialogResult dr = MessageBox.Show(
       "Vùng dữ liệu đích đã có sẵn dữ liệu. Bạn có muốn ghi đè lên không?",
       "Chuyển Văn Bản Thành Cột",
       MessageBoxButtons.YesNo,
       MessageBoxIcon.Warning);
   if (dr != DialogResult.Yes) return;
   ```
4. **Execution Pipeline**:
   - `SaveUndoState()`.
   - Iterate each source row, parse into tokens.
   - For each token at index $i$:
     - Target cell $(r_{\text{dest}}, c_{\text{dest}} + i)$.
     - Type inference: attempt `double.TryParse(trimmedToken, NumberStyles.Any, CultureInfo.InvariantCulture, out numVal)`.
     - If numeric: `CurrentSheet.SetCellNumber(r, c, numVal)`.
     - Else: `CurrentSheet.SetCellText(r, c, trimmedToken)`.
   - If destination starts at a different column than source, optionally clear source column or leave as is (Excel overwrites source when destination equals source).
   - Recalculate sheet formulas: `FormulaEngine.EvaluateSheet(CurrentSheet)`.
   - Invalidate canvas and close dialog.

---

## 6. CalcView & Ribbon Integration Strategy

### 6.1 `CalcView.cs` API Extensions
Add the following public methods to `src/ui/CalcView.cs`:
1. `public void OpenFindReplaceDialog(Form parentForm, bool isReplace = false)`
   - Instantiates `CalcFindReplaceDialog(this, parentForm, isReplace)`.
   - Displays as modeless window: `dlg.Show(parentForm)`.
2. `public void OpenRemoveDuplicatesDialog(Form parentForm)`
   - Instantiates `CalcRemoveDuplicatesDialog(this, parentForm)`.
   - Displays as modal dialog: `dlg.ShowDialog(parentForm)`.
3. `public void OpenTextToColumnsDialog(Form parentForm)`
   - Instantiates `CalcTextToColumnsDialog(this, parentForm)`.
   - Displays as modal dialog: `dlg.ShowDialog(parentForm)`.
4. Supporting search & replace primitives:
   - `public bool FindNextCellEx(string query, bool matchCase, bool matchEntire, bool byRows, bool lookInFormulas, bool searchBackwards, out string statusMsg)`
   - `public int ReplaceCellEx(string find, string replace, bool matchCase, bool matchEntire, bool byRows, bool lookInFormulas)`
   - `public int ReplaceAllInSheetEx(string find, string replace, bool matchCase, bool matchEntire, bool lookInFormulas)`
   - `public List<Tuple<string, uint, uint, string>> FindAllCellsEx(string query, bool matchCase, bool matchEntire, bool lookInFormulas)`
5. Supporting data utility primitives:
   - `public void ExecuteRemoveDuplicates(uint minR, uint maxR, uint minC, uint maxC, List<uint> checkedCols, bool hasHeaders, out int dupCount, out int uniqueCount)`
   - `public void ExecuteTextToColumns(uint srcCol, uint startRow, uint endRow, uint destCol, uint destRow, List<char> delimiters, bool consecutiveAsOne, char textQualifier, List<int> fixedWidths, bool isFixedWidth)`

### 6.2 Keyboard Shortcuts Wiring
- In `CalcView.cs` line 3865 (inside `HandleKeyDown`):
  ```csharp
  if (ctrl && rawKey == Keys.F)
  {
      OpenFindReplaceDialog(Form.ActiveForm, false);
      return;
  }
  if (ctrl && rawKey == Keys.H)
  {
      OpenFindReplaceDialog(Form.ActiveForm, true);
      return;
  }
  ```
- In `AppForm.cs` line 2997 (inside `ProcessCmdKey` switch `case Keys.F:`):
  ```csharp
  else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
  {
      CalcView.OpenFindReplaceDialog(this, false);
      return;
  }
  ```
- In `AppForm.cs` line 3035 (add `case Keys.H:`):
  ```csharp
  case Keys.H:
      if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
      {
          CalcView.OpenFindReplaceDialog(this, true);
          return;
      }
      break;
  ```

### 6.3 Ribbon Bar Buttons (`RibbonBar.cs`)
1. **Home Tab ("Trang đầu")**:
   - Lines 19827-19828 already wire Find and Replace dropdown items to `calcView.OpenFindReplaceDialog(parentForm, false)` and `calcView.OpenFindReplaceDialog(parentForm, true)`.
2. **Data Tab ("Dữ liệu", `ActiveRibbonTab == 4`)**:
   - In `DrawCalcTabData` (line 11654):
     - Keep `Xóa Trùng Lặp` button at `curX = 196..272`.
     - Append `Phân tách Cột` (Text to Columns) button immediately adjacent at `curX = 276..362` (width 86, height 54).
     - Update group label `Công cụ Dữ liệu` center coordinate.
   - In `HandleCalcTabClick` (line 20286):
     - When `x >= 196 && x <= 272`: call `calcView.OpenRemoveDuplicatesDialog(parentForm)`.
     - When `x >= 276 && x <= 362`: call `calcView.OpenTextToColumnsDialog(parentForm)`.

---

## 7. Concrete C# 5 Implementation Specifications

### 7.1 `src/ui/CalcFindReplaceDialog.cs`
Full implementation blueprint complying 100% with C# 5.0:

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class CalcFindReplaceDialog : Form
    {
        private CalcView _calcView;
        private Form _parentForm;

        private TabControl tabControl;
        private TabPage tabFind;
        private TabPage tabReplace;

        private TextBox txtFind;
        private TextBox txtReplace;
        private CheckBox chkMatchCase;
        private CheckBox chkMatchEntire;
        private ComboBox cboSearchBy;
        private ComboBox cboLookIn;

        private Button btnFindNext;
        private Button btnFindPrev;
        private Button btnFindAll;
        private Button btnReplace;
        private Button btnReplaceAll;
        private Button btnClose;

        private ListView lvResults;
        private Label lblStatus;

        public CalcFindReplaceDialog(CalcView calcView, Form parentForm, bool isReplace)
        {
            _calcView = calcView;
            _parentForm = parentForm;

            InitializeComponent();
            SetMode(isReplace);
        }

        public void SetMode(bool isReplace)
        {
            if (tabControl != null)
            {
                tabControl.SelectedTab = isReplace ? tabReplace : tabFind;
            }
            UpdateControlVisibility(isReplace);
            txtFind.Focus();
            txtFind.SelectAll();
        }

        private void InitializeComponent()
        {
            this.Text = "Tìm kiếm & Thay thế - ZenQ Calc";
            this.Size = new Size(540, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(243, 242, 241);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            tabControl = new TabControl();
            tabControl.Location = new Point(12, 10);
            tabControl.Size = new Size(500, 30);
            tabFind = new TabPage("Tìm kiếm (Find)");
            tabReplace = new TabPage("Thay thế (Replace)");
            tabControl.TabPages.Add(tabFind);
            tabControl.TabPages.Add(tabReplace);
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            this.Controls.Add(tabControl);

            Label lblFind = new Label();
            lblFind.Text = "Nội dung tìm kiếm:";
            lblFind.Location = new Point(15, 52);
            lblFind.AutoSize = true;
            this.Controls.Add(lblFind);

            txtFind = new TextBox();
            txtFind.Location = new Point(150, 49);
            txtFind.Size = new Size(240, 25);
            txtFind.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) DoFindNext(); };
            this.Controls.Add(txtFind);

            Label lblReplace = new Label();
            lblReplace.Name = "lblReplace";
            lblReplace.Text = "Thay thế bằng:";
            lblReplace.Location = new Point(15, 85);
            lblReplace.AutoSize = true;
            this.Controls.Add(lblReplace);

            txtReplace = new TextBox();
            txtReplace.Name = "txtReplace";
            txtReplace.Location = new Point(150, 82);
            txtReplace.Size = new Size(240, 25);
            this.Controls.Add(txtReplace);

            chkMatchCase = new CheckBox();
            chkMatchCase.Text = "Khớp chữ hoa/thường (Match case)";
            chkMatchCase.Location = new Point(15, 120);
            chkMatchCase.AutoSize = true;
            this.Controls.Add(chkMatchCase);

            chkMatchEntire = new CheckBox();
            chkMatchEntire.Text = "Khớp toàn bộ ô (Match entire cell)";
            chkMatchEntire.Location = new Point(15, 145);
            chkMatchEntire.AutoSize = true;
            this.Controls.Add(chkMatchEntire);

            Label lblSearchBy = new Label();
            lblSearchBy.Text = "Tìm kiếm theo:";
            lblSearchBy.Location = new Point(270, 122);
            lblSearchBy.AutoSize = true;
            this.Controls.Add(lblSearchBy);

            cboSearchBy = new ComboBox();
            cboSearchBy.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSearchBy.Items.AddRange(new object[] { "Theo Hàng (By Rows)", "Theo Cột (By Columns)" });
            cboSearchBy.SelectedIndex = 0;
            cboSearchBy.Location = new Point(365, 118);
            cboSearchBy.Size = new Size(145, 23);
            this.Controls.Add(cboSearchBy);

            Label lblLookIn = new Label();
            lblLookIn.Text = "Tìm trong:";
            lblLookIn.Location = new Point(270, 147);
            lblLookIn.AutoSize = true;
            this.Controls.Add(lblLookIn);

            cboLookIn = new ComboBox();
            cboLookIn.DropDownStyle = ComboBoxStyle.DropDownList;
            cboLookIn.Items.AddRange(new object[] { "Giá trị (Values)", "Công thức (Formulas)" });
            cboLookIn.SelectedIndex = 0;
            cboLookIn.Location = new Point(365, 144);
            cboLookIn.Size = new Size(145, 23);
            this.Controls.Add(cboLookIn);

            // Action Buttons
            btnFindNext = new Button();
            btnFindNext.Text = "Tìm tiếp";
            btnFindNext.Location = new Point(405, 47);
            btnFindNext.Size = new Size(105, 28);
            btnFindNext.Click += (s, e) => DoFindNext();
            this.Controls.Add(btnFindNext);

            btnFindPrev = new Button();
            btnFindPrev.Text = "Tìm trước";
            btnFindPrev.Location = new Point(405, 80);
            btnFindPrev.Size = new Size(105, 28);
            btnFindPrev.Click += (s, e) => DoFindPrev();
            this.Controls.Add(btnFindPrev);

            btnReplace = new Button();
            btnReplace.Text = "Thay thế";
            btnReplace.Location = new Point(15, 180);
            btnReplace.Size = new Size(100, 30);
            btnReplace.Click += (s, e) => DoReplace();
            this.Controls.Add(btnReplace);

            btnReplaceAll = new Button();
            btnReplaceAll.Text = "Thay thế tất cả";
            btnReplaceAll.Location = new Point(125, 180);
            btnReplaceAll.Size = new Size(115, 30);
            btnReplaceAll.Click += (s, e) => DoReplaceAll();
            this.Controls.Add(btnReplaceAll);

            btnFindAll = new Button();
            btnFindAll.Text = "Tìm tất cả";
            btnFindAll.Location = new Point(250, 180);
            btnFindAll.Size = new Size(100, 30);
            btnFindAll.Click += (s, e) => DoFindAll();
            this.Controls.Add(btnFindAll);

            btnClose = new Button();
            btnClose.Text = "Đóng";
            btnClose.Location = new Point(410, 180);
            btnClose.Size = new Size(100, 30);
            btnClose.Click += (s, e) => this.Hide();
            this.Controls.Add(btnClose);

            // Results List
            lvResults = new ListView();
            lvResults.View = View.Details;
            lvResults.FullRowSelect = true;
            lvResults.GridLines = true;
            lvResults.Location = new Point(15, 220);
            lvResults.Size = new Size(495, 180);
            lvResults.Columns.Add("Bảng tính", 80);
            lvResults.Columns.Add("Ô", 60);
            lvResults.Columns.Add("Nội dung", 330);
            lvResults.SelectedIndexChanged += LvResults_SelectedIndexChanged;
            this.Controls.Add(lvResults);

            lblStatus = new Label();
            lblStatus.Text = "Sẵn sàng tìm kiếm.";
            lblStatus.Location = new Point(15, 410);
            lblStatus.AutoSize = true;
            this.Controls.Add(lblStatus);
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControlVisibility(tabControl.SelectedTab == tabReplace);
        }

        private void UpdateControlVisibility(bool isReplace)
        {
            Control lblRepl = this.Controls["lblReplace"];
            if (lblRepl != null) lblRepl.Visible = isReplace;
            txtReplace.Visible = isReplace;
            btnReplace.Visible = isReplace;
            btnReplaceAll.Visible = isReplace;
        }

        private void DoFindNext()
        {
            string q = txtFind.Text;
            if (string.IsNullOrEmpty(q)) return;

            string statusMsg;
            bool found = _calcView.FindNextCellEx(q, chkMatchCase.Checked, chkMatchEntire.Checked,
                cboSearchBy.SelectedIndex == 0, cboLookIn.SelectedIndex == 1, false, out statusMsg);

            lblStatus.Text = statusMsg;
            if (!found)
            {
                MessageBox.Show(this, "Không tìm thấy nội dung: \"" + q + "\"", "Tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (_parentForm != null) _parentForm.Invalidate();
        }

        private void DoFindPrev()
        {
            string q = txtFind.Text;
            if (string.IsNullOrEmpty(q)) return;

            string statusMsg;
            bool found = _calcView.FindNextCellEx(q, chkMatchCase.Checked, chkMatchEntire.Checked,
                cboSearchBy.SelectedIndex == 0, cboLookIn.SelectedIndex == 1, true, out statusMsg);

            lblStatus.Text = statusMsg;
            if (!found)
            {
                MessageBox.Show(this, "Không tìm thấy nội dung: \"" + q + "\"", "Tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (_parentForm != null) _parentForm.Invalidate();
        }

        private void DoReplace()
        {
            string find = txtFind.Text;
            if (string.IsNullOrEmpty(find)) return;
            string repl = txtReplace.Text ?? "";

            int rep = _calcView.ReplaceCellEx(find, repl, chkMatchCase.Checked, chkMatchEntire.Checked,
                cboSearchBy.SelectedIndex == 0, cboLookIn.SelectedIndex == 1);

            if (rep > 0)
            {
                lblStatus.Text = "Đã thay thế ô hiện tại.";
                DoFindNext();
            }
            else
            {
                DoFindNext();
            }
            if (_parentForm != null) _parentForm.Invalidate();
        }

        private void DoReplaceAll()
        {
            string find = txtFind.Text;
            if (string.IsNullOrEmpty(find)) return;
            string repl = txtReplace.Text ?? "";

            int count = _calcView.ReplaceAllInSheetEx(find, repl, chkMatchCase.Checked, chkMatchEntire.Checked,
                cboLookIn.SelectedIndex == 1);

            lblStatus.Text = string.Format("Đã hoàn tất thay thế {0} vị trí.", count);
            MessageBox.Show(this, string.Format("ZenQ Calc đã hoàn thành việc tìm kiếm và thực hiện {0} lượt thay thế.", count),
                "Thay thế tất cả", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (_parentForm != null) _parentForm.Invalidate();
        }

        private void DoFindAll()
        {
            string q = txtFind.Text;
            if (string.IsNullOrEmpty(q)) return;

            lvResults.Items.Clear();
            var matches = _calcView.FindAllCellsEx(q, chkMatchCase.Checked, chkMatchEntire.Checked, cboLookIn.SelectedIndex == 1);

            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                ListViewItem lvi = new ListViewItem(m.Item1);
                lvi.SubItems.Add(SparseMatrix.GetCellReference(m.Item2, m.Item3));
                lvi.SubItems.Add(m.Item4);
                lvi.Tag = Tuple.Create(m.Item2, m.Item3);
                lvResults.Items.Add(lvi);
            }

            lblStatus.Text = string.Format("Đã tìm thấy {0} kết quả phù hợp.", matches.Count);
        }

        private void LvResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvResults.SelectedItems.Count > 0)
            {
                var tag = lvResults.SelectedItems[0].Tag as Tuple<uint, uint>;
                if (tag != null)
                {
                    _calcView.SelectedRow = tag.Item1;
                    _calcView.SelectedCol = tag.Item2;
                    _calcView.SelEndRow = tag.Item1;
                    _calcView.SelEndCol = tag.Item2;
                    _calcView.EnsureCellVisible(tag.Item1, tag.Item2);
                    if (_parentForm != null) _parentForm.Invalidate();
                }
            }
        }
    }
}
```

### 7.2 `src/ui/CalcRemoveDuplicatesDialog.cs`
Full implementation blueprint for Remove Duplicates:

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class CalcRemoveDuplicatesDialog : Form
    {
        private CalcView _calcView;
        private Form _parentForm;

        private uint _minR;
        private uint _maxR;
        private uint _minC;
        private uint _maxC;

        private Label lblRange;
        private CheckBox chkHasHeaders;
        private CheckedListBox clbColumns;
        private Button btnSelectAll;
        private Button btnUnselectAll;
        private Button btnOK;
        private Button btnCancel;

        public CalcRemoveDuplicatesDialog(CalcView calcView, Form parentForm)
        {
            _calcView = calcView;
            _parentForm = parentForm;

            DetermineRange();
            InitializeComponent();
            PopulateColumns();
        }

        private void DetermineRange()
        {
            _minR = Math.Min(_calcView.SelectedRow, _calcView.SelEndRow);
            _maxR = Math.Max(_calcView.SelectedRow, _calcView.SelEndRow);
            _minC = Math.Min(_calcView.SelectedCol, _calcView.SelEndCol);
            _maxC = Math.Max(_calcView.SelectedCol, _calcView.SelEndCol);

            if (_minR == _maxR && _minC == _maxC)
            {
                _calcView.GetContiguousDataRange(_calcView.SelectedRow, _calcView.SelectedCol,
                    out _minR, out _maxR, out _minC, out _maxC);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Xóa Trùng Lặp (Remove Duplicates)";
            this.Size = new Size(380, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.BackColor = Color.FromArgb(243, 242, 241);

            Label lblPrompt = new Label();
            lblPrompt.Text = "Để xóa các dòng trùng lặp, hãy chọn một hoặc nhiều cột:";
            lblPrompt.Location = new Point(15, 12);
            lblPrompt.Size = new Size(340, 32);
            this.Controls.Add(lblPrompt);

            lblRange = new Label();
            string refStart = SparseMatrix.GetCellReference(_minR, _minC);
            string refEnd = SparseMatrix.GetCellReference(_maxR, _maxC);
            lblRange.Text = "Vùng dữ liệu: " + refStart + ":" + refEnd;
            lblRange.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblRange.Location = new Point(15, 45);
            lblRange.AutoSize = true;
            this.Controls.Add(lblRange);

            chkHasHeaders = new CheckBox();
            chkHasHeaders.Text = "Dữ liệu có hàng tiêu đề (My data has headers)";
            chkHasHeaders.Location = new Point(15, 72);
            chkHasHeaders.AutoSize = true;
            chkHasHeaders.Checked = DetectHeaderHeuristic();
            chkHasHeaders.CheckedChanged += (s, e) => PopulateColumns();
            this.Controls.Add(chkHasHeaders);

            btnSelectAll = new Button();
            btnSelectAll.Text = "Chọn tất cả";
            btnSelectAll.Location = new Point(15, 100);
            btnSelectAll.Size = new Size(95, 26);
            btnSelectAll.Click += (s, e) => SetAllColumnsChecked(true);
            this.Controls.Add(btnSelectAll);

            btnUnselectAll = new Button();
            btnUnselectAll.Text = "Bỏ chọn tất cả";
            btnUnselectAll.Location = new Point(115, 100);
            btnUnselectAll.Size = new Size(105, 26);
            btnUnselectAll.Click += (s, e) => SetAllColumnsChecked(false);
            this.Controls.Add(btnUnselectAll);

            clbColumns = new CheckedListBox();
            clbColumns.Location = new Point(15, 132);
            clbColumns.Size = new Size(335, 190);
            clbColumns.CheckOnClick = true;
            this.Controls.Add(clbColumns);

            btnOK = new Button();
            btnOK.Text = "Xóa trùng lặp";
            btnOK.Location = new Point(145, 335);
            btnOK.Size = new Size(110, 32);
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);

            btnCancel = new Button();
            btnCancel.Text = "Hủy";
            btnCancel.Location = new Point(265, 335);
            btnCancel.Size = new Size(85, 32);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private bool DetectHeaderHeuristic()
        {
            if (_maxR <= _minR) return false;
            SparseMatrix sheet = _calcView.CurrentSheet;
            if (sheet == null) return false;

            int stringCountRow1 = 0;
            int numCountRow2 = 0;
            for (uint c = _minC; c <= _maxC; c++)
            {
                CellData c1 = sheet.GetCell(_minR, c);
                if (c1 != null && c1.Type == CellType.String && !string.IsNullOrEmpty(c1.RawValue))
                {
                    stringCountRow1++;
                }
                CellData c2 = sheet.GetCell(_minR + 1, c);
                if (c2 != null && (c2.Type == CellType.Number || c2.Type == CellType.Formula))
                {
                    numCountRow2++;
                }
            }
            return (stringCountRow1 > 0 && numCountRow2 > 0) || stringCountRow1 >= ((_maxC - _minC + 1) / 2);
        }

        private void PopulateColumns()
        {
            clbColumns.Items.Clear();
            SparseMatrix sheet = _calcView.CurrentSheet;

            for (uint c = _minC; c <= _maxC; c++)
            {
                string colLetter = SparseMatrix.ColIndexToName(c);
                string text = "Cột " + colLetter;
                if (chkHasHeaders.Checked && sheet != null)
                {
                    CellData headerCell = sheet.GetCell(_minR, c);
                    string headerVal = (headerCell != null) ? (headerCell.EvaluatedStr ?? headerCell.RawValue ?? "") : "";
                    if (!string.IsNullOrEmpty(headerVal))
                    {
                        text += ": " + headerVal;
                    }
                }
                clbColumns.Items.Add(new ColumnItem(c, text), true);
            }
        }

        private void SetAllColumnsChecked(bool isChecked)
        {
            for (int i = 0; i < clbColumns.Items.Count; i++)
            {
                clbColumns.SetItemChecked(i, isChecked);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            List<uint> checkedCols = new List<uint>();
            for (int i = 0; i < clbColumns.CheckedItems.Count; i++)
            {
                ColumnItem item = clbColumns.CheckedItems[i] as ColumnItem;
                if (item != null) checkedCols.Add(item.ColIndex);
            }

            if (checkedCols.Count == 0)
            {
                MessageBox.Show(this, "Vui lòng chọn ít nhất một cột để kiểm tra trùng lặp!",
                    "Xóa Trùng Lặp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int dupCount;
            int uniqueCount;
            _calcView.ExecuteRemoveDuplicates(_minR, _maxR, _minC, _maxC, checkedCols,
                chkHasHeaders.Checked, out dupCount, out uniqueCount);

            MessageBox.Show(this, string.Format("Đã tìm thấy và xóa {0} dòng trùng lặp!\nGiữ lại {1} dòng dữ liệu duy nhất.",
                dupCount, uniqueCount), "Xóa Trùng Lặp Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (_parentForm != null) _parentForm.Invalidate();
            this.Close();
        }

        private class ColumnItem
        {
            public uint ColIndex { get; private set; }
            public string DisplayText { get; private set; }

            public ColumnItem(uint colIndex, string text)
            {
                ColIndex = colIndex;
                DisplayText = text;
            }

            public override string ToString()
            {
                return DisplayText;
            }
        }
    }
}
```

### 7.3 `src/ui/CalcTextToColumnsDialog.cs`
Full implementation blueprint for Text to Columns wizard with live preview:

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class CalcTextToColumnsDialog : Form
    {
        private CalcView _calcView;
        private Form _parentForm;

        private uint _srcCol;
        private uint _startRow;
        private uint _endRow;

        private RadioButton radDelimited;
        private RadioButton radFixedWidth;
        private CheckBox chkTab;
        private CheckBox chkSemicolon;
        private CheckBox chkComma;
        private CheckBox chkSpace;
        private CheckBox chkOther;
        private TextBox txtOther;
        private CheckBox chkConsecutive;
        private ComboBox cboQualifier;
        private TextBox txtBreakPositions;
        private TextBox txtDestination;
        private DataGridView dgvPreview;
        private Button btnOK;
        private Button btnCancel;

        public CalcTextToColumnsDialog(CalcView calcView, Form parentForm)
        {
            _calcView = calcView;
            _parentForm = parentForm;

            _srcCol = Math.Min(_calcView.SelectedCol, _calcView.SelEndCol);
            _startRow = Math.Min(_calcView.SelectedRow, _calcView.SelEndRow);
            _endRow = Math.Max(_calcView.SelectedRow, _calcView.SelEndRow);

            InitializeComponent();
            UpdateLivePreview();
        }

        private void InitializeComponent()
        {
            this.Text = "Chuyển Văn Bản Thành Cột (Convert Text to Columns)";
            this.Size = new Size(580, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.BackColor = Color.FromArgb(243, 242, 241);

            GroupBox grpMode = new GroupBox();
            grpMode.Text = "Kiểu dữ liệu gốc";
            grpMode.Location = new Point(15, 10);
            grpMode.Size = new Size(535, 65);

            radDelimited = new RadioButton();
            radDelimited.Text = "Phân tách bằng ký tự (Delimited - Tab, phẩy, chấm phẩy, v.v.)";
            radDelimited.Location = new Point(15, 20);
            radDelimited.AutoSize = true;
            radDelimited.Checked = true;
            radDelimited.CheckedChanged += (s, e) => { ToggleMode(); UpdateLivePreview(); };
            grpMode.Controls.Add(radDelimited);

            radFixedWidth = new RadioButton();
            radFixedWidth.Text = "Độ rộng cố định (Fixed width - Các cột canh đều theo vị trí ký tự)";
            radFixedWidth.Location = new Point(15, 42);
            radFixedWidth.AutoSize = true;
            grpMode.Controls.Add(radFixedWidth);
            this.Controls.Add(grpMode);

            // Delimiters Group
            GroupBox grpDelim = new GroupBox();
            grpDelim.Name = "grpDelim";
            grpDelim.Text = "Ký tự phân cách (Delimiters)";
            grpDelim.Location = new Point(15, 80);
            grpDelim.Size = new Size(535, 95);

            chkTab = new CheckBox { Text = "Tab", Location = new Point(15, 20), AutoSize = true, Checked = true };
            chkTab.CheckedChanged += (s, e) => UpdateLivePreview();
            grpDelim.Controls.Add(chkTab);

            chkSemicolon = new CheckBox { Text = "Chấm phẩy (;)", Location = new Point(110, 20), AutoSize = true };
            chkSemicolon.CheckedChanged += (s, e) => UpdateLivePreview();
            grpDelim.Controls.Add(chkSemicolon);

            chkComma = new CheckBox { Text = "Dấu phẩy (,)", Location = new Point(230, 20), AutoSize = true };
            chkComma.CheckedChanged += (s, e) => UpdateLivePreview();
            grpDelim.Controls.Add(chkComma);

            chkSpace = new CheckBox { Text = "Khoảng trắng", Location = new Point(340, 20), AutoSize = true };
            chkSpace.CheckedChanged += (s, e) => UpdateLivePreview();
            grpDelim.Controls.Add(chkSpace);

            chkOther = new CheckBox { Text = "Khác:", Location = new Point(15, 45), AutoSize = true };
            chkOther.CheckedChanged += (s, e) => UpdateLivePreview();
            grpDelim.Controls.Add(chkOther);

            txtOther = new TextBox { Location = new Point(70, 43), Size = new Size(30, 22), MaxLength = 2 };
            txtOther.TextChanged += (s, e) => { chkOther.Checked = !string.IsNullOrEmpty(txtOther.Text); UpdateLivePreview(); };
            grpDelim.Controls.Add(txtOther);

            chkConsecutive = new CheckBox { Text = "Coi các dấu phân cách liên tiếp là một", Location = new Point(150, 45), AutoSize = true, Checked = true };
            chkConsecutive.CheckedChanged += (s, e) => UpdateLivePreview();
            grpDelim.Controls.Add(chkConsecutive);

            Label lblQual = new Label { Text = "Ký tự định danh văn bản:", Location = new Point(15, 70), AutoSize = true };
            grpDelim.Controls.Add(lblQual);

            cboQualifier = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(160, 68), Size = new Size(80, 22) };
            cboQualifier.Items.AddRange(new object[] { "\"", "'", "{Không có}" });
            cboQualifier.SelectedIndex = 0;
            cboQualifier.SelectedIndexChanged += (s, e) => UpdateLivePreview();
            grpDelim.Controls.Add(cboQualifier);

            this.Controls.Add(grpDelim);

            // Fixed Width Box
            GroupBox grpFixed = new GroupBox();
            grpFixed.Name = "grpFixed";
            grpFixed.Text = "Độ rộng cố định (Fixed Width Breakpoints)";
            grpFixed.Location = new Point(15, 80);
            grpFixed.Size = new Size(535, 95);
            grpFixed.Visible = false;

            Label lblFixedHelp = new Label { Text = "Nhập danh sách các vị trí cắt cột (phân cách bằng dấu phẩy, VD: 10, 25, 40):", Location = new Point(15, 25), AutoSize = true };
            grpFixed.Controls.Add(lblFixedHelp);

            txtBreakPositions = new TextBox { Location = new Point(15, 52), Size = new Size(400, 24) };
            txtBreakPositions.TextChanged += (s, e) => UpdateLivePreview();
            grpFixed.Controls.Add(txtBreakPositions);

            this.Controls.Add(grpFixed);

            // Destination
            Label lblDest = new Label { Text = "Ô đích (Destination):", Location = new Point(15, 185), AutoSize = true };
            this.Controls.Add(lblDest);

            txtDestination = new TextBox { Location = new Point(150, 182), Size = new Size(120, 24) };
            txtDestination.Text = "$" + SparseMatrix.ColIndexToName(_srcCol) + "$" + _startRow;
            this.Controls.Add(txtDestination);

            // Live Preview Grid
            Label lblPrev = new Label { Text = "Xem trước dữ liệu phân tách (Data Preview):", Location = new Point(15, 215), AutoSize = true };
            this.Controls.Add(lblPrev);

            dgvPreview = new DataGridView();
            dgvPreview.Location = new Point(15, 235);
            dgvPreview.Size = new Size(535, 185);
            dgvPreview.ReadOnly = true;
            dgvPreview.AllowUserToAddRows = false;
            dgvPreview.AllowUserToDeleteRows = false;
            dgvPreview.AllowUserToResizeRows = false;
            dgvPreview.RowHeadersVisible = false;
            dgvPreview.BackgroundColor = Color.White;
            dgvPreview.BorderStyle = BorderStyle.Fixed3D;
            this.Controls.Add(dgvPreview);

            btnOK = new Button { Text = "Thực hiện (Finish)", Location = new Point(320, 435), Size = new Size(130, 32) };
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);

            btnCancel = new Button { Text = "Hủy", Location = new Point(460, 435), Size = new Size(90, 32) };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private void ToggleMode()
        {
            bool isDelim = radDelimited.Checked;
            this.Controls["grpDelim"].Visible = isDelim;
            this.Controls["grpFixed"].Visible = !isDelim;
        }

        private List<char> GetActiveDelimiters()
        {
            List<char> delims = new List<char>();
            if (chkTab.Checked) delims.Add('\t');
            if (chkSemicolon.Checked) delims.Add(';');
            if (chkComma.Checked) delims.Add(',');
            if (chkSpace.Checked) delims.Add(' ');
            if (chkOther.Checked && !string.IsNullOrEmpty(txtOther.Text))
            {
                delims.Add(txtOther.Text[0]);
            }
            if (delims.Count == 0) delims.Add('\t');
            return delims;
        }

        private char GetTextQualifier()
        {
            if (cboQualifier.SelectedIndex == 0) return '"';
            if (cboQualifier.SelectedIndex == 1) return '\'';
            return '\0';
        }

        private List<int> GetBreakPositions()
        {
            List<int> list = new List<int>();
            if (string.IsNullOrEmpty(txtBreakPositions.Text)) return list;
            string[] parts = txtBreakPositions.Text.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val) && val > 0)
                {
                    if (!list.Contains(val)) list.Add(val);
                }
            }
            list.Sort();
            return list;
        }

        private void UpdateLivePreview()
        {
            dgvPreview.Columns.Clear();
            dgvPreview.Rows.Clear();

            SparseMatrix sheet = _calcView.CurrentSheet;
            if (sheet == null) return;

            uint previewLimit = Math.Min(_endRow, _startRow + 10);
            List<List<string>> rowTokens = new List<List<string>>();
            int maxCols = 1;

            bool isDelim = radDelimited.Checked;
            List<char> delims = GetActiveDelimiters();
            char qual = GetTextQualifier();
            List<int> breaks = GetBreakPositions();

            for (uint r = _startRow; r <= previewLimit; r++)
            {
                CellData cell = sheet.GetCell(r, _srcCol);
                string text = (cell != null) ? (cell.RawValue ?? "") : "";
                List<string> tokens;
                if (isDelim)
                {
                    tokens = ParseDelimitedLine(text, delims, chkConsecutive.Checked, qual);
                }
                else
                {
                    tokens = ParseFixedWidthLine(text, breaks);
                }
                rowTokens.Add(tokens);
                if (tokens.Count > maxCols) maxCols = tokens.Count;
            }

            for (int c = 0; c < maxCols; c++)
            {
                dgvPreview.Columns.Add("Col" + c, "Cột " + (c + 1));
            }

            for (int r = 0; r < rowTokens.Count; r++)
            {
                var toks = rowTokens[r];
                object[] rowVals = new object[maxCols];
                for (int c = 0; c < maxCols; c++)
                {
                    rowVals[c] = (c < toks.Count) ? toks[c] : "";
                }
                dgvPreview.Rows.Add(rowVals);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            Tuple<uint, uint> dest = SparseMatrix.ParseCellAddress(txtDestination.Text);
            uint destR = dest != null ? dest.Item1 : _startRow;
            uint destC = dest != null ? dest.Item2 : _srcCol;

            // Check overwrite collision
            bool hasCollision = false;
            SparseMatrix sheet = _calcView.CurrentSheet;
            uint rowSpan = _endRow - _startRow;
            for (uint r = 0; r <= rowSpan; r++)
            {
                for (uint c = 1; c < (uint)dgvPreview.Columns.Count; c++)
                {
                    uint targetR = destR + r;
                    uint targetC = destC + c;
                    if (targetC == _srcCol && targetR >= _startRow && targetR <= _endRow) continue;
                    CellData existing = sheet.GetCell(targetR, targetC);
                    if (existing != null && !string.IsNullOrEmpty(existing.RawValue))
                    {
                        hasCollision = true;
                        break;
                    }
                }
                if (hasCollision) break;
            }

            if (hasCollision)
            {
                DialogResult dr = MessageBox.Show(this,
                    "Vùng dữ liệu đích đã có sẵn dữ liệu. Bạn có muốn ghi đè lên các ô này không?",
                    "Cảnh báo ghi đè", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr != DialogResult.Yes) return;
            }

            _calcView.ExecuteTextToColumns(_srcCol, _startRow, _endRow, destC, destR,
                GetActiveDelimiters(), chkConsecutive.Checked, GetTextQualifier(),
                GetBreakPositions(), !radDelimited.Checked);

            if (_parentForm != null) _parentForm.Invalidate();
            this.Close();
        }

        public static List<string> ParseDelimitedLine(string line, List<char> delimiters, bool consecutiveAsOne, char textQualifier)
        {
            List<string> tokens = new List<string>();
            if (string.IsNullOrEmpty(line))
            {
                tokens.Add("");
                return tokens;
            }

            System.Text.StringBuilder current = new System.Text.StringBuilder();
            bool insideQuotes = false;
            bool hasQualifier = (textQualifier != '\0' && textQualifier != ' ');

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (hasQualifier && ch == textQualifier)
                {
                    if (insideQuotes && i + 1 < line.Length && line[i + 1] == textQualifier)
                    {
                        current.Append(ch);
                        i++;
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (!insideQuotes && delimiters.Contains(ch))
                {
                    if (consecutiveAsOne)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                        while (i + 1 < line.Length && delimiters.Contains(line[i + 1]))
                        {
                            i++;
                        }
                    }
                    else
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }
            tokens.Add(current.ToString());
            return tokens;
        }

        public static List<string> ParseFixedWidthLine(string line, List<int> breaks)
        {
            List<string> tokens = new List<string>();
            if (line == null) line = "";
            int lastPos = 0;
            for (int i = 0; i < breaks.Count; i++)
            {
                int p = breaks[i];
                if (p > lastPos)
                {
                    if (lastPos < line.Length)
                    {
                        int len = Math.Min(p - lastPos, line.Length - lastPos);
                        tokens.Add(line.Substring(lastPos, len));
                    }
                    else
                    {
                        tokens.Add("");
                    }
                    lastPos = p;
                }
            }
            if (lastPos < line.Length)
            {
                tokens.Add(line.Substring(lastPos));
            }
            else if (tokens.Count == 0)
            {
                tokens.Add(line);
            }
            return tokens;
        }
    }
}
```

---

## 8. Verification & Test Protocol

### 8.1 Acceptance Assertion Matrix
| Test Case | Precondition | Input / Trigger | Expected Outcome |
|---|---|---|---|
| **TC-FR-01** | Sheet with formulas `=SUM(A1:A5)` evaluating to `100`, static cell `100` | Look in Values: replace `100` with `200` | Static cell becomes `200`. Formula cell `=SUM(A1:A5)` is **not corrupted** and retains formula. |
| **TC-FR-02** | Sheet with `=TAX*10` | Look in Formulas: replace `TAX` with `VAT` | Formula transforms cleanly to `=VAT*10`. Evaluates without syntax exception. |
| **TC-FR-03** | Modeless navigation | Click "Find Next" | Active selection moves, scrolls into viewport, dialog stays open. |
| **TC-RD-01** | Table A1:D10 with headers, 3 duplicate rows across columns A & B | Check columns A & B only, "My data has headers" checked | Deletes exactly 3 rows. Displays "3 giá trị trùng lặp đã được tìm thấy và xóa bỏ; 7 giá trị duy nhất còn lại." Remaining rows shifted cleanly up. |
| **TC-RD-02** | Selection with data to the right (Column F has notes) | Deduplicate table A1:D10 | Column F is completely unaffected. |
| **TC-TC-01** | Column A contains `"Nguyen, Van A", 25, IT` | Comma delimited, text qualifier `"` | Splits into 3 columns: `Nguyen, Van A` (Column 1), `25` (Column 2, numeric), `IT` (Column 3). |
| **TC-TC-02** | Consecutive spaces `A    B    C` | Space delimited, "Coi dấu liên tiếp là một" checked | Splits into 3 columns: `A`, `B`, `C` (no empty intermediate columns). |
| **TC-TC-03** | Destination column B already contains data | Execute Text to Columns to destination $B$1 | Displays overwrite confirmation prompt. If "No", sheet is untouched. |
| **TC-UND-01**| Any data utility execution | Press `Ctrl+Z` | 100% of sheet grid, cells, and selection are restored identically to prior state. |

---

## 9. Conclusion & Delivery Milestones

This architecture provides a complete, robust, C# 5-compliant blueprint for ZenQ Calc's Core Data Utilities:
1. **Find & Replace**: Dedicated floating Form supporting By Rows/Cols, Match Case, Entire Cell, Look in Values/Formulas, safe formula replacement, and interactive Find All.
2. **Remove Duplicates**: Column-selection checklist, header detection, multi-column row equality hashing, and safe row shifting.
3. **Text to Columns**: Real-time live preview `DataGridView`, delimited & fixed-width engines, consecutive delimiter collapsing, and collision warning.
4. **Integration**: Keyboard shortcuts `Ctrl+F` and `Ctrl+H` in `CalcView` and `AppForm`, ribbon menu buttons in `RibbonBar`, and atomic undo snapshot protection.
