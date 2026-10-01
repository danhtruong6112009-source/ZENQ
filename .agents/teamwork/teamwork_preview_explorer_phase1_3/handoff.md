# Phase 1 Explorer 3 Handoff Report: Core Data Utilities Specialist

**Role**: Phase 1 Explorer 3 (Core Data Utilities Specialist)  
**Target Milestone**: Phase 1: Critical Data Integrity & Core Productivity Tools  
**Working Directory**: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_3`  
**Artifacts Produced**:
- `utilities_plan.md`: Authoritative technical specification and C# 5.0 architecture design for `CalcFindReplaceDialog.cs`, `CalcRemoveDuplicatesDialog.cs`, `CalcTextToColumnsDialog.cs`, and their integration with `CalcView.cs`, `RibbonBar.cs`, `AppForm.cs`.

---

## 1. Observation

1. **Find & Replace Current Baseline**:
   - In `src/ui/FindReplaceForm.cs` (lines 7–57), the existing dialog is hardcoded for ZenQ Writer (`Text = "Tìm kiếm & Thay thế - ZenQ Writer"`). It has only two TextBoxes and lacks Match Case, Match Entire Cell, Search By Rows/Columns, Look In Formulas/Values, and Find All results.
   - In `src/ui/CalcView.cs` (lines 5053–5091), `OpenFindReplaceDialog` instantiates the Writer `FindReplaceForm`.
   - In `src/ui/CalcView.cs` (lines 5002–5051), `ReplaceAllInSheet` directly mutates `cell.RawValue` using regex without checking if the cell is a formula or if the user is looking in values, risking silent formula overwriting and corruption.
   - In `src/ui/AppForm.cs` (lines 2997–3035), `case Keys.F:` handles Writer and PDF modes but omits Calc mode. `case Keys.H:` does not exist in `AppForm.cs`.

2. **Remove Duplicates Current Baseline**:
   - In `src/ui/CalcView.cs` (lines 7062–7125), `RemoveDuplicates(Form parentForm)` is an unconfigured stub without a user dialog.
   - It assumes the first row is always a header (`for (uint r = minR + 1; r <= maxR; r++)`), combines all columns unconditionally, and executes `CurrentSheet.ShiftCellsUp(rowsToDelete[i], 1, 1000, 100);` (line 7119), which hardcodes arbitrary bounds (`1000` rows, `100` columns).
   - In `src/ui/RibbonBar.cs` (line 20288), clicking the Remove Duplicates button directly calls `calcView.RemoveDuplicates(parentForm)`.

3. **Text to Columns Current Baseline**:
   - Completely absent from the codebase.
   - In `src/ui/RibbonBar.cs` (lines 11654–11662 and 20285–20291), the "Công cụ Dữ liệu" (Data Tools) group renders and handles only the Remove Duplicates button. No Text to Columns UI button or click handler exists.

4. **Platform & Compiler Constraints**:
   - `build.bat` directly invokes `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` with `/recurse:src\*.cs`.
   - The compiler banner confirms `Microsoft (R) Visual C# Compiler version 4.8.9232.0 for C# 5`.
   - Prohibited syntax: `$"..."`, `?.`, `=>`, local functions, inline `out var`, tuple literals `(a, b)`.

5. **State & Undo Management**:
   - In `src/ui/CalcView.cs` (lines 88–99 and 854–890), `SaveUndoState()` captures a 50-step snapshot of `CurrentSheet.CloneGrid()`, selection coordinates, row heights, and column widths. `Undo()` and `Redo()` restore full grid states.

---

## 2. Logic Chain

1. **Find & Replace Dialog (`CalcFindReplaceDialog.cs`)**:
   - *Premise*: Modeless dialogs in spreadsheet applications must allow users to view matching cells on the active canvas while the dialog remains open.
   - *Design*: Implement `CalcFindReplaceDialog : Form` with `TabControl` ("Tìm kiếm" and "Thay thế"). Wire `Ctrl+F` to open the Find tab and `Ctrl+H` to open the Replace tab.
   - *Formula-Safety Logic*: When `LookIn == Values`, cells with `cell.Type == CellType.Formula` evaluate to dynamic results. Overwriting their `RawValue` or `FormulaStr` destroys the calculation logic. Therefore, when `LookIn == Values`, the replacement engine must strictly protect and skip formula cells. When `LookIn == Formulas`, string replacement applies directly to `cell.FormulaStr` and `cell.RawValue`, followed by `FormulaEngine.EvaluateSheet(CurrentSheet)`.
   - *Navigation*: Implement circular coordinate traversal with modular arithmetic wrapping, supporting both By Rows (`(r, c)` sorted by row then col) and By Columns (`(r, c)` sorted by col then row), for forward (`FindNext`) and backward (`FindPrev`) passes.

