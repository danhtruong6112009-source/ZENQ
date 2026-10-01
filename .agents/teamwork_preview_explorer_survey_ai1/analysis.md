# Technical Survey & Codebase Investigation Report: MS Word Shortcut & Navigation Fidelity

**Document ID**: `survey-ai1-shortcuts-nav`  
**Date**: 2026-09-17  
**Author**: Explorer AI 1 (Survey & Investigation)  
**Target Audience**: Task AI 1, Orchestrator, Peer Engineers  
**Target Scope**: Task AI 1 ("Complete the MS Word standard shortcut & navigation system (Ctrl+L/E/R/J, Ctrl+B/I/U, Ctrl+Z/Y, Home/End, PageUp/Down) and fix remaining text behavioral issues. Instant updates for caret blinking, selection, and Ribbon UI states.")

---

## 1. Executive Summary

GenOffice is a desktop office productivity suite developed in **C# targeting .NET Framework 4.0/4.5+ Windows Forms (WinForms)**, compiled using `csc.exe` via `build.bat`. The core word processing component is `GenOffice.UI.WriterView`, hosted inside `GenOffice.UI.AppForm` and controlled by `GenOffice.UI.RibbonBar`.

An exhaustive survey of the codebase revealed the exact mechanisms governing keyboard dispatch, document text layout, caret rendering, and Ribbon synchronization. Critical architectural gaps and root causes were uncovered:
1. **`IsInputKey` Modifier Masking**: `AppForm.IsInputKey` matched raw `keyData` against unmodified keys (`switch (keyData)`), dropping `Ctrl+Home`, `Ctrl+End`, `Shift+Home`, `Shift+End`, and entirely omitting `PageUp` and `PageDown`.
2. **Visual Line vs. Paragraph Jump**: `Home` and `End` in `WriterView.HandleKeyDown` navigated to paragraph delimiters (`\n`) rather than visual screen lines (`RenderedLineInfo`), and jumped across markdown table pipe boundaries (`|`), corrupting table in-cell editing.
3. **Missing Viewport Auto-Scroll**: `Ctrl+Home` and `Ctrl+End` adjusted `CursorPosition` without scrolling `ScrollY` / `TargetScrollY`, leaving the cursor off-screen. `EnsureCursorVisible` was completely missing.
4. **Non-Functional PageUp/PageDown Navigation**: `PageUp` and `PageDown` in `AppForm.cs` performed an arbitrary 200px view nudge without moving `CursorPosition` or supporting selection expansion.
5. **Formatting Shortcut State Isolation**: `Ctrl+B`, `Ctrl+I`, and `Ctrl+U` did not record undo snapshots (`SaveUndoState`), did not mark the active document modified (`MarkActiveTabModified`), did not update `Typing*` state when moving cursor, and failed to notify Ribbon UI buttons.
6. **Undo/Redo UI Desynchronization**: `Ctrl+Z` and `Ctrl+Y` restored text and cursor position but left Ribbon buttons in a stale state, did not mark active tab dirty for Writer, and failed to bring the restored cursor into view.
7. **Caret Blink Timer Isolation**: `cursorTimer` was declared as a local variable in the `AppForm` constructor, preventing keyboard events from resetting the blink phase, resulting in erratic caret blinking or immediate disappearance during typing.

All 8 tables (`DocTableObject`, `DocumentTables`), in-cell editing, vector shapes (`DocShapeObject`), and wrap modes are fully isolated and protected by the R4 guardrails.

---

## 2. Project Architecture & Build System

### 2.1 Technology Stack & Compiler Environment
- **Platform**: Windows Forms (.NET Framework 4.0 / 4.8 runtime)
- **Language**: C# 5.0 (compiled using `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`)
- **Key Assemblies**: `System.Windows.Forms.dll`, `System.Drawing.dll`, `System.Xml.dll`, `System.IO.Compression.dll`, `PdfiumViewer.dll`, `PdfSharp.dll`.
- **Compiler Compatibility Note**: The build environment uses C# 5.0. Modern C# 6+ language features (such as `$"..."` string interpolation, `?.` null-conditional operators, or expression-bodied members `=>`) are **unsupported** and will cause compiler errors. String formatting must use `string.Format()` or string concatenation.

