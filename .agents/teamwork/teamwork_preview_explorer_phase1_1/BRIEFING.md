# BRIEFING — 2026-09-28T10:51:10Z

## Mission
Formulate exact C# 5.0 implementation strategy for OpenXML Drawings Roundtrip in XlsxHandler.cs and CalcView.cs.

## 🔒 My Identity
- Archetype: teamwork_preview_explorer
- Roles: OpenXML Drawings Specialist
- Working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_1
- Original parent: ddf357e9-0181-4327-b59b-767a2b9b4c82
- Milestone: Phase 1 Deep Architecture & Implementation Planning

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Strict compliance with C# 5.0 (.NET Framework 4.5 / VS2013 / Mono compatible: no string interpolation $"...", no null-conditional ?., no expression-bodied members =>, no pattern matching is ...)
- Output drawings_plan.md and handoff.md in working directory
- Communicate via send_message to parent (ddf357e9-0181-4327-b59b-767a2b9b4c82)

## Current Parent
- Conversation ID: ddf357e9-0181-4327-b59b-767a2b9b4c82
- Updated: 2026-09-28T10:57:30Z

## Investigation State
- **Explored paths**:
  - `src/format/XlsxHandler.cs` (LoadXlsx lines 300-778, SaveXlsx lines 780-1450)
  - `src/ui/CalcView.cs` (CalcSheetState lines 101-129, CalcView lines 131-622, shape rendering lines 1903-1940, shape bounds line 6662)
  - `src/ui/DocShapeObject.cs` (models, geometry, styling, cached image, chart data)
  - `src/ui/ShapeRenderer.cs` (preset shape paths, case mapping, chart dispatch)
  - `src/ui/ChartDataExcelForm.cs` & `ChartRenderer.cs` (DocChartData structure, types, values matrix)
  - `TEST_INFRA.md` & `tests/TestHarness.cs` (compiler validation, 4-tier test runner)
- **Key findings**:
  - `SaveXlsx` serializes drawings to `xl/drawings/drawing1.xml` and `xl/media/`, but `LoadXlsx` completely omits reading drawings, causing 100% data loss upon reload.
  - Coordinate conversion: $1 \text{ px} = 9525 \text{ EMUs}$. In `SaveXlsx`, `colOff = (sh.PageX - accW) * 9525`. Reversing this via `accW + Math.Round(colOff / 9525.0)` yields exact zero-drift mathematical roundtrip.
  - `CalcView.FloatingObjects` alias property mapping directly to `Shapes` satisfies Interface Contract 1 without breaking existing code.
  - OpenXML charts can be fully reconstructed into `DocChartData` from `xl/charts/chart*.xml` by parsing `<c:plotArea>` and transposing `<c:cat>` and `<c:val>` into `Values[catIdx][seriesIdx]`.
  - `SaveXlsx` must call `calcView.SaveCurrentSheetState()` to prevent active sheet state mismatch.
- **Unexplored areas**: None for drawings roundtrip; all necessary mechanisms fully designed and documented.

## Key Decisions Made
- Use `LocalName` and null-safe attribute checks for all DrawingML parsing to guarantee robustness against namespace variations.
- Detach GDI+ bitmaps from zip entry streams using intermediate `MemoryStream` copies to avoid resource locks.
- Support both native OpenXML charts (`<xdr:graphicFrame>` + `chart*.xml`) and ZenQ Calc rasterized chart snapshots (`<xdr:pic>`).
- Implement full production-ready C# 5.0 blueprint in `drawings_plan.md`.

## Artifact Index
- DISPATCH.md — Task assignment
- BRIEFING.md — Working memory
- progress.md — Liveness heartbeat
- drawings_plan.md — Detailed OpenXML Drawings implementation plan (completed)
- handoff.md — 5-component handoff report (completed)
