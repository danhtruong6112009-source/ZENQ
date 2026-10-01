## 2026-09-21T13:10:50Z
You are the Word Sample Generator & Ground Truth Engineer for GenOffice.
Your working directory is: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m2`.
You MUST read:
1. `c:\Users\Danh\Desktop\genofficepy\.agents\ORIGINAL_REQUEST.md` (specifically section `## 2026-09-21T12:58:21Z`)
2. `c:\Users\Danh\Desktop\genofficepy\PROJECT.md`
3. `c:\Users\Danh\Desktop\genofficepy\TEST_INFRA.md`
4. `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_explorer_survey_3\survey_generator_report.md`
5. `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_spec_miner_survey_1\survey_schema_report.md`

Your exclusive write ownership:
- `tools/generate_word_samples.ps1`
- `tools/extract_ground_truth.py`
- `tests/` directory (including `tests/samples/`, `tests/ground_truth/`, `tests/ground_truth_manifest.json`)

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Key implementation tasks:
1. Develop `tools/generate_word_samples.ps1`:
   - Automate Microsoft Word 16.0 via COM (`New-Object -ComObject Word.Application`).
   - Generate canonical DOCX sample documents containing:
     - 200+ SmartArt diagrams across all 8 standard categories (Cycle, Hierarchy, Process, Matrix, Pyramid, Relationship, List, Picture), testing varied node counts (3, 4, 5, 7 nodes) and color/style variants.
     - Full shape library (rectangles, basic shapes, block arrows, equation, flowchart, stars/banners, callouts) with solid/dash/double/3D borders, drop shadow, gradient, 3D effect.
     - OMML mathematical equations (`OMaths.Add` + `OMaths.Item(1).BuildUp()`), special symbols, standard fonts.
     - Tables with various border styles (single, double, dashed, 3D emboss/engrave) and shading.
   - Save the generated documents into `tests/samples/`.
2. Develop `tools/extract_ground_truth.py`:
   - Unpack DOCX packages into structured ground truth XML parts (`document.xml`, `diagrams/data1.xml`, `diagrams/layout1.xml`, `diagrams/drawing1.xml`, `theme1.xml`).
   - Extract quantitative metadata (EMU coordinates, node count, layout type, color theme, border styles) into `tests/ground_truth_manifest.json`.
3. Execute the generator script and extraction script:
   - Run `tools/generate_word_samples.ps1` and `tools/extract_ground_truth.py` via powershell / python.
   - Verify that all sample files and XML extractions exist and are valid.
4. Document all generator scripts, execution outputs, and dataset manifests in `changes.md`, `handoff.md`, and `progress.md` in your working directory. Send a message to the orchestrator when completed.
