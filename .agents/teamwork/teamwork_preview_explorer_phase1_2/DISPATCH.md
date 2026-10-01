# DISPATCH: Phase 1 Explorer 2 (Native PDF Export Specialist)

**Role**: Native PDF Export Specialist
**Type**: teamwork_preview_explorer
**Working Directory**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_2
**Authoritative Request**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md
**Project Index**: c:\Users\Danh\Desktop\genofficepy\PROJECT.md
**Survey References**:
- `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_2\spec_report.md`
- `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_survey_1\survey_report.md`

## Objectives
1. Read `ORIGINAL_REQUEST.md`, `PROJECT.md`, and the survey reports.
2. Investigate the exact implementation strategy for Native Calc PDF Export in `src/format/FormatHandlers.cs`, `src/ui/CalcView.cs`, and `src/ui/AppForm.cs`:
   - Inspect `PdfSharp.dll` APIs available in the project (`PdfDocument`, `PdfPage`, `XGraphics`, `XFont`, `XPen`, `XBrush`, `XRect`).
   - Design `CalcView.ExportToPdf(string filePath)`:
     * Honor orientation (Portrait / Landscape), paper size (A4, Letter, Legal, A3), margins.
     * Gridlines rendering (toggle on/off).
     * Cell styling: fonts, bold, italic, text alignment, foreground color, cell background fills, cell borders (thin, medium, double).
     * 2D multi-page row/column pagination banding (splitting wide sheets across pages with header repetition).
     * Floating objects rendering (shapes, images, charts).
   - Hook into `AppForm.cs` line 4200 (`PerformExportPdf`): when `OfficeAppMode.Calc`, invoke `CalcView.ExportToPdf`.
3. Ensure the implementation strategy complies strictly with **C# 5.0**.
4. Write detailed implementation plan in `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_2\pdf_plan.md` and deliver `handoff.md`.
