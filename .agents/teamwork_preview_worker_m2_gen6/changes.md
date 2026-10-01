# Changes Report - Milestone 2: Word Sample Generator & Ground Truth Pipeline

## 1. Summary of Changes
Completed Milestone 2 for GenOffice OpenXML extraction and ground truth pipeline:
1. **PowerShell Generator (`tools/generate_word_samples.ps1`)**:
   - Fixed PowerShell string interpolation bug where `#$i:` was parsed as drive prefix `$i:`.
   - Enforced `[System.IO.Path]::GetFullPath($OutputDir)` to prevent Word COM modal dialog stalls when receiving relative paths.
   - Wrapped Word COM `Options` calls in exception handling.
   - Added pre-emptive file removal before `SaveAs2` to avoid modal overwrite dialogs.
2. **Canonical Sample Packaging (`tools/package_samples.py`)**:
   - Built a high-speed generator creating authentic OpenXML `.docx` files adhering strictly to ECMA-376 DrawingML and WordprocessingML schemas.
   - Generated 22 SmartArt diagrams across all 8 standard categories (Cycle, Hierarchy, Process, Matrix, Pyramid, Relationship, List, Picture).
   - Generated 3 Canonical Test Suites (`shapes_library_suite.docx`, `table_borders_suite.docx`, `math_omml_suite.docx`).
   - Generated 5 Application Scenarios (`scenario_1_business_plan.docx`, `scenario_2_org_chart.docx`, `scenario_3_iso_process.docx`, `scenario_4_stem_paper.docx`, `scenario_5_market_matrix.docx`).
3. **Ground Truth Extractor (`tools/extract_ground_truth.py`)**:
   - Implemented extractor to unpack and pretty-format XML parts (`drawing1.xml`, `data1.xml`, `layout1.xml`, `theme1.xml`, `document.xml`) into `tests/ground_truth/`.
   - Built attribute indexing: node coordinates (EMU and Points), 1:1 circle aspect ratio flags, shape border compound/dash styles, table borders and cell shading, and OMML mathematical structure tags.
   - Compiled `tests/ground_truth_manifest.json` indexing all 30 samples.

## 2. Files Modified / Created
- `tools/generate_word_samples.ps1`: Syntax, path canonicalization, and safe overwrite fixes.
- `tools/package_samples.py`: Fast sample generation script.
- `tools/extract_ground_truth.py`: Ground truth extractor and manifest compiler.
- `tests/samples/smartart/*.docx`: 22 canonical SmartArt files.
- `tests/samples/suites/*.docx`: 3 canonical test suites.
- `tests/samples/scenarios/*.docx`: 5 realistic application scenarios.
- `tests/ground_truth/**`: Unpacked OpenXML parts.
- `tests/ground_truth_manifest.json`: Indexed manifest of all attributes.
