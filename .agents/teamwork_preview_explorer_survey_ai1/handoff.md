# Handoff Report: MS Word Standard Shortcuts, Navigation System & Text Behavioral Fixes

**Handoff Type**: Hard (Investigation Complete)  
**Agent**: Explorer AI 1 (Survey & Investigation)  
**Working Directory**: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_explorer_survey_ai1`  
**Recipient**: Task AI 1 / Orchestrator  
**Mission**: Map the codebase and provide a self-contained, actionable blueprint for Task AI 1 to complete MS Word standard shortcuts (`Ctrl+L/E/R/J`, `Ctrl+B/I/U`, `Ctrl+Z/Y`, `Home/End`, `PageUp/Down`), instant caret blinking/selection/Ribbon state synchronization, and text behavioral fixes while strictly preserving core Writer features (8 tables, in-cell editing, shapes, images, wrap modes).

---

## 1. Observation

Direct observations from tool executions and codebase inspection:

### 1.1 Project Structure & Build Pipeline
- **Command & Output**: Executed `cmd.exe /c build.bat` in `c:\Users\Danh\Desktop\genofficepy`.
  ```
  Microsoft (R) Visual C# Compiler version 4.8.9232.0 for C# 5
  [SUCCESS] GenOffice.exe built successfully!
  09/17/2026 01:37 PM 3,380,736 GenOffice.exe
  ```
- **Observation**:
  - The project is written in **C# targeting .NET Framework 4.0/4.8 WinForms**.
  - Compiler is **C# 5.0** (`csc.exe` in .NET 4.0 directory). C# 6+ syntax (such as `$"..."`, `?.`, or expression-bodied properties) **must not be used**.
  - All C# source code files reside under `src/` (140 `.cs` files).

### 1.2 Input Key Interception (`AppForm.cs:2940–2960`)
- **Verbatim Code**:
  ```csharp
  2940: protected override bool IsInputKey(Keys keyData)
  2941: {
  2942:     // Allow Up/Down/Left/Right/Home/End/Enter/Escape/Tab to be processed by OnKeyDown in Writer, Slides, Calc, and Pdf mode
  2943:     if (Ribbon != null && (Ribbon.CurrentMode == OfficeAppMode.Writer || ...))
  2944:     {
  2945:         switch (keyData)
  2946:         {
  2947:             case Keys.Up:
  2948:             case Keys.Down:
  2949:             case Keys.Left:
  2950:             case Keys.Right:
  2951:             case Keys.Home:
  2952:             case Keys.End:
  2953:             case Keys.Enter:
  2954:             case Keys.Tab:
  2955:             case Keys.Escape:
  2956:                 return true;
  2957:         }
  2958:     }
  2959:     return base.IsInputKey(keyData);
  2960: }
  ```
- **Observation**:
  - `switch (keyData)` tests the full enum containing modifiers. For example, `Ctrl+Home` has value `131108` (`Keys.Control | Keys.Home`), which does not match `Keys.Home` (`36`).
  - `Keys.PageUp` (`33`) and `Keys.PageDown` (`34`) are entirely missing from the switch statement.

### 1.3 Home & End Logic in WriterView (`WriterView.cs:14062–14115`)
- **Verbatim Code**:
  ```csharp
  14062: else if (key == Keys.Home)
  14063: {
  14064:     if (isCtrl)
  14065:     {
  14066:         // Về đầu tài liệu (Ctrl + Home)
  14067:         CursorPosition = 0;
  14068:     }
  14069:     else
  14070:     {
  14071:         // Về đầu dòng (Home)
  14072:         int pos = CursorPosition - 1;
  14073:         while (pos >= 0 && text.Length > pos && text[pos] != '\n') pos--;
  14074:         CursorPosition = pos + 1;
  14075:     }
  ...
  14089: else if (key == Keys.End)
  14090: {
  14091:     if (isCtrl)
  14092:     {
  14093:         // Về cuối tài liệu (Ctrl + End)
  14094:         CursorPosition = PieceTable.Length;
  14095:     }
  14096:     else
  14097:     {
  14098:         // Về cuối dòng (End)
  14099:         int pos = CursorPosition;
  14100:         while (pos < text.Length && text[pos] != '\n') pos++;
  14101:         CursorPosition = pos;
  14102:     }
  ```
- **Observation**:
  - `Home` searches for `\n`, jumping to the start of the entire paragraph, not the visual wrapped line.
  - Inside table cells, it jumps past the `|` delimiters to the markdown line beginning.
  - `Ctrl+Home` and `Ctrl+End` do not modify `ScrollY` or `TargetScrollY`.
  - `End` searches for `\n`, jumping to the end of the entire paragraph, not the visual wrapped line.

### 1.4 PageDown & PageUp Handling (`AppForm.cs:2819–2823` & `WriterView.cs:13977`)
- **Verbatim Code**:
  ```csharp
  2819: if (e.KeyCode == Keys.PageDown) WriterView.ScrollY = Math.Min(WriterView.GetMaxScrollY(ClientSize.Height, Ribbon), WriterView.ScrollY + 200);
  2820: else if (e.KeyCode == Keys.PageUp) WriterView.ScrollY = Math.Max(0, WriterView.ScrollY - 200);
  2821: WriterView.HandleKeyDown(e.KeyCode, Ribbon);
  2822: // Scroll to keep cursor visible
  ```
- **Observation**:
  - Fixed 200px offset is used instead of 1 screen page height.
  - `WriterView.HandleKeyDown` has no branch for `Keys.PageUp` or `Keys.PageDown`. `CursorPosition` is never updated.
  - Selection expansion with `Shift+PageUp`/`Shift+PageDown` does not work.
  - Line 2822 is an empty comment: `// Scroll to keep cursor visible`.

### 1.5 Formatting Shortcuts (`AppForm.cs:2381–2417` & `WriterView.cs:929–970`)
- **Verbatim Code**:
  ```csharp
  2381: case Keys.B:
  2382:     if (Ribbon.CurrentMode == OfficeAppMode.Writer)
  2383:     {
  2384:         Ribbon.IsBold = !Ribbon.IsBold;
  2385:         WriterView.ApplyBoldToSelectionOrTyping(Ribbon.IsBold);
  2386:     }
  ...
  2393: case Keys.I:
  ...
  2405: case Keys.U:
  ```
- **Observation**:
  - `SaveUndoState()` is not called. `Ctrl+Z` cannot undo formatting.
  - `MarkActiveTabModified()` is not called.
  - `UpdateRibbonState` is not called to synchronize with mixed selections.
  - Cursor visibility and blink timer are not refreshed.

### 1.6 Undo/Redo Shortcuts (`AppForm.cs:2193–2230`)
- **Verbatim Code**:
  ```csharp
  2193: case Keys.Z:
  2194:     if (Ribbon.CurrentMode == OfficeAppMode.Writer) WriterView.Undo();
  ...
  2210:     Invalidate();
  2211:     return;
  2212: case Keys.Y:
  2213:     if (Ribbon.CurrentMode == OfficeAppMode.Writer) WriterView.Redo();
  ...
  2229:     Invalidate();
  2230:     return;
  ```
- **Observation**:
  - `WriterView.UpdateRibbonState(Ribbon)` is not invoked; Ribbon UI buttons remain in their pre-undo state.
  - `MarkActiveTabModified()` is called for Slides/Calc/Pdf, but skipped for Writer.
  - Restored cursor position is not scrolled into view.

### 1.7 Caret Blink Timer (`AppForm.cs:302–311`)
- **Verbatim Code**:
  ```csharp
  302: Timer cursorTimer = new Timer();
  303: cursorTimer.Interval = 530;
  304: cursorTimer.Tick += (s, e) =>
  305: {
  306:     WriterView.CursorVisible = !WriterView.CursorVisible;
  307:     if (PdfView != null) PdfView.ShowCursor = !PdfView.ShowCursor;
  308:     if ((Ribbon.CurrentMode == OfficeAppMode.Writer || Ribbon.CurrentMode == OfficeAppMode.Pdf) && !SlidesView.IsPresenting)
  309:         Invalidate();
  310: };
  311: cursorTimer.Start();
  ```
- **Observation**:
  - `cursorTimer` is a local variable in the constructor and cannot be accessed or restarted by event handlers.
  - Key presses and shortcuts do not restart the timer.

### 1.8 Guardrails (R4) & Verification File
- **Command & Output**: Executed `powershell -Command "python scratch/inspect_tables.py"`
  ```
  Total tables found: 8
  === TABLE 0 === ... Rows count: 1
  === TABLE 1 === ... Rows count: 1
  === TABLE 2 === ... Rows count: 1
  === TABLE 3 === ... Rows count: 1
  === TABLE 4 === ... Rows count: 8
  === TABLE 5 === ... Rows count: 1
  === TABLE 6 === ... Rows count: 1
  === TABLE 7 === ... Rows count: 1
  ```
- **Observation**:
  - Sample file exists at `C:\Users\Danh\Downloads\Bài 1.ND GHI.docx`.
  - It contains exactly 8 tables (Tables 0 through 7).
  - In `WriterView.HandleKeyDown`, line 13979:
    `if (ActiveShape != null || ActiveChart != null) return;`
    ensures shape/image editing is isolated from text key processing.

---

## 2. Logic Chain

1. **Premise**: Standard MS Word behavior requires that:
   - `Home`/`End` navigate to the visual start and end of the current line on screen, and within table cells stay bounded to cell text.
   - `Ctrl+Home`/`Ctrl+End` jump to document boundaries and scroll the viewport to show the destination immediately.
   - `PageUp`/`PageDown` scroll exactly one screen viewport and reposition the caret to the new visible area.
   - `Ctrl+B/I/U` toggle formatting, record undo history, mark document dirty, and update Ribbon buttons instantly.
   - `Ctrl+Z/Y` restore document state, update Ribbon buttons, scroll restored caret into view, and mark document dirty.
   - Caret blinks smoothly and remains solidly visible during typing/shortcuts without flickering off.

2. **Step 1 — Input Routing**:
   - Observation 1.2 shows that `IsInputKey(Keys keyData)` failed to match modified keys because of missing bitmasking (`keyData & Keys.KeyCode`) and omitted `PageUp`/`PageDown`.
   - Fixing this allows WinForms to pass all navigation keys directly to `OnKeyDown`.

3. **Step 2 — Visual Line & Table-Cell Navigation**:
   - Observation 1.3 shows that `Home`/`End` jumped across `\n` and table cell `|` markers.
   - Using `TableCellHits` when inside a table cell bounds `Home`/`End` to `hit.CharStartIndex` and `hit.CharStartIndex + hit.CharLength`.
   - Outside tables, using `RenderedLines` bounds `Home`/`End` to `rl.StartCharIndex` and `rl.StartCharIndex + rl.Length`.
   - This delivers true MS Word line navigation and protects table in-cell editing.

4. **Step 3 — Viewport Synchronization & PageUp/PageDown**:
   - Observation 1.4 shows `ScrollY ± 200` without cursor repositioning.
   - Calculating `viewH = Math.Max(150, clientHeight - topOffset - 26)` and scrolling by `viewH` while finding the corresponding line in `RenderedLines` aligns `CursorPosition` and `ScrollY`.
   - Implementing `EnsureCursorVisible(int clientHeight, RibbonBar ribbon)` guarantees that any movement (Home, End, PageUp, PageDown, typing) keeps the caret inside the visible viewport.

5. **Step 4 — Formatting, Undo History & Ribbon Synchronization**:
   - Observation 1.5 shows `ApplyBoldToSelectionOrTyping` lacked undo snapshotting and tab modification marking.
   - Observation 1.6 shows `Undo`/`Redo` did not call `UpdateRibbonState(Ribbon)`.
   - Creating helper toggle methods in `WriterView` (`ToggleBold`, `ToggleItalic`, `ToggleUnderline`) that call `SaveUndoState()`, apply formatting, and sync `Typing*` and `Ribbon` properties resolves formatting history.
   - Calling `UpdateRibbonState(Ribbon)` and `MarkActiveTabModified()` on Undo, Redo, Alignment (`Ctrl+L/E/R/J`), and Formatting (`Ctrl+B/I/U`) gives instant Ribbon button visual feedback.

6. **Step 5 — Caret Blink Stabilization**:
   - Observation 1.7 shows `cursorTimer` could not be restarted.
   - Promoting `cursorTimer` to `_cursorTimer` on `AppForm` and adding `ResetCursorBlink()` (which sets `CursorVisible = true` and restarts the 530ms timer) ensures the caret never flickers off while typing or navigating.

---

## 3. Caveats

1. **Non-Writer Modes**: `AppForm.OnKeyDown` handles Calc, Slides, and PDF modes. Changes must be restricted strictly to `Ribbon.CurrentMode == OfficeAppMode.Writer` branches to avoid regressing other applications.
2. **C# 5 Language Constraint**: The compiler is Microsoft C# 5.0. Do not use C# 6+ features (`$""`, `?.`, expression bodies `=>` for methods/properties).
3. **Guardrails**: Do not modify table loading in `FormatHandlers.cs` or table drawing in `WriterView.cs`. The sample file `C:\Users\Danh\Downloads\Bài 1.ND GHI.docx` must load all 8 tables properly.
4. **No caveats** outside the documented scope.

---

## 4. Conclusion

The investigation is complete. The architectural defects and root causes are pinpointed to:
- `src/ui/AppForm.cs`: `IsInputKey`, `cursorTimer` encapsulation, shortcut handlers (`Ctrl+B/I/U`, `Ctrl+L/E/R/J`, `Ctrl+Z/Y`), and `WriterView.HandleKeyDown` delegation.
- `src/ui/WriterView.cs`: `HandleKeyDown` (Home, End, PageUp, PageDown), `EnsureCursorVisible`, `UpdateRibbonState`, and `ToggleBold/Italic/Underline`.

Implementing these targeted additions satisfies all R1 requirements without touching table, shape, or wrap mode subsystems (R4).

---

## 5. Verification Method

### 5.1 Build Verification Command
Run from project root:
```bat
cmd.exe /c build.bat
```
Must output `[SUCCESS] GenOffice.exe built successfully!` with return code 0.

### 5.2 Functional Keystroke Verification Matrix
Launch `GenOffice.exe` and verify on Writer:

| Keystroke | Expected Result |
|---|---|
| `Ctrl+L` | Paragraph aligns Left, Ribbon Left Align button turns blue. |
| `Ctrl+E` | Paragraph aligns Center, Ribbon Center Align button turns blue. |
| `Ctrl+R` | Paragraph aligns Right, Ribbon Right Align button turns blue. |
| `Ctrl+J` | Paragraph aligns Justify, Ribbon Justify button turns blue. |
| `Ctrl+B` | Toggles bold on selection/caret; Ribbon Bold button updates immediately; `Ctrl+Z` undos. |
| `Ctrl+I` | Toggles italic on selection/caret; Ribbon Italic button updates immediately; `Ctrl+Z` undos. |
| `Ctrl+U` | Toggles underline on selection/caret; Ribbon Underline button updates immediately; `Ctrl+Z` undos. |
| `Ctrl+Z` / `Ctrl+Y` | Undoes / redoes text and formatting; Ribbon buttons reflect restored format immediately. |
| `Home` / `End` | Moves cursor to start / end of current visual line. Inside table cells, stays bounded within cell text. |
| `Shift+Home` / `Shift+End` | Selects text from caret to visual line start / end. |
| `Ctrl+Home` / `Ctrl+End` | Moves cursor to doc start / end; view scrolls immediately to top / bottom page. |
| `PageUp` / `PageDown` | View scrolls 1 screen page; cursor moves immediately to corresponding visible line. |
| `Shift+PageUp` / `Shift+PageDown` | Extends selection by 1 screen page. |

### 5.3 Non-Regression Guardrail Verification
1. Run Python table check:
   ```powershell
   python scratch/inspect_tables.py
   ```
   Confirms 8 tables in `C:\Users\Danh\Downloads\Bài 1.ND GHI.docx`.
2. Open `Bài 1.ND GHI.docx` in `GenOffice.exe`:
   - Verify all 8 tables are rendered.
   - Click inside Table 4 and type characters to confirm in-cell editing works.
   - Press `Home` and `End` inside a table cell to verify cursor stays within the cell.
