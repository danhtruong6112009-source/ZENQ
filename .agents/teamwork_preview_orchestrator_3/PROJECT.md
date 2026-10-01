# Project: GenOffice 2-AI Focused Overhaul (Undo/Redo & Shapes/SmartArt)

## Architecture
- Application: WinForms .NET 4.8 compiled via uild.bat / Microsoft C# 5.0 compiler (csc.exe).
- Architecture Pattern: 2 Dedicated Specialized AI Workers with strict file boundaries.

### Subsystem 1: Per-Tab Undo/Redo & Deep Snapshot Fidelity (Worker AI 1)
- DocTab in src/ui/RibbonBar.cs owns its own UndoStack and RedoStack instances (Stack<WriterUndoSnapshot>).
- On tab switch (SaveTabState / LoadTabState in RibbonBar.cs and AppForm.cs), active history stacks are swapped cleanly.
- WriterUndoSnapshot in src/ui/WriterView.cs is expanded to snapshot:
  - Text & piece table state
  - Formatted runs (FormattedRuns)
  - Cursor position & selection bounds (CursorPosition, SelectionStart, SelectionEnd)
  - Table data & layout (DocumentTables, TablePageX/Y, row heights, col widths, borders, cell backgrounds)
  - Shape list (DocumentShapes, positions, dimensions, rotation, colors, text content)
  - Paragraph formatting (ParagraphAlignments, ParagraphLineSpacings, FirstLineIndent, LeftIndent, RightIndent, spacing)
- Rollback on Ctrl+Z / Ctrl+Y is 100% faithful without caret jump to doc start or broken structures.

### Subsystem 2: MS Office Standard Shapes & SmartArt DOCX Fidelity (Worker AI 2)
- Shape Gallery (src/ui/ShapesGalleryDropDownForm.cs & RibbonBar.cs):
  - Strip non-standard categories (Botanical, Luxury Jewels).
  - Implement 9 standard MS Office categories: Recently Used, Lines, Rectangles, Basic Shapes, Block Arrows, Equation Shapes, Flowchart, Stars and Banners, Callouts.
- Shape Vector Rendering & Editing (src/ui/ShapeRenderer.cs, src/ui/DocShapeObject.cs):
  - Elegant monoline vector drawing with accurate mathematical proportions.
  - In-shape text editing (TextContent, font, color, alignment, word wrap).
  - Rotation, resize, and yellow adjustment handles.
- SmartArt DOCX Import/Export (src/format/FormatHandlers.cs, src/ui/SmartArtRenderer.cs):
  - DOCX Import: parse SmartArt / diagrams, render cycle, linear process, org tree, and block list on Writer page without crashes.
  - DOCX Export: write compliant OpenXML / drawingml vector fallbacks so MS Word opens cleanly with 0 corruption prompts.

## Feature Inventory
| # | Feature | Worker | Source |
|---|---|---|---|
| 1 | Tab History Stack Isolation | Worker AI 1 | ORIGINAL_REQUEST §AI 1.1 |
| 2 | Deep Snapshot Fidelity (Text, Format, Cursor, Selection, Tables, Shapes, Paragraphs) | Worker AI 1 | ORIGINAL_REQUEST §AI 1.2 |
| 3 | 9 Standard MS Office Shape Groups Dropdown | Worker AI 2 | ORIGINAL_REQUEST §AI 2.1 |
| 4 | Monoline Vector Rendering & In-Shape Text Editing | Worker AI 2 | ORIGINAL_REQUEST §AI 2.2 |
| 5 | Rotation, Resize & Adjustment Handles | Worker AI 2 | ORIGINAL_REQUEST §AI 2.2 |
| 6 | SmartArt DOCX Import Fidelity | Worker AI 2 | ORIGINAL_REQUEST §AI 2.3 |
| 7 | SmartArt DOCX Export MS Word Integrity | Worker AI 2 | ORIGINAL_REQUEST §AI 2.3 |
| 8 | Clean Compilation with csc.exe & Non-regression | Both | Acceptance Criteria |

## Code Layout & Write Ownership
- Worker AI 1 owns:
  - src/ui/WriterView.cs (Undo/Redo logic, WriterUndoSnapshot, snapshot capture & restore)
  - src/ui/RibbonBar.cs (DocTab history stacks, SaveTabState/LoadTabState undo/redo hookup)
  - src/ui/AppForm.cs (Tab switching undo/redo hooks if needed)
- Worker AI 2 owns:
  - src/ui/ShapesGalleryDropDownForm.cs
  - src/ui/ShapeRenderer.cs
  - src/ui/DocShapeObject.cs
  - src/ui/SmartArtRenderer.cs
  - src/ui/SmartArtGalleryForm.cs
  - src/ui/SmartArtEditorDialog.cs
  - src/format/FormatHandlers.cs
