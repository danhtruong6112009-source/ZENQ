# Handoff Report: Survey Agent 2 (Phases 1 & 2 Spec & API Miner)

**Role**: Phases 1 & 2 Spec & API Miner  
**Working Directory**: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_2`  
**Primary Deliverable**: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_2\spec_report.md`  
**Target Milestone**: Survey & Spec Mining for Phases 1 & 2  

---

## 1. Observation

1. **Compilation Environment & Language Version**:
   Direct execution of `build.bat` via command line (`cmd.exe /c build.bat`) yielded:
   ```text
   Microsoft (R) Visual C# Compiler version 4.8.9232.0 for C# 5
   Copyright (C) Microsoft Corporation. All rights reserved.
   [SUCCESS] GenOffice.exe built successfully!
   ```
   The build compiler is strictly C# 5.0 running on .NET Framework 4.0/4.8. Modern C# features (string interpolation `$"..."`, expression-bodied members `=>`, null-propagation `?.`, tuple literals, and inline `out var`) will cause compilation failure if introduced.

2. **OpenXML Drawings Asymmetry in `XlsxHandler.cs`**:
   - `SaveXlsx` (`src/format/XlsxHandler.cs` lines 1216–1425) serializes floating shapes, charts, and embedded pictures to `xl/drawings/drawing1.xml`, `xl/drawings/_rels/drawing1.xml.rels`, and `xl/media/image{N}.png`.
   - `LoadXlsx` (`src/format/XlsxHandler.cs` lines 300–778) reads only `xl/sharedStrings.xml`, `xl/styles.xml`, and `xl/worksheets/sheet{N}.xml`. It contains zero references to `xl/drawings/`, `xl/drawings/_rels/`, or `xl/media/`. Opening any workbook with visual assets completely ignores them, and saving subsequently purges them.
   - DrawingML uses EMUs (English Metric Units): 1 pixel at 96 DPI = 9,525 EMUs.

3. **Drawing Model in `CalcView.cs` & `DocShapeObject.cs`**:
   - `CalcView` line 260 defines `public List<DocShapeObject> Shapes { get; set; }`.
   - `CalcSheetState` line 111 defines `public List<DocShapeObject> Shapes { get; set; }`.
   - `DocShapeObject` (`src/ui/DocShapeObject.cs`) contains all required properties for shapes, images (`CachedImage`, `ImagePath`), and charts (`ChartData` of type `DocChartData`).
   - The user specification in `ORIGINAL_REQUEST.md` requires exposing `CalcView.FloatingObjects`. Exposing `public List<DocShapeObject> FloatingObjects { get { return Shapes; } set { Shapes = value; } }` achieves 100% contract compliance and backwards compatibility.

4. **PDF Export Gap for Calc**:
   - `AppForm.cs` line 4200:
     ```csharp
     if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
     {
         ok = WriterView.ExportToPdf(sfd.FileName, Ribbon);
     }
     else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null && PdfView.Document != null)
     {
         ok = GenOffice.Format.PdfHandler.SavePdf(PdfView.Document, sfd.FileName);
     }
     else
     {
         var blankDoc = new GenOffice.Core.PdfDocument(System.IO.Path.GetFileName(sfd.FileName));
         ok = GenOffice.Format.PdfHandler.SavePdf(blankDoc, sfd.FileName);
     }
     ```
     When in `OfficeAppMode.Calc`, the application generates an empty PDF document (`blankDoc`).
   - `FormatHandlers.cs` has Docx/Pptx PDF export but no spreadsheet PDF export handler.
   - `PdfHandler.SavePdf` (`src/format/PdfHandler.cs` line 1862) is a fully functional vector PDF 1.7 engine supporting text (`/F1`, `/F2`), rectangles, lines, and custom coordinates.

5. **Core Data Utilities Current State**:
   - `FindReplaceForm.cs` (`src/ui/FindReplaceForm.cs`) is a simple dialog titled `"Tìm kiếm & Thay thế - ZenQ Writer"` with only two textboxes and no search options (no Match Case, Match Entire Cell, Search By Rows/Cols, or Look In Formulas/Values).
   - `RemoveDuplicates` (`src/ui/CalcView.cs` line 7062) performs deduplication across all columns without any modal or column selection checkboxes.
   - `Text to Columns` is completely absent from the codebase.