2. **Remove Duplicates Dialog (`CalcRemoveDuplicatesDialog.cs`)**:
   - *Premise*: The existing stub executes without user confirmation, hardcodes row shifts, and deletes all columns blindly.
   - *Design*: Implement modal `CalcRemoveDuplicatesDialog : Form`.
   - *Header Detection Logic*: Inspect row `minR` vs `minR + 1`. If row `minR` contains string labels while `minR + 1` contains numbers/dates, check `chkHasHeaders` by default. Provide `clbColumns` with "Chọn tất cả" and "Bỏ chọn tất cả".
   - *Safe Shifting Logic*: Composite key constructed from selected columns separated by unit separator `\x1F`. Duplicate rows identified into `rowsToDelete`. Delete in reverse row order. For sub-range tables, shift cells up within $[C_{\min} \dots C_{\max}]$ and clear the trailing vacated row, preventing corruption of surrounding data. Recalculate formulas and report summary message: `"{0} giá trị trùng lặp đã được tìm thấy và xóa bỏ; {1} giá trị duy nhất còn lại."`.

3. **Text to Columns Wizard (`CalcTextToColumnsDialog.cs`)**:
   - *Premise*: Users need to convert raw delimited (CSV, TSV, log formats) or fixed-width text into spreadsheet columns.
   - *Design*: Modal wizard supporting Delimited (Tab, Semicolon, Comma, Space, Custom, consecutive delimiters, text qualifiers) and Fixed-Width breakpoints.
   - *Interactive Feedback*: Implement live preview `DataGridView` updated in real time via `UpdateLivePreview()` on every control change.
   - *Collision Safety*: Inspect destination column range $[C_{\text{dest}} + 1 \dots C_{\text{dest}} + W - 1]$. If non-empty cells exist, prompt user for overwrite confirmation before writing.
   - *Data Typing*: Parse trimmed tokens: if `double.TryParse` succeeds, write via `SetCellNumber`; else write via `SetCellText`.

4. **Integration**:
   - Adding `CalcFindReplaceDialog.cs`, `CalcRemoveDuplicatesDialog.cs`, and `CalcTextToColumnsDialog.cs` to `src/ui/` automatically incorporates them into `GenOffice.exe` via `build.bat` (`/recurse:src\*.cs`).
   - Wire ribbon click handlers in `RibbonBar.cs` tab 4 (Data) for both Remove Duplicates and Text to Columns.
   - Wire `Ctrl+F` and `Ctrl+H` in `CalcView.HandleKeyDown` and `AppForm.ProcessCmdKey`.

---

## 3. Caveats

1. **Modeless Window Focus in WinForms**: When `CalcFindReplaceDialog` is shown modelessly with `dlg.Show(parentForm)`, clicking inside `CalcView` shifts focus to `AppForm`. The dialog must handle `Owner = parentForm` so it stays visually in front without blocking spreadsheet interaction.
2. **Multi-Sheet Find All**: The primary search scope is the active sheet (`CurrentSheet`). Multi-sheet search across all sheets in `calcView.Sheets` is supported in the architecture, but requires sheet tab switching when navigating between results in `lvResults`.
3. **Merged Cells in Deduplication**: If a row targeted for deletion contains merged cell parents or children, `UnmergeCells` must be invoked on the affected coordinates before shifting to avoid orphaned child merge state.

---

## 4. Conclusion

The Core Data Utilities architecture is fully formulated, documented, and verified against project requirements. It provides:
1. Complete C# 5-compliant source blueprints for `CalcFindReplaceDialog.cs`, `CalcRemoveDuplicatesDialog.cs`, and `CalcTextToColumnsDialog.cs`.
2. Exact integration points and method signatures for `CalcView.cs`, `RibbonBar.cs`, and `AppForm.cs`.
3. Strict formula-safety mechanisms preventing formula corruption during find/replace.
4. Comprehensive test assertions covering all functional paths.

---

## 5. Verification Method

1. **Compilation Verification**:
   Execute `build.bat` from `c:\Users\Danh\Desktop\genofficepy`:
   ```cmd
   build.bat
   ```
   Must succeed with exit code 0 and emit `GenOffice.exe`.

2. **Automated Headless Test Runner Verification**:
   Compile and run a standalone C# 5 test runner referencing `GenOffice.exe`:
   ```cmd
   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /r:GenOffice.exe /r:System.Drawing.dll /r:System.Windows.Forms.dll /out:tests\test_utilities.exe tests\test_utilities.cs
   tests\test_utilities.exe
   ```
   Assert:
   - `FindNextCellEx` returns true on matching text and wraps around.
   - `ReplaceAllInSheetEx` preserves formula cells when `lookInFormulas == false`.
   - `ExecuteRemoveDuplicates` removes redundant rows and updates `uniqueCount` and `dupCount` accurately.
   - `ExecuteTextToColumns` correctly splits comma/space separated text and collapses consecutive delimiters.
   - `SaveUndoState` + `Undo` restores 100% of cell values across all 3 operations.

3. **Invalidation Conditions**:
   - Any compilation failure on `csc.exe` version 4.8.9232 (e.g., C# 6 syntax leak).
   - Any formula cell overwritten by a value replacement when looking in Values.
   - Any out-of-bounds row shifting in Remove Duplicates corrupting columns outside the selected range.
