# BRIEFING — 2026-09-18T13:59:12Z

## Mission
Overhaul Shape Library (9 standard MS Office/OnlyOffice groups, monoline vector rendering, handles, text in shapes) and implement SmartArt DOCX Import/Export Fidelity without corruption.

## 🔒 My Identity
- Archetype: Worker AI 2
- Roles: implementer, qa, specialist
- Working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai2_shapes
- Original parent: 9c5e2640-7a86-4733-8779-c429de06dba4
- Milestone: Shape Library Overhaul & SmartArt DOCX Specialist

## 🔒 Key Constraints
- EXCLUSIVE WRITE OWNERSHIP:
  - src/ui/ShapesGalleryDropDownForm.cs
  - src/ui/ShapeRenderer.cs
  - src/ui/DocShapeObject.cs
  - src/ui/SmartArtRenderer.cs
  - src/ui/SmartArtGalleryForm.cs
  - src/ui/SmartArtEditorDialog.cs
  - src/format/FormatHandlers.cs
- Do NOT write to files owned by AI 1 (WriterView.cs, RibbonBar.cs, AppForm.cs) unless strictly necessary for minimal interface binding that does not conflict with Undo/Redo.
- Integrity Mandate: Genuine implementation, no hardcoded test shortcuts, no fake outputs.
- Build command must succeed with 0 errors. Word must open exported docx without warnings or unreadable content.

## Current Parent
- Conversation ID: 9c5e2640-7a86-4733-8779-c429de06dba4
- Updated: not yet

## Task Summary
- **What to build**:
  1. ShapesGalleryDropDownForm overhaul: Replace custom non-standard groups (botanical, luxury badges) with 9 standard MS Office/OnlyOffice groups. Crisp vector monoline icons.
  2. ShapeRenderer & DocShapeObject: Monoline vector rendering with smooth anti-aliasing. In-shape text editing & rendering (font, size, color, alignments, word wrap). Interactive handles (rotation, 8 sizing, yellow adjustments for corner radius/arrow neck/callout tail).
  3. SmartArt DOCX Import/Export: Parse SmartArt diagrams (drawingml dgm:relIds or fallbacks) to standard diagrams (Cycle, Linear Process, Hierarchy, Block List). Preserve nodes & text. Export valid OpenXML drawingml or compliant fallback without Word corruption.
- **Success criteria**:
  - Exactly 9 standard groups in dropdown, 0 botanical/luxury.
  - High fidelity vector rendering with handles and text formatting.
  - SmartArt DOCX roundtrip without unreadable content error.
  - Clean build with 0 errors, verification tests pass.
- **Interface contracts**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_orchestrator_3\PROJECT.md
- **Code layout**: src/ui/, src/format/

## Key Decisions Made
- [TBD]

## Artifact Index
- c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai2_shapes\DISPATCH.md — Assignment instructions
- c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai2_shapes\progress.md — Liveness & progress tracking
- c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai2_shapes\handoff.md — Final 5-component handoff report

## Change Tracker
- **Files modified**: None yet
- **Build status**: Untested
- **Pending issues**: None

## Quality Status
- **Build/test result**: Not run yet
- **Lint status**: 0
- **Tests added/modified**: None yet

## Loaded Skills
- None
