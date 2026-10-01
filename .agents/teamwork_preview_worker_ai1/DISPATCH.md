## 2026-09-17T07:09:57Z
You are Worker AI 1 (teamwork_preview_worker).
Your working directory is: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai1
You report to the Project Orchestrator (Conversation ID: 7725578a-eac1-45ac-9011-435ff59e4c5b).

MANDATORY FIRST ACTIONS:
1. Initialize BRIEFING.md and progress.md in your working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai1
2. Read the authoritative user request: c:\Users\Danh\Desktop\genofficepy\ORIGINAL_REQUEST.md
3. Read the architecture specification: c:\Users\Danh\Desktop\genofficepy\PROJECT.md
4. Read the detailed blueprint in Explorer AI 1's handoff: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_explorer_survey_ai1\handoff.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

EXCLUSIVE WRITE BOUNDARIES:
- You exclusively own:
  - `src/ui/AppForm.cs`
  - `src/ui/WriterView.cs`
- DO NOT MODIFY:
  - `src/ui/RibbonBar.cs` (owned by Worker AI 2)
  - `src/ui/NewTabRadialLauncher.cs` (owned by Worker AI 3)
  - `src/ui/DocTableObject.cs`, `src/ui/DocShapeObject.cs`, `src/ui/FormatHandlers.cs` (PROTECTED R4 Guardrails)

COMPILER ENVIRONMENT:
- C# 5.0 strictly enforced via csc.exe in build.bat. DO NOT use C# 6+ syntax (no `$"..."`, no `?.`, no expression-bodied members `=>`).

DETAILED TASKS TO IMPLEMENT:
1. Fix `IsInputKey(Keys keyData)` in `src/ui/AppForm.cs`:
   - Mask with `Keys.KeyCode` (`(keyData & Keys.KeyCode)`) so modifier combinations like `Ctrl+Home`, `Ctrl+End`, `Shift+Home`, `Shift+End`, `Shift+PageUp`, `Shift+PageDown` are captured and routed to `OnKeyDown`.
   - Add `Keys.PageUp` and `Keys.PageDown` to the allowed input keys in Writer mode.
2. Caret Blink Timer & Smooth Visibility (`AppForm.cs`):
   - Promote `cursorTimer` to field `private Timer _cursorTimer` and expose `public void ResetCursorBlink()` (which sets `WriterView.CursorVisible = true; _cursorTimer.Stop(); _cursorTimer.Start(); Invalidate();`).
   - Call `ResetCursorBlink()` on any typing, navigation key, or formatting shortcut so the caret remains solidly visible and never flickers off while interacting.
3. MS Word Shortcuts & Ribbon State Sync (`AppForm.cs` & `WriterView.cs`):
   - Alignment shortcuts in Writer mode: `Ctrl+L` (Left), `Ctrl+E` (Center), `Ctrl+R` (Right), `Ctrl+J` (Justify). Call `WriterView.SetParagraphAlignment(...)`, record undo state with `SaveUndoState()`, sync Ribbon alignment buttons, mark active tab modified, reset cursor blink.
   - Formatting shortcuts in Writer mode: `Ctrl+B` (Bold), `Ctrl+I` (Italic), `Ctrl+U` (Underline). Toggle formatting on selection or typing style, record undo state (`SaveUndoState()`), sync Ribbon buttons, mark active tab modified, reset cursor blink.
   - Undo/Redo: `Ctrl+Z` (Undo) and `Ctrl+Y` (Redo). Call `WriterView.Undo()` / `WriterView.Redo()`, call `WriterView.UpdateRibbonState(Ribbon)` to sync Ribbon UI, mark active tab modified, scroll restored cursor into view, and reset cursor blink.
4. Rapid Navigation (`WriterView.cs` & `AppForm.cs`):
   - `Home`: Navigate to the visual start of current line on screen. If inside a table cell, stay bounded within the cell text (do not jump past `|` delimiters).
   - `End`: Navigate to the visual end of current line on screen. If inside a table cell, stay bounded within the cell text (do not jump past `|` delimiters).
   - `Shift+Home` / `Shift+End`: Expand selection to line start/end.
   - `Ctrl+Home`: Move cursor to document beginning (index 0), reset scroll to top (`ScrollY = 0`, `TargetScrollY = 0`), reset blink.
   - `Ctrl+End`: Move cursor to document end (`PieceTable.Length`), scroll to bottom (`ScrollY = GetMaxScrollY(...)`), reset blink.
   - `PageUp` / `PageDown`: Scroll viewport by 1 page (viewport height), update cursor position to maintain visible context, support `Shift+PageUp`/`Shift+PageDown` selection, scroll to keep cursor visible.
5. NewTabRadialLauncher Integration Hooks (`AppForm.cs`):
   Per PROJECT.md § Interface Contracts:
   - In `AppForm.OnPaint` (at end of paint):
     `if (NewTabRadialLauncher.IsOpen) NewTabRadialLauncher.Draw(e.Graphics, ClientSize.Width, ClientSize.Height);`
   - In `AppForm.OnMouseDown`:
     `if (NewTabRadialLauncher.IsOpen && NewTabRadialLauncher.HandleMouseDown(e.Location, Ribbon, (mode) => { SwitchAppMode(mode); })) { Invalidate(); return; }`
   - In `AppForm.OnMouseMove`:
     `if (NewTabRadialLauncher.IsOpen && NewTabRadialLauncher.HandleMouseMove(e.Location)) { Invalidate(); return; }`
   - In `AppForm.OnKeyDown`:
     `if (NewTabRadialLauncher.IsOpen && NewTabRadialLauncher.HandleKeyDown(e.KeyCode)) { Invalidate(); return; }`
   - In `animTimer.Tick` (or animation timer in AppForm):
     `if (NewTabRadialLauncher.IsOpen) { NewTabRadialLauncher.UpdateAnimation(); Invalidate(); }`
6. R4 Guardrail Preservation:
   - Ensure table editing (8 tables in `Bài 1.ND GHI.docx`), in-cell editing, shapes, and wrap text are 100% preserved and undamaged.
7. Verification:
   - Run `cmd /c build.bat` and ensure `GenOffice.exe` compiles with 0 errors.
   - Write automated tests or verification scripts to test all shortcuts and navigation behaviors.
   - Write a comprehensive `handoff.md` in your working directory and notify the orchestrator with `send_message`.