6. **Formula Engine Pipeline & Function Inventory**:
   - `FormulaEngine.cs` (`src/core/FormulaEngine.cs`, 1447 lines) uses `SplitArgs`, `SetResultFromToken`, `EvaluateCondition`, and `ParseAndEvaluateNumeric`.
   - 52 functions across 6 categories were identified from `ORIGINAL_REQUEST.md` item 2.1:
     - Lookup & Reference (10): `INDEX`, `MATCH`, `XLOOKUP`, `XMATCH`, `ROW`, `COLUMN`, `ROWS`, `COLUMNS`, `INDIRECT`, `OFFSET`.
     - Logical & Error (5): `IFS`, `SWITCH`, `IFERROR`, `IFNA`, `XOR`.
     - Date & Time (11): `YEAR`, `MONTH`, `DAY`, `HOUR`, `MINUTE`, `SECOND`, `WEEKDAY`, `EDATE`, `EOMONTH`, `NETWORKDAYS`, `DATEDIF`.
     - Text (12): `TEXT`, `VALUE`, `FIND`, `SEARCH`, `REPLACE`, `REPT`, `TEXTSPLIT`, `TEXTBEFORE`, `TEXTAFTER`, `CHAR`, `CODE`, `CLEAN`.
     - Math & Statistical (9): `RANDBETWEEN`, `RAND`, `CEILING`, `FLOOR`, `SIGN`, `STDEV`, `VAR`, `RANK`, `MODE`.
     - Information (5): `ISNUMBER`, `ISTEXT`, `ISBLANK`, `ISERROR`, `ISNA`.
   - Error constants in Excel (`#N/A`, `#VALUE!`, `#REF!`, `#DIV/0!`, `#NUM!`, `#NAME?`) need standardized propagation.

7. **Extended File Formats Current State**:
   - In `XlsxHandler.cs` line 28, `.xls` files are currently forwarded to `LoadXlsx`, which attempts to open them as `ZipArchive`, throwing an `InvalidDataException` on authentic BIFF8 binary files.
   - `.ods` format support is currently absent.
   - HTML, TSV, and PRN export are currently absent from Calc handlers.

---

## 2. Logic Chain

1. **Drawings Roundtrip**:
   - Observation 2 demonstrates that drawings are written by `SaveXlsx` but ignored by `LoadXlsx`.
   - Observation 3 confirms `DocShapeObject` and `DocChartData` already exist to hold shape geometry, image bitmaps, and chart data.
   - Therefore, adding drawing relationship parsing, DrawingML XML anchor extraction, and media loading to `LoadXlsx` will restore shapes and images into `CalcView.FloatingObjects` / `Shapes`, closing the roundtrip loop and eliminating data loss.

2. **Native PDF Export**:
   - Observation 4 shows that Calc PDF export emits a blank PDF file.
   - `PdfHandler.SavePdf` already provides the necessary vector PDF 1.7 primitive engine.
   - Therefore, implementing a spreadsheet layout and 2D pagination engine in `FormatHandlers.cs` / `CalcView.cs` that converts cells, borders, fills, and text to `PdfPageModel` text/shape elements will produce high-fidelity printable PDFs and hook directly into `AppForm.cs`.

3. **Core Data Utilities**:
   - Observation 5 shows the lack of dedicated Calc modals for Find & Replace, Remove Duplicates column selection, and Text to Columns.
   - Creating dedicated forms (`CalcFindReplaceDialog`, `CalcRemoveDuplicatesDialog`, `CalcTextToColumnsDialog`) and wiring them to `CalcView` methods provides complete Excel-compatible productivity workflows.

4. **Formula Engine & File Formats**:
   - Observation 6 establishes the evaluation pipeline and the full list of 52 functions with exact error handling.
   - Observation 7 proves that `.xls` cannot be loaded as ZIP and requires an authentic Compound File Binary / BIFF8 record parser, while `.ods` requires OpenDocument XML/ZIP transformation.
   - All designs documented in `spec_report.md` adhere strictly to C# 5.0 (Observation 1).

---

## 3. Caveats

- **External COM Dependencies**: No external Microsoft Office interop or COM objects should be required; all parsers and exporters are native C# implementations using `System.IO.Compression` and custom binary/XML parsers.
- **Bi-directional ChartML**: Full DrawingML chart roundtrip (saving back complete `<c:chartSpace>` XML rather than rendered PNG fallback) can be handled incrementally: reading chart title, series, categories, and values into `DocChartData` preserves full fidelity for chart rendering and editing within Calc.

---

## 4. Conclusion

Phase 1 and Phase 2 requirements are fully specified, architected, and validated against the existing codebase. The comprehensive specification report is committed at:
`c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_2\spec_report.md`

All models, interfaces, math coordinate formulas, error semantics, WinForms dialog layouts, and binary/XML structures are defined with exact C# 5 signatures ready for immediate implementation by developer agents.

---

## 5. Verification Method

1. **Build Verification**:
   Execute the build harness in the project root:
   ```cmd
   cd c:\Users\Danh\Desktop\genofficepy
   build.bat
   ```
   **Expected**: Exit code 0, compilation with `csc.exe` completes with 0 errors and generates `GenOffice.exe`.

2. **Artifact Verification**:
   Inspect `spec_report.md`:
   - Verify Section 2 contains the EMU coordinate transformation math and DrawingML parser algorithms.
   - Verify Section 3 contains the 2D pagination and PDF vector rendering pipeline.
   - Verify Section 4 contains specifications for `CalcFindReplaceDialog`, `CalcRemoveDuplicatesDialog`, and `CalcTextToColumnsDialog`.
   - Verify Section 6 contains exhaustive specifications for all 52 functions across all 6 categories.
   - Verify Section 7 contains ODS, XLS BIFF8, HTML, TSV, and PRN format architectures.
   - Verify Section 8 contains the Features Discovered and Edge Cases tables.