### 2.2 Compilation Pipeline
- **Script**: `c:\Users\Danh\Desktop\genofficepy\build.bat`
- **Command Line**:
  ```bat
  C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize /main:GenOffice.Program /win32icon:GenOffice.ico /resource:GENOFFICE.png,app_logo.png /resource:GenOffice.ico,app.ico /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:Microsoft.CSharp.dll /r:PdfiumViewer.dll /r:PdfSharp.dll /out:GenOffice.exe /recurse:src\*.cs
  ```
- **Verification Result**: Current build succeeds cleanly with exit code 0 producing `GenOffice.exe` (~3.38 MB).

---

## 3. Detailed Component Mapping

| Component | File Path | Line Range | Responsibilities |
|---|---|---|---|
| **Form Container & Key Routing** | `src/ui/AppForm.cs` | 290–315, 1729–2960 | Window frame, cursor blink timer, `OnKeyDown`, `OnKeyPress`, shortcut dispatch, `IsInputKey`. |
| **Document View & Editor Engine** | `src/ui/WriterView.cs` | 814–1035, 6100–8400, 13700–14306 | Text layout, `PieceTable`, `FormattedRuns`, `RenderedLines`, `TableCellHits`, `HandleKeyDown`, `UpdateRibbonState`. |
| **Ribbon Toolbar** | `src/ui/RibbonBar.cs` | 170–200, 2460–2585, 11220–11450 | Ribbon state (`IsBold`, `IsItalic`, `IsUnderline`, `Alignment`), button rendering, mouse click handling. |
| **Table Model & Guardrails** | `src/ui/DocTableObject.cs` | 1–80 | `DocTableObject`, `DocTableRow`, `DocTableCell` (in-cell dimensions, borders, background, text). |
| **Shape Model & Guardrails** | `src/ui/DocShapeObject.cs` | 1–261 | `DocShapeObject`, `WrapMode` (Inline, Square, InFront, Behind), picture formatting. |

---

## 4. In-Depth Root Cause Analysis & Behavioral Findings

### 4.1 Input Key Interception Failure (`AppForm.IsInputKey`)
- **Location**: `src/ui/AppForm.cs`, lines 2940–2960.
- **Problem**:
  ```csharp
  protected override bool IsInputKey(Keys keyData)
  {
      if (Ribbon != null && ...)
      {
          switch (keyData)
          {
              case Keys.Up:
              case Keys.Down:
              case Keys.Left:
              case Keys.Right:
              case Keys.Home:
              case Keys.End:
              case Keys.Enter:
              case Keys.Tab:
              case Keys.Escape:
                  return true;
          }
      }
      return base.IsInputKey(keyData);
  }
  ```
- **Analysis**:
  - `keyData` contains modifier flags (e.g., `Keys.Shift | Keys.Home` is `0x10024`, `Keys.Control | Keys.Home` is `0x20024`). When compared with `Keys.Home` (`0x24`), the equality fails.
  - `Keys.PageUp` and `Keys.PageDown` were never included in the switch statement.
  - As a result, Windows Forms default navigation preprocessing intercepted or consumed `PageUp`, `PageDown`, `Ctrl+Home`, `Ctrl+End`, `Shift+Home`, and `Shift+End` before `OnKeyDown` received them.
- **Remedy**: Mask with `(keyData & Keys.KeyCode)` and add `Keys.PageUp` and `Keys.PageDown`.

---

### 4.2 Home and End Navigation Flaws
- **Location**: `src/ui/WriterView.cs`, lines 14062–14115.
- **Current Code**:
  ```csharp
  else if (key == Keys.Home)
  {
      if (isCtrl) { CursorPosition = 0; }
      else
      {
          int pos = CursorPosition - 1;
          while (pos >= 0 && text.Length > pos && text[pos] != '\n') pos--;
          CursorPosition = pos + 1;
      }
      ...
  }
  else if (key == Keys.End)
  {
      if (isCtrl) { CursorPosition = PieceTable.Length; }
      else
      {
          int pos = CursorPosition;
          while (pos < text.Length && text[pos] != '\n') pos++;
          CursorPosition = pos;
      }
      ...
  }
  ```
