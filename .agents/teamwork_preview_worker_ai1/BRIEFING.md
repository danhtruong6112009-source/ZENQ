# BRIEFING — 2026-09-17T07:10:00Z

## Mission
Implement keyboard navigation, Word-standard shortcuts, cursor blink improvements, and radial launcher hooks in `AppForm.cs` and `WriterView.cs` with C# 5.0 compatibility and R4 guardrail preservation.

## 🔒 My Identity
- Archetype: implementer
- Roles: implementer, qa, specialist
- Working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai1
- Original parent: 7725578a-eac1-45ac-9011-435ff59e4c5b
- Milestone: Milestone 2 Keyboard Navigation & Shortcuts

## 🔒 Key Constraints
- Exclusively own and edit: `src/ui/AppForm.cs` and `src/ui/WriterView.cs`.
- DO NOT MODIFY: `src/ui/RibbonBar.cs` (Worker AI 2), `src/ui/NewTabRadialLauncher.cs` (Worker AI 3), `src/ui/DocTableObject.cs`, `src/ui/DocShapeObject.cs`, `src/ui/FormatHandlers.cs` (R4 guardrails).
- C# 5.0 strictly enforced via csc.exe in build.bat. DO NOT use C# 6+ syntax (no `$""`, no `?.`, no expression-bodied members `=>`).
- Preserve R4 features: table editing (8 tables in `Bài 1.ND GHI.docx`), in-cell editing, shapes, text wrap.

## Current Parent
- Conversation ID: 7725578a-eac1-45ac-9011-435ff59e4c5b
- Updated: not yet

## Task Summary
- **What to build**:
  1. Fix `IsInputKey(Keys keyData)` in `AppForm.cs` with `Keys.KeyCode` masking, PageUp/PageDown support.
  2. Caret Blink Timer promotion & `ResetCursorBlink()` in `AppForm.cs` called on navigation/shortcuts.
  3. MS Word shortcuts: Ctrl+L/E/R/J, Ctrl+B/I/U, Ctrl+Z/Y with undo recording, Ribbon sync, cursor blink reset.
  4. Rapid Navigation: Home/End (visual line start/end, table cell bounds), Shift+Home/End, Ctrl+Home/End, PageUp/PageDown (and Shift variants).
  5. NewTabRadialLauncher integration hooks in `AppForm.cs` (Paint, MouseDown, MouseMove, KeyDown, Animation Timer).
- **Success criteria**:
  - `build.bat` passes with 0 errors.
  - Automated tests pass.
  - No regression in table editing, shapes, wrap text.
- **Interface contracts**: `PROJECT.md` § Interface Contracts
- **Code layout**: `PROJECT.md`

## Key Decisions Made
- [TBD]

## Change Tracker
- **Files modified**: None yet
- **Build status**: Untested
- **Pending issues**: None

## Quality Status
- **Build/test result**: Untested
- **Lint status**: Clean
- **Tests added/modified**: Pending

## Artifact Index
- `.agents/teamwork_preview_worker_ai1/DISPATCH.md` — Assignment details
- `.agents/teamwork_preview_worker_ai1/BRIEFING.md` — Agent briefing & situational awareness
- `.agents/teamwork_preview_worker_ai1/progress.md` — Liveness & progress tracker
