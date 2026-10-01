## 2026-09-17T07:10:00Z
You are Worker AI 2 (teamwork_preview_worker).
Your working directory is: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai2
You report to the Project Orchestrator (Conversation ID: 7725578a-eac1-45ac-9011-435ff59e4c5b).

MANDATORY FIRST ACTIONS:
1. Initialize BRIEFING.md and progress.md in your working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai2
2. Read the authoritative user request: c:\Users\Danh\Desktop\genofficepy\ORIGINAL_REQUEST.md
3. Read the architecture specification: c:\Users\Danh\Desktop\genofficepy\PROJECT.md
4. Read the detailed blueprint in Explorer AI 2's handoff: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_explorer_survey_ai2\handoff.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

EXCLUSIVE WRITE BOUNDARIES:
- You exclusively own:
  - `src/ui/RibbonBar.cs`
- DO NOT MODIFY:
  - `src/ui/AppForm.cs`, `src/ui/WriterView.cs` (owned by Worker AI 1)
  - `src/ui/NewTabRadialLauncher.cs` (owned by Worker AI 3)
  - `src/ui/DocTableObject.cs`, `src/ui/DocShapeObject.cs`, `src/ui/FormatHandlers.cs` (PROTECTED R4 Guardrails)

COMPILER ENVIRONMENT:
- C# 5.0 strictly enforced via csc.exe in build.bat. DO NOT use C# 6+ syntax (no `$"..."`, no `?.`, no expression-bodied members `=>`).

DETAILED TASKS TO IMPLEMENT:
1. Eliminate 1-pixel seam gap between Active Tab Bottom and Ribbon Top (`src/ui/RibbonBar.cs`):
   - Implement `CreateActiveTabBorderPath(RectangleF rect, float radius)`:
     Create an open path (rounded top-left, top horizontal line, rounded top-right, right vertical line down to rect.Bottom, and left vertical line down to rect.Bottom) WITHOUT closing the bottom horizontal line! Stroking this open path prevents drawing a bottom border stroke at Y = 36.
   - Clip rectangle expansion:
     In `docClipRect`, expand height so it extends to `ribbonTop + 2` (`Rectangle docClipRect = new Rectangle(docAreaX, 0, docAreaW, ribbonTop + 2);`), allowing the active tab body and seamless bridge to paint across the junction without being clipped at Y = 36.
   - Seamless bridge fill:
     Fill a 2-pixel bridge rectangle `[activeTab.X + 1, ribbonTop - 1, activeTab.Width - 2, 2]` using Ribbon Header color `Color.FromArgb(248, 250, 252)` (matching active tab porcelain fill) across the seam junction. This unifies active tab and Ribbon surface into a single seamless continuous body.
   - Coordinate calculation and multi-DPI matching:
     Standardize coordinate calculations across 100%, 125%, 150% DPI scales and Normal/Maximized window states:
     `int tabBottom = ribbonTop = (int)Math.Round(36f * dpiScale);`
     `int tabHeight = tabBottom - tabY;`
     Ensuring exact pixel matching at any DPI scale with no integer drift.
   - Apply identically to both Pinned Home Tab and Document Tabs when active.
2. Wire '+' New Tab Button to `NewTabRadialLauncher`:
   - In `RibbonBar.OnMouseDown`, when clicking on the '+' button (`DocTabPlusBounds.Contains(e.Location)`):
     Toggle the radial launcher:
     `Point plusCenter = new Point(DocTabPlusBounds.X + DocTabPlusBounds.Width / 2, DocTabPlusBounds.Y + DocTabPlusBounds.Height / 2);`
     `NewTabRadialLauncher.Toggle(plusCenter);`
     `Parent?.Invalidate();`
     (Make sure to handle or trigger repaint on parent form).
3. R4 Guardrail Preservation:
   - Ensure core Writer features are completely unaffected.
4. Verification:
   - Run `cmd /c build.bat` and ensure `GenOffice.exe` compiles with 0 errors.
   - Verify pixel colors at Y = 35, 36, 37 across scale factors (1.0, 1.25, 1.50) using `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_explorer_survey_ai2\test_seam.ps1` or a dedicated test script. Ensure Y = 36 is seamless (`R >= 248, G >= 250, B >= 252`).
   - Write a comprehensive `handoff.md` in your working directory and notify the orchestrator with `send_message`.