- **Analysis**:
  - **Issue A (Visual Line vs. Paragraph)**: In Word, `Home` jumps to the start of the current *visual line*, not the start of the *paragraph*. In wrapped multi-line paragraphs, searching for `\n` jumps several lines up to the beginning of the entire paragraph.
  - **Issue B (Table In-Cell Editing Violation)**: Table rows are stored as pipe-delimited text (e.g. `| Cell 1 | Cell 2 | Cell 3 |`). When inside Cell 2, pressing `Home` jumps across the `|` delimiters all the way to the beginning of the table line outside the cell, corrupting in-cell editing.
  - **Issue C (Lack of Viewport Scroll)**: When `Ctrl+Home` sets `CursorPosition = 0` or `Ctrl+End` sets `CursorPosition = PieceTable.Length`, `ScrollY` / `TargetScrollY` is untouched, leaving the caret outside the viewport.
- **Remedy**:
  - Check `TableCellHits` first: if inside a cell hit, clamp to `hit.CharStartIndex` (for Home) or `hit.CharStartIndex + hit.CharLength` (for End).
  - Otherwise, lookup the current line in `RenderedLines`: jump to `rl.StartCharIndex` (Home) or `rl.StartCharIndex + rl.Length` (End, avoiding trailing `\n`).
  - For `Ctrl+Home`, set `TargetScrollY = 0; SmoothScrollY = 0;`.
  - For `Ctrl+End`, set `TargetScrollY = GetMaxScrollY(clientHeight, ribbon); SmoothScrollY = TargetScrollY;`.

---

### 4.3 PageUp and PageDown Behavior
- **Location**: `src/ui/AppForm.cs`, lines 2819–2820; `src/ui/WriterView.cs`, line 13977.
- **Current Code**:
  ```csharp
  if (e.KeyCode == Keys.PageDown) WriterView.ScrollY = Math.Min(WriterView.GetMaxScrollY(ClientSize.Height, Ribbon), WriterView.ScrollY + 200);
  else if (e.KeyCode == Keys.PageUp) WriterView.ScrollY = Math.Max(0, WriterView.ScrollY - 200);
  WriterView.HandleKeyDown(e.KeyCode, Ribbon);
  ```
- **Analysis**:
  - 200 pixels is an arbitrary constant, not one screen page.
  - `WriterView.HandleKeyDown` did not handle `Keys.PageUp` or `Keys.PageDown` at all. The cursor remained in place while the view scrolled, violating the requirement: *"Phím Home, End, Ctrl+Home, Ctrl+End, PageUp, PageDown đưa con trỏ đến đúng vị trí văn bản ngay lập tức."*
  - Selection expansion with `Shift+PageUp` / `Shift+PageDown` was impossible.
- **Remedy**:
  - Implement full `Keys.PageUp` and `Keys.PageDown` handling inside `WriterView.HandleKeyDown`:
    - Viewport height `viewH = Math.Max(150, clientHeight - topOffset - 26)`.
    - Adjust `TargetScrollY` by `viewH`.
    - Find the line in `RenderedLines` matching the target screen Y offset (or first/last line), update `CursorPosition`.
    - Respect `isShift` to update `SelectionEnd`.

---

### 4.4 Formatting Shortcuts (`Ctrl+B`, `Ctrl+I`, `Ctrl+U`) & Undo History
- **Location**: `src/ui/AppForm.cs`, lines 2381–2417; `src/ui/WriterView.cs`, lines 929–970.
- **Current Code**:
  ```csharp
  case Keys.B:
      if (Ribbon.CurrentMode == OfficeAppMode.Writer)
      {
          Ribbon.IsBold = !Ribbon.IsBold;
          WriterView.ApplyBoldToSelectionOrTyping(Ribbon.IsBold);
      }
      Invalidate();
      return;
  ```
