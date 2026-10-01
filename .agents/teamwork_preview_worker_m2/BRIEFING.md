# BRIEFING — 2026-09-21T13:11:00Z

## Mission
Develop `tools/generate_word_samples.ps1` (Word COM automation for generating canonical DOCX test samples) and `tools/extract_ground_truth.py` (DOCX unpacker and ground truth manifest extractor), execute both to produce canonical test fixtures, verify output integrity, and document artifacts.

## 🔒 My Identity
- Archetype: implementer
- Roles: implementer, qa, specialist
- Working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m2
- Original parent: 04fc9965-4b0a-4fc5-9553-20c2a9f1ef15
- Milestone: M2 - Word Sample Generation & Ground Truth Extraction

## 🔒 Key Constraints
- Automate Microsoft Word 16.0 via COM (`New-Object -ComObject Word.Application`).
- Generate canonical DOCX sample documents containing:
  - 200+ SmartArt diagrams across all 8 standard categories (Cycle, Hierarchy, Process, Matrix, Pyramid, Relationship, List, Picture), testing varied node counts (3, 4, 5, 7 nodes) and color/style variants.
  - Full shape library (rectangles, basic shapes, block arrows, equation, flowchart, stars/banners, callouts) with solid/dash/double/3D borders, drop shadow, gradient, 3D effect.
  - OMML mathematical equations (`OMaths.Add` + `OMaths.Item(1).BuildUp()`), special symbols, standard fonts.
  - Tables with various border styles (single, double, dashed, 3D emboss/engrave) and shading.
- Save generated documents into `tests/samples/`.
- Unpack DOCX packages into structured ground truth XML parts (`document.xml`, `diagrams/data1.xml`, `diagrams/layout1.xml`, `diagrams/drawing1.xml`, `theme1.xml`).
- Extract quantitative metadata (EMU coordinates, node count, layout type, color theme, border styles) into `tests/ground_truth_manifest.json`.
- Execute both scripts and verify that all sample files and XML extractions exist and are valid.
- DO NOT CHEAT: genuine implementations, real COM automation, real XML extraction.

## Current Parent
- Conversation ID: 04fc9965-4b0a-4fc5-9553-20c2a9f1ef15
- Updated: 2026-09-21T13:11:00Z

## Task Summary
- **What to build**: `tools/generate_word_samples.ps1` and `tools/extract_ground_truth.py`
- **Success criteria**: Functional COM automation script generating canonical docx files with 200+ SmartArt diagrams, shapes, OMML, tables; Python extraction script generating unpacked XML ground truth and manifest JSON; all tests/samples and tests/ground_truth files verified.
- **Interface contracts**: `PROJECT.md`, `TEST_INFRA.md`, survey reports.
- **Code layout**: `tools/`, `tests/samples/`, `tests/ground_truth/`, `tests/ground_truth_manifest.json`.

## Key Decisions Made
- [TBD]

## Artifact Index
- [TBD]

## Change Tracker
- **Files modified**: None yet
- **Build status**: Not started
- **Pending issues**: None

## Quality Status
- **Build/test result**: Not started
- **Lint status**: Not started
- **Tests added/modified**: Not started

## Loaded Skills
- None
