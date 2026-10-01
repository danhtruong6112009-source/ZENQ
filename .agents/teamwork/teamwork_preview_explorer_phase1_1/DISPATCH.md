# DISPATCH: Phase 1 Explorer 1 (OpenXML Drawings Specialist)

**Role**: OpenXML Drawings Specialist
**Type**: teamwork_preview_explorer
**Working Directory**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_1
**Authoritative Request**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md
**Project Index**: c:\Users\Danh\Desktop\genofficepy\PROJECT.md
**Survey References**:
- `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_2\spec_report.md`
- `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_survey_1\survey_report.md`

## Objectives
1. Read `ORIGINAL_REQUEST.md`, `PROJECT.md`, and the survey reports.
2. Investigate the exact implementation strategy for OpenXML Drawings Roundtrip in `src/format/XlsxHandler.cs` and `src/ui/CalcView.cs`:
   - Exact methods to add/modify in `LoadXlsx` to parse `xl/worksheets/_rels/sheet1.xml.rels`, `xl/drawings/drawing1.xml`, `xl/drawings/_rels/drawing1.xml.rels`, and `xl/media/`.
   - Parsing of `xdr:twoCellAnchor` and `xdr:oneCellAnchor` tags, `xdr:from`, `xdr:to`, `xdr:pic`, `xdr:sp`, `xdr:graphicFrame` (charts).
   - Relationship ID mapping (`r:embed`) to media parts.
   - Coordinate conversion: EMUs (English Metric Units) to pixels/cell bounds (`1 px = 9,525 EMUs`).
   - Populating `calcView.FloatingObjects` (and `Shapes`) with `DocShapeObject`, images, and charts.
   - Preserving drawing assets on subsequent save (`SaveXlsx`).
3. Ensure the implementation strategy complies strictly with **C# 5.0** (no `$"..."`, no `?.`, no `=>`).
4. Write detailed implementation plan in `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_1\drawings_plan.md` and deliver `handoff.md`.