- **Analysis**:
  - `SaveUndoState()` was never called before applying formatting. Pressing `Ctrl+Z` afterwards could not undo formatting.
  - `MarkActiveTabModified()` was not called.
  - If text was selected with mixed formatting, toggling based on `Ribbon.IsBold` could produce inverted results instead of standard Word behavior (make all bold first, unbold if all bold).
  - When no text was selected, `TypingBold` was updated, but moving the cursor elsewhere did not synchronize `TypingBold` from the underlying text, and calling `UpdateRibbonState` overwrote `Ribbon.IsBold` with `fmt.Bold`.
- **Remedy**:
  - Encapsulate `ToggleBold(RibbonBar ribbon)`, `ToggleItalic(RibbonBar ribbon)`, and `ToggleUnderline(RibbonBar ribbon)` inside `WriterView`.
  - Check selection: if range selected, call `SaveUndoState()`, apply new uniform format, update `Typing*` and `Ribbon`. If caret only, toggle `Typing*` and `Ribbon`.
  - In `AppForm.cs`, invoke the method, mark tab modified, reset caret blink, and invalidate.

---

### 4.5 Paragraph Alignment Shortcuts (`Ctrl+L`, `Ctrl+E`, `Ctrl+R`, `Ctrl+J`)
- **Location**: `src/ui/AppForm.cs`, lines 1999–2038.
- **Current Code**:
  ```csharp
  case Keys.L:
      if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
      {
          WriterView.ApplyAlignmentToSelectionOrParagraph(0);
          WriterView.UpdateRibbonState(Ribbon);
          MarkActiveTabModified();
          Invalidate();
          return;
      }
      break;
  ```
- **Analysis**:
  - `ApplyAlignmentToSelectionOrParagraph` already calls `SaveUndoState()` and operates on paragraphs correctly.
  - However, `WriterView.CursorVisible = true` was not set, cursor blinking was not reset, and `Ribbon.Alignment` was not explicitly updated before `UpdateRibbonState`.
- **Remedy**:
  - Explicitly set `Ribbon.Alignment = align;`
  - Ensure caret is visible and reset blink timer.
  - Document and verify alignment shortcuts 0 (Left), 1 (Center), 2 (Right), 3 (Justify).

---

### 4.6 Undo/Redo Synchronization (`Ctrl+Z`, `Ctrl+Y`)
- **Location**: `src/ui/AppForm.cs`, lines 2193–2230; `src/ui/WriterView.cs`, lines 1459–1515.
- **Analysis**:
  - In `AppForm.cs`:
    ```csharp
    case Keys.Z:
        if (Ribbon.CurrentMode == OfficeAppMode.Writer) WriterView.Undo();
        Invalidate();
        return;
    ```
  - Missing:
    1. `WriterView.UpdateRibbonState(Ribbon)` — Ribbon buttons stayed in outdated state.
    2. `MarkActiveTabModified()` — Tab state was not updated.
    3. `WriterView.EnsureCursorVisible(ClientSize.Height, Ribbon)` — Restored cursor was not brought into view.
    4. Reset caret blink timer.
- **Remedy**:
  - Call `WriterView.UpdateRibbonState(Ribbon)`, `WriterView.EnsureCursorVisible(...)`, `MarkActiveTabModified()`, and reset blink timer on both `Ctrl+Z` and `Ctrl+Y`.

---

### 4.7 Caret Blinking Responsiveness
- **Location**: `src/ui/AppForm.cs`, lines 302–311.
- **Analysis**:
  - `Timer cursorTimer` was defined locally in the constructor.
  - It could not be reset when user pressed keys or moved cursor.
  - As a result, the timer could toggle `CursorVisible = false` immediately after a keypress, causing caret flicker.
- **Remedy**:
  - Promote `cursorTimer` to a private field `private Timer _cursorTimer;` in `AppForm`.
  - Add `public void ResetCursorBlink()`:
    ```csharp
    public void ResetCursorBlink()
    {
        if (WriterView != null) WriterView.CursorVisible = true;
        if (_cursorTimer != null)
        {
            _cursorTimer.Stop();
            _cursorTimer.Start();
        }
    }
    ```
  - Call `ResetCursorBlink()` in `OnKeyDown`, `OnKeyPress`, mouse clicks, and shortcut invocations.

