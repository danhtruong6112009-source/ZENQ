# BRIEFING — 2026-09-28T10:57:30Z

## Mission
Formulate the exact C# 5.0 implementation strategy for Native Calc PDF Export in FormatHandlers.cs, CalcView.cs, and AppForm.cs using PdfSharp (orientation, margins, paper size, gridlines, fonts, styling, 2D pagination, floating objects).

## 🔒 My Identity
- Archetype: teamwork_preview_explorer
- Roles: Native PDF Export Specialist (Phase 1 Explorer 2)
- Working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_2
- Original parent: ddf357e9-0181-4327-b59b-767a2b9b4c82
- Milestone: M1 (Phase 1: Critical Data Integrity & Core Productivity Tools)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement directly in source code.
- Strict C# 5.0 compliance enforced by csc.exe: no string interpolation ($"..."), no expression-bodied members (=>), no null-conditional (?.), no local functions, no pattern matching, no tuple literals, no inline out var.
- All proposals communicated via pdf_plan.md and handoff.md.

## Current Parent
- Conversation ID: ddf357e9-0181-4327-b59b-767a2b9b4c82
- Updated: 2026-09-28T10:57:30Z

## Investigation State
- **Explored paths**:
  - `PdfSharp.dll` API surface via reflection (`PdfDocument`, `PdfPage`, `XGraphics`, `XFont`, `XPen`, `XBrush`, `XRect`, `XImage`, `XColor`, `XStringFormat`, `PageSize`, `PageOrientation`).
  - `src/ui/CalcView.cs`: Page layout setup, `PaperSize`, `IsLandscape`, `MarginPreset`, `ShowGridlines`, `GetColWidth`, `GetRowHeight`, `GetShapeScreenBounds`, `Shapes` / `FloatingObjects`.
  - `src/core/SparseMatrix.cs`: Cell data models (`CellData`, `CellFormat`, `CellBorderSettings`), merged cells, cell display text.
  - `src/ui/AppForm.cs`: Line 4200 (`PerformExportPdf`).
  - `build.bat`: Baseline compilation verification (0 errors).
- **Key findings**:
  - `PdfSharp.Drawing.XGraphics` uses top-left origin $(0, 0)$ with $Y$ downwards and point units, aligning with GDI+.
  - `XImage.FromStream(MemoryStream)` enables high-fidelity rendering of floating vector shapes, embedded pictures, charts, and SmartArt without duplicate vector engines.
  - 2D pagination banding decomposes sheets into column bands and row bands, emitting pages in standard "Down, then Over" sequence with repeating header row support.
- **Unexplored areas**: None. Full specification and implementation strategy formulated.

## Key Decisions Made
- Formulated `CalcPdfExporter` class for `src/format/CalcPdfExporter.cs` (or `FormatHandlers.cs`) with 100% C# 5 compliance.
- Outlined exact 4-line surgical hook in `AppForm.cs` line 4200.
- Outlined delegate method `CalcView.ExportToPdf` in `CalcView.cs`.
- Authored comprehensive `pdf_plan.md`.

## Artifact Index
- `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_2\progress.md` — Liveness heartbeat.
- `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_2\pdf_plan.md` — Authoritative PDF export design and code plan.
- `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_2\handoff.md` — 5-component handoff report.