---

## 5. Strict Guardrails Verification (R4)

### 5.1 Tables & In-Cell Editing (`DocTableObject`, `DocumentTables`)
- **Guardrail Requirement**: Do not alter 8 tables, in-cell editing, row height, column width, borders, or background in `Bài 1.ND GHI.docx`.
- **Architectural Boundary**:
  - Table parsing occurs in `src/format/FormatHandlers.cs` (`LoadDocxFile`).
  - Table rendering occurs in `src/ui/WriterView.cs` (`Draw`, lines 7650–7965).
  - In-cell text boundaries are indexed in `TableCellHits`.
- **Safety Guarantee**:
  - The shortcut and navigation fixes operate on `CursorPosition`, `SelectionStart`/`SelectionEnd`, and `RenderedLines`.
  - The Home/End enhancements explicitly check `TableCellHits` to **protect** table cell text from being escaped by boundary jumps.
  - No changes are made to table geometry, column widths, row heights, or cell painting.

### 5.2 Shapes, Images & Wrap Modes (`DocShapeObject`)
- **Guardrail Requirement**: Do not alter image/shape insertion or wrap modes (Inline, Square, InFront, Behind).
- **Architectural Boundary**:
  - In `WriterView.HandleKeyDown`, line 13979:
    ```csharp
    if (ActiveShape != null || ActiveChart != null) return;
    ```
  - Shape transformation and movement are handled exclusively in `AppForm.cs` lines 2701–2816.
- **Safety Guarantee**:
  - When an image or shape is selected, text key handling is completely bypassed.
  - Text wrapping logic in `LayoutParagraphLines` remains untouched.

---

## 6. Implementation Plan & File Change Blueprint for Task AI 1

### File 1: `src/ui/WriterView.cs`
1. Add `EnsureCursorVisible(int clientHeight, RibbonBar ribbon)`.
2. Update `UpdateRibbonState(RibbonBar ribbon)`: synchronize `Typing*` format when no selection.
3. Add `ToggleBold(RibbonBar ribbon)`, `ToggleItalic(RibbonBar ribbon)`, `ToggleUnderline(RibbonBar ribbon)`.
4. Update `HandleKeyDown(Keys key, RibbonBar ribbon = null, int clientHeight = 800)`:
   - Visual line-aware `Home` and `End` with `TableCellHits` protection.
   - Immediate scroll to top/bottom on `Ctrl+Home` / `Ctrl+End`.
   - Implement `Keys.PageUp` and `Keys.PageDown` with 1 screen page scroll, cursor repositioning, and selection support.
   - Call `EnsureCursorVisible` and `UpdateRibbonState`.

### File 2: `src/ui/AppForm.cs`
1. Promote `cursorTimer` to field `private Timer _cursorTimer;` and add `ResetCursorBlink()`.
2. Fix `IsInputKey`: change to `switch (keyData & Keys.KeyCode)` and include `Keys.PageUp` and `Keys.PageDown`.
3. In `OnKeyDown`:
   - `Ctrl+B`: call `WriterView.ToggleBold(Ribbon)`.
   - `Ctrl+I`: call `WriterView.ToggleItalic(Ribbon)`.
   - `Ctrl+U`: call `WriterView.ToggleUnderline(Ribbon)`.
   - `Ctrl+L/E/R/J`: ensure `Ribbon.Alignment` updated, tab marked modified, caret blink reset.
   - `Ctrl+Z` / `Ctrl+Y`: add `UpdateRibbonState(Ribbon)`, `EnsureCursorVisible`, `MarkActiveTabModified()`, and `ResetCursorBlink()`.
   - Keydown dispatch (lines 2818–2824): replace `ScrollY ± 200` with direct call to `WriterView.HandleKeyDown(e.KeyCode, Ribbon, ClientSize.Height)` and `ResetCursorBlink()`.
4. In `OnKeyPress`: call `ResetCursorBlink()`.
5. In `OnMouseDown`: call `ResetCursorBlink()`.
